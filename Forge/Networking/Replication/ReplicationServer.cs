// Copyright © Gamesmiths Guild.

using System.Runtime.CompilerServices;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Networking.Messages;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Networking.Replication;

/// <summary>
/// The server's half of replication: it keeps the entities the host registers, looks at them every send, and sends each
/// client what changed since the send that client last acknowledged, as current values, in one unreliable packet per
/// send that fits a budget. A client that acknowledged nothing of an entity gets the entity whole, spawn included, so
/// joining late and coming back into relevancy cost nothing special.
/// </summary>
/// <remarks>
/// <para>What replicates: attributes as current values with their range, tags as the final set with what was added and
/// removed, persistent cues as state, executed cues as events that expire, and the body for interpolation. Effects,
/// abilities and graphs don't: a client's copy holds the results, not what produced them.</para>
/// <para>The host calls <see cref="Send"/> once per tick after simulating it; the server sends every
/// <see cref="SendInterval"/> ticks, a packet with no entries when nothing changed, so a client knows the ticks in
/// which nothing did. The host registers an entity when it spawns, with the id from <see cref="AllocateId"/> set on it
/// first, and unregisters it when it goes.</para>
/// </remarks>
public sealed class ReplicationServer
{
	/// <summary>
	/// How many bytes a state packet aims to stay under.
	/// </summary>
	public const int DefaultBudgetBytes = 1200;

	/// <summary>
	/// How many ticks pass between sends by default: half the tick rate.
	/// </summary>
	public const int DefaultSendInterval = 2;

	/// <summary>
	/// How long an executed cue is worth sending for, in seconds.
	/// </summary>
	public const double EventExpirySeconds = 0.2;

	// How many sent packets a client is remembered for, so an acknowledgement finds what the packet carried. An
	// acknowledgement covers the newest packet and the 32 before it.
	private const int SentPacketsKept = 64;

	// The byte that ends a packet's entries: entity id 0, which no entity has.
	private const int TerminatorBytes = 1;

	private readonly NetworkSession _session;
	private readonly Dictionary<NetworkEntityId, ReplicatedEntity> _entities = [];
	private readonly Dictionary<IForgeEntity, ReplicatedEntity> _byEntity = new(ReferenceComparer.Instance);
	private readonly List<ReplicatedEntity> _entityList = [];
	private readonly List<ReplicatedEntity> _departed = [];
	private readonly List<ReplicatedEntity> _departedPlaced = [];
	private readonly Dictionary<PeerId, ClientState> _clients = [];
	private readonly HashSet<CuesManager> _observed = [];
	private readonly BitWriter _packet = new();
	private readonly BitWriter _entry = new();
	private readonly List<EntityView> _candidates = [];
	private readonly List<ushort> _tagScratch = [];
	private readonly CueObserver _cueObserver;
	private uint _nextId = 1;

	/// <summary>
	/// Gets or sets how many bytes a state packet aims to stay under. An entity whose entry alone is over it still
	/// goes, on its own.
	/// </summary>
	public int BudgetBytes { get; set; } = DefaultBudgetBytes;

	/// <summary>
	/// Gets or sets how many ticks pass between sends.
	/// </summary>
	public int SendInterval { get; set; } = DefaultSendInterval;

	/// <summary>
	/// Gets or sets what decides which entities each client hears about. Everything, by default.
	/// </summary>
	public IRelevancy Relevancy { get; set; } = AlwaysRelevant.Instance;

	/// <summary>
	/// Gets or sets where an entity's body state comes from: its mover's, by default. A host whose entities have bodies
	/// without movers reads them here.
	/// </summary>
	public Func<IForgeEntity, BodyState?> BodySource { get; set; } = entity => entity.Mover?.CaptureState();

	/// <summary>
	/// Gets the entities being replicated, in the order they were registered.
	/// </summary>
	public IReadOnlyList<ReplicatedEntity> Entities => _entityList;

	/// <summary>
	/// Gets the tick of the last send, or -1 before any.
	/// </summary>
	public long LastSendTick { get; private set; } = -1;

