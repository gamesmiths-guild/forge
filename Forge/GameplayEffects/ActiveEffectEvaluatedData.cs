// Copyright © 2024 Gamesmiths Guild.

namespace Gamesmiths.Forge.GameplayEffects;

/// <summary>
/// Represents the precomputed static data for an active gameplay effect at a given moment.
/// </summary>
/// <remarks>
/// Serves as data for event arguments, not really used for optimization.
/// </remarks>
/// <param name="activeGameplayEffect">The active gameplay effect for this evaluated data.</param>
/// <param name="gameplayEffectEvaluatedData">The evaluated data for the applied gameplay effect.</param>
/// <param name="remainingDuration">The remaining duration for this active effect.</param>
/// <param name="nextPeriodicTick">The next periodic tick for this active effect, if it's a periodic effect.</param>
/// <param name="executionCount">The execution count for this active effect, if it's a periodic effect.</param>
public readonly struct ActiveEffectEvaluatedData(
	ActiveGameplayEffect activeGameplayEffect,
	GameplayEffectEvaluatedData gameplayEffectEvaluatedData,
	double remainingDuration,
	double nextPeriodicTick,
	int executionCount)
{
	/// <summary>
	/// Gets the active gameplay effect for this evaluated data.
	/// </summary>
	public ActiveGameplayEffect ActiveGameplayEffect { get; } = activeGameplayEffect;

	/// <summary>
	/// Gets the pre-computed evaluated data for the gameplay effect.
	/// </summary>
	public GameplayEffectEvaluatedData GameplayEffectEvaluatedData { get; } = gameplayEffectEvaluatedData;

	/// <summary>
	/// Gets the remaining duration for this active effect at the moment of evaluation.
	/// </summary>
	public double RemainingDuration { get; } = remainingDuration;

	/// <summary>
	/// Gets the next periodic tick for this active effect at the moment of the evaluation in case it's a periodic
	/// effect.
	/// </summary>
	public double NextPeriodicTick { get; } = nextPeriodicTick;

	/// <summary>
	/// Gets the count of executions for this active effect at the moment of the evaluation in case it's a periodic
	/// effect.
	/// </summary>
	public int ExecutionCount { get; } = executionCount;
}
