// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.GameplayEffects.Stacking;

/// <summary>
/// Defines how the stack owner affects stack application.
/// </summary>
/// <remarks>
/// Only used when <see cref="StackPolicy.AggregateByTarget"/> is set.
/// </remarks>
public enum StackOwnerDenialPolicy : byte
{
	/// <summary>
	/// Always allow new stacks to be applied, no matter the owner.
	/// </summary>
	AlwaysAllow = 0,

	/// <summary>
	/// Deny stack application in case the owner is different from the current effect instance's owner.
	/// </summary>
	DenyIfDifferent = 1,
}