	/// <summary>
	/// Gets how many state packets were sent, to all clients together.
	/// </summary>
	public long PacketsSent { get; private set; }

	/// <summary>
	/// Gets how many bytes of state were sent, payloads only.
	/// </summary>
	public long BytesSent { get; private set; }

	/// <summary>
	/// Gets how many acknowledgements arrived.
	/// </summary>
	public long AcksReceived { get; private set; }

	internal ReplicationServer(NetworkSession session)
	{
		_session = session;
		_cueObserver = new CueObserver(this);
	}

	/// <summary>
	/// Gives out the next id for an entity about to be registered. Ids are never reused.
	/// </summary>
	/// <returns>The id.</returns>
	public NetworkEntityId AllocateId()
	{
		return new NetworkEntityId(_nextId++);
	}

	/// <summary>
	/// Starts replicating an entity. Its <see cref="IForgeEntity.NetworkId"/> must be set, to an id from
	/// <see cref="AllocateId"/> for an entity the server spawned, or to the id every machine gives it for one placed in
	/// the scene.
	/// </summary>
	/// <param name="entity">The entity.</param>
	/// <param name="sceneIndex">The index of the scene clients spawn it from, in the registry's
	/// <see cref="AssetKind.Scene"/> kind, or -1 for an entity placed in the scene.</param>
	/// <param name="owner">The peer whose player it is, or <see cref="PeerId.None"/>.</param>
	/// <returns>The entity's replication record.</returns>
	/// <exception cref="ArgumentException">Thrown when the entity has no id, its id is taken, it's registered already,
	/// or the registry has no such scene.</exception>
	public ReplicatedEntity Register(IForgeEntity entity, int sceneIndex, PeerId owner)
	{
		if (!entity.NetworkId.IsValid)
		{
			throw new ArgumentException("Give the entity its network id before registering it.", nameof(entity));
		}

		if (_entities.ContainsKey(entity.NetworkId) || _byEntity.ContainsKey(entity))
		{
			throw new ArgumentException($"{entity.NetworkId} is already being replicated.", nameof(entity));
		}

		if (sceneIndex >= _session.Registry.Count(AssetKind.Scene))
		{
			throw new ArgumentException($"The registry has no scene {sceneIndex}.", nameof(sceneIndex));
		}

		var replicated = new ReplicatedEntity(
			entity,
			sceneIndex,
			owner,
			_session.TagsManager,
			_session.Clock.Tick);
		_entities[entity.NetworkId] = replicated;
		_byEntity[entity] = replicated;
		_entityList.Add(replicated);
		_nextId = Math.Max(_nextId, entity.NetworkId.Value + 1);

		if (_observed.Add(entity.CuesManager))
		{
			entity.CuesManager.AddObserver(_cueObserver);
		}

		return replicated;
	}

	/// <summary>
	/// Stops replicating an entity: every client that has it despawns it. A client that joins later and placed it
	/// despawns it too.
	/// </summary>
	/// <param name="entity">The entity.</param>
	/// <returns><see langword="false"/> if it wasn't being replicated.</returns>
	public bool Unregister(IForgeEntity entity)
	{
		if (!_byEntity.Remove(entity, out ReplicatedEntity? replicated))
		{
			return false;
		}

		_entities.Remove(replicated.Id);
		_entityList.Remove(replicated);
		replicated.DespawnedAtTick = _session.Clock.Tick;
		replicated.DespawnStamp = ReplicatedEntity.Pending;
		replicated.Cues.Clear();
		replicated.Events.Clear();

		if (replicated.IsPlaced)
		{
			_departedPlaced.Add(replicated);
		}

		bool anyView = false;

		foreach (ClientState client in _clients.Values)
		{
			if (client.Views.TryGetValue(replicated.Id, out EntityView? view))
			{
				anyView = true;
				view.Leaving = true;
			}
		}

		if (anyView)
		{
			_departed.Add(replicated);
		}

		return true;
	}

