// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Networking.Messages;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Networking.Replication;

/// <summary>
/// A client's half of replication: it reads the server's state packets and keeps the client's copies of the server's
/// entities, its mirrors, as the server says they are. A mirror simulates nothing of its own: it's torn down as it's
/// bound, and from then on its attributes, tags, active effects and persistent cues are what the server last sent, its
/// executed cues play as they arrive, and its body is where the server said it was a moment ago, from
/// <see cref="BodyOf"/>.
/// </summary>
/// <remarks>
/// <para>The engine integration sets <see cref="Host"/> before the client joins, to make and take away the entities the
/// server spawns and despawns, and binds the entities placed in the scene with <see cref="BindPlaced"/>, with the same
/// ids the server gives them, before the session is ready.</para>
/// <para>A state packet older than one already read is dropped and not acknowledged, so the server sends what it
/// carried again.</para>
/// </remarks>
public sealed class ReplicationClient
{
	private readonly NetworkSession _session;
	private readonly Dictionary<NetworkEntityId, Mirror> _mirrors = [];
	private readonly Dictionary<IForgeEntity, Mirror> _byEntity = new(EntityReferenceComparer.Instance);
	private readonly List<IForgeEntity> _entities = [];
	private readonly List<Tag> _added = [];
	private readonly List<Tag> _removed = [];
	private readonly Dictionary<Tag, float> _setByCallers = [];
	private readonly Func<NetworkEntityId, IForgeEntity?> _resolve;
	private uint _ackMask;
	private bool _ackPending;
	private bool _effectOwnersStale;

	/// <summary>
	/// Raised when the server spawned an entity and the host made it, after it was bound.
	/// </summary>
	public event Action<IForgeEntity>? Spawned;

	/// <summary>
	/// Raised when an entity is about to be handed to the host to take away.
	/// </summary>
	public event Action<IForgeEntity>? Despawned;

	/// <summary>
	/// Gets or sets what makes and takes away the server's entities on this machine.
	/// </summary>
	public IReplicationHost? Host { get; set; }

	/// <summary>
	/// Gets the entities the server replicates to this client, in the order they were bound.
	/// </summary>
	public IReadOnlyList<IForgeEntity> Entities => _entities;

	/// <summary>
	/// Gets the newest server tick a state packet was of, or -1 before any.
	/// </summary>
	public long LastServerTick { get; private set; } = -1;

	/// <summary>
	/// Gets the newest state packet sequence received, or 0 before any.
	/// </summary>
	public uint LastSequence { get; private set; }

	/// <summary>
	/// Gets how many state packets arrived.
	/// </summary>
	public long PacketsReceived { get; private set; }

	/// <summary>
	/// Gets how many state packets arrived after a newer one and were dropped.
	/// </summary>
	public long PacketsLate { get; private set; }

	/// <summary>
	/// Gets how many entities the server spawned that the host couldn't make.
	/// </summary>
	public long SpawnsRefused { get; private set; }

	internal ReplicationClient(NetworkSession session)
	{
		_session = session;
		_resolve = id => _mirrors.GetValueOrDefault(id)?.Entity;
	}

	/// <summary>
	/// Finds the entity the server knows by an id.
	/// </summary>
	/// <param name="id">The id.</param>
	/// <returns>The entity, or <see langword="null"/> if this client has none by that id.</returns>
	public IForgeEntity? Find(NetworkEntityId id)
	{
		return _mirrors.GetValueOrDefault(id)?.Entity;
	}

	/// <summary>
	/// Gets the body states the server sent for an entity, to show it between them.
	/// </summary>
	/// <param name="entity">The entity.</param>
	/// <returns>The body, or <see langword="null"/> if the entity isn't replicated to this client or has no body.
	/// </returns>
	public RemoteBody? BodyOf(IForgeEntity entity)
	{
		return _byEntity.TryGetValue(entity, out Mirror? mirror) && mirror.Body.Count > 0 ? mirror.Body : null;
	}

	/// <summary>
	/// Gets the peer whose player an entity is, as the server said.
	/// </summary>
	/// <param name="entity">The entity.</param>
	/// <returns>The owner, or <see cref="PeerId.None"/> for the world's or an entity that isn't replicated here.
	/// </returns>
	public PeerId OwnerOf(IForgeEntity entity)
	{
		return _byEntity.TryGetValue(entity, out Mirror? mirror) ? mirror.Owner : PeerId.None;
	}

