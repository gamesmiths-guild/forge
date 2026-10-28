// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Effects;

// An active effect as an entity snapshot took it: its timing, stacks and inhibition, a copy of its evaluation, the
// level and set by caller values of the effect it applies, and what each component instance captured.
internal sealed record ActiveEffectSnapshot(
	double RemainingDuration,
	double NextPeriodicTick,
	double InternalTime,
	int ExecutionCount,
	int StackCount,
	bool IsInhibited,
	EffectEvaluatedData EffectEvaluatedData,
	int EffectLevel,
	KeyValuePair<Tag, float>[] EffectDataTags,
	object?[] Components);
