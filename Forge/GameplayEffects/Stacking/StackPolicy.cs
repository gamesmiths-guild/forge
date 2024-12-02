// Copyright © 2024 Gamesmiths Guild.

namespace Gamesmiths.Forge.GameplayEffects.Stacking;

/// <summary>
/// Defines how stacks are aggregated into the target.
/// </summary>
public enum StackPolicy : byte
{
	/// <summary>
	/// Stacks are aggregated by source, each instigator has their own stack.
	/// </summary>
	AggregateBySource = 0,

	/// <summary>
	/// Stacks are aggregated by target, targets can have only one stack of a given effect at a time.
	/// </summary>
	AggregateByTarget = 1,
}