	/// <summary>
	/// Finds the record of an entity by id.
	/// </summary>
	/// <param name="id">The id.</param>
	/// <returns>The record, or <see langword="null"/> if no such entity is being replicated.</returns>
	public ReplicatedEntity? Find(NetworkEntityId id)
	{
		return _entities.GetValueOrDefault(id);
	}

	/// <summary>
	/// Finds the record of an entity.
	/// </summary>
	/// <param name="entity">The entity.</param>
	/// <returns>The record, or <see langword="null"/> if it isn't being replicated.</returns>
	public ReplicatedEntity? Find(IForgeEntity entity)
	{
		return _byEntity.GetValueOrDefault(entity);
	}

	/// <summary>
	/// Looks at every entity and, when a send is due, sends each client what it hasn't acknowledged. Call it once per
	/// tick, after simulating the tick.
	/// </summary>
	/// <param name="tick">The tick just simulated, which the state is of.</param>
	public void Send(long tick)
	{
		if (LastSendTick >= 0 && tick - LastSendTick < SendInterval)
		{
			return;
		}

		LastSendTick = tick;
		long expiry = (long)Math.Ceiling(_session.Clock.TicksIn(EventExpirySeconds));

		foreach (ReplicatedEntity entity in _entityList)
		{
			entity.Capture(tick, BodySource);
			entity.Events.RemoveAll(x => tick - x.CreatedAtTick > expiry);
		}

		foreach (ReplicatedEntity entity in _departed)
		{
			if (entity.DespawnStamp == ReplicatedEntity.Pending)
			{
				entity.DespawnStamp = tick;
			}
		}

		foreach (ClientState client in _clients.Values)
		{
			UpdateViews(client, tick);
			SendTo(client, tick);
		}

		Prune();
	}

	internal void AddClient(NetworkPeer peer)
	{
		var client = new ClientState(peer);
		_clients[peer.Id] = client;

		// The client has every entity placed in the scene, including the ones gone here before it joined.
		foreach (ReplicatedEntity entity in _departedPlaced)
		{
			client.Views[entity.Id] = new EntityView(entity, _session.Clock.Tick) { Sent = true, Leaving = true };
		}
	}

	internal void RemoveClient(NetworkPeer peer)
	{
		_clients.Remove(peer.Id);
		Prune();
	}

	internal void ReceiveAck(NetworkPeer peer, BitReader reader)
	{
		var ack = StateAckMessage.Read(reader);

		if (!_clients.TryGetValue(peer.Id, out ClientState? client))
		{
			return;
		}

		AcksReceived++;
		Acknowledge(client, ack.Latest);

		for (int i = 0; i < 32; i++)
		{
			if ((ack.Mask & (1u << i)) != 0 && ack.Latest > (uint)i + 1)
			{
				Acknowledge(client, ack.Latest - (uint)i - 1);
			}
		}
	}

	// Hands a client's command to the input of every entity the client owns that has one.
	internal void DeliverInput(PeerId owner, InputCommand command, TagContainer? raisableEvents)
	{
		foreach (ReplicatedEntity entity in _entityList)
		{
			if (entity.Owner != owner || entity.Entity.Input is not EntityInput input)
			{
				continue;
			}

			input.Submit(command.Frame);

			if (raisableEvents is not null)
			{
				command.RaiseEvents(entity.Entity, raisableEvents);
			}
		}
	}

	private static void Acknowledge(ClientState client, uint sequence)
	{
		SentPacket packet = client.Sent[sequence % SentPacketsKept];

		// Evicted by a newer packet, or acknowledged already.
		if (packet.Sequence != sequence)
		{
			return;
		}

		packet.Sequence = 0;

		foreach (SentEntry sent in packet.Entries)
		{
			if (sent.Despawn)
			{
				if (sent.View.Leaving)
				{
					client.Views.Remove(sent.View.Entity.Id);
				}

				continue;
			}

			sent.View.Acked = Math.Max(sent.View.Acked, sent.Stamp);
			sent.View.EventsAcked = Math.Max(sent.View.EventsAcked, sent.MaxEventSequence);
		}

		packet.Entries.Clear();
	}

