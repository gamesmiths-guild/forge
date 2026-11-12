// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Networking.Replication;

/// <summary>
/// An entity the server replicates: what it is to the network, and what the server last saw of it, stamped with the
/// send it was seen at, so each client is sent what changed since the send it last acknowledged.
/// </summary>
public sealed class ReplicatedEntity
{
	// A change the server has seen but not yet stamped with a send.
	internal const long Pending = long.MaxValue;

	private readonly TagsManager _tagsManager;
	private readonly HashSet<ushort> _currentTags = [];
	private readonly List<ushort> _scratch = [];
	private readonly Dictionary<ActiveEffect, ReplicatedEffect> _effectsByActive = [];
	private readonly Dictionary<GraphProcessor, ReplicatedGraph> _graphsByProcessor = [];
	private readonly List<Ability.BehaviorBinding> _behaviors = [];

	/// <summary>
	/// Gets the id the entity is known by on every machine.
	/// </summary>
	public NetworkEntityId Id { get; }

	/// <summary>
	/// Gets the entity.
	/// </summary>
	public IForgeEntity Entity { get; }

	/// <summary>
	/// Gets the index of the scene the entity spawns from, in the registry's <see cref="AssetKind.Scene"/> kind, or -1
	/// for an entity placed in the scene, which every machine already has.
	/// </summary>
	public int SceneIndex { get; }

	/// <summary>
	/// Gets a value indicating whether the entity was placed in the scene rather than spawned: it's never spawned or
	/// despawned on a client for relevancy, and it's always relevant.
	/// </summary>
	public bool IsPlaced => SceneIndex < 0;

	/// <summary>
	/// Gets the peer whose player the entity is, or <see cref="PeerId.None"/> for the world's.
	/// </summary>
	public PeerId Owner { get; }

	/// <summary>
	/// Gets or sets how much the entity's changes matter next to other entities' when a packet can't hold them all:
	/// what's left out accumulates priority until it's sent. 1 by default.
	/// </summary>
	public float Priority { get; set; } = 1;

	/// <summary>
	/// Gets the body state the server last captured, or <see langword="null"/> when the entity has no body.
	/// </summary>
	public BodyState? Body { get; private set; }

	/// <summary>
	/// Gets the tick the entity was registered at.
	/// </summary>
	public long SpawnedAtTick { get; }

	/// <summary>
	/// Gets the tick the entity was unregistered at, once it was.
	/// </summary>
	public long? DespawnedAtTick { get; internal set; }

	/// <summary>
	/// Gets what the entity is known by to the client that predicts the graph that spawned it, which made it ahead of
	/// the server under the same key, or <see langword="null"/> for one no graph of a predicted entity spawned.
	/// </summary>
	public SpawnKey? SpawnKey { get; }

	internal EntityAttribute[] Attributes { get; }

	internal int[] ShadowValues { get; }

	internal int[] ShadowMins { get; }

	internal int[] ShadowMaxes { get; }

	internal int[] ShadowBases { get; }

	internal long[] ValueStamps { get; }

	internal long[] RangeStamps { get; }

	// Only the owner hears of base values: everyone else's mirror has its current value for a base value.
	internal long[] BaseStamps { get; }

	// The tags the entity has, by net index, with the send each was first seen at.
	internal Dictionary<ushort, long> LiveTags { get; } = [];

	// The tags it no longer has, with the send they were seen gone at, kept until every client that could have seen
	// them has heard.
	internal Dictionary<ushort, long> RemovedTags { get; } = [];

	internal List<CueInstance> Cues { get; } = [];

	internal List<CueTombstone> RemovedCues { get; } = [];

	internal List<CueEvent> Events { get; } = [];

	// The effects active on the entity, in the order they were first seen, which is the order they were applied in.
	internal List<ReplicatedEffect> Effects { get; } = [];

	internal List<EffectTombstone> RemovedEffects { get; } = [];

	// The graphs the entity's abilities run that clients run something of, in the order they were first seen, with the
	// ones that ended kept until every client that could have them has heard.
	internal List<ReplicatedGraph> Graphs { get; } = [];

	internal uint NextCueInstance { get; set; } = 1;

	internal uint NextEffectInstance { get; set; } = 1;

	internal uint NextGraphInstance { get; set; } = 1;

	internal bool HasRemovedGraphs
	{
		get
		{
			foreach (ReplicatedGraph graph in Graphs)
			{
				if (graph.Removed)
				{
					return true;
				}
			}

			return false;
		}
	}

	internal uint LastEventSequence { get; set; }

	// The newest event the last send could have carried. A client that hears of the entity later never gets those, but
	// gets the ones since, such as what it did in the tick it spawned.
	internal uint SentEventSequence { get; set; }

	internal QuantizedBody? ShadowBody { get; private set; }

	// The body the entity had as it was registered, where its setup ran.
	internal BodyState? SpawnBody { get; }

