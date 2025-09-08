// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// Represents the precomputed static data for an active effect at a given moment.
/// </summary>
/// <remarks>
/// Serves as data for event arguments, not really used for optimization.
/// </remarks>
/// <param name="ActiveEffectHandle">The active effect handle for this evaluated data.</param>
/// <param name="EffectEvaluatedData">The evaluated data for the applied effect.</param>
/// <param name="RemainingDuration">The remaining duration for this active effect.</param>
/// <param name="NextPeriodicTick">The next periodic tick for this active effect, if it's a periodic effect.</param>
/// <param name="ExecutionCount">The execution count for this active effect, if it's a periodic effect.</param>
public readonly record struct ActiveEffectEvaluatedData(
	ActiveEffectHandle ActiveEffectHandle,
	EffectEvaluatedData EffectEvaluatedData,
	double RemainingDuration,
	double NextPeriodicTick,
	int ExecutionCount);
