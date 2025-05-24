// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects.Stacking;

/// <summary>
/// Defines what happens with the stack count when a different owner successfully applies a new stack of the
/// effect.
/// </summary>
/// <remarks>
/// Only valid when <see cref="StackPolicy.AggregateByTarget"/> and <see cref="StackOwnerOverridePolicy.Override"/>
/// are set.
/// </remarks>
public enum StackOwnerOverrideStackCountPolicy : byte
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