	// What the entity's owner predicts of it, hashed at the last send, for the owner to check its prediction against,
	// or null when it couldn't be.
	internal ulong? PredictionChecksum { get; set; }

	internal long BodyStamp { get; private set; } = -1;

	internal ReplicatedEntity(
		IForgeEntity entity,
		int sceneIndex,
		PeerId owner,
		TagsManager tagsManager,
		long spawnedAtTick,
		BodyState? spawnBody,
		SpawnKey? spawnKey)
	{
		Entity = entity;
		Id = entity.NetworkId;
		SceneIndex = sceneIndex;
		Owner = owner;
		SpawnKey = spawnKey;
		_tagsManager = tagsManager;
		Attributes = [.. entity.Attributes];
		ShadowValues = new int[Attributes.Length];
		ShadowMins = new int[Attributes.Length];
		ShadowMaxes = new int[Attributes.Length];
		ShadowBases = new int[Attributes.Length];
		ValueStamps = new long[Attributes.Length];
		RangeStamps = new long[Attributes.Length];
		BaseStamps = new long[Attributes.Length];
		SpawnedAtTick = spawnedAtTick;
		SpawnBody = spawnBody;

		// Nothing has been seen yet, so the first capture stamps everything.
		Array.Fill(ShadowValues, int.MinValue);
		Array.Fill(ShadowMins, int.MinValue);
		Array.Fill(ShadowMaxes, int.MinValue);
		Array.Fill(ShadowBases, int.MinValue);
	}

	// Looks at the entity and stamps what changed since the last look with this send's tick.
	internal void Capture(long tick, Func<IForgeEntity, BodyState?> bodySource)
	{
		for (int i = 0; i < Attributes.Length; i++)
		{
			EntityAttribute attribute = Attributes[i];

			if (attribute.CurrentValue != ShadowValues[i])
			{
				ShadowValues[i] = attribute.CurrentValue;
				ValueStamps[i] = tick;
			}

			if (attribute.Min != ShadowMins[i] || attribute.Max != ShadowMaxes[i])
			{
				ShadowMins[i] = attribute.Min;
				ShadowMaxes[i] = attribute.Max;
				RangeStamps[i] = tick;
			}

			if (attribute.BaseValue != ShadowBases[i])
			{
				ShadowBases[i] = attribute.BaseValue;
				BaseStamps[i] = tick;
			}
		}

		CaptureTags(tick);

		foreach (CueInstance cue in Cues)
		{
			if (cue.Stamp == Pending)
			{
				cue.Stamp = tick;
			}
		}

		for (int i = 0; i < RemovedCues.Count; i++)
		{
			if (RemovedCues[i].Stamp == Pending)
			{
				RemovedCues[i] = RemovedCues[i] with { Stamp = tick };
			}
		}

		BodyState? body = bodySource(Entity);
		Body = body;

		if (body is BodyState state)
		{
			QuantizedBody quantized = BodyCodec.Quantize(state);

			if (ShadowBody != quantized)
			{
				ShadowBody = quantized;
				BodyStamp = tick;
			}
		}
		else if (ShadowBody is not null)
		{
			ShadowBody = null;
			BodyStamp = tick;
		}
	}

	// Looks at the effects active on the entity: one seen for the first time gets a record, and one that was there and
	// isn't any more leaves a tombstone.
	// Whether the entity's owner has the effect: one the entity's own tick applied, which the owner predicts, or one
	// that landed from outside, which the owner takes from the server when it hears of it.
	internal bool OwnerHas(ActiveEffect activeEffect)
	{
		return !activeEffect.IsFromOutside || HeardByOwner(activeEffect);
	}

	internal bool HeardByOwner(ActiveEffect activeEffect)
	{
		return _effectsByActive.TryGetValue(activeEffect, out ReplicatedEffect? effect)
			&& effect.Visibility != EffectVisibility.ServerOnly;
	}

	// A persistent cue the entity's owner shows itself: one its own tick applied, or one of an effect it has.
	internal bool OwnerShows(CueInstance cue)
	{
		return cue.FromOwnTick
			|| (cue.Application is EffectEvaluatedData data
				&& _effectsByActive.Keys.FirstOrDefault(x => x.EffectEvaluatedData == data) is ActiveEffect activeEffect
				&& OwnerHas(activeEffect));
	}

