// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects.Stacking;

/// <summary>
/// Defines how to handle owner overriding for stackable effects.
/// </summary>
/// <remarks>
/// Only valid when <see cref="StackPolicy.AggregateByTarget"/> is set.
/// </remarks>
public enum StackOwnerOverridePolicy : byte
{
	/// <summary>
	/// The first owner who applied the effect will always be kept.
	/// </summary>
	KeepCurrent = 0,

	/// <summary>
	/// Whenever a new owner applies a stack, the owner is updated with the new one.
	/// </summary>
	Override = 1,
}
