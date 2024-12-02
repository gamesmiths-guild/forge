// Copyright © 2024 Gamesmiths Guild.

namespace Gamesmiths.Forge.GameplayEffects.Stacking;

/// <summary>
/// Defines what happens with the stack count when a different instigator successfully applies a new stack of the
/// effect.
/// </summary>
/// <remarks>
/// Only valid when <see cref="StackPolicy.AggregateByTarget"/> and <see cref="StackInstigatorOverridePolicy.Override"/>
/// are set.
/// </remarks>
public enum StackInstigatorOverrideStackCountPolicy : byte
{
	/// <summary>
	/// Try to increases the stack if not maxed out.
	/// </summary>
	IncreaseStacks = 0,

	/// <summary>
	/// Resets the current stack count.
	/// </summary>
	ResetStacks = 1,
}