	internal void CaptureEffects(long tick, ReplicationServer server, SimulationClock clock)
	{
		IReadOnlyList<ActiveEffect> activeEffects = Entity.EffectsManager.ActiveEffects;

		for (int i = 0; i < activeEffects.Count; i++)
		{
			ActiveEffect activeEffect = activeEffects[i];

			if (!_effectsByActive.TryGetValue(activeEffect, out ReplicatedEffect? effect))
			{
				effect = server.DescribeEffect(NextEffectInstance++, activeEffect);
				_effectsByActive[activeEffect] = effect;
				Effects.Add(effect);
			}

			effect.Capture(tick, clock, _tagsManager);
		}

		for (int i = Effects.Count - 1; i >= 0; i--)
		{
			ReplicatedEffect effect = Effects[i];

			if (effect.SeenAt == tick)
			{
				continue;
			}

			Effects.RemoveAt(i);
			_effectsByActive.Remove(effect.Active);

			if (effect.Visibility is EffectVisibility.Public or EffectVisibility.OwnerOnly)
			{
				RemovedEffects
					.Add(new EffectTombstone(effect.Instance, effect.Visibility, tick, effect.Active.IsExpired));
			}
		}
	}

	internal void ClearEffects()
	{
		_effectsByActive.Clear();
		Effects.Clear();
		RemovedEffects.Clear();
	}

	// Looks at the graphs the entity's abilities run: one seen for the first time gets a record, and one that was there
	// and isn't any more is marked ended.
	internal void CaptureGraphs(long tick, ReplicationServer server)
	{
		_behaviors.Clear();
		Entity.Abilities.AddRunningBehaviors(_behaviors);

		foreach (Ability.BehaviorBinding binding in _behaviors)
		{
			if (binding.Behavior is not GraphAbilityBehavior behavior)
			{
				continue;
			}

			// The run on the processor is its last activation's, which a shared behavior's binding may not be.
			AbilityBehaviorContext context =
				behavior.Processor.GraphContext.ActivationContext as AbilityBehaviorContext ?? binding.Context;

			// A graph clients run nothing of still goes to an owner that takes its run in.
			if (behavior.Processor.Graph.NetworkTable.RunsOnClients || ReplicatedGraph.IsTakenInByOwner(context))
			{
				GraphFor(behavior.Processor, context, server, tick).Capture(tick);
			}
		}

		foreach (ReplicatedGraph graph in Graphs)
		{
			if (!graph.Removed && graph.SeenAt != tick)
			{
				graph.Remove(tick);
				_graphsByProcessor.Remove(graph.Processor);
			}
		}
	}

	// The record of a graph run, made the first time a send or an action executed in it shows it. A behavior an ability
	// shares between activations runs each on the same processor, so a new activation's context ends the last run's.
	internal ReplicatedGraph GraphFor(
		GraphProcessor processor,
		AbilityBehaviorContext context,
		ReplicationServer server,
		long tick)
	{
		if (_graphsByProcessor.TryGetValue(processor, out ReplicatedGraph? graph) && graph.Context == context)
		{
			return graph;
		}

		graph?.Remove(tick);
		graph = server.DescribeGraph(NextGraphInstance++, processor, context);
		_graphsByProcessor[processor] = graph;
		Graphs.Add(graph);
		return graph;
	}

	private void CaptureTags(long tick)
	{
		_currentTags.Clear();

		foreach (Tag tag in Entity.Tags.AllTags.InternalTags)
		{
			ushort index = _tagsManager.GetNetIndexFromTag(tag);

			if (index == _tagsManager.InvalidTagNetIndex)
			{
				continue;
			}

			_currentTags.Add(index);

			if (!LiveTags.ContainsKey(index))
			{
				LiveTags[index] = tick;
				RemovedTags.Remove(index);
			}
		}

		_scratch.Clear();

		foreach (ushort index in LiveTags.Keys)
		{
			if (_currentTags.Contains(index))
			{
				continue;
			}

			_scratch.Add(index);
			RemovedTags[index] = tick;
		}

		foreach (ushort index in _scratch)
		{
			LiveTags.Remove(index);
		}
	}

	// A persistent cue applied on the entity, numbered so a client can tell it from another of the same cue.
	internal sealed class CueInstance(
		uint instance,
		object? application,
		Tag cueTag,
		CueParameters? parameters,
		bool fromOwnTick)
	{
		public uint Instance { get; } = instance;

		public object? Application { get; } = application;

		// Applied by the entity's own tick, which its owner predicts, rather than by an effect or from elsewhere.
		public bool FromOwnTick { get; } = fromOwnTick;

		public Tag CueTag { get; } = cueTag;

		public CueParameters? Parameters { get; set; } = parameters;

		public long Stamp { get; set; } = Pending;
	}

	internal readonly record struct CueTombstone(uint Instance, long Stamp, bool Interrupted, bool OwnersOwn);

	internal readonly record struct EffectTombstone(
		uint Instance,
		EffectVisibility Visibility,
		long Stamp,
		bool Expired);

	// A cue executed on the entity: one-shot, numbered in order, and forgotten once it's too old to be worth showing.
	// One its own tick executed, which its owner predicts, goes to everyone but the owner.
	internal readonly record struct CueEvent(
		uint Sequence,
		Tag CueTag,
		CueParameters? Parameters,
		long CreatedAtTick,
		bool OwnersOwn);
}
