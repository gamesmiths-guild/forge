// Copyright © Gamesmiths Guild.

using System.Runtime.ExceptionServices;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Networking.Messages;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
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
/// <para>A mirror's abilities run copies of the graphs the server's entity runs, as far as clients run them: the
/// presentation states start and end as the server says, caught up by how long they ran there, its presentation actions
/// execute as the server executed them, and its client-only regions run here from what leads into them. No link is
/// followed otherwise. The host advances and presents them with the mirror's abilities, as any entity's:
/// <see cref="EntityAbilities.FixedUpdateAbilities"/>, <see cref="EntityAbilities.UpdateAbilities"/> and
/// <see cref="EntityAbilities.PresentAbilities"/>.</para>
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
	private readonly List<RunningState> _graphStates = [];
	private readonly List<KeyValuePair<int, Variant128>> _graphValues = [];
	private readonly List<KeyValuePair<int, Variant128[]>> _graphArrays = [];
	private readonly List<GraphAction> _graphEvents = [];
	private readonly List<KeyValuePair<int, ulong>> _endingStates = [];
	private readonly Func<NetworkEntityId, IForgeEntity?> _resolve;
	private readonly Queue<HeldPacket> _held = new();
	private readonly Dictionary<NetworkEntityId, RemoteBody> _pendingBodies = [];
	private readonly BitReader _heldReader = new(ReadOnlyMemory<byte>.Empty);
	private readonly List<Prediction> _toReconcile = [];
	private readonly List<Prediction> _unjoined = [];
	private readonly List<Prediction> _rejoining = [];
	private readonly List<Prediction> _correcting = [];
	private readonly HashSet<SpawnKey> _ended = [];
	private readonly HashSet<NetworkEntityId> _forgotten = [];
	private readonly SpawnKeys _spawnKeys = new();
	private PredictionTimeline? _timeline;
	private long _shownTick = -1;
	private uint _ackMask;
	private bool _ackPending;
	private bool _effectOwnersStale;
	private bool _holdsUntilShown;

	/// <summary>
	/// Raised when the server spawned an entity and the host made it, after it was bound.
	/// </summary>
	public event Action<IForgeEntity>? Spawned;

	/// <summary>
	/// Raised when an entity is about to be handed to the host to take away.
	/// </summary>
	public event Action<IForgeEntity>? Despawned;

	/// <summary>
	/// Raised after a state packet put the entities this client predicts right and their ticks since ran again, once
	/// for each entity in the world: what it shows can ease from where it was to where it is now.
	/// </summary>
	public event Action<IForgeEntity>? Corrected;

	/// <summary>
	/// Raised once a correction ran the ticks after the one it rolled back to again, with that tick. What a node made
	/// in those ticks, such as a cosmetic scene's instance, is made again only where the ticks run again reached that
	/// node: a host that found such instances again by their <see cref="SpawnKey"/> frees the ones this correction
	/// didn't.
	/// </summary>
	public event Action<long>? Replayed;

	/// <summary>
	/// Raised when an entity this client predicts goes to limbo, once it let go of everything: one freed, one a
	/// correction rolled back to before the tick it was made in, or one this client spawned that the server didn't.
	/// The host hides it and takes it out of everything that isn't Forge's, such as its physics, until it comes back
	/// or the replication hands it to <see cref="IReplicationHost.Despawn"/>.
	/// </summary>
	public event Action<IForgeEntity>? EnteredLimbo;

	/// <summary>
	/// Raised when an entity this client predicts comes back from limbo, before it's restored: the host shows it again.
	/// </summary>
	public event Action<IForgeEntity>? LeftLimbo;

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

	/// <summary>
	/// Gets the entities this client predicts that are in the world, in the order they joined its timeline: the ones
	/// the server spawned for this client's peer, and the ones this client spawned ahead of the server
	/// (<see cref="PredictSpawn"/>). They aren't mirrors. The host advances them all through each tick with
	/// <see cref="Predict"/>, ahead of the server, and each state packet puts them right where the server's word for
	/// its tick differs from what this client predicted then, rolling them back and running their ticks again together.
	/// </summary>
	public IReadOnlyList<IForgeEntity> Predicted => _timeline?.Live ?? [];

	/// <summary>
	/// Gets or sets how many ticks the entities this client predicts keep to roll back to, read as the first one is
	/// bound: a state packet older than that can't be checked. Zero keeps as many as the input may lead the server by,
	/// <see cref="NetworkSession.MaxInputLead"/>.
	/// </summary>
	public int PredictionHistoryTicks { get; set; }

	/// <summary>
	/// Gets or sets what an effect whose <see cref="EffectData.Visibility"/> is <see cref="EffectVisibility.Default"/>
	/// is, as <see cref="ReplicationServer.DefaultEffectVisibility"/> is on the server: a predicted entity is checked
	/// on the effects this client hears of.
	/// </summary>
	public EffectVisibility DefaultEffectVisibility { get; set; } = EffectVisibility.Public;

	/// <summary>
	/// Gets how many state packets agreed with what this client predicted of an entity at their tick.
	/// </summary>
	public long Confirmations { get; private set; }

	/// <summary>
	/// Gets how many times a state packet put a predicted entity right.
	/// </summary>
	public long Corrections { get; private set; }

	/// <summary>
	/// Gets how many ticks the last correction rolled back and ran again.
	/// </summary>
	public int LastCorrectionTicks { get; private set; }

	/// <summary>
	/// Gets or sets a value indicating whether a state packet waits for <see cref="Show"/> to reach its tick before
	/// anything but its bodies applies, so what a mirror shows keeps time with its body, which is shown behind the
	/// newest state. Otherwise a packet applies as it arrives.
	/// </summary>
	/// <remarks>
	/// Turned off, it applies every packet still held first, in the order they arrived: they were acknowledged as they
	/// arrived, so the server won't send them again. An exception from that comes out as <see cref="Show"/>'s does,
	/// with holding off and every held packet applied.
	/// </remarks>
	public bool HoldsUntilShown
	{
		get => _holdsUntilShown;
		set
		{
			bool releasing = _holdsUntilShown && !value;
			_holdsUntilShown = value;

			if (releasing)
			{
				Show(double.PositiveInfinity);
			}
		}
	}

	// The entities this client predicts, made as the first one is bound or spawned.
	private PredictionTimeline Timeline
	{
		get
		{
			if (_timeline is null)
			{
				int capacity = PredictionHistoryTicks > 0
					? PredictionHistoryTicks
					: (int)Math.Ceiling(_session.Clock.TicksIn(NetworkSession.MaxInputLead));
				_timeline = new PredictionTimeline(capacity, _session.Clock.FixedDeltaTime);
				_timeline.EnteredLimbo += x => EnteredLimbo?.Invoke(x);
				_timeline.LeftLimbo += x => LeftLimbo?.Invoke(x);
			}

			return _timeline;
		}
	}

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
	/// Applies the packets held for <see cref="HoldsUntilShown"/> whose tick the shown tick has reached, in the order
	/// they arrived. Call it as the shown tick moves, after placing the bodies at it, so what it sets off happens where
	/// the bodies are shown. An entity it spawns is raised through <see cref="Spawned"/> as it's bound, before its
	/// graphs run, to be placed there too.
	/// </summary>
	/// <remarks>
	/// A held packet was acknowledged as it arrived, so the server won't send it again. An exception from what an entry
	/// sets off, a cue handler or a graph's node, leaves the rest of that entity's entry unapplied and every other
	/// entry applied, and a despawn still takes its entity away. The first exception comes out once every packet
	/// reached is applied.
	/// </remarks>
	/// <param name="tick">The server tick shown now.</param>
	public void Show(double tick)
	{
		ExceptionDispatchInfo? failure = null;

		while (_held.Count > 0 && _held.Peek().Tick <= tick)
		{
			HeldPacket packet = _held.Dequeue();

			foreach (int start in packet.Entries)
			{
				_heldReader.Reset(packet.Data.AsMemory(start));
				var id = new NetworkEntityId(ReplicationCodec.ReadEntityId(_heldReader));

				if (_mirrors.GetValueOrDefault(id)?.Prediction is not null)
				{
					continue;
				}

				try
				{
					ReadEntry(_heldReader, id, packet.Tick, previousTick: -1, bodies: false, state: true);
				}
				catch (Exception exception)
				{
					failure ??= ExceptionDispatchInfo.Capture(exception);
				}
			}

			_shownTick = packet.Tick;
			FollowEffects();
		}

		failure?.Throw();
	}

	/// <summary>
	/// Advances every entity this client predicts through a tick, ahead of the server, and records how the tick left
	/// them. Every one begins the tick before any advances, so one reading another's input sees this tick's frame. Call
	/// it once per tick the client's clock simulates, after the input frames for the tick are submitted, then send them
	/// for the tick.
	/// </summary>
	/// <remarks>
	/// <para>Ticks the clock skipped since the last call run first, with the input held and no presses or releases, as
	/// the server runs a tick whose input never arrived.</para>
	/// <para>Before the clock sync's first estimate nothing is predicted: the client's clock is still at the server's
	/// tick, so the input it sends arrives after the server ran it. The entities follow the server meanwhile, run with
	/// what was last held up to each state packet's tick.</para>
	/// </remarks>
	/// <param name="tick">The tick to simulate.</param>
	/// <param name="fixedDeltaTime">The length of the fixed step, in seconds.</param>
	public void Predict(long tick, double fixedDeltaTime)
	{
		if (!_session.ClockSync.HasEstimate || _timeline?.NewestTick is not long newest)
		{
			return;
		}

		// One the clock already passed by more than the timeline holds starts over.
		long next = newest + 1 <= tick - _timeline.Capacity ? tick : newest + 1;

		for (long held = next; held < tick; held++)
		{
			_timeline.TickHeld(held, fixedDeltaTime);
		}

		if (next <= tick)
		{
			_timeline.Tick(tick, fixedDeltaTime);
		}

		ReleaseUnreachable();
	}

	/// <summary>
	/// Gives the key of a spawn a graph's node is making on this client, for a graph of an entity this client
	/// predicts: the key the server gives the spawn it makes in the same tick, which binds the entity this client made
	/// ahead of it (<see cref="PredictSpawn"/>).
	/// </summary>
	/// <remarks>
	/// Ask once per spawn, as the node makes it, whether it makes a networked entity or something only this machine
	/// shows: each spawn a node makes in a tick takes the next key, and a correction running the tick again numbers
	/// them as they were, so what it makes again is found by its key (<see cref="FindSpawn"/>).
	/// </remarks>
	/// <param name="graphContext">The context of the graph making the spawn.</param>
	/// <param name="node">The index of the node making it, its <see cref="Node.Index"/>.</param>
	/// <returns>The key, or <see langword="null"/> for a graph this client doesn't predict, such as a mirror's copy of
	/// the server's, one of an entity the server hasn't bound yet, or one that runs before the entity's first tick.
	/// </returns>
	public SpawnKey? KeySpawn(GraphContext graphContext, int node)
	{
		return !graphContext.IsRemote
			&& graphContext.TryGetActivationContext(out AbilityBehaviorContext? context)
			&& context.Owner.Abilities.IsPredicted
			&& context.Owner.NetworkId.IsValid
			? _spawnKeys.Next(context, node)
			: null;
	}

	/// <summary>
	/// Finds the entity this client spawned ahead of the server under a key. A correction that runs again the tick the
	/// spawn was made in, having rolled back to before it, brings it back here as it was made, instead of the host
	/// making another: it joins the timeline again as the tick is recorded. One the server's packets reached the tick
	/// of without spawning it is found, and stays in limbo.
	/// </summary>
	/// <param name="key">The key <see cref="KeySpawn"/> gave the spawn.</param>
	/// <returns>The entity, or <see langword="null"/> when this client made none under the key.</returns>
	public IForgeEntity? FindSpawn(SpawnKey key)
	{
		return _timeline?.Remake(key, keptOut: key.Tick <= LastServerTick)?.Entity;
	}

	/// <summary>
	/// Takes an entity this client just spawned ahead of the server into its timeline, predicted, from the tick a
	/// graph's node made it in: it advances with the entities this client predicts from the next tick on, and the
	/// server's spawn of it, arriving with its key, binds it. Call it while the node runs, after the entity is made and
	/// before its setup runs, so its setup runs as a predicted entity's.
	/// </summary>
	/// <remarks>
	/// <para>A spawn the server's packets reach the tick of without the server spawning it was never made there: it
	/// goes to limbo (<see cref="EnteredLimbo"/>), where the server's spawn of it, arriving late, finds it and brings
	/// it back, and it's handed to <see cref="IReplicationHost.Despawn"/> once no correction can reach that tick.
	/// </para>
	/// <para>Give the entity its own random, seeded with <see cref="SpawnKey.SeedFor"/>, as the server's must be, so
	/// its setup and its ticks roll the same on both machines.</para>
	/// </remarks>
	/// <param name="entity">The entity, made under the key.</param>
	/// <param name="key">The key <see cref="KeySpawn"/> gave the spawn.</param>
	/// <param name="bind">Gives the entity the id of the server's spawn as it arrives, for its
	/// <see cref="IForgeEntity.NetworkId"/> to return.</param>
	/// <returns><see langword="false"/> when the server already despawned the entity of this key, which a correction
	/// running the tick again doesn't make again: the host frees what it made.</returns>
	public bool PredictSpawn(IForgeEntity entity, SpawnKey key, Action<NetworkEntityId> bind)
	{
		if (_ended.Contains(key))
		{
			return false;
		}

		var prediction = new Prediction(
			entity,
			entity.Attributes.Count(),
			Timeline.Capacity,
			Timeline.FixedDeltaTime,
			key)
		{
			Bind = bind,
		};

		MarkPredicted(entity, predicted: true);

		if (_correcting.Count > 0)
		{
			BeginCorrecting(prediction);
		}

		Timeline.Join(prediction, key.Tick);
		return true;
	}

	/// <summary>
	/// Takes an entity this client predicts out of its timeline, as when its graph frees it: it goes to limbo as the
	/// tick is recorded (<see cref="EnteredLimbo"/>), and a correction rolling back to a tick it was in brings it back.
	/// The server's despawn of it hands it to <see cref="IReplicationHost.Despawn"/>.
	/// </summary>
	/// <param name="entity">The entity.</param>
	/// <returns><see langword="false"/> for an entity this client doesn't predict, which the host frees as it would.
	/// </returns>
	public bool PredictDespawn(IForgeEntity entity)
	{
		if (_timeline?.Members.FirstOrDefault(x => ReferenceEquals(x.Entity, entity)) is not Prediction prediction)
		{
			return false;
		}

		_timeline.Leave(prediction);
		return true;
	}

	/// <summary>
	/// Lets go for good of an entity this client predicts that the host took away itself rather than through
	/// <see cref="PredictDespawn"/>, as when a node it lives under is freed: it leaves the timeline, a correction
	/// running the tick it was made in doesn't make it again, and it isn't handed to
	/// <see cref="IReplicationHost.Despawn"/>. What the server still sends of it finds nothing here.
	/// </summary>
	/// <param name="entity">The entity.</param>
	/// <returns><see langword="false"/> for an entity this client doesn't predict.</returns>
	public bool ForgetPredicted(IForgeEntity entity)
	{
		// A bound one may not have joined the timeline yet, as when its setup frees it as it spawns.
		Prediction? prediction = _byEntity.TryGetValue(entity, out Mirror? mirror)
			? mirror.Prediction
			: _timeline?.Members.FirstOrDefault(x => ReferenceEquals(x.Entity, entity));

		if (prediction is null)
		{
			return false;
		}

		if (prediction.Key is SpawnKey key)
		{
			_ended.Add(key);
		}

		if (mirror is not null)
		{
			// A copy of its spawn still on its way makes nothing, until the server's despawn of it.
			_forgotten.Add(entity.NetworkId);
			Unbind(mirror, despawn: false);
		}
		else
		{
			_timeline!.Remove(prediction);
			MarkPredicted(entity, predicted: false);
		}

		return true;
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

		// What this client spawned that the server's spawn never bound goes too.
		foreach (Prediction prediction in _timeline?.Members.ToArray() ?? [])
		{
			_timeline!.Shelve(prediction);
			Release(prediction);
		}

		_spawnKeys.ForgetAfter(long.MinValue);

		_timeline = null;
		_unjoined.Clear();
		_rejoining.Clear();
		_ended.Clear();
		_forgotten.Clear();
		LastServerTick = -1;
		LastSequence = 0;
		_held.Clear();
		_pendingBodies.Clear();
		_shownTick = -1;
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
		List<int>? entries = HoldsUntilShown ? [] : null;
		_toReconcile.Clear();
		ReadEntries(reader, tick, previousTick, bodies: true, state: !HoldsUntilShown, entries);

		// Only a packet read to its end counts: one that isn't is never acknowledged, so the server sends it all again.
		LastServerTick = Math.Max(LastServerTick, tick);
		Acknowledge((uint)sequence);

		// The packet is acknowledged, so a correction that throws costs nothing else of it.
		ExceptionDispatchInfo? failure = null;

		if (_timeline is not null)
		{
			try
			{
				ShelveUnconfirmed(tick);
				Reconcile(tick);
			}
			catch (Exception exception)
			{
				failure = ExceptionDispatchInfo.Capture(exception);
			}
		}

		if (entries is not null)
		{
			_held.Enqueue(new HeldPacket(tick, reader.CopyData(), entries));
		}
		else
		{
			_shownTick = tick;
			FollowEffects();
		}

		failure?.Throw();
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

			if (mirror.Prediction is Prediction prediction)
			{
				Prediction.ServerAttribute known = prediction.Attributes[index]
					?? new Prediction.ServerAttribute(mirror.Attributes[index].BaseValue, int.MinValue, int.MaxValue);
				prediction.Attributes[index] = new Prediction.ServerAttribute(
					baseValue ?? known.BaseValue,
					min ?? (known.Min == int.MinValue ? mirror.Attributes[index].Min : known.Min),
					max ?? (known.Max == int.MaxValue ? mirror.Attributes[index].Max : known.Max));
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

	// The oldest tick the timeline can go back to: one it holds, and not before a spawn of the server's joined it,
	// which nothing on this client makes again.
	private static long? Floor(PredictionTimeline timeline)
	{
		long? floor = timeline.OldestTick;

		foreach (Prediction prediction in timeline.Members)
		{
			if (prediction.Key is null && prediction.JoinTick > floor)
			{
				floor = prediction.JoinTick;
			}
		}

		return floor;
	}

	private static void MarkPredicted(IForgeEntity entity, bool predicted)
	{
		entity.Abilities.IsPredicted = predicted;
		entity.Abilities.TracksPrediction = predicted;
		entity.EffectsManager.IsPredicted = predicted;
	}

	private static SpawnKey ReadSpawnKey(BitReader reader, long tick)
	{
		var spawner = new NetworkEntityId(ReplicationCodec.ReadEntityId(reader));
		ulong ago = reader.ReadVarUInt();

		if (ago > (ulong)tick)
		{
			throw new MalformedPacketException($"A spawn {ago} ticks before tick {tick} is not a spawn's tick.");
		}

		uint ability = reader.ReadUInt32();
		int node = ReplicationCodec.ReadCount(reader, int.MaxValue);
		return new SpawnKey(spawner, tick - (long)ago, ability, node, ReplicationCodec.ReadCount(reader, int.MaxValue));
	}

	private static void ReadBody(BitReader reader, Mirror? mirror, RemoteBody? body, long tick, long previousTick)
	{
		bool leftOut = reader.ReadBool();

		if (!reader.ReadBool())
		{
			body?.Clear();

			if (mirror?.Prediction is Prediction gone)
			{
				gone.HasBody = false;
			}

			return;
		}

		QuantizedBody quantized = BodyCodec.Read(reader);

		if (mirror?.Prediction is Prediction prediction)
		{
			prediction.Body = quantized;
			prediction.HasBody = true;
			return;
		}

		if (body is null)
		{
			return;
		}

		BodyState state = BodyCodec.Dequantize(quantized);

		// A body missing from the packet before this one hadn't changed by then, unless the budget left it out, so it
		// moved only after it: held there, it isn't shown drifting from where it last stood all the way from when it
		// stopped.
		if (!leftOut && body.Latest is BodyState held && body.LatestTick < previousTick)
		{
			body.Add(previousTick, held);
		}

		body.Add(tick, state);
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

	// A packet's entries, read for their bodies, the rest of their state, or both: a held packet is read for its bodies
	// as it arrives, noting where each entry starts, and for the rest once it's shown. Entries are byte aligned.
	private void ReadEntries(
		BitReader reader,
		long tick,
		long previousTick,
		bool bodies,
		bool state,
		List<int>? entries = null)
	{
		while (true)
		{
			int start = reader.BitPosition >> 3;
			uint id = ReplicationCodec.ReadEntityId(reader);

			if (id == 0)
			{
				break;
			}

			entries?.Add(start);
			ReadEntry(reader, new NetworkEntityId(id), tick, previousTick, bodies, state);
		}
	}

	private void ReadEntry(BitReader reader, NetworkEntityId id, long tick, long previousTick, bool bodies, bool state)
	{
		var kind = (EntryKind)reader.ReadBits(2);
		_mirrors.TryGetValue(id, out Mirror? mirror);

		// An entity this client predicts takes the server's word as it arrives, checked against the tick it's of rather
		// than shown behind it.
		state |= bodies && mirror?.Prediction is not null;

		switch (kind)
		{
			case EntryKind.Despawn:
				Mirror? going = state ? mirror : null;

				// What it did as it went shows before it goes, and it goes even if showing that fails.
				try
				{
					if (reader.ReadBool())
					{
						ReadEvents(reader, going, _session.TagsManager.NetIndexCount);
					}

					if (reader.ReadBool())
					{
						ReadGraphs(reader, going, tick);
					}
				}
				finally
				{
					if (state)
					{
						_pendingBodies.Remove(id);
						_forgotten.Remove(id);

						if (mirror is not null)
						{
							Unbind(mirror, despawn: true);
						}
					}
				}

				reader.Align();
				return;

			case EntryKind.Spawn:
				SpawnFields fields = ReadSpawnFields(reader, tick);

				if (state || (bodies && Owns(fields)))
				{
					mirror = Spawn(fields, id, mirror);
					state = true;
				}
				else if (!fields.Placed && mirror is null && !_pendingBodies.ContainsKey(id))
				{
					// Its bodies arrive before the spawn is shown, and wait for it.
					_pendingBodies[id] = new RemoteBody();
				}

				break;

			case EntryKind.State:
				break;

			default:
				throw new MalformedPacketException($"{kind} is not an entry kind.");
		}

		RemoteBody? body = !bodies || mirror?.Prediction is not null
			? null
			: mirror?.Body ?? _pendingBodies.GetValueOrDefault(id);
		ReadState(reader, state ? mirror : null, body, tick, previousTick);
		reader.Align();
	}

	// Whether a spawn is of an entity this client predicts: one the server spawned for this client's peer.
	private bool Owns(SpawnFields fields)
	{
		return !fields.Placed && fields.Owner == (ulong)_session.LocalPeer.Value;
	}

	private SpawnFields ReadSpawnFields(BitReader reader, long tick)
	{
		bool placed = reader.ReadBool();
		int sceneIndex = -1;

		if (!placed)
		{
			sceneIndex = (int)reader.ReadBits(_session.Registry.BitsFor(AssetKind.Scene));

			if (sceneIndex >= _session.Registry.Count(AssetKind.Scene))
			{
				throw new MalformedPacketException($"The registry has no scene {sceneIndex}.");
			}
		}

		ulong owner = reader.ReadVarUInt();
		int attributeCount = ReplicationCodec.ReadCount(reader);

		if (owner > int.MaxValue)
		{
			throw new MalformedPacketException($"{owner} is not a peer id.");
		}

		BodyState? body = reader.ReadBool() ? BodyCodec.ReadExact(reader) : null;
		SpawnKey? key = reader.ReadBool() ? ReadSpawnKey(reader, tick) : null;
		return new SpawnFields(placed, sceneIndex, owner, attributeCount, body, key);
	}

	private Mirror? Spawn(SpawnFields fields, NetworkEntityId id, Mirror? mirror)
	{
		bool placed = fields.Placed;
		int sceneIndex = fields.SceneIndex;
		ulong owner = fields.Owner;
		int attributeCount = fields.AttributeCount;

		if (mirror is not null)
		{
			// Placed and bound before the server said whose it is, or spawned already, from an earlier copy of the
			// spawn.
			mirror.Owner = new PeerId((int)owner);
			return mirror;
		}

		// A placed entity this machine doesn't have, or one its host took away: its state is skipped until it's
		// despawned.
		if (placed || _forgotten.Contains(id))
		{
			return null;
		}

		// One this client made ahead of the server is bound, rather than made again.
		Prediction? made = Owns(fields) && fields.Key is SpawnKey key
			? _timeline?.Members.FirstOrDefault(x => x.Key == key && !x.IsBound)
			: null;
		made?.Bind?.Invoke(id);

		IForgeEntity? entity = made?.Entity ?? (Owns(fields)
			? Host?.SpawnPredicted(id, sceneIndex, new PeerId((int)owner), fields.Body, fields.Key)
			: Host?.Spawn(id, sceneIndex, new PeerId((int)owner)));

		if (entity is null || entity.NetworkId != id || _byEntity.ContainsKey(entity))
		{
			SpawnsRefused++;
			_pendingBodies.Remove(id);
			return null;
		}

		_pendingBodies.Remove(id, out RemoteBody? pending);
		mirror = Bind(entity, new PeerId((int)owner), placed: false, pending, made, fields.Key);

		if (made is not null)
		{
			made.IsBound = true;

			// One the timeline no longer makes comes back from the tick it was made in.
			if (made.Unconfirmed)
			{
				_rejoining.Add(made);
			}
		}
		else
		{
			Spawned?.Invoke(entity);
		}

		if (mirror.Attributes.Length != attributeCount)
		{
			Validation.Fail(
				$"The server's {id} has {attributeCount} attributes and this machine's copy has " +
				$"{mirror.Attributes.Length}: the two don't play the same scene. Its attributes won't follow.");
		}

		return mirror;
	}

	private void ReadState(BitReader reader, Mirror? mirror, RemoteBody? body, long tick, long previousTick)
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

			if (mirror?.Prediction is Prediction predicted)
			{
				predicted.Tags.UnionWith(_added);
				predicted.Tags.ExceptWith(_removed);
			}
			else
			{
				mirror?.Entity.Tags.MirrorTags(_added, _removed);
			}
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

		if (reader.ReadBool())
		{
			ReadGraphs(reader, mirror, tick);
		}

		if (reader.ReadBool())
		{
			ReadBody(reader, mirror, body, tick, previousTick);
		}

		if (!reader.ReadBool())
		{
			return;
		}

		ulong checksum = reader.ReadBits(64);

		if (mirror?.Prediction is Prediction prediction)
		{
			prediction.Checksum = checksum;
			prediction.ChecksumTick = tick;
			_toReconcile.Add(prediction);

			// The server has one the timeline no longer makes: it comes back from the tick it was made in.
			if (prediction.Unconfirmed && !_rejoining.Contains(prediction))
			{
				_rejoining.Add(prediction);
			}
		}
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

				if (mirror?.Prediction is Prediction gone)
				{
					gone.Effects.Remove((uint)instance);
					continue;
				}

				mirror?.RemoveEffect(
					(uint)instance,
					expired ? EffectRemovalReason.Expired : EffectRemovalReason.Removed);
				continue;
			}

			EffectData? data = null;
			EffectApplicationContext? context = null;
			long? afterTick = null;

			if (reader.ReadBool())
			{
				data = ReadEffectData(reader);
				afterTick = reader.ReadBool() ? tick - reader.ReadVarInt() : null;
				context = ReadEffectContext(reader);
			}

			EffectState? state = reader.ReadBool() ? ReadEffectState(reader, tagCount, tick) : null;
			int? executionCount = reader.ReadBool() ? ReplicationCodec.ReadCount(reader, int.MaxValue) : null;

			if (mirror?.Prediction is Prediction prediction)
			{
				KnowEffect(prediction, (uint)instance, data, context, afterTick, state, executionCount);
			}
			else if (mirror is not null)
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

	private EffectOwnership Ownership(uint ownerId, uint sourceId)
	{
		return new EffectOwnership(_resolve(new NetworkEntityId(ownerId)), _resolve(new NetworkEntityId(sourceId)));
	}

	private double RemainingDuration(EffectState state, long tick)
	{
		return state.IsTimed ? Math.Max(state.EndTick - tick, 0) * _session.Clock.FixedDeltaTime : 0;
	}

	private void ReadGraphs(BitReader reader, Mirror? mirror, long tick)
	{
		int count = ReplicationCodec.ReadCount(reader);
		mirror?
			.ForgetEndedGraphs(tick - (long)Math.Ceiling(_session.Clock.TicksIn(ReplicationServer.EventExpirySeconds)));

		for (int i = 0; i < count; i++)
		{
			ulong instance = reader.ReadVarUInt();

			if (instance is 0 or > uint.MaxValue)
			{
				throw new MalformedPacketException($"{instance} is not a graph instance.");
			}

			GraphCreation? creation = reader.ReadBool() ? ReadGraphCreation(reader, tick) : null;
			_graphStates.Clear();
			_graphValues.Clear();
			_graphArrays.Clear();
			_graphEvents.Clear();
			bool states = reader.ReadBool();

			if (states)
			{
				ReadGraphStates(reader);
			}

			if (reader.ReadBool())
			{
				ReadGraphValues(reader);
			}

			if (reader.ReadBool())
			{
				ReadGraphEvents(reader);
			}

			bool removed = reader.ReadBool();

			if (mirror?.Prediction is Prediction prediction
				&& (creation?.AfterTick is not null || prediction.Runs.ContainsKey((uint)instance)))
			{
				KnowRun(prediction, (uint)instance, creation, removed);
			}
			else if (mirror is not null)
			{
				ApplyGraph(mirror, (uint)instance, creation, states, removed, tick);
			}
		}
	}

	// A run the server started from outside this client's own entity's ticks, which this client takes in: known from
	// its start until the server says it ended.
	private void KnowRun(Prediction prediction, uint instance, GraphCreation? creation, bool removed)
	{
		if (removed)
		{
			prediction.Runs.Remove(instance);
		}
		else if (creation is GraphCreation created && created.AfterTick is long after)
		{
			prediction.Runs[instance] = new Prediction.ServerRun(
				_session.Registry.Get<AbilityData>(AssetKind.Ability, created.Ability),
				created.Target,
				created.Source,
				created.Magnitude,
				after);
		}
	}

	private GraphCreation ReadGraphCreation(BitReader reader, long tick)
	{
		int ability = ReplicationCodec.ReadCount(reader, int.MaxValue);

		if (ability >= _session.Registry.Count(AssetKind.Ability))
		{
			throw new MalformedPacketException($"The registry has no ability {ability}.");
		}

		uint target = ReplicationCodec.ReadEntityId(reader);
		uint source = ReplicationCodec.ReadEntityId(reader);
		int level = ReplicationCodec.ReadInt(reader);
		float magnitude = reader.ReadFloat();
		long? afterTick = reader.ReadBool() ? tick - reader.ReadVarInt() : null;
		return new GraphCreation(ability, target, source, level, magnitude, afterTick);
	}

	private void ReadGraphStates(BitReader reader)
	{
		int count = ReplicationCodec.ReadCount(reader);

		for (int i = 0; i < count; i++)
		{
			int state = ReplicationCodec.ReadCount(reader);
			ulong activation = reader.ReadVarUInt();
			_graphStates.Add(new RunningState(state, activation, ReplicationCodec.ReadCount(reader, int.MaxValue)));
		}
	}

	private void ReadGraphValues(BitReader reader)
	{
		int values = ReplicationCodec.ReadCount(reader);

		for (int i = 0; i < values; i++)
		{
			int index = ReplicationCodec.ReadCount(reader);
			_graphValues.Add(new KeyValuePair<int, Variant128>(index, ReplicationCodec.ReadVariant(reader)));
		}

		int arrays = ReplicationCodec.ReadCount(reader);

		for (int i = 0; i < arrays; i++)
		{
			int index = ReplicationCodec.ReadCount(reader);
			var elements = new Variant128[ReplicationCodec.ReadCount(reader)];

			for (int j = 0; j < elements.Length; j++)
			{
				elements[j] = ReplicationCodec.ReadVariant(reader);
			}

			_graphArrays.Add(new KeyValuePair<int, Variant128[]>(index, elements));
		}
	}

	private void ReadGraphEvents(BitReader reader)
	{
		int count = ReplicationCodec.ReadCount(reader);

		for (int i = 0; i < count; i++)
		{
			ulong sequence = reader.ReadVarUInt();

			if (sequence > uint.MaxValue)
			{
				throw new MalformedPacketException($"{sequence} is not an event sequence.");
			}

			int action = ReplicationCodec.ReadCount(reader);
			_graphEvents.Add(new GraphAction((uint)sequence, action, reader.ReadVarUInt()));
		}
	}

	// Values first, so a state starting here reads what it read there, then states and actions as the server's run had
	// them.
	private void ApplyGraph(
		Mirror mirror,
		uint instance,
		GraphCreation? creation,
		bool states,
		bool removed,
		long tick)
	{
		if (!mirror.Graphs.TryGetValue(instance, out RemoteGraph? graph))
		{
			// A run this client never had, which it can't tell what it is, or one it ended, sent again while the server
			// waits for the acknowledgement.
			if (creation is not GraphCreation created || mirror.EndedGraphs.ContainsKey(instance))
			{
				return;
			}

			graph = StartGraph(mirror, created);
			mirror.Graphs[instance] = graph;
		}
		else if (graph is not null)
		{
			ApplyValues(graph.Behavior.Processor.GraphContext.GraphVariables, graph.Table);
		}

		if (graph is not null)
		{
			Follow(graph, states);
		}

		if (removed)
		{
			mirror.RemoveGraph(instance);
			mirror.EndedGraphs[instance] = tick;
		}
	}

	// A run of a graph ability another kind of behavior runs here has nothing to follow.
	private RemoteGraph? StartGraph(Mirror mirror, GraphCreation creation)
	{
		AbilityData data = _session.Registry.Get<AbilityData>(AssetKind.Ability, creation.Ability);

		if (data.BehaviorFactory?.Invoke() is not GraphAbilityBehavior behavior)
		{
			return null;
		}

		// Found as they're read, so one that arrives after the run started still is.
		var source = new NetworkEntityId(creation.Source);
		var target = new NetworkEntityId(creation.Target);
		var context = new AbilityBehaviorContext(
			mirror.Entity,
			() => _resolve(source),
			() => _resolve(target),
			creation.Level,
			creation.Magnitude);
		var graph = new RemoteGraph(behavior, context);

		behavior.StartRemote(context, x => ApplyValues(x, graph.Table));
		mirror.Entity.Abilities.AddRemoteBehavior(behavior);
		return graph;
	}

	private void ApplyValues(Variables variables, GraphNetworkTable table)
	{
		foreach (KeyValuePair<int, Variant128> value in _graphValues)
		{
			if (value.Key >= table.ValueVariables.Length)
			{
				throw new MalformedPacketException($"The graph has no value variable {value.Key}.");
			}

			variables.DefineVariable(table.ValueVariables[value.Key], value.Value);
		}

		foreach (KeyValuePair<int, Variant128[]> array in _graphArrays)
		{
			if (array.Key >= table.ArrayVariables.Length)
			{
				throw new MalformedPacketException($"The graph has no array variable {array.Key}.");
			}

			variables.DefineArrayVariable(table.ArrayVariables[array.Key], array.Value);
		}
	}

	// What the server ended or started over ends here, newest first. Then what it started and the actions it executed
	// run in the order they did there, each action after the activations it followed, so one that makes what a state
	// acts on runs before it, and each state is caught up by how long it already ran.
	private void Follow(RemoteGraph graph, bool states)
	{
		GraphContext context = graph.Behavior.Processor.GraphContext;
		Node[] actions = graph.Table.Actions;

		for (int i = 0; i < _graphEvents.Count; i++)
		{
			if (_graphEvents[i].Action >= actions.Length)
			{
				throw new MalformedPacketException($"The graph has no action {_graphEvents[i].Action} clients run.");
			}
		}

		if (states)
		{
			EndStates(graph, context);
		}

		int started = 0;

		foreach (GraphAction action in _graphEvents)
		{
			if (action.Sequence <= graph.LastEvent)
			{
				continue;
			}

			if (states)
			{
				started = StartStates(graph, context, started, action.AfterActivation);
			}

			graph.LastEvent = action.Sequence;
			actions[action.Action].InputPorts[ActionNode.InputPort].ReceiveMessage(context);
		}

		if (states)
		{
			StartStates(graph, context, started, ulong.MaxValue);
		}
	}

	private void EndStates(RemoteGraph graph, GraphContext context)
	{
		Node[] states = graph.Table.States;
		_endingStates.Clear();

		for (int i = 0; i < _graphStates.Count; i++)
		{
			if (_graphStates[i].State >= states.Length)
			{
				throw new MalformedPacketException($"The graph has no state {_graphStates[i].State} clients run.");
			}
		}

		foreach (KeyValuePair<int, ulong> applied in graph.Applied)
		{
			if (!Runs(applied.Key, applied.Value))
			{
				_endingStates.Add(applied);
			}
		}

		_endingStates.Sort(static (x, y) => y.Value.CompareTo(x.Value));

		for (int i = 0; i < _endingStates.Count; i++)
		{
			int ending = _endingStates[i].Key;
			graph.Applied.Remove(ending);
			states[ending].DeactivateRemotely(context);
		}
	}

	// Starts what the server started up to the activation numbered, from where the last call stopped, and returns where
	// this one stopped.
	private int StartStates(RemoteGraph graph, GraphContext context, int from, ulong upTo)
	{
		int next = from;

		for (; next < _graphStates.Count && _graphStates[next].Activation <= upTo; next++)
		{
			RunningState running = _graphStates[next];

			if (graph.Applied.ContainsKey(running.State))
			{
				continue;
			}

			graph.Applied[running.State] = running.Activation;
			Node node = graph.Table.States[running.State];
			node.ActivateRemotely(context);

			if (running.Ticks > 0)
			{
				node.CatchUp(running.Ticks * _session.Clock.FixedDeltaTime, context);
			}
		}

		return next;
	}

	private bool Runs(int state, ulong activation)
	{
		foreach (RunningState running in _graphStates)
		{
			if (running.State == state && running.Activation == activation)
			{
				return true;
			}
		}

		return false;
	}

	// Keeps what the server says of an effect on an entity this client predicts.
	private void KnowEffect(
		Prediction prediction,
		uint instance,
		EffectData? data,
		EffectApplicationContext? context,
		long? afterTick,
		EffectState? state,
		int? executionCount)
	{
		if (!prediction.Effects.TryGetValue(instance, out ServerEffect? effect))
		{
			// One known only by its state was sent again after this client forgot it, and stays forgotten.
			if (data is not EffectData effectData || state is null)
			{
				return;
			}

			effect = new ServerEffect(effectData, context, afterTick is not null, afterTick ?? -1);
			prediction.Effects[instance] = effect;
		}

		if (state is EffectState known)
		{
			effect.Level = known.Level;
			effect.StackCount = known.StackCount;
			effect.IsInhibited = known.IsInhibited;
			effect.IsTimed = known.IsTimed;
			effect.EndTick = known.EndTick;
			effect.OwnerId = known.OwnerId;
			effect.SourceId = known.SourceId;
			effect.SetByCallers = [.. _setByCallers];
		}

		if (executionCount is int count)
		{
			effect.ExecutionCount = count;
		}
	}

	// Makes the timeline what the server said of the entities this client predicts, after a packet of a tick. One just
	// bound joins the timeline, and one the server has that the timeline no longer makes comes back at the tick it was
	// made in. Each entity the packet checks is checked against what this client simulated for that tick, and the whole
	// timeline is put right when one differs or has to change before then. A tick older than the timeline holds can't
	// be checked, and one newer than it simulated, with the client's clock behind the server's, is run up to first.
	private void Reconcile(long tick)
	{
		PredictionTimeline timeline = Timeline;

		for (long behind = (timeline.NewestTick ?? tick) + 1; behind <= tick; behind++)
		{
			timeline.TickHeld(behind, timeline.FixedDeltaTime);
		}

		long from = tick;
		bool correct = false;

		// Just bound: how a spawn of the server's is now is how it starts, at the tick the server first sent it,
		// rolling as it did there. One a graph spawned starts at the tick it was made in, as the server made it then,
		// once the others joined, so the timeline can go back to it.
		foreach (Prediction prediction in _unjoined.OrderBy(x => x.Key is not null).ToList())
		{
			long join = Math.Max(prediction.Key?.Tick ?? tick, Floor(timeline) ?? tick);

			if (prediction.Key is null)
			{
				prediction.Entity.Random?.BeginTick(join);
			}

			timeline.Join(prediction, join);

			if (join < timeline.NewestTick)
			{
				from = Math.Min(from, join);
				correct = true;
			}
		}

		_unjoined.Clear();
		long floor = Floor(timeline)!.Value;

		foreach (Prediction prediction in _rejoining.Where(x => timeline.Members.Contains(x)))
		{
			prediction.Unconfirmed = false;

			// One whose ticks the timeline can't go back to anymore, as after a spawn of the server's joined it since,
			// comes back at the oldest tick it can.
			if (!prediction.WasIn(Math.Max(prediction.JoinTick, floor)))
			{
				timeline.Rejoin(prediction, floor);
			}

			from = Math.Min(from, prediction.JoinTick);
			correct = true;
		}

		_rejoining.Clear();

		if (tick < floor)
		{
			return;
		}

		var checks = _toReconcile.Where(x => timeline.Members.Contains(x) && x.WasIn(tick)).ToList();

		foreach (Prediction prediction in checks)
		{
			foreach (KeyValuePair<uint, ServerEffect> effect in prediction.Effects)
			{
				if (effect.Value.FromOutside && !prediction.Taken.ContainsKey(effect.Key))
				{
					from = Math.Min(from, Math.Max(effect.Value.AfterTick, prediction.JoinTick));
					correct = true;
				}
			}

			foreach (KeyValuePair<uint, Prediction.ServerRun> run in prediction.Runs)
			{
				if (!prediction.TakenRuns.ContainsKey(run.Key))
				{
					from = Math.Min(from, Math.Max(run.Value.AfterTick, prediction.JoinTick));
					correct = true;
				}
			}

			foreach (KeyValuePair<uint, ActiveEffect> taken in prediction.Taken.ToArray())
			{
				if (prediction.Effects.ContainsKey(taken.Key))
				{
					continue;
				}

				// One that ended here as it did there is only forgotten.
				if (!taken.Value.Handle.IsValid)
				{
					prediction.Taken.Remove(taken.Key);
					continue;
				}

				correct = true;
			}

			foreach (KeyValuePair<uint, AbilityInstance> taken in prediction.TakenRuns.ToArray())
			{
				if (prediction.Runs.ContainsKey(taken.Key))
				{
					continue;
				}

				if (!taken.Value.IsActive)
				{
					prediction.TakenRuns.Remove(taken.Key);
					continue;
				}

				correct = true;
			}
		}

		if (!correct && checks.TrueForAll(x => Agrees(x, x.History.SnapshotAt(tick))))
		{
			foreach (Prediction prediction in checks)
			{
				prediction.ConfirmedTick = tick;
				Confirmations++;
			}
		}
		else
		{
			Correct(Math.Max(from, floor), tick, timeline.NewestTick!.Value, checks);
		}

		ReleaseUnreachable();
	}

	// Whether the entity as a tick left it here is what the server said of that tick: the checksum of what the server
	// sends, and the body within a step.
	private bool Agrees(Prediction prediction, EntitySnapshot snapshot)
	{
		if (prediction.HasBody
			&& prediction.Body is QuantizedBody body
			&& snapshot.Body is BodyState ours
			&& !BodyCodec.WithinAStep(BodyCodec.Quantize(ours), body))
		{
			return false;
		}

		return EntitySimulation.PredictionChecksum(snapshot, new PredictionScope(HeardByOwner, _session.Clock.TickRate))
			== prediction.Checksum;
	}

	// Whether the server tells this client of an effect on the client's own entity, as ReplicationServer decides it.
	private bool HeardByOwner(ActiveEffect activeEffect)
	{
		EffectData data = activeEffect.EffectData;
		EffectVisibility visibility = data.Visibility == EffectVisibility.Default
			? DefaultEffectVisibility
			: data.Visibility;

		return visibility != EffectVisibility.ServerOnly
			&& _session.Registry.IndexOfAsset(AssetKind.Effect, data) >= 0;
	}

	// Rolls the timeline back to the earliest tick it has to change after, takes in at the tick each came after the
	// effects and runs that came from outside and that an entity lacks, runs the ticks to the packet's and makes each
	// entity the packet checked what the server says there, then runs the ticks since again. What the entities show
	// changes only by the net change.
	private void Correct(long from, long tick, long newest, List<Prediction> checks)
	{
		PredictionTimeline timeline = Timeline;
		ExceptionDispatchInfo? failure = null;

		// A frame submitted for a tick not simulated yet, as in a step the clock waited through, isn't in the history.
		List<WaitingFrame> waiting = WaitingFrame.Of(timeline.Members.Select(x => x.Entity));

		foreach (Prediction prediction in timeline.Members)
		{
			BeginCorrecting(prediction);
		}

		try
		{
			timeline.Rollback(from);
			_spawnKeys.ForgetAfter(from);
			long at = from;

			// What was taken in after the tick rolled back to went with the rollback, and goes back in at its tick, for
			// every entity the rollback took back, checked by the packet or not: the effects that came after a tick
			// first, then the runs, which they may have started.
			var members = timeline.Members.ToList();
			var takeIn = members
				.SelectMany((prediction, index) => prediction.Effects
					.Where(x => x.Value.FromOutside
						&& (!prediction.Taken.TryGetValue(x.Key, out ActiveEffect? taken) || !taken.Handle.IsValid))
					.Select(x => new Arrival(
						Math.Max(Math.Max(x.Value.AfterTick, prediction.JoinTick), from),
						IsRun: false,
						index,
						x.Key))
					.Concat(prediction.Runs
						.Where(x => !prediction.TakenRuns.TryGetValue(x.Key, out AbilityInstance? run) || !run.IsActive)
						.Select(x => new Arrival(
							Math.Max(Math.Max(x.Value.AfterTick, prediction.JoinTick), from),
							IsRun: true,
							index,
							x.Key))))
				.OrderBy(x => x.After)
				.ThenBy(x => x.IsRun)
				.ThenBy(x => x.Member)
				.ThenBy(x => x.Key)
				.ToList();

			foreach (Arrival arrival in takeIn)
			{
				if (arrival.After > at)
				{
					timeline.Replay(arrival.After);
					at = arrival.After;
				}

				Prediction prediction = members[arrival.Member];

				if (prediction.State != PredictionState.InWorld)
				{
					continue;
				}

				if (arrival.IsRun)
				{
					prediction.TakenRuns.Remove(arrival.Key);

					if (TakeIn(prediction.Entity, prediction.Runs[arrival.Key]) is AbilityInstance run)
					{
						prediction.TakenRuns[arrival.Key] = run;
					}

					continue;
				}

				prediction.Taken.Remove(arrival.Key);

				if (TakeIn(prediction.Entity, prediction.Effects[arrival.Key], arrival.After, fromOutside: true)
					is ActiveEffect taken)
				{
					prediction.Taken[arrival.Key] = taken;
				}
			}

			if (tick > at)
			{
				timeline.Replay(tick);
			}

			foreach (Prediction prediction in checks)
			{
				if (prediction.State == PredictionState.InWorld
					&& _byEntity.TryGetValue(prediction.Entity, out Mirror? mirror))
				{
					Settle(mirror, tick);
				}
			}

			if (newest > tick)
			{
				timeline.Replay(newest);
			}
			else
			{
				timeline.RecordAgain();
			}

			timeline.ForgetUnmade(from);
		}
		catch (Exception exception)
		{
			failure = ExceptionDispatchInfo.Capture(exception);
		}
		finally
		{
			foreach (WaitingFrame frame in waiting)
			{
				frame.PutBack();
			}

			foreach (IForgeEntity entity in _correcting.Select(x => x.Entity))
			{
				entity.Abilities.IsReplaying = false;
				ExceptionDispatchInfo? cueFailure = entity.CuesManager.EndCorrection();
				failure ??= cueFailure;
			}

			_correcting.Clear();
		}

		// One that failed part way put nothing right to count or announce.
		failure?.Throw();
		Corrections++;
		LastCorrectionTicks = (int)(newest - from);

		foreach (Prediction prediction in checks)
		{
			prediction.ConfirmedTick = tick;
		}

		Replayed?.Invoke(from);

		foreach (IForgeEntity entity in timeline.Live.ToArray())
		{
			Corrected?.Invoke(entity);
		}
	}

	// An entity a correction runs ticks of again: what it shows changes only by the net change, and it shows nothing as
	// it executes.
	private void BeginCorrecting(Prediction prediction)
	{
		prediction.Entity.CuesManager.BeginCorrection();
		prediction.Entity.Abilities.IsReplaying = true;
		_correcting.Add(prediction);
	}

	// A spawn this client made that the server's packets reached the tick of without the server spawning it was never
	// made there: it goes to limbo, where the server's spawn of it, arriving late, finds it.
	private void ShelveUnconfirmed(long tick)
	{
		foreach (Prediction prediction in _timeline!.Members
			.Where(x => !x.IsBound && !x.Unconfirmed && x.Key?.Tick <= tick)
			.ToList())
		{
			_timeline.Shelve(prediction);
		}
	}

	// Lets go for good of what no correction can reach anymore: a spawn this client made that the server didn't, once
	// the timeline no longer holds the tick it was made in, and the keys of the spawns the server despawned.
	private void ReleaseUnreachable()
	{
		if (_timeline?.OldestTick is not long oldest)
		{
			return;
		}

		foreach (Prediction prediction in _timeline.Members
			.Where(x => x.Unconfirmed && !x.IsBound && x.JoinTick < oldest)
			.ToList())
		{
			Release(prediction);
		}

		_ended.RemoveWhere(x => x.Tick < oldest);
		_spawnKeys.ForgetBefore(oldest);
	}

	// Hands an entity the timeline lets go of to the host to take away.
	private void Release(Prediction prediction)
	{
		_timeline!.Remove(prediction);
		Despawned?.Invoke(prediction.Entity);
		Host?.Despawn(prediction.Entity);
	}

	// An effect the server says is on the entity, taken in here after a tick, as the server had it then.
	private ActiveEffect? TakeIn(IForgeEntity entity, ServerEffect server, long after, bool fromOutside)
	{
		var effect = new Effect(server.Data, Ownership(server.OwnerId, server.SourceId), server.Level);
		effect.RestoreSnapshot(server.Level, server.SetByCallers);
		ActiveEffect? taken = entity.EffectsManager.ApplyServerEffect(effect, server.Context, fromOutside);

		taken?.TakeServerState(
			server.Level,
			server.SetByCallers,
			server.StackCount,
			server.IsInhibited,
			server.IsTimed ? Math.Max(server.EndTick - after, 0) * _session.Clock.FixedDeltaTime : null,
			taken.ExecutionCount);

		return taken;
	}

	// A run the server started from outside the entity's ticks, started here as the server did.
	private AbilityInstance? TakeIn(IForgeEntity entity, Prediction.ServerRun server)
	{
		return entity.Abilities.TryGetAbility(
				server.Data,
				out AbilityHandle? handle,
				_resolve(new NetworkEntityId(server.SourceId)))
			? handle.Ability?.ActivateForServer(_resolve(new NetworkEntityId(server.TargetId)), server.Magnitude)
			: null;
	}

	// Makes the entity, as the packet's tick left it here, what the server says: its body, unless within a step, its
	// base values and ranges, and the effects that came from outside, the ones gone going, as the runs it took in that
	// the server ended do.
	private void Settle(Mirror mirror, long tick)
	{
		Prediction prediction = mirror.Prediction!;
		IForgeEntity entity = mirror.Entity;

		if (prediction.HasBody && prediction.Body is QuantizedBody body && entity.Mover is IEntityMover mover
			&& !BodyCodec.WithinAStep(BodyCodec.Quantize(mover.CaptureState()), body))
		{
			mover.RestoreState(BodyCodec.Dequantize(body));
		}

		for (int i = 0; i < mirror.Attributes.Length; i++)
		{
			EntityAttribute attribute = mirror.Attributes[i];

			if (prediction.Attributes[i] is Prediction.ServerAttribute server
				&& (attribute.BaseValue != server.BaseValue || attribute.Min != server.Min
					|| attribute.Max != server.Max))
			{
				attribute.TakeServerValues(server.BaseValue, server.Min, server.Max);
			}
		}

		SettleOwnEffects(mirror, tick);

		foreach (KeyValuePair<uint, ActiveEffect> taken in prediction.Taken.ToArray())
		{
			if (!prediction.Effects.TryGetValue(taken.Key, out ServerEffect? server))
			{
				prediction.Taken.Remove(taken.Key);

				if (taken.Value.Handle.IsValid)
				{
					entity.EffectsManager.RemoveEffect(taken.Value.Handle, forceRemoval: true);
				}

				continue;
			}

			if (!taken.Value.Handle.IsValid)
			{
				continue;
			}

			taken.Value.Effect.Ownership = Ownership(server.OwnerId, server.SourceId);
			taken.Value.TakeServerState(
				server.Level,
				server.SetByCallers,
				server.StackCount,
				server.IsInhibited,
				server.IsTimed ? Math.Max(server.EndTick - tick, 0) * _session.Clock.FixedDeltaTime : null,
				server.ExecutionCount);
		}

		foreach (KeyValuePair<uint, AbilityInstance> taken in prediction.TakenRuns.ToArray())
		{
			if (!prediction.Runs.ContainsKey(taken.Key))
			{
				prediction.TakenRuns.Remove(taken.Key);
				taken.Value.Cancel();
			}
		}

		SettleTags(mirror);
	}

	// With its effects and runs as the server has them, a tag the server has that nothing gives the entity here is a
	// base tag, as one the server's own code added, and a base tag the server lacks goes.
	private void SettleTags(Mirror mirror)
	{
		EntityTags tags = mirror.Entity.Tags;
		TagsManager tagsManager = _session.TagsManager;
		_added.Clear();
		_removed.Clear();

		foreach (Tag tag in mirror.Prediction!.Tags)
		{
			if (!tags.AllTags.HasTagExact(tag))
			{
				_added.Add(tag);
			}
		}

		foreach (Tag tag in tags.BaseTags.InternalTags)
		{
			if (!mirror.Prediction.Tags.Contains(tag)
				&& tagsManager.GetNetIndexFromTag(tag) != tagsManager.InvalidTagNetIndex)
			{
				_removed.Add(tag);
			}
		}

		tags.MirrorTags(_added, _removed);
	}

	// The effects the entity's own ticks applied that this client hears of, matched in the order they were applied with
	// the server's, and put as the server has them: one the server has and this client lacks is taken in, one this
	// client predicted that the server never applied goes, and the rest take the server's state.
	private void SettleOwnEffects(Mirror mirror, long tick)
	{
		Prediction prediction = mirror.Prediction!;
		IForgeEntity entity = mirror.Entity;
		var own = entity.EffectsManager.ActiveEffects.Where(x => !x.IsFromOutside && HeardByOwner(x)).ToList();
		var matched = new HashSet<ActiveEffect>();

		foreach (ServerEffect server in prediction.Effects.OrderBy(x => x.Key).Select(x => x.Value))
		{
			if (server.FromOutside)
			{
				continue;
			}

			ActiveEffect? match = own.Find(x => !matched.Contains(x) && x.EffectData.Equals(server.Data));

			if (match is null)
			{
				if (TakeIn(entity, server, tick, fromOutside: false) is ActiveEffect made)
				{
					matched.Add(made);
				}

				continue;
			}

			matched.Add(match);
			match.Effect.Ownership = Ownership(server.OwnerId, server.SourceId);
			match.TakeServerState(
				server.Level,
				server.SetByCallers,
				server.StackCount,
				server.IsInhibited,
				server.IsTimed ? Math.Max(server.EndTick - tick, 0) * _session.Clock.FixedDeltaTime : null,
				server.ExecutionCount);
		}

		foreach (ActiveEffect effect in own)
		{
			if (!matched.Contains(effect) && effect.Handle.IsValid)
			{
				entity.EffectsManager.RemoveEffect(effect.Handle, forceRemoval: true);
			}
		}
	}

	// After every packet: what's left of each effect follows the newest state, so it hears nothing to count down, and
	// an effect's owner and source are whatever this client has of them now.
	private void FollowEffects()
	{
		foreach (Mirror mirror in _mirrors.Values)
		{
			foreach (MirroredEffect effect in mirror.Effects.Values)
			{
				effect.Active.RemainingDuration = RemainingDuration(effect.State, _shownTick);

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

	// Binds an entity to its id. One this client predicts joins the timeline as the packet is reconciled, unless it's
	// one this client made, which is on it already.
	private Mirror Bind(
		IForgeEntity entity,
		PeerId owner,
		bool placed,
		RemoteBody? body = null,
		Prediction? made = null,
		SpawnKey? key = null)
	{
		bool predicted = !placed && owner == _session.LocalPeer;

		if (!predicted)
		{
			EntitySimulation.Teardown(entity);
			entity.Tags.MirrorClear();
		}

		var mirror = new Mirror(entity, owner, placed, body ?? new RemoteBody());

		if (predicted)
		{
			mirror.Prediction = made;

			if (made is null)
			{
				mirror.Prediction = new Prediction(
					entity,
					mirror.Attributes.Length,
					Timeline.Capacity,
					Timeline.FixedDeltaTime,
					key)
				{
					IsBound = true,
				};

				MarkPredicted(entity, predicted: true);
				_unjoined.Add(mirror.Prediction);
			}
		}

		_mirrors[entity.NetworkId] = mirror;
		_byEntity[entity] = mirror;
		_entities.Add(entity);
		_effectOwnersStale = true;
		return mirror;
	}

	// Lets go of a mirror. What it shows goes first, each part even if one before it throws, and it's let go of all the
	// same, so nothing keeps an entity its host took away: the first exception comes out after.
	private void Unbind(Mirror mirror, bool despawn)
	{
		ExceptionDispatchInfo? failure = null;

		foreach (Action letGo in (Action[])[mirror.RemoveAllGraphs, mirror.RemoveAllCues, mirror.RemoveAllEffects])
		{
			try
			{
				letGo();
			}
			catch (Exception exception)
			{
				failure ??= ExceptionDispatchInfo.Capture(exception);
			}
		}

		if (mirror.Prediction is Prediction prediction)
		{
			_timeline?.Remove(prediction);
			_unjoined.Remove(prediction);
			MarkPredicted(mirror.Entity, predicted: false);

			// A correction running the tick it was made in again doesn't make it again.
			if (despawn && prediction.Key is SpawnKey key)
			{
				_ended.Add(key);
			}
		}

		_mirrors.Remove(mirror.Entity.NetworkId);
		_byEntity.Remove(mirror.Entity);
		_entities.Remove(mirror.Entity);
		_effectOwnersStale = true;

		if (despawn)
		{
			Despawned?.Invoke(mirror.Entity);
			Host?.Despawn(mirror.Entity);
		}

		failure?.Throw();
	}

	private sealed class Mirror(IForgeEntity entity, PeerId owner, bool placed, RemoteBody body)
	{
		private readonly Dictionary<uint, AppliedCue> _cues = [];

		public IForgeEntity Entity { get; } = entity;

		public PeerId Owner { get; set; } = owner;

		public bool Placed { get; } = placed;

		public EntityAttribute[] Attributes { get; } = [.. entity.Attributes];

		public RemoteBody Body { get; } = body;

		// Set for an entity this client predicts, which the server's word checks rather than sets.
		public Prediction? Prediction { get; set; }

		public uint LastEvent { get; set; }

		public Dictionary<uint, MirroredEffect> Effects { get; } = [];

		// The server's graph runs, with nothing for a run this machine can't follow.
		public Dictionary<uint, RemoteGraph?> Graphs { get; } = [];

		// The runs the server ended, by the tick of the packet that said so, kept while a copy of that packet's record,
		// sent again until it's acknowledged, could still bring their actions.
		public Dictionary<uint, long> EndedGraphs { get; } = [];

		public void ForgetEndedGraphs(long before)
		{
			List<uint>? forgotten = null;

			foreach (KeyValuePair<uint, long> ended in EndedGraphs)
			{
				if (ended.Value >= before)
				{
					continue;
				}

				forgotten ??= [];
				forgotten.Add(ended.Key);
			}

			forgotten?.ForEach(x => EndedGraphs.Remove(x));
		}

		public void RemoveGraph(uint instance)
		{
			if (Graphs.Remove(instance, out RemoteGraph? graph))
			{
				graph?.End(Entity);
			}
		}

		public void RemoveAllGraphs()
		{
			LetGoOfAll(Graphs, x => x?.End(Entity));
		}

		public void RemoveEffect(uint instance, EffectRemovalReason reason)
		{
			if (Effects.Remove(instance, out MirroredEffect? effect))
			{
				Entity.EffectsManager.RemoveMirrorEffect(effect.Active, reason);
			}
		}

		public void RemoveAllEffects()
		{
			LetGoOfAll(Effects, x => Entity.EffectsManager.RemoveMirrorEffect(x.Active, EffectRemovalReason.Destroyed));
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
			LetGoOfAll(_cues, x => Entity.CuesManager.RemoveCue(x.CueTag, Entity, interrupted: true, x));
		}

		// Lets go of each, even if one before it throws, and forgets them all: the first exception comes out after.
		private static void LetGoOfAll<TKey, TValue>(Dictionary<TKey, TValue> all, Action<TValue> letGo)
			where TKey : notnull
		{
			ExceptionDispatchInfo? failure = null;

			foreach (TValue value in all.Values)
			{
				try
				{
					letGo(value);
				}
				catch (Exception exception)
				{
					failure ??= ExceptionDispatchInfo.Capture(exception);
				}
			}

			all.Clear();
			failure?.Throw();
		}

		// The application a replicated persistent cue is applied for on this machine: one object per instance.
		private sealed class AppliedCue(Tag cueTag, CueParameters? parameters)
		{
			public Tag CueTag { get; } = cueTag;

			public CueParameters? Parameters { get; set; } = parameters;
		}
	}

	// A mirror's copy of a graph run of the server's: the behavior running it here, and which activation of the
	// server's each state clients run is running as.
	private sealed class RemoteGraph(GraphAbilityBehavior behavior, AbilityBehaviorContext context)
	{
		public GraphAbilityBehavior Behavior { get; } = behavior;

		public GraphNetworkTable Table { get; } = behavior.Processor.Graph.NetworkTable;

		public Dictionary<int, ulong> Applied { get; } = [];

		public uint LastEvent { get; set; }

		// What it still shows ends in the mirror's next presentation pass.
		public void End(IForgeEntity entity)
		{
			Behavior.OnEnded(context);
			entity.Abilities.RemoveRemoteBehavior(Behavior);
		}
	}

	// An effect the server says is active on a mirror, with what the server last said of it.
	private sealed class MirroredEffect(ActiveEffect active, EffectState state)
	{
		public ActiveEffect Active { get; } = active;

		public EffectState State { get; set; } = state;
	}

	// What a spawn is, with the body the server's entity had as it was registered and the key a graph spawned it under,
	// for the client that predicts it.
	private readonly record struct SpawnFields(
		bool Placed,
		int SceneIndex,
		ulong Owner,
		int AttributeCount,
		BodyState? Body,
		SpawnKey? Key);

	// A packet waiting to be shown, with where each of its entries starts.
	private readonly record struct HeldPacket(long Tick, byte[] Data, List<int> Entries);

	// What a correction takes in after a tick: an effect, or a run, by the server's instance, of one of the timeline's
	// members.
	private readonly record struct Arrival(long After, bool IsRun, int Member, uint Key);

	// What a graph run is, as the server sent it once: its ability and that activation's context.
	// A run, with the tick it started after for one this client takes in.
	private readonly record struct GraphCreation(
		int Ability,
		uint Target,
		uint Source,
		int Level,
		float Magnitude,
		long? AfterTick);

	// A state clients run, running on the server as an activation it numbered, for some ticks before the packet's.
	private readonly record struct RunningState(int State, ulong Activation, int Ticks);

	// An action clients run, executed on the server after the activation numbered.
	private readonly record struct GraphAction(uint Sequence, int Action, ulong AfterActivation);

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