	/// <summary>
	/// Binds an entity placed in the scene, which the server replicates under the id every machine gives it: its
	/// <see cref="IForgeEntity.NetworkId"/>. It's torn down and emptied, to hold only what the server sends.
	/// </summary>
	/// <param name="entity">The entity, with its id set.</param>
	/// <exception cref="ArgumentException">Thrown when the entity has no id or the id is bound already.</exception>
	public void BindPlaced(IForgeEntity entity)
	{
		if (!entity.NetworkId.IsValid)
		{
			throw new ArgumentException("Give the entity its network id before binding it.", nameof(entity));
		}

		if (_mirrors.ContainsKey(entity.NetworkId))
		{
			throw new ArgumentException($"{entity.NetworkId} is bound already.", nameof(entity));
		}

		Bind(entity, PeerId.None, placed: true);
	}

	/// <summary>
	/// Lets go of every entity: the ones the server spawned are handed to the host to take away, and the placed ones
	/// are unbound and left as they are. For leaving a session.
	/// </summary>
	public void Clear()
	{
		foreach (Mirror mirror in new List<Mirror>(_mirrors.Values))
		{
			Unbind(mirror, despawn: !mirror.Placed);
		}

		LastServerTick = -1;
		LastSequence = 0;
		_ackMask = 0;
		_ackPending = false;
	}

	internal void Receive(BitReader reader)
	{
		ulong sequence = reader.ReadVarUInt();

		if (sequence is 0 or > uint.MaxValue)
		{
			throw new MalformedPacketException($"{sequence} is not a packet sequence.");
		}

		PacketsReceived++;

		if (sequence <= LastSequence)
		{
			PacketsLate++;
			return;
		}

		long tick = reader.ReadVarInt();

		if (tick < 0)
		{
			throw new MalformedPacketException($"{tick} is not a server tick.");
		}

		long previousTick = LastServerTick;

		while (true)
		{
			uint id = ReplicationCodec.ReadEntityId(reader);

			if (id == 0)
			{
				break;
			}

			ReadEntry(reader, new NetworkEntityId(id), tick, previousTick);
		}

		// Only a packet read to its end counts: one that isn't is never acknowledged, so the server sends it all again.
		LastServerTick = Math.Max(LastServerTick, tick);
		Acknowledge((uint)sequence);
		FollowEffects();
	}

	internal bool TryWriteAck(BitWriter writer)
	{
		if (!_ackPending)
		{
			return false;
		}

		_ackPending = false;
		new StateAckMessage(LastSequence, _ackMask).Write(writer);
		return true;
	}

	private static void ReadAttributes(BitReader reader, Mirror? mirror, PeerId localPeer)
	{
		int count = ReplicationCodec.ReadCount(reader);

		for (int i = 0; i < count; i++)
		{
			int index = ReplicationCodec.ReadCount(reader);
			int value = ReplicationCodec.ReadInt(reader);
			bool range = reader.ReadBool();
			int? min = null;
			int? max = null;

			if (range)
			{
				min = ReplicationCodec.ReadInt(reader);
				max = ReplicationCodec.ReadInt(reader);

				if (min > max)
				{
					throw new MalformedPacketException($"{min} to {max} is not an attribute's range.");
				}
			}

			int? baseValue = reader.ReadBool() ? ReplicationCodec.ReadInt(reader) : null;

			if (mirror is null || index >= mirror.Attributes.Length)
			{
				continue;
			}

			EntityAttribute attribute = mirror.Attributes[index];
			attribute.Mirror(
				value,
				min ?? attribute.Min,
				max ?? attribute.Max,
				baseValue ?? (mirror.Owner == localPeer ? attribute.BaseValue : value));
		}
	}

	// Records a sequence newer than any read before for the next acknowledgement.
	private void Acknowledge(uint sequence)
	{
		uint shift = sequence - LastSequence;
		uint mask = shift < 32 ? _ackMask << (int)shift : 0;

		if (LastSequence != 0 && shift <= 32)
		{
			mask |= 1u << (int)(shift - 1);
		}

		_ackMask = mask;
		LastSequence = sequence;
		_ackPending = true;
	}

