// Copyright © 2024 Gamesmiths Guild.

namespace Gamesmiths.Forge.GameplayEffects.Stacking;

/// <summary>
/// Defines how the stack instigator affects stack application.
/// </summary>
/// <remarks>
/// Only used when <see cref="StackPolicy.AggregateByTarget"/> is set.
/// </remarks>
public enum StackInstigatorDenialPolicy : byte
{
	/// <summary>
	/// Always allow new stacks to be applied, no matter the instigator.
	/// </summary>
	AlwaysAllow = 0,

	/// <summary>
	/// Deny stack application in case the instigator is different from the current effect instance's instigator.
	/// </summary>
	DenyIfDifferent = 1,
}
