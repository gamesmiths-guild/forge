// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects.Stacking;

/// <summary>
/// Defines what happens with the stack count when an effect of a different level is applied on the target.
/// </summary>
/// <remarks>
/// Only valid when <see cref="StackLevelPolicy.AggregateLevels"/> and LevelOverridePolicy are set.
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
