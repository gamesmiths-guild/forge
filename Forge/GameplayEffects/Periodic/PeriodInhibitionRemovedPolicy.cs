// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.GameplayEffects.Periodic;

/// <summary>
/// Defines what happens with the period when inhibition is removed from the effect.
/// </summary>
public enum PeriodInhibitionRemovedPolicy
{
	/// <summary>
	/// Never reset the period.
	/// </summary>
	NeverReset = 0,

	/// <summary>
	/// Reset the period.
	/// </summary>
	ResetPeriod = 1,

	/// <summary>
	/// Execute the effect and reset it.
	/// </summary>
	ExecuteAndResetPeriod = 2,
}
