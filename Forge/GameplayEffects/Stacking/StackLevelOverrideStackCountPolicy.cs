// Copyright © 2024 Gamesmiths Guild.

namespace Gamesmiths.Forge.GameplayEffects.Stacking;

/// <summary>
/// Defines what happens with the stack count when an effect of a different level is applied on the target.
/// </summary>
/// <remarks>
/// Only valid when <see cref="StackLevelPolicy.AggregateLevels"/> and <see cref="StackLevelOverridePolicy"/> are
/// set.
/// </remarks>
public enum StackLevelOverrideStackCountPolicy : byte
{
	/// <summary>
	/// Increases the stack of the currently applied effect.
	/// </summary>
	IncreaseStacks = 0,

	/// <summary>
	/// Resets the stack of the currently applied effect.
	/// </summary>
	ResetStacks = 1,
}