	private static bool HasPending(EntityView view)
	{
		if (view.Leaving || view.Acked < 0)
		{
			return true;
		}

		ReplicatedEntity entity = view.Entity;
		long acked = view.Acked;
		long seen = view.Seen;

		for (int i = 0; i < entity.Attributes.Length; i++)
		{
			if (entity.ValueStamps[i] > acked || entity.RangeStamps[i] > acked)
			{
				return true;
			}
		}

		foreach (long stamp in entity.LiveTags.Values)
		{
			if (stamp > acked)
			{
				return true;
			}
		}

		foreach (long stamp in entity.RemovedTags.Values)
		{
			if (stamp > seen)
			{
				return true;
			}
		}

		foreach (ReplicatedEntity.CueInstance cue in entity.Cues)
		{
			if (cue.Stamp > acked)
			{
				return true;
			}
		}

		foreach (ReplicatedEntity.CueTombstone tombstone in entity.RemovedCues)
		{
			if (tombstone.Stamp > seen)
			{
				return true;
			}
		}

		foreach (ReplicatedEntity.CueEvent cueEvent in entity.Events)
		{
			if (cueEvent.Sequence > view.EventsAcked)
			{
				return true;
			}
		}

		return entity.BodyStamp > acked;
	}

	private void UpdateViews(ClientState client, long tick)
	{
		foreach (ReplicatedEntity entity in _entityList)
		{
			bool relevant = entity.IsPlaced || Relevancy.IsRelevant(client.Peer.Id, entity, this);
			client.Views.TryGetValue(entity.Id, out EntityView? view);

			if (relevant && view is null)
			{
				client.Views[entity.Id] = new EntityView(entity, tick) { EventsAcked = entity.LastEventSequence };
			}
			else if (!relevant && view?.Leaving == false)
			{
				// A client that never heard of it has nothing to despawn.
				if (!view.Sent)
				{
					client.Views.Remove(entity.Id);
					continue;
				}

				view.Leaving = true;
			}
		}
	}

	private void SendTo(ClientState client, long tick)
	{
		_candidates.Clear();

		foreach (EntityView view in client.Views.Values)
		{
			if (!HasPending(view))
			{
				continue;
			}

			view.Accumulated += view.Entity.Priority;
			_candidates.Add(view);
		}

		_candidates.Sort(static (a, b) =>
		{
			int byPriority = b.Accumulated.CompareTo(a.Accumulated);
			return byPriority != 0 ? byPriority : a.Entity.Id.Value.CompareTo(b.Entity.Id.Value);
		});

		uint sequence = ++client.NextSequence;
		SentPacket packet = client.Sent[sequence % SentPacketsKept];
		packet.Sequence = sequence;
		packet.Entries.Clear();
		_packet.Clear();
		_packet.WriteVarUInt(sequence);
		_packet.WriteVarInt(tick);

		foreach (EntityView view in _candidates)
		{
			uint maxEvent = WriteEntry(view);

			if (packet.Entries.Count > 0 && _packet.ByteLength + _entry.ByteLength + TerminatorBytes > BudgetBytes)
			{
				continue;
			}

			_packet.WriteBytes(_entry.WrittenBytes);
			view.Accumulated = 0;
			view.Sent = true;
			packet.Entries.Add(new SentEntry(view, tick, maxEvent, view.Leaving));
		}

		_packet.WriteVarUInt(0);
		_session.Transport.Send(client.Peer.Id, Protocol.StateChannel, reliable: false, _packet.WrittenBytes);
		PacketsSent++;
		BytesSent += _packet.ByteLength;
	}

