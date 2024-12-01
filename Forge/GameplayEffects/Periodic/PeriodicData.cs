// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.GameplayEffects.Magnitudes;

namespace Gamesmiths.Forge.GameplayEffects.Periodic;

/// <summary>
/// The periodic data for a Gameplay Effect.
/// </summary>
/// <param name="period">The duration for the periodic triggers for the effect.</param>
/// <param name="executeOnApplication">Should the periodic effect trigger on application?.</param>
public readonly struct PeriodicData(ScalableFloat period, bool executeOnApplication)
{
	/// <summary>
	/// Gets the period duration for the periodic effect.
	/// </summary>
	public ScalableFloat Period { get; } = period;

	/// <summary>
	/// Gets a value indicating whether this effect should execute its periodic effect on application or not.
	/// </summary>
	public bool ExecuteOnApplication { get; } = executeOnApplication;
}