	private void ReadEntry(BitReader reader, NetworkEntityId id, long tick, long previousTick)
	{
		var kind = (EntryKind)reader.ReadBits(2);
		_mirrors.TryGetValue(id, out Mirror? mirror);

		switch (kind)
		{
			case EntryKind.Despawn:
				if (mirror is not null)
				{
					Unbind(mirror, despawn: true);
				}

				reader.Align();
				return;

			case EntryKind.Spawn:
				mirror = ReadSpawn(reader, id, mirror);
				break;

			case EntryKind.State:
				break;

			default:
				throw new MalformedPacketException($"{kind} is not an entry kind.");
		}

		ReadState(reader, mirror, tick, previousTick);
		reader.Align();
	}

	private Mirror? ReadSpawn(BitReader reader, NetworkEntityId id, Mirror? mirror)
	{
		bool placed = reader.ReadBool();
		int sceneIndex = -1;

		if (!placed)
		{
			sceneIndex = (int)reader.ReadBits(_session.Registry.BitsFor(AssetKind.Scene));
		}

		ulong owner = reader.ReadVarUInt();
		int attributeCount = ReplicationCodec.ReadCount(reader);

		if (owner > int.MaxValue)
		{
			throw new MalformedPacketException($"{owner} is not a peer id.");
		}

		if (mirror is not null)
		{
			// Placed and bound before the server said whose it is, or spawned already, from an earlier copy of the
			// spawn.
			mirror.Owner = new PeerId((int)owner);
			return mirror;
		}

		if (placed)
		{
			// A placed entity this machine doesn't have: its state is skipped until it's despawned.
			return null;
		}

		if (sceneIndex >= _session.Registry.Count(AssetKind.Scene))
		{
			throw new MalformedPacketException($"The registry has no scene {sceneIndex}.");
		}

		IForgeEntity? entity = Host?.Spawn(id, sceneIndex, new PeerId((int)owner));

		if (entity is null || entity.NetworkId != id || _byEntity.ContainsKey(entity))
		{
			SpawnsRefused++;
			return null;
		}

		mirror = Bind(entity, new PeerId((int)owner), placed: false);
		Spawned?.Invoke(entity);

		if (mirror.Attributes.Length != attributeCount)
		{
			Validation.Fail(
				$"The server's {id} has {attributeCount} attributes and this machine's copy has " +
				$"{mirror.Attributes.Length}: the two don't play the same scene. Its attributes won't follow.");
		}

		return mirror;
	}

	private void ReadState(BitReader reader, Mirror? mirror, long tick, long previousTick)
	{
		int tagCount = _session.TagsManager.NetIndexCount;

		if (reader.ReadBool())
		{
			ReadAttributes(reader, mirror, _session.LocalPeer);
		}

		if (reader.ReadBool())
		{
			_added.Clear();
			_removed.Clear();
			int added = ReplicationCodec.ReadCount(reader);

			for (int i = 0; i < added; i++)
			{
				_added.Add(TagAt(ReplicationCodec.ReadTagIndex(reader, tagCount)));
			}

			int removed = ReplicationCodec.ReadCount(reader);

			for (int i = 0; i < removed; i++)
			{
				_removed.Add(TagAt(ReplicationCodec.ReadTagIndex(reader, tagCount)));
			}

			mirror?.Entity.Tags.MirrorTags(_added, _removed);
		}

		if (reader.ReadBool())
		{
			ReadCues(reader, mirror, tagCount);
		}

		if (reader.ReadBool())
		{
			ReadEvents(reader, mirror, tagCount);
		}

		if (reader.ReadBool())
		{
			ReadEffects(reader, mirror, tagCount, tick);
		}

		if (!reader.ReadBool())
		{
			return;
		}

		bool leftOut = reader.ReadBool();

		if (!reader.ReadBool())
		{
			mirror?.Body.Clear();
			return;
		}

		BodyState body = BodyCodec.Dequantize(BodyCodec.Read(reader));

		if (mirror is null)
		{
			return;
		}

		// A body missing from the packet before this one hadn't changed by then, unless the budget left it out, so it
		// moved only after it: held there, it isn't shown drifting from where it last stood all the way from when it
		// stopped.
		if (!leftOut && mirror.Body.Latest is BodyState held && mirror.Body.LatestTick < previousTick)
		{
			mirror.Body.Add(previousTick, held);
		}

		mirror.Body.Add(tick, body);
	}