	// Writes the view's entry into the entry writer, byte aligned so it can be copied whole, and returns the newest
	// event sequence it carries.
	private uint WriteEntry(EntityView view)
	{
		ReplicatedEntity entity = view.Entity;
		_entry.Clear();
		_entry.WriteVarUInt(entity.Id.Value);

		if (view.Leaving)
		{
			_entry.WriteBits((ulong)EntryKind.Despawn, 2);
			_entry.Align();
			return view.EventsAcked;
		}

		long acked = view.Acked;
		long seen = view.Seen;
		_entry.WriteBits((ulong)(acked < 0 ? EntryKind.Spawn : EntryKind.State), 2);

		if (acked < 0)
		{
			_entry.WriteBool(entity.IsPlaced);

			if (!entity.IsPlaced)
			{
				_entry.WriteBits((ulong)entity.SceneIndex, _session.Registry.BitsFor(AssetKind.Scene));
			}

			_entry.WriteVarUInt((ulong)entity.Owner.Value);
			_entry.WriteVarUInt((ulong)entity.Attributes.Length);
		}

		WriteAttributes(entity, acked);
		WriteTags(entity, acked, seen);
		WriteCues(entity, acked, seen);
		uint maxEvent = WriteEvents(entity, view.EventsAcked);

		bool body = entity.BodyStamp > acked && entity.ShadowBody is not null;
		_entry.WriteBool(body);

		if (body)
		{
			BodyCodec.Write(_entry, entity.ShadowBody!.Value);
		}

		_entry.Align();
		return maxEvent;
	}

	private void WriteAttributes(ReplicatedEntity entity, long acked)
	{
		int changed = 0;

		for (int i = 0; i < entity.Attributes.Length; i++)
		{
			if (entity.ValueStamps[i] > acked || entity.RangeStamps[i] > acked)
			{
				changed++;
			}
		}

		_entry.WriteBool(changed > 0);

		if (changed == 0)
		{
			return;
		}

		_entry.WriteVarUInt((ulong)changed);

		for (int i = 0; i < entity.Attributes.Length; i++)
		{
			bool range = entity.RangeStamps[i] > acked;

			if (entity.ValueStamps[i] <= acked && !range)
			{
				continue;
			}

			_entry.WriteVarUInt((ulong)i);
			_entry.WriteVarInt(entity.ShadowValues[i]);
			_entry.WriteBool(range);

			if (range)
			{
				_entry.WriteVarInt(entity.ShadowMins[i]);
				_entry.WriteVarInt(entity.ShadowMaxes[i]);
			}
		}
	}

	private void WriteTags(ReplicatedEntity entity, long acked, long seen)
	{
		_tagScratch.Clear();
		int added = 0;

		foreach (KeyValuePair<ushort, long> tag in entity.LiveTags)
		{
			if (tag.Value <= acked)
			{
				continue;
			}

			_tagScratch.Add(tag.Key);
			added++;
		}

		foreach (KeyValuePair<ushort, long> tag in entity.RemovedTags)
		{
			if (tag.Value <= seen)
			{
				continue;
			}

			_tagScratch.Add(tag.Key);
		}

		_entry.WriteBool(_tagScratch.Count > 0);

		if (_tagScratch.Count == 0)
		{
			return;
		}

		_entry.WriteVarUInt((ulong)added);

		for (int i = 0; i < added; i++)
		{
			_entry.WriteVarUInt(_tagScratch[i]);
		}

		_entry.WriteVarUInt((ulong)(_tagScratch.Count - added));

		for (int i = added; i < _tagScratch.Count; i++)
		{
			_entry.WriteVarUInt(_tagScratch[i]);
		}
	}

	private void WriteCues(ReplicatedEntity entity, long acked, long seen)
	{
		int cues = 0;

		foreach (ReplicatedEntity.CueInstance cue in entity.Cues)
		{
			if (cue.Stamp > acked)
			{
				cues++;
			}
		}

		foreach (ReplicatedEntity.CueTombstone tombstone in entity.RemovedCues)
		{
			if (tombstone.Stamp > seen)
			{
				cues++;
			}
		}

		_entry.WriteBool(cues > 0);

		if (cues == 0)
		{
			return;
		}

		_entry.WriteVarUInt((ulong)cues);

		foreach (ReplicatedEntity.CueInstance cue in entity.Cues)
		{
			if (cue.Stamp <= acked)
			{
				continue;
			}

			_entry.WriteVarUInt(cue.Instance);
			_entry.WriteBool(false);
			_entry.WriteVarUInt(_session.TagsManager.GetNetIndexFromTag(cue.CueTag));
			ReplicationCodec.WriteParameters(_entry, cue.Parameters);
		}

		foreach (ReplicatedEntity.CueTombstone tombstone in entity.RemovedCues)
		{
			if (tombstone.Stamp <= seen)
			{
				continue;
			}

			_entry.WriteVarUInt(tombstone.Instance);
			_entry.WriteBool(true);
		}
	}

