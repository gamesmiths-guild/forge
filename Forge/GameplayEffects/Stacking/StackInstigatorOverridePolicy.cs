// Copyright © 2024 Gamesmiths Guild.

namespace Gamesmiths.Forge.GameplayEffects.Stacking;

/// <summary>
/// Defines how to handle instigator overriding for stackable effects.
/// </summary>
/// <remarks>
/// Only valid when <see cref="StackPolicy.AggregateByTarget"/> is set.
/// </remarks>
public enum StackInstigatorOverridePolicy : byte
{
	/// <summary>
	/// The first instigator who applied the effect will always be kept.
	/// </summary>
	KeepCurrent = 0,

	/// <summary>
	/// Whenever a new instigator applies a stack, the instigator is updated with the new one.
	/// </summary>
	Override = 1,
}