	private void ReadCues(BitReader reader, Mirror? mirror, int tagCount)
	{
		int count = ReplicationCodec.ReadCount(reader);

		for (int i = 0; i < count; i++)
		{
			ulong instance = reader.ReadVarUInt();

			if (instance is 0 or > uint.MaxValue)
			{
				throw new MalformedPacketException($"{instance} is not a cue instance.");
			}

			if (reader.ReadBool())
			{
				bool interrupted = reader.ReadBool();
				mirror?.RemoveCue((uint)instance, interrupted);
				continue;
			}

			Tag cueTag = TagAt(ReplicationCodec.ReadTagIndex(reader, tagCount));
			CueParameters? parameters = ReplicationCodec.ReadParameters(reader, _resolve);
			mirror?.ApplyCue((uint)instance, cueTag, parameters);
		}
	}

	private void ReadEvents(BitReader reader, Mirror? mirror, int tagCount)
	{
		int count = ReplicationCodec.ReadCount(reader);

		for (int i = 0; i < count; i++)
		{
			ulong sequence = reader.ReadVarUInt();

			if (sequence > uint.MaxValue)
			{
				throw new MalformedPacketException($"{sequence} is not an event sequence.");
			}

			Tag cueTag = TagAt(ReplicationCodec.ReadTagIndex(reader, tagCount));
			CueParameters? parameters = ReplicationCodec.ReadParameters(reader, _resolve);

			if (mirror is not null && sequence > mirror.LastEvent)
			{
				mirror.LastEvent = (uint)sequence;
				mirror.Entity.CuesManager.ExecuteCue(cueTag, mirror.Entity, parameters);
			}
		}
	}

	private void ReadEffects(BitReader reader, Mirror? mirror, int tagCount, long tick)
	{
		int count = ReplicationCodec.ReadCount(reader);

		for (int i = 0; i < count; i++)
		{
			ulong instance = reader.ReadVarUInt();

			if (instance is 0 or > uint.MaxValue)
			{
				throw new MalformedPacketException($"{instance} is not an effect instance.");
			}

			if (reader.ReadBool())
			{
				bool expired = reader.ReadBool();
				mirror?.RemoveEffect(
					(uint)instance,
					expired ? EffectRemovalReason.Expired : EffectRemovalReason.Removed);
				continue;
			}

			EffectData? data = null;
			EffectApplicationContext? context = null;

			if (reader.ReadBool())
			{
				data = ReadEffectData(reader);
				context = ReadEffectContext(reader);
			}

			EffectState? state = reader.ReadBool() ? ReadEffectState(reader, tagCount, tick) : null;
			int? executionCount = reader.ReadBool() ? ReplicationCodec.ReadCount(reader, int.MaxValue) : null;

			if (mirror is not null)
			{
				ApplyEffect(mirror, (uint)instance, data, context, state, executionCount, tick);
			}
		}
	}

	private EffectData ReadEffectData(BitReader reader)
	{
		NetworkAssetRegistry registry = _session.Registry;
		int index = (int)reader.ReadBits(registry.BitsFor(AssetKind.Effect));

		return registry.TryGet(AssetKind.Effect, index, out EffectData data)
			? data
			: throw new MalformedPacketException($"The registry has no effect {index}.");
	}

	private EffectApplicationContext? ReadEffectContext(BitReader reader)
	{
		if (!reader.ReadBool())
		{
			return null;
		}

		NetworkAssetRegistry registry = _session.Registry;
		int index = (int)reader.ReadBits(registry.BitsFor(AssetKind.EffectContext));

		return registry.TryGet(AssetKind.EffectContext, index, out EffectContextCodec codec)
			? codec.Read(reader)
			: throw new MalformedPacketException($"The registry has no effect context {index}.");
	}