	private uint WriteEvents(ReplicatedEntity entity, uint eventsAcked)
	{
		int events = 0;

		foreach (ReplicatedEntity.CueEvent cueEvent in entity.Events)
		{
			if (cueEvent.Sequence > eventsAcked)
			{
				events++;
			}
		}

		_entry.WriteBool(events > 0);

		if (events == 0)
		{
			return eventsAcked;
		}

		_entry.WriteVarUInt((ulong)events);
		uint maxEvent = eventsAcked;

		foreach (ReplicatedEntity.CueEvent cueEvent in entity.Events)
		{
			if (cueEvent.Sequence <= eventsAcked)
			{
				continue;
			}

			maxEvent = Math.Max(maxEvent, cueEvent.Sequence);
			_entry.WriteVarUInt(cueEvent.Sequence);
			_entry.WriteVarUInt(_session.TagsManager.GetNetIndexFromTag(cueEvent.CueTag));
			ReplicationCodec.WriteParameters(_entry, cueEvent.Parameters);
		}

		return maxEvent;
	}

	// Drops what no client needs any more: tombstones every client that could have seen the tag or cue has heard about,
	// and departed entities every client has despawned.
	private void Prune()
	{
		foreach (ReplicatedEntity entity in _entityList)
		{
			PruneTombstones(entity);
		}

		_departed.RemoveAll(x => !AnyViewOf(x.Id));
	}

	private bool AnyViewOf(NetworkEntityId id)
	{
		return _clients.Values.Any(x => x.Views.ContainsKey(id));
	}

	private void PruneTombstones(ReplicatedEntity entity)
	{
		long needed = long.MaxValue;

		foreach (ClientState client in _clients.Values)
		{
			if (client.Views.TryGetValue(entity.Id, out EntityView? view) && !view.Leaving)
			{
				needed = Math.Min(needed, view.Seen);
			}
		}

		_tagScratch.Clear();

		foreach (KeyValuePair<ushort, long> tag in entity.RemovedTags)
		{
			if (tag.Value != ReplicatedEntity.Pending && tag.Value <= needed)
			{
				_tagScratch.Add(tag.Key);
			}
		}

		foreach (ushort index in _tagScratch)
		{
			entity.RemovedTags.Remove(index);
		}

		entity.RemovedCues.RemoveAll(x => x.Stamp != ReplicatedEntity.Pending && x.Stamp <= needed);
	}

	// What one client knows of one entity.
	private sealed class EntityView(ReplicatedEntity entity, long baseTick)
	{
		public ReplicatedEntity Entity { get; } = entity;

		// The send the client first heard of the entity at: anything that went before it never reached the client.
		public long Base { get; } = baseTick;

		// The newest send of the entity the client acknowledged, or -1 for none, which asks for the entity whole.
		public long Acked { get; set; } = -1;

		// Removals older than this never need sending: the client acknowledged a state without them, or first heard
		// of the entity after them.
		public long Seen => Math.Max(Acked, Base);

		public uint EventsAcked { get; set; }

		public float Accumulated { get; set; }

		public bool Sent { get; set; }

		// The client is to despawn the entity: it left, or stopped being relevant.
		public bool Leaving { get; set; }
	}

