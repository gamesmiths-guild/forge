// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects.Stacking;

/// <summary>
/// Defines what happens with the periodic time when a new stack is applied.
/// </summary>
public enum StackApplicationResetPeriodPolicy : byte // done, not tested
{
	/// <summary>
	/// Resets the current periodit time when successfully applied.
	/// </summary>
	ResetOnSuccessfulApplication = 0,

	/// <summary>
	/// Doesn't change anything. Never resets the periodic time.
	/// </summary>
	NeverReset = 1,
}