	private EffectState ReadEffectState(BitReader reader, int tagCount, long tick)
	{
		int level = ReplicationCodec.ReadInt(reader);
		int stackCount = ReplicationCodec.ReadCount(reader, int.MaxValue);
		bool isInhibited = reader.ReadBool();
		bool isTimed = reader.ReadBool();
		float duration = isTimed ? reader.ReadFloat() : 0;
		long endTick = isTimed ? tick + reader.ReadVarInt() : 0;
		uint ownerId = ReplicationCodec.ReadEntityId(reader);
		uint sourceId = ReplicationCodec.ReadEntityId(reader);
		int setByCallers = ReplicationCodec.ReadCount(reader);

		_setByCallers.Clear();

		for (int i = 0; i < setByCallers; i++)
		{
			Tag tag = TagAt(ReplicationCodec.ReadTagIndex(reader, tagCount));

			if (!_setByCallers.TryAdd(tag, reader.ReadFloat()))
			{
				throw new MalformedPacketException($"An effect's state names {tag} twice.");
			}
		}

		return new EffectState(level, stackCount, isInhibited, duration, isTimed, endTick, ownerId, sourceId);
	}

	private void ApplyEffect(
		Mirror mirror,
		uint instance,
		EffectData? data,
		EffectApplicationContext? context,
		EffectState? state,
		int? executionCount,
		long tick)
	{
		if (!mirror.Effects.TryGetValue(instance, out MirroredEffect? effect))
		{
			if (data is EffectData effectData && state is EffectState first)
			{
				var created = new Effect(effectData, Ownership(first), first.Level);
				created.RestoreSnapshot(first.Level, [.. _setByCallers]);
				ActiveEffect active = mirror.Entity.EffectsManager.AddMirrorEffect(
					created,
					context,
					first.Level,
					first.StackCount,
					first.Duration,
					first.IsInhibited,
					RemainingDuration(first, tick),
					executionCount ?? 0);
				mirror.Effects[instance] = new MirroredEffect(active, first);
				mirror.Entity.EffectsManager.RaiseMirrorEffect(active, added: true);
			}

			return;
		}

		// The entity's teardown ended it here, and nothing the server says brings it back.
		if (!effect.Active.Handle.IsValid)
		{
			return;
		}

		if (executionCount is int count)
		{
			effect.Active.SetMirroredExecutionCount(count);
		}

		if (state is not EffectState changed || (effect.State == changed && SameSetByCallers(effect.Active.Effect)))
		{
			return;
		}

		effect.State = changed;
		effect.Active.Effect.Ownership = Ownership(changed);
		effect.Active.Effect.RestoreSnapshot(changed.Level, [.. _setByCallers]);
		effect.Active.SetMirrored(changed.Level, changed.StackCount, changed.Duration, changed.IsInhibited);
		effect.Active.RemainingDuration = RemainingDuration(changed, tick);
		mirror.Entity.EffectsManager.RaiseMirrorEffect(effect.Active, added: false);
	}

	private bool SameSetByCallers(Effect effect)
	{
		if (effect.DataTag.Count != _setByCallers.Count)
		{
			return false;
		}

		foreach (KeyValuePair<Tag, float> setByCaller in _setByCallers)
		{
			if (!effect.DataTag.TryGetValue(setByCaller.Key, out float value)
				|| !value.Equals(setByCaller.Value))
			{
				return false;
			}
		}

		return true;
	}

	private EffectOwnership Ownership(EffectState state)
	{
		return new EffectOwnership(
			_resolve(new NetworkEntityId(state.OwnerId)),
			_resolve(new NetworkEntityId(state.SourceId)));
	}

	private double RemainingDuration(EffectState state, long tick)
	{
		return state.IsTimed ? Math.Max(state.EndTick - tick, 0) * _session.Clock.FixedDeltaTime : 0;
	}

	// After every packet: what's left of each effect follows the newest state, so it hears nothing to count down, and
	// an effect's owner and source are whatever this client has of them now.
	private void FollowEffects()
	{
		foreach (Mirror mirror in _mirrors.Values)
		{
			foreach (MirroredEffect effect in mirror.Effects.Values)
			{
				effect.Active.RemainingDuration = RemainingDuration(effect.State, LastServerTick);

				if (_effectOwnersStale)
				{
					effect.Active.Effect.Ownership = Ownership(effect.State);
				}
			}
		}

		_effectOwnersStale = false;
	}