	// Keeps each replicated entity's persistent cues and executed cues as the cues manager is told about them.
	private sealed class CueObserver(ReplicationServer server) : ICueObserver
	{
		public void CueApplied(Tag cueTag, IForgeEntity? target, CueParameters? parameters, object? application)
		{
			if (target is null || !server._byEntity.TryGetValue(target, out ReplicatedEntity? entity))
			{
				return;
			}

			ReplicatedEntity.CueInstance? existing = FindCue(entity, cueTag, application);

			if (existing is not null)
			{
				existing.Parameters = parameters;
				existing.Stamp = ReplicatedEntity.Pending;
				return;
			}

			entity.Cues
				.Add(new ReplicatedEntity.CueInstance(entity.NextCueInstance++, application, cueTag, parameters));
		}

		public void CueUpdated(Tag cueTag, IForgeEntity? target, CueParameters? parameters, object? application)
		{
			if (target is null || !server._byEntity.TryGetValue(target, out ReplicatedEntity? entity))
			{
				return;
			}

			// An update without an application of its own is for whichever application of the cue is on the target, as
			// the handlers take it.
			ReplicatedEntity.CueInstance? cue = FindCue(entity, cueTag, application)
				?? (application is null ? FirstCue(entity, cueTag) : null);

			if (cue is null)
			{
				return;
			}

			cue.Parameters = parameters;
			cue.Stamp = ReplicatedEntity.Pending;
		}

		public void CueRemoved(Tag cueTag, IForgeEntity? target, bool interrupted, object? application)
		{
			if (target is null || !server._byEntity.TryGetValue(target, out ReplicatedEntity? entity))
			{
				return;
			}

			ReplicatedEntity.CueInstance? cue = FindCue(entity, cueTag, application);

			if (cue is null)
			{
				return;
			}

			entity.Cues.Remove(cue);
			entity.RemovedCues.Add(new ReplicatedEntity.CueTombstone(cue.Instance, ReplicatedEntity.Pending));
		}

		public void CueExecuted(Tag cueTag, IForgeEntity? target, CueParameters? parameters)
		{
			if (target is null || !server._byEntity.TryGetValue(target, out ReplicatedEntity? entity))
			{
				return;
			}

			entity.Events.Add(new ReplicatedEntity.CueEvent(
				++entity.LastEventSequence,
				cueTag,
				parameters,
				server._session.Clock.Tick));
		}

		private static ReplicatedEntity.CueInstance? FindCue(ReplicatedEntity entity, Tag cueTag, object? application)
		{
			foreach (ReplicatedEntity.CueInstance cue in entity.Cues)
			{
				if (cue.CueTag == cueTag && ReferenceEquals(cue.Application, application))
				{
					return cue;
				}
			}

			return null;
		}

		private static ReplicatedEntity.CueInstance? FirstCue(ReplicatedEntity entity, Tag cueTag)
		{
			foreach (ReplicatedEntity.CueInstance cue in entity.Cues)
			{
				if (cue.CueTag == cueTag)
				{
					return cue;
				}
			}

			return null;
		}
	}

	private sealed class SentPacket
	{
		public uint Sequence { get; set; }

		public List<SentEntry> Entries { get; } = [];
	}

	private sealed class ClientState
	{
		public NetworkPeer Peer { get; }

		public Dictionary<NetworkEntityId, EntityView> Views { get; } = [];

		// The packets sent and not yet acknowledged, by sequence modulo the ring's length.
		public SentPacket[] Sent { get; } = new SentPacket[SentPacketsKept];

		public uint NextSequence { get; set; }

		public ClientState(NetworkPeer peer)
		{
			Peer = peer;

			for (int i = 0; i < Sent.Length; i++)
			{
				Sent[i] = new SentPacket();
			}
		}
	}

	private readonly record struct SentEntry(EntityView View, long Stamp, uint MaxEventSequence, bool Despawn);

	private sealed class ReferenceComparer : IEqualityComparer<IForgeEntity>
	{
		public static readonly ReferenceComparer Instance = new();

		public bool Equals(IForgeEntity? x, IForgeEntity? y)
		{
			return ReferenceEquals(x, y);
		}

		public int GetHashCode(IForgeEntity obj)
		{
			return RuntimeHelpers.GetHashCode(obj);
		}
	}
}
