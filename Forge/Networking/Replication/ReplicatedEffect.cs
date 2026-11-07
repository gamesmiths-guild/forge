// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Networking.Replication;

// An effect active on a replicated entity: what it is on the wire, and what the server last saw of it, stamped with the
// send it was seen at. Its end is a tick rather than the time left, so an effect that's only running changes nothing,
// and a client that hears of it late knows how much of it is left.
internal sealed class ReplicatedEffect(
	uint instance,
	ActiveEffect active,
	int assetIndex,
	EffectVisibility visibility,
	int contextCodec)
{
	// A duration is a sum of fixed steps with their rounding errors, and it mustn't look a tick longer for them.
	private const double TickTolerance = 1e-6;

	private readonly List<KeyValuePair<ushort, float>> _setByCallers = [];
	private readonly List<KeyValuePair<ushort, float>> _scratch = [];

	public uint Instance { get; } = instance;

	public ActiveEffect Active { get; } = active;

	// The effect's index in the registry's Effect kind, or -1 when it isn't one a client can be told about.
	public int AssetIndex { get; } = assetIndex;

	public EffectVisibility Visibility { get; } = visibility;

	// The index of the codec its custom data travels by, in the registry's EffectContext kind, or -1.
	public int ContextCodec { get; set; } = contextCodec;

	public long CreatedStamp { get; private set; } = ReplicatedEntity.Pending;

	public long StateStamp { get; private set; }

	public long ExecutionStamp { get; private set; } = -1;

	public long SeenAt { get; private set; }

	public int Level { get; private set; }

	public int StackCount { get; private set; }

	public bool IsInhibited { get; private set; }

	public bool IsTimed { get; } = active.EffectData.DurationData.DurationType == DurationType.HasDuration;

	public float Duration { get; private set; }

	public long EndTick { get; private set; }

	public uint OwnerId { get; private set; }

	public uint SourceId { get; private set; }

	public int ExecutionCount { get; private set; }

	public IReadOnlyList<KeyValuePair<ushort, float>> SetByCallers => _setByCallers;

	public long LastChange => Math.Max(StateStamp, ExecutionStamp);

	// Stamps what changed since the last look with this send's tick.
	public void Capture(long tick, SimulationClock clock, TagsManager tagsManager)
	{
		SeenAt = tick;

		if (Visibility == EffectVisibility.ServerOnly)
		{
			return;
		}

		EffectEvaluatedData evaluated = Active.EffectEvaluatedData;
		EffectOwnership ownership = evaluated.Effect.Ownership;
		uint ownerId = ownership.Owner?.NetworkId.Value ?? 0;
		uint sourceId = ownership.Source?.NetworkId.Value ?? 0;
		long endTick = IsTimed
			? tick + (long)Math.Ceiling(clock.TicksIn(Active.RemainingDuration) - TickTolerance)
			: 0;

		bool setByCallersChanged = CaptureSetByCallers(evaluated.Effect, tagsManager);

		if (CreatedStamp == ReplicatedEntity.Pending
			|| setByCallersChanged
			|| Level != evaluated.Level
			|| StackCount != Active.StackCount
			|| IsInhibited != Active.IsInhibited
			|| !SameFloat(Duration, evaluated.Duration)
			|| EndTick != endTick
			|| OwnerId != ownerId
			|| SourceId != sourceId)
		{
			StateStamp = tick;
			Level = evaluated.Level;
			StackCount = Active.StackCount;
			IsInhibited = Active.IsInhibited;
			Duration = evaluated.Duration;
			EndTick = endTick;
			OwnerId = ownerId;
			SourceId = sourceId;
		}

		if (CreatedStamp == ReplicatedEntity.Pending)
		{
			CreatedStamp = tick;
		}

		if (ExecutionCount != Active.ExecutionCount)
		{
			ExecutionCount = Active.ExecutionCount;
			ExecutionStamp = tick;
		}
	}

	private static bool SameFloat(float first, float second)
	{
		return BitConverter.SingleToInt32Bits(first) == BitConverter.SingleToInt32Bits(second);
	}

	private bool CaptureSetByCallers(Effect effect, TagsManager tagsManager)
	{
		_scratch.Clear();

		foreach (KeyValuePair<Tag, float> setByCaller in effect.DataTag)
		{
			ushort index = tagsManager.GetNetIndexFromTag(setByCaller.Key);

			if (index != tagsManager.InvalidTagNetIndex)
			{
				_scratch.Add(new KeyValuePair<ushort, float>(index, setByCaller.Value));
			}
		}

		bool changed = _scratch.Count != _setByCallers.Count;

		for (int i = 0; !changed && i < _scratch.Count; i++)
		{
			changed = _scratch[i].Key != _setByCallers[i].Key || !SameFloat(_scratch[i].Value, _setByCallers[i].Value);
		}

		if (changed)
		{
			_setByCallers.Clear();
			_setByCallers.AddRange(_scratch);
		}

		return changed;
	}
}
