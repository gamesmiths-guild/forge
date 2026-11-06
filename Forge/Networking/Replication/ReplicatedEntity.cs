// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
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

	internal uint NextCueInstance { get; set; } = 1;

	internal uint NextEffectInstance { get; set; } = 1;

	internal uint LastEventSequence { get; set; }

	internal QuantizedBody? ShadowBody { get; private set; }

	internal long BodyStamp { get; private set; } = -1;

	internal ReplicatedEntity(
		IForgeEntity entity,
		int sceneIndex,
		PeerId owner,
		TagsManager tagsManager,
		long spawnedAtTick)
	{
		Entity = entity;
		Id = entity.NetworkId;
		SceneIndex = sceneIndex;
		Owner = owner;
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
			RemovedEffects.Add(new EffectTombstone(effect.Instance, effect.Visibility, tick, effect.Active.IsExpired));
		}
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
	internal sealed class CueInstance(uint instance, object? application, Tag cueTag, CueParameters? parameters)
	{
		public uint Instance { get; } = instance;

		public object? Application { get; } = application;

		public Tag CueTag { get; } = cueTag;

		public CueParameters? Parameters { get; set; } = parameters;

		public long Stamp { get; set; } = Pending;
	}

	internal readonly record struct CueTombstone(uint Instance, long Stamp, bool Interrupted);

	internal readonly record struct EffectTombstone(
		uint Instance,
		EffectVisibility Visibility,
		long Stamp,
		bool Expired);

	// A cue executed on the entity: one-shot, numbered in order, and forgotten once it's too old to be worth showing.
	internal readonly record struct CueEvent(uint Sequence, Tag CueTag, CueParameters? Parameters, long CreatedAtTick);
}