	private Tag TagAt(ushort index)
	{
		TagsManager tagsManager = _session.TagsManager;
		return Tag.RequestTag(tagsManager, tagsManager.GetTagKeyFromNetIndex(index));
	}

	private Mirror Bind(IForgeEntity entity, PeerId owner, bool placed)
	{
		EntitySimulation.Teardown(entity);
		entity.Tags.MirrorClear();
		var mirror = new Mirror(entity, owner, placed);
		_mirrors[entity.NetworkId] = mirror;
		_byEntity[entity] = mirror;
		_entities.Add(entity);
		_effectOwnersStale = true;
		return mirror;
	}

	private void Unbind(Mirror mirror, bool despawn)
	{
		mirror.RemoveAllCues();
		mirror.RemoveAllEffects();
		_mirrors.Remove(mirror.Entity.NetworkId);
		_byEntity.Remove(mirror.Entity);
		_entities.Remove(mirror.Entity);
		_effectOwnersStale = true;

		if (despawn)
		{
			Despawned?.Invoke(mirror.Entity);
			Host?.Despawn(mirror.Entity);
		}
	}

	private sealed class Mirror(IForgeEntity entity, PeerId owner, bool placed)
	{
		private readonly Dictionary<uint, AppliedCue> _cues = [];

		public IForgeEntity Entity { get; } = entity;

		public PeerId Owner { get; set; } = owner;

		public bool Placed { get; } = placed;

		public EntityAttribute[] Attributes { get; } = [.. entity.Attributes];

		public RemoteBody Body { get; } = new();

		public uint LastEvent { get; set; }

		public Dictionary<uint, MirroredEffect> Effects { get; } = [];

		public void RemoveEffect(uint instance, EffectRemovalReason reason)
		{
			if (Effects.Remove(instance, out MirroredEffect? effect))
			{
				Entity.EffectsManager.RemoveMirrorEffect(effect.Active, reason);
			}
		}

		public void RemoveAllEffects()
		{
			foreach (MirroredEffect effect in Effects.Values)
			{
				Entity.EffectsManager.RemoveMirrorEffect(effect.Active, EffectRemovalReason.Destroyed);
			}

			Effects.Clear();
		}

		public void ApplyCue(uint instance, Tag cueTag, CueParameters? parameters)
		{
			if (_cues.TryGetValue(instance, out AppliedCue? applied))
			{
				// The server sends a cue again until it's acknowledged, which is no update unless it changed.
				if (!CuesManager.SameParameters(applied.Parameters, parameters))
				{
					applied.Parameters = parameters;
					Entity.CuesManager.UpdateCue(cueTag, Entity, parameters, applied);
				}

				return;
			}

			applied = new AppliedCue(cueTag, parameters);
			_cues[instance] = applied;
			Entity.CuesManager.ApplyCue(cueTag, Entity, parameters, applied);
		}

		public void RemoveCue(uint instance, bool interrupted)
		{
			if (_cues.Remove(instance, out AppliedCue? applied))
			{
				Entity.CuesManager.RemoveCue(applied.CueTag, Entity, interrupted, applied);
			}
		}

		public void RemoveAllCues()
		{
			foreach (AppliedCue applied in _cues.Values)
			{
				Entity.CuesManager.RemoveCue(applied.CueTag, Entity, interrupted: true, applied);
			}

			_cues.Clear();
		}

		// The application a replicated persistent cue is applied for on this machine: one object per instance.
		private sealed class AppliedCue(Tag cueTag, CueParameters? parameters)
		{
			public Tag CueTag { get; } = cueTag;

			public CueParameters? Parameters { get; set; } = parameters;
		}
	}

	// An effect the server says is active on a mirror, with what the server last said of it.
	private sealed class MirroredEffect(ActiveEffect active, EffectState state)
	{
		public ActiveEffect Active { get; } = active;

		public EffectState State { get; set; } = state;
	}

	private readonly record struct EffectState(
		int Level,
		int StackCount,
		bool IsInhibited,
		float Duration,
		bool IsTimed,
		long EndTick,
		uint OwnerId,
		uint SourceId);
}
