// Copyright © Gamesmiths Guild.

using System.Runtime.ExceptionServices;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Networking.Messages;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Networking.Replication;

/// <summary>
/// The server's half of replication: it keeps the entities the host registers, looks at them every send, and sends each
/// client what changed since the send that client last acknowledged, as current values, in one unreliable packet per
/// send that fits a budget. A client that acknowledged nothing of an entity gets the entity whole, spawn included, so
/// joining late and coming back into relevancy cost nothing special.
/// </summary>
/// <remarks>
/// <para>What replicates: attributes as current values with their range, and base values to the owner, tags as the
/// final set with what was added and removed, active effects as records of what they are, as far as each effect's
/// <see cref="EffectVisibility"/> allows, persistent cues as state, executed cues as events that expire, the graphs the
/// entity's abilities run, as far as clients run them, and the body for interpolation. A graph goes as which of its
/// presentation states run and since when, its value variables, and its presentation actions as events that expire,
/// so a client shows them without following a link. Abilities don't: a client's copy holds the results, not what
/// produced them.</para>
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
	private readonly Dictionary<IForgeEntity, ReplicatedEntity> _byEntity = new(EntityReferenceComparer.Instance);
	private readonly List<ReplicatedEntity> _entityList = [];
	private readonly List<ReplicatedEntity> _departedPlaced = [];
	private readonly Dictionary<PeerId, ClientState> _clients = [];
	private readonly List<ReplicatedEntity> _inputBuffer = [];
	private readonly BitWriter _packet = new();
	private readonly BitWriter _entry = new();
	private readonly List<EntityView> _candidates = [];
	private readonly List<ushort> _tagScratch = [];
	private readonly Dictionary<Type, int> _contextCodecs = [];
	private readonly HashSet<EffectData> _effectsNotListed = [];
	private readonly HashSet<AbilityData> _abilitiesNotListed = [];
	private readonly SpawnKeys _spawnKeys = new();
	private readonly CueObserver _cueObserver;
	private readonly GraphObserver _graphObserver;
	private EffectVisibility _defaultEffectVisibility = EffectVisibility.Public;
	private uint _nextId = 1;

	/// <summary>
	/// Raised the first time an effect the registry doesn't list goes active on a replicated entity, unless it's
	/// <see cref="EffectVisibility.ServerOnly"/>: no client can be told about it, so it stays on the server.
	/// </summary>
	public event Action<EffectData>? EffectNotListed;

	/// <summary>
	/// Raised the first time a graph that clients run something of runs for an ability the registry doesn't list: no
	/// client can be told about it, so it runs on the server alone.
	/// </summary>
	public event Action<AbilityData>? AbilityNotListed;

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
	/// Gets or sets who hears about an effect whose data says <see cref="EffectVisibility.Default"/>. Read as an effect
	/// first goes active.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when set to <see cref="EffectVisibility.Default"/>, which
	/// would say nothing.</exception>
	public EffectVisibility DefaultEffectVisibility
	{
		get => _defaultEffectVisibility;
		set => _defaultEffectVisibility = value != EffectVisibility.Default
			? value
			: throw new ArgumentOutOfRangeException(nameof(value), value, "Say who hears about those effects.");
	}

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
		_graphObserver = new GraphObserver(this);

		for (int i = 0; i < session.Registry.Count(AssetKind.EffectContext); i++)
		{
			if (session.Registry.TryGet(AssetKind.EffectContext, i, out EffectContextCodec codec))
			{
				_contextCodecs[codec.ContextType] = i;
			}
		}
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
	/// <param name="spawnKey">The key <see cref="KeySpawn"/> gave the graph that spawned it, which its owner's client
	/// finds the one it made ahead of the server by.</param>
	/// <returns>The entity's replication record.</returns>
	/// <exception cref="ArgumentException">Thrown when the entity has no id, its id is taken, it's registered already,
	/// or the registry has no such scene.</exception>
	public ReplicatedEntity Register(IForgeEntity entity, int sceneIndex, PeerId owner, SpawnKey? spawnKey = null)
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
			_session.Clock.Tick,
			BodySource(entity),
			spawnKey);
		_entities[entity.NetworkId] = replicated;
		_byEntity[entity] = replicated;
		_entityList.Add(replicated);
		_nextId = Math.Max(_nextId, entity.NetworkId.Value + 1);

		entity.CuesManager.AddObserver(_cueObserver);
		entity.Abilities.GraphObserver = _graphObserver;

		// A client predicts only an entity spawned for it: one placed in its scene is a mirror there, whoever owns it.
		entity.Abilities.TracksPrediction = sceneIndex >= 0 && owner != PeerId.None && owner != PeerId.Server;

		foreach (PersistentCue cue in EntitySimulation.PersistentCues(entity))
		{
			_cueObserver.CueApplied(cue.CueTag, cue.Target, cue.Parameters, cue.Application);
		}

		return replicated;
	}

	/// <summary>
	/// Gives the key of a spawn a graph's node is making for an entity whose owner's client predicts it: register the
	/// spawned entity with it. That client makes the spawn ahead of the server under the same key, and the server's
	/// spawn, arriving there, binds that one instead of making another.
	/// </summary>
	/// <remarks>
	/// Ask once per spawn, as the node makes it: each spawn a node makes in a tick takes the next key.
	/// </remarks>
	/// <param name="graphContext">The context of the graph making the spawn.</param>
	/// <param name="node">The index of the node making it, its <see cref="Node.Index"/>.</param>
	/// <returns>The key, or <see langword="null"/> for a graph that isn't an ability's run on an entity a client
	/// predicts.</returns>
	public SpawnKey? KeySpawn(GraphContext graphContext, int node)
	{
		return graphContext.TryGetActivationContext(out AbilityBehaviorContext? context)
			&& context.Owner.Abilities.TracksPrediction
			? _spawnKeys.Next(context, node)
			: null;
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

		// Its events and graphs stay: its last events go with its despawn, so what it did as it went, a burst as it
		// dies, still shows.
		replicated.Cues.Clear();
		replicated.ClearEffects();
		entity.Abilities.GraphObserver = null;

		if (!_entityList.Exists(x => x.Entity.CuesManager == entity.CuesManager))
		{
			entity.CuesManager.RemoveObserver(_cueObserver);
		}

		if (replicated.IsPlaced)
		{
			_departedPlaced.Add(replicated);
		}

		foreach (ClientState client in _clients.Values)
		{
			if (client.Views.TryGetValue(replicated.Id, out EntityView? view))
			{
				view.Leaving = true;
			}
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
	/// <param name="tick">The tick just simulated, which the state is of. Before the first one, nothing is sent.
	/// </param>
	/// <remarks>
	/// An exception from an <see cref="EffectContextCodec"/>'s write comes out of here once the send is done, the first
	/// one if several failed. The entity whose write failed is left out of that send to that client, and from then on,
	/// other clients in the same send included, the effect goes without its custom data.
	/// </remarks>
	public void Send(long tick)
	{
		if (tick < 0 || (LastSendTick >= 0 && tick - LastSendTick < SendInterval))
		{
			return;
		}

		LastSendTick = tick;
		_spawnKeys.ForgetBefore(tick);
		long oldestEvent = tick - (long)Math.Ceiling(_session.Clock.TicksIn(EventExpirySeconds));
		ExceptionDispatchInfo? failure = null;

		foreach (ReplicatedEntity entity in _entityList)
		{
			entity.Capture(tick, BodySource);
			entity.CaptureEffects(tick, this, _session.Clock);
			entity.CaptureGraphs(tick, this);
			ExpireEvents(entity, oldestEvent);
			entity.PredictionChecksum = null;

			if (!entity.Entity.Abilities.TracksPrediction || !_clients.ContainsKey(entity.Owner))
			{
				continue;
			}

			// A coded behavior or a state hook that can't be captured costs its entity this send's check alone.
			try
			{
				entity.PredictionChecksum = EntitySimulation.PredictionChecksum(
					EntitySimulation.CaptureSnapshot(entity.Entity),
					new PredictionScope(entity.HeardByOwner, _session.Clock.TickRate));
			}
			catch (Exception exception)
			{
				failure ??= ExceptionDispatchInfo.Capture(exception);
			}
		}

		foreach (ClientState client in _clients.Values)
		{
			UpdateViews(client, tick);

			// A despawn sent again, or to a client joining after a placed entity went, brings only what's still fresh.
			foreach (EntityView view in client.Views.Values.Where(x => x.Entity.DespawnedAtTick is not null))
			{
				ExpireEvents(view.Entity, oldestEvent);
			}

			SendTo(client, tick, ref failure);
		}

		foreach (ReplicatedEntity entity in _entityList)
		{
			entity.SentEventSequence = entity.LastEventSequence;
		}

		Prune();
		failure?.Throw();
	}

	// What an effect seen for the first time is on the wire, and who hears about it.
	internal ReplicatedEffect DescribeEffect(uint instance, ActiveEffect activeEffect)
	{
		EffectData data = activeEffect.EffectData;
		EffectVisibility visibility = data.Visibility == EffectVisibility.Default
			? DefaultEffectVisibility
			: data.Visibility;
		int assetIndex = -1;

		if (visibility != EffectVisibility.ServerOnly)
		{
			assetIndex = _session.Registry.IndexOfAsset(AssetKind.Effect, data);

			if (assetIndex < 0)
			{
				visibility = EffectVisibility.ServerOnly;

				if (_effectsNotListed.Add(data))
				{
					EffectNotListed?.Invoke(data);
				}
			}
		}

		int contextCodec = activeEffect.EffectEvaluatedData.ApplicationContext is EffectApplicationContext context
			&& _contextCodecs.TryGetValue(context.GetType(), out int codec)
			? codec
			: -1;

		return new ReplicatedEffect(instance, activeEffect, assetIndex, visibility, contextCodec);
	}

	// What a graph run seen for the first time is on the wire: the ability it runs for and that activation's context.
	internal ReplicatedGraph DescribeGraph(uint instance, GraphProcessor processor, AbilityBehaviorContext context)
	{
		int assetIndex = -1;

		if (context.AbilityHandle.Ability is Ability ability)
		{
			assetIndex = _session.Registry.IndexOfAsset(AssetKind.Ability, ability.AbilityData);

			if (assetIndex < 0 && _abilitiesNotListed.Add(ability.AbilityData))
			{
				AbilityNotListed?.Invoke(ability.AbilityData);
			}
		}

		return new ReplicatedGraph(instance, processor, context, assetIndex);
	}

	// The session is over: the entities live on, and their cues and graphs are no longer anything to this server.
	internal void StopObserving()
	{
		foreach (IForgeEntity entity in _entityList.Select(x => x.Entity))
		{
			entity.CuesManager.RemoveObserver(_cueObserver);
			entity.Abilities.GraphObserver = null;
		}
	}

	internal void AddClient(NetworkPeer peer)
	{
		var client = new ClientState(peer);
		_clients[peer.Id] = client;

		// The client has every entity placed in the scene, including the ones gone here before it joined.
		foreach (ReplicatedEntity entity in _departedPlaced)
		{
			client.Views[entity.Id] = new EntityView(entity, _session.Clock.Tick, isOwner: false)
			{
				Sent = true,
				Leaving = true,
			};
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

	// Hands a client's command to the input of every entity the client owns that has one. Walks a copy, since what an
	// event sets off can spawn or despawn entities.
	internal void DeliverInput(PeerId owner, InputCommand command, TagContainer? raisableEvents)
	{
		_inputBuffer.Clear();
		_inputBuffer.AddRange(_entityList);

		foreach (ReplicatedEntity entity in _inputBuffer)
		{
			if (entity.DespawnedAtTick is not null
				|| entity.Owner != owner
				|| entity.Entity.Input is not EntityInput input)
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
				// An older copy of the despawn, acknowledged late, mustn't take the view of the entity that came back.
				if (client.Views.GetValueOrDefault(sent.View.Entity.Id) == sent.View)
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

	private static void ExpireEvents(ReplicatedEntity entity, long oldestEvent)
	{
		while (entity.Events.Count > 0 && entity.Events[0].CreatedAtTick < oldestEvent)
		{
			entity.Events.RemoveAt(0);
		}

		for (int i = 0; i < entity.Graphs.Count; i++)
		{
			List<ReplicatedGraph.GraphEvent> events = entity.Graphs[i].Events;

			while (events.Count > 0 && events[0].CreatedAtTick < oldestEvent)
			{
				events.RemoveAt(0);
			}
		}
	}

	private static bool HasPending(EntityView view)
	{
		if (view.Leaving || view.Acked < 0 || ChecksPrediction(view))
		{
			return true;
		}

		ReplicatedEntity entity = view.Entity;
		long acked = view.Acked;
		long seen = view.Seen;

		for (int i = 0; i < entity.Attributes.Length; i++)
		{
			if (AttributeChanged(view, i))
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

		foreach (ReplicatedEffect effect in entity.Effects)
		{
			if (view.Hears(effect.Visibility) && effect.LastChange > acked)
			{
				return true;
			}
		}

		foreach (ReplicatedEntity.EffectTombstone tombstone in entity.RemovedEffects)
		{
			if (view.Hears(tombstone.Visibility) && tombstone.Stamp > seen)
			{
				return true;
			}
		}

		for (int i = 0; i < entity.Graphs.Count; i++)
		{
			if (GraphChanged(view, entity.Graphs[i], despawning: false))
			{
				return true;
			}
		}

		return entity.BodyStamp > acked;
	}

	// Whether the view is the owner's of an entity it predicts, which gets the prediction checksum every send.
	private static bool ChecksPrediction(EntityView view)
	{
		return view.IsOwner && view.Entity.Entity.Abilities.TracksPrediction;
	}

	// A graph run goes to a client while the client lacks what it is, how it is or what it did lately, and once it has
	// ended, to a client that has it. One the client never had goes only for its events, as a run too short to have
	// been seen does. A despawning entity's graphs go only for their events. The owner of an entity runs the runs it
	// predicts itself, so it gets none of them, and of a run it takes in, only that it started and that it ended.
	private static bool GraphChanged(EntityView view, ReplicatedGraph graph, bool despawning)
	{
		if (!graph.IsListed || (view.IsOwner && graph.PredictedByOwner))
		{
			return false;
		}

		if (view.IsOwner && graph.TakenInByOwner)
		{
			return !despawning && (graph.Removed ? graph.RemovedStamp > view.Seen : graph.CreatedStamp > view.Acked);
		}

		// Recorded only for an owner that takes it in.
		if (!graph.Table.RunsOnClients)
		{
			return false;
		}

		bool events = HasEvents(graph, view.EventsAcked);

		if (despawning)
		{
			return events;
		}

		// The end goes until acknowledged, as a removed tag does, whether or not the client acknowledged the start: it
		// may have the run with its acknowledgement still on the way.
		if (graph.Removed)
		{
			return events || graph.RemovedStamp > view.Seen;
		}

		return graph.CreatedStamp > view.Acked
			|| graph.StatesStamp > view.Acked
			|| graph.ValuesChanged(view.Acked)
			|| events;
	}

	private static bool HasEvents(ReplicatedGraph graph, uint eventsAcked)
	{
		return graph.Events.Count > 0 && graph.Events[^1].Sequence > eventsAcked;
	}

	private static int Newer(long[] stamps, long acked)
	{
		int newer = 0;

		for (int i = 0; i < stamps.Length; i++)
		{
			if (stamps[i] > acked)
			{
				newer++;
			}
		}

		return newer;
	}

	private static bool AttributeChanged(EntityView view, int attribute)
	{
		ReplicatedEntity entity = view.Entity;
		long acked = view.Acked;

		return entity.ValueStamps[attribute] > acked
			|| entity.RangeStamps[attribute] > acked
			|| (view.IsOwner && entity.BaseStamps[attribute] > acked);
	}

	private void UpdateViews(ClientState client, long tick)
	{
		foreach (ReplicatedEntity entity in _entityList)
		{
			bool relevant = entity.IsPlaced || Relevancy.IsRelevant(client.Peer.Id, entity, this);
			client.Views.TryGetValue(entity.Id, out EntityView? view);

			if (relevant && view is null)
			{
				client.Views[entity.Id] = new EntityView(entity, tick, entity.Owner == client.Peer.Id)
				{
					EventsAcked = entity.SentEventSequence,
				};
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

	private void SendTo(ClientState client, long tick, ref ExceptionDispatchInfo? failure)
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
		bool full = false;

		foreach (EntityView view in _candidates)
		{
			if (full)
			{
				view.LeftOut = true;
				continue;
			}

			uint maxEvent;

			try
			{
				maxEvent = WriteEntry(view, tick);
			}
			catch (Exception exception)
			{
				failure ??= ExceptionDispatchInfo.Capture(exception);
				view.LeftOut = true;
				continue;
			}

			if (packet.Entries.Count > 0 && _packet.ByteLength + _entry.ByteLength + TerminatorBytes > BudgetBytes)
			{
				full = true;
				view.LeftOut = true;
				continue;
			}

			_packet.WriteBytes(_entry.WrittenBytes);
			view.Accumulated = 0;
			view.LeftOut = false;
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
	private uint WriteEntry(EntityView view, long tick)
	{
		ReplicatedEntity entity = view.Entity;
		_entry.Clear();
		_entry.WriteVarUInt(entity.Id.Value);

		if (view.Leaving)
		{
			_entry.WriteBits((ulong)EntryKind.Despawn, 2);
			uint lastEvent = WriteEvents(view);
			lastEvent = Math.Max(lastEvent, WriteGraphs(view, tick, despawning: true));
			_entry.Align();
			return lastEvent;
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

			// The owner makes an entity it predicts where the server's body was as it was registered, exactly, so the
			// entity's setup runs where the server's did.
			BodyState? spawnBody = view.IsOwner && !entity.IsPlaced ? entity.SpawnBody : null;
			_entry.WriteBool(spawnBody is not null);

			if (spawnBody is BodyState body)
			{
				BodyCodec.WriteExact(_entry, body);
			}

			// And finds by its key the spawn it made ahead of the server.
			SpawnKey? spawnKey = view.IsOwner ? entity.SpawnKey : null;
			_entry.WriteBool(spawnKey is not null);

			if (spawnKey is SpawnKey key)
			{
				WriteSpawnKey(key, tick);
			}
		}

		WriteAttributes(view);
		WriteTags(entity, acked, seen);
		WriteCues(view);
		uint maxEvent = WriteEvents(view);
		WriteEffects(view, tick);
		maxEvent = Math.Max(maxEvent, WriteGraphs(view, tick, despawning: false));

		bool bodyChanged = entity.BodyStamp > acked;
		_entry.WriteBool(bodyChanged);

		if (bodyChanged)
		{
			_entry.WriteBool(view.LeftOut);
			_entry.WriteBool(entity.ShadowBody is not null);

			if (entity.ShadowBody is QuantizedBody body)
			{
				BodyCodec.Write(_entry, body);
			}
		}

		ulong? checksum = ChecksPrediction(view) ? entity.PredictionChecksum : null;
		_entry.WriteBool(checksum is not null);

		if (checksum is ulong value)
		{
			_entry.WriteBits(value, 64);
		}

		_entry.Align();
		return maxEvent;
	}

	private void WriteSpawnKey(SpawnKey key, long tick)
	{
		_entry.WriteVarUInt(key.Spawner.Value);
		_entry.WriteVarUInt((ulong)(tick - key.Tick));
		_entry.WriteUInt32(key.Ability);
		_entry.WriteVarUInt((ulong)key.Node);
		_entry.WriteVarUInt((ulong)key.Sequence);
	}

	private void WriteAttributes(EntityView view)
	{
		ReplicatedEntity entity = view.Entity;
		long acked = view.Acked;
		int changed = 0;

		for (int i = 0; i < entity.Attributes.Length; i++)
		{
			if (AttributeChanged(view, i))
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
			if (!AttributeChanged(view, i))
			{
				continue;
			}

			bool range = entity.RangeStamps[i] > acked;
			bool baseValue = view.IsOwner && entity.BaseStamps[i] > acked;
			_entry.WriteVarUInt((ulong)i);
			_entry.WriteVarInt(entity.ShadowValues[i]);
			_entry.WriteBool(range);

			if (range)
			{
				_entry.WriteVarInt(entity.ShadowMins[i]);
				_entry.WriteVarInt(entity.ShadowMaxes[i]);
			}

			_entry.WriteBool(baseValue);

			if (baseValue)
			{
				_entry.WriteVarInt(entity.ShadowBases[i]);
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

	// The owner of an entity it predicts shows the cues it predicts itself, so it gets only the others.
	private void WriteCues(EntityView view)
	{
		ReplicatedEntity entity = view.Entity;
		long acked = view.Acked;
		long seen = view.Seen;
		bool owner = ChecksPrediction(view);
		int cues = 0;

		foreach (ReplicatedEntity.CueInstance cue in entity.Cues)
		{
			if (cue.Stamp > acked && !(owner && entity.OwnerShows(cue)))
			{
				cues++;
			}
		}

		foreach (ReplicatedEntity.CueTombstone tombstone in entity.RemovedCues)
		{
			if (tombstone.Stamp > seen && !(owner && tombstone.OwnersOwn))
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
			if (cue.Stamp <= acked || (owner && entity.OwnerShows(cue)))
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
			if (tombstone.Stamp <= seen || (owner && tombstone.OwnersOwn))
			{
				continue;
			}

			_entry.WriteVarUInt(tombstone.Instance);
			_entry.WriteBool(true);
			_entry.WriteBool(tombstone.Interrupted);
		}
	}

	private uint WriteEvents(EntityView view)
	{
		ReplicatedEntity entity = view.Entity;
		uint eventsAcked = view.EventsAcked;
		bool owner = ChecksPrediction(view);
		int events = 0;

		foreach (ReplicatedEntity.CueEvent cueEvent in entity.Events)
		{
			if (cueEvent.Sequence > eventsAcked && !(owner && cueEvent.OwnersOwn))
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

			if (owner && cueEvent.OwnersOwn)
			{
				continue;
			}

			_entry.WriteVarUInt(cueEvent.Sequence);
			_entry.WriteVarUInt(_session.TagsManager.GetNetIndexFromTag(cueEvent.CueTag));
			ReplicationCodec.WriteParameters(_entry, cueEvent.Parameters);
		}

		return maxEvent;
	}

	private void WriteEffects(EntityView view, long tick)
	{
		ReplicatedEntity entity = view.Entity;
		long acked = view.Acked;
		long seen = view.Seen;
		int effects = 0;

		foreach (ReplicatedEffect effect in entity.Effects)
		{
			if (view.Hears(effect.Visibility) && effect.LastChange > acked)
			{
				effects++;
			}
		}

		foreach (ReplicatedEntity.EffectTombstone tombstone in entity.RemovedEffects)
		{
			if (view.Hears(tombstone.Visibility) && tombstone.Stamp > seen)
			{
				effects++;
			}
		}

		_entry.WriteBool(effects > 0);

		if (effects == 0)
		{
			return;
		}

		_entry.WriteVarUInt((ulong)effects);

		foreach (ReplicatedEffect effect in entity.Effects)
		{
			if (view.Hears(effect.Visibility) && effect.LastChange > acked)
			{
				WriteEffect(effect, acked, tick, ChecksPrediction(view));
			}
		}

		foreach (ReplicatedEntity.EffectTombstone tombstone in entity.RemovedEffects)
		{
			if (view.Hears(tombstone.Visibility) && tombstone.Stamp > seen)
			{
				_entry.WriteVarUInt(tombstone.Instance);
				_entry.WriteBool(true);
				_entry.WriteBool(tombstone.Expired);
			}
		}
	}

	// What the effect is goes until the client has it, what it's like now whenever that changes, and how often it
	// executed on its own, since that changes the most.
	private void WriteEffect(ReplicatedEffect effect, long acked, long tick, bool forOwner)
	{
		NetworkAssetRegistry registry = _session.Registry;
		_entry.WriteVarUInt(effect.Instance);
		_entry.WriteBool(false);

		bool created = effect.CreatedStamp > acked;
		_entry.WriteBool(created);

		if (created)
		{
			_entry.WriteBits((ulong)effect.AssetIndex, registry.BitsFor(AssetKind.Effect));

			// The owner of an entity it predicts takes in what landed from outside after the tick it landed after.
			bool outside = forOwner && effect.Active.IsFromOutside;
			_entry.WriteBool(outside);

			if (outside)
			{
				_entry.WriteVarInt(tick - effect.Active.AfterTick);
			}

			_entry.WriteBool(effect.ContextCodec >= 0);

			if (effect.ContextCodec >= 0)
			{
				_entry.WriteBits((ulong)effect.ContextCodec, registry.BitsFor(AssetKind.EffectContext));

				try
				{
					registry.Get<EffectContextCodec>(AssetKind.EffectContext, effect.ContextCodec)
						.Write(_entry, effect.Active.EffectEvaluatedData.ApplicationContext!);
				}
				catch
				{
					// It would fail every send for as long as the effect lasts, so the data stays on the server, as
					// data with no codec does, and the entity's entry goes without it from the next send.
					effect.ContextCodec = -1;
					throw;
				}
			}
		}

		bool state = effect.StateStamp > acked;
		_entry.WriteBool(state);

		if (state)
		{
			_entry.WriteVarInt(effect.Level);
			_entry.WriteVarUInt((ulong)effect.StackCount);
			_entry.WriteBool(effect.IsInhibited);
			_entry.WriteBool(effect.IsTimed);

			if (effect.IsTimed)
			{
				_entry.WriteFloat(effect.Duration);
				_entry.WriteVarInt(effect.EndTick - tick);
			}

			_entry.WriteVarUInt(effect.OwnerId);
			_entry.WriteVarUInt(effect.SourceId);
			_entry.WriteVarUInt((ulong)effect.SetByCallers.Count);

			for (int i = 0; i < effect.SetByCallers.Count; i++)
			{
				_entry.WriteVarUInt(effect.SetByCallers[i].Key);
				_entry.WriteFloat(effect.SetByCallers[i].Value);
			}
		}

		bool executions = effect.ExecutionStamp > acked;
		_entry.WriteBool(executions);

		if (executions)
		{
			_entry.WriteVarUInt((ulong)effect.ExecutionCount);
		}
	}

	// Writes the graph runs the client lacks something of, and returns the newest event sequence written.
	private uint WriteGraphs(EntityView view, long tick, bool despawning)
	{
		List<ReplicatedGraph> graphs = view.Entity.Graphs;
		int changed = 0;

		for (int i = 0; i < graphs.Count; i++)
		{
			if (GraphChanged(view, graphs[i], despawning))
			{
				changed++;
			}
		}

		_entry.WriteBool(changed > 0);

		if (changed == 0)
		{
			return view.EventsAcked;
		}

		_entry.WriteVarUInt((ulong)changed);
		uint maxEvent = view.EventsAcked;

		for (int i = 0; i < graphs.Count; i++)
		{
			if (GraphChanged(view, graphs[i], despawning))
			{
				maxEvent = Math.Max(maxEvent, WriteGraph(view, graphs[i], tick, despawning));
			}
		}

		return maxEvent;
	}

	// What the run is goes until the client has it, which states run and for how long whenever that changes, and the
	// variables that changed, then the events the client hasn't acknowledged and whether the run ended.
	private uint WriteGraph(EntityView view, ReplicatedGraph graph, long tick, bool despawning)
	{
		long acked = view.Acked;
		_entry.WriteVarUInt(graph.Instance);

		// The owner runs a run it takes in itself, from after the tick it started after, so it hears nothing of how the
		// run goes.
		bool takenIn = view.IsOwner && graph.TakenInByOwner;

		// An ended run with no actions left to bring goes as its end alone: a client that never had it has nothing to
		// end.
		bool live = !despawning && !graph.Removed;
		bool brings = live || (!takenIn && HasEvents(graph, view.EventsAcked));
		bool created = brings && graph.CreatedStamp > acked;
		_entry.WriteBool(created);

		if (created)
		{
			_entry.WriteVarUInt((ulong)graph.AssetIndex);
			_entry.WriteVarUInt(graph.TargetId);
			_entry.WriteVarUInt(graph.SourceId);
			_entry.WriteVarInt(graph.Level);
			_entry.WriteFloat(graph.Magnitude);
			_entry.WriteBool(takenIn);

			if (takenIn)
			{
				_entry.WriteVarInt(tick - graph.AfterTick);
			}
		}

		bool states = !takenIn && live && graph.StatesStamp > acked;
		_entry.WriteBool(states);

		if (states)
		{
			_entry.WriteVarUInt((ulong)graph.States.Count);

			foreach (ReplicatedGraph.RunningState running in graph.States)
			{
				_entry.WriteVarUInt((ulong)running.State);
				_entry.WriteVarUInt(running.ActivationId);
				_entry.WriteVarUInt((ulong)(tick - running.SinceTick));
			}
		}

		// A run that ended still sends its values with the actions it brings: they read them.
		bool values = !takenIn && brings && graph.ValuesChanged(acked);
		_entry.WriteBool(values);

		if (values)
		{
			WriteGraphValues(graph, acked);
		}

		uint maxEvent = view.EventsAcked;
		int events = 0;

		foreach (ReplicatedGraph.GraphEvent graphEvent in graph.Events)
		{
			if (!takenIn && graphEvent.Sequence > view.EventsAcked)
			{
				events++;
			}
		}

		_entry.WriteBool(events > 0);

		if (events > 0)
		{
			_entry.WriteVarUInt((ulong)events);

			foreach (ReplicatedGraph.GraphEvent graphEvent in graph.Events)
			{
				if (graphEvent.Sequence > view.EventsAcked)
				{
					maxEvent = Math.Max(maxEvent, graphEvent.Sequence);
					_entry.WriteVarUInt(graphEvent.Sequence);
					_entry.WriteVarUInt((ulong)graphEvent.Action);
					_entry.WriteVarUInt(graphEvent.AfterActivation);
				}
			}
		}

		_entry.WriteBool(!despawning && graph.Removed);
		return maxEvent;
	}

	private void WriteGraphValues(ReplicatedGraph graph, long acked)
	{
		_entry.WriteVarUInt((ulong)Newer(graph.ValueStamps, acked));

		for (int i = 0; i < graph.Values.Length; i++)
		{
			if (graph.ValueStamps[i] > acked)
			{
				_entry.WriteVarUInt((ulong)i);
				ReplicationCodec.WriteVariant(_entry, graph.Values[i]);
			}
		}

		_entry.WriteVarUInt((ulong)Newer(graph.ArrayStamps, acked));

		for (int i = 0; i < graph.Arrays.Length; i++)
		{
			if (graph.ArrayStamps[i] <= acked)
			{
				continue;
			}

			_entry.WriteVarUInt((ulong)i);
			_entry.WriteVarUInt((ulong)graph.Arrays[i].Length);

			foreach (Variant128 element in graph.Arrays[i])
			{
				ReplicationCodec.WriteVariant(_entry, element);
			}
		}
	}

	// Drops the tombstones every client that could have seen the tag, cue or effect has heard about.
	private void Prune()
	{
		foreach (ReplicatedEntity entity in _entityList)
		{
			if (entity.RemovedTags.Count > 0
				|| entity.RemovedCues.Count > 0
				|| entity.RemovedEffects.Count > 0
				|| entity.HasRemovedGraphs)
			{
				PruneTombstones(entity);
			}
		}
	}

	private void PruneTombstones(ReplicatedEntity entity)
	{
		long needed = long.MaxValue;
		long neededByOwner = long.MaxValue;

		foreach (ClientState client in _clients.Values)
		{
			if (client.Views.TryGetValue(entity.Id, out EntityView? view) && !view.Leaving)
			{
				needed = Math.Min(needed, view.Seen);

				if (view.IsOwner)
				{
					neededByOwner = view.Seen;
				}
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

		// Everyone else never hears of an owner-only effect, so what they acknowledged says nothing about it, and an
		// idle entity may never give them anything to acknowledge.
		entity.RemovedEffects.RemoveAll(
			x => x.Stamp <= (x.Visibility == EffectVisibility.OwnerOnly ? neededByOwner : needed));

		// A run's actions go only with its record, so it stays until they expire: a client that first heard of the
		// entity as the run ended hasn't acknowledged them yet.
		entity.Graphs.RemoveAll(x => x.Removed && x.RemovedStamp <= needed && x.Events.Count == 0);
	}

	// What one client knows of one entity.
	private sealed class EntityView(ReplicatedEntity entity, long baseTick, bool isOwner)
	{
		public ReplicatedEntity Entity { get; } = entity;

		// The entity is the client's own, so it hears of owner-only effects and of base values.
		public bool IsOwner { get; } = isOwner;

		// The send the client first heard of the entity at: anything that went before it never reached the client.
		public long Base { get; } = baseTick;

		// The newest send of the entity the client acknowledged, or -1 for none, which asks for the entity whole.
		public long Acked { get; set; } = -1;

		// Removals older than this never need sending: the client acknowledged a state without them, or first heard
		// of the entity after them.
		public long Seen => Math.Max(Acked, Base);

		public uint EventsAcked { get; set; }

		public float Accumulated { get; set; }

		// The budget left the entity out of the last send, so its absence there says nothing about what changed.
		public bool LeftOut { get; set; }

		public bool Sent { get; set; }

		// The client is to despawn the entity: it left, or stopped being relevant.
		public bool Leaving { get; set; }

		public bool Hears(EffectVisibility visibility)
		{
			return visibility == EffectVisibility.Public || (visibility == EffectVisibility.OwnerOnly && IsOwner);
		}
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

			entity.Cues.Add(new ReplicatedEntity.CueInstance(
				entity.NextCueInstance++,
				application,
				cueTag,
				parameters,
				application is not ActiveEffect && target.EffectsManager.RaisesOwnTick));
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
			entity.RemovedCues
				.Add(new ReplicatedEntity.CueTombstone(
					cue.Instance,
					ReplicatedEntity.Pending,
					interrupted,
					entity.OwnerShows(cue)));
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
				server._session.Clock.Tick,
				target.EffectsManager.RaisesOwnTick));
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

	// Keeps the actions clients run that each replicated entity's graphs execute, as they execute.
	private sealed class GraphObserver(ReplicationServer server) : IGraphObserver
	{
		public void ActionExecuted(GraphContext graphContext, Node node)
		{
			if (graphContext.Processor is not GraphProcessor processor
				|| graphContext.ActivationContext is not AbilityBehaviorContext context
				|| !server._byEntity.TryGetValue(context.Owner, out ReplicatedEntity? entity))
			{
				return;
			}

			int action = processor.Graph.NetworkTable.ActionIndexOf(node);

			if (action < 0)
			{
				return;
			}

			long tick = server._session.Clock.Tick;
			ReplicatedGraph graph = entity.GraphFor(processor, context, server, tick);

			if (graph.IsListed)
			{
				// The values the action read, for a run that may end before the next send looks at it.
				graph.CaptureValues(tick);
				graph.Events.Add(new ReplicatedGraph.GraphEvent(
					++entity.LastEventSequence,
					action,
					graphContext.ActivationCount,
					tick));
			}
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
}
