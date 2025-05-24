// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects.Stacking;

/// <summary>
/// Defines how stacks are aggregated into the target.
/// </summary>
public enum StackPolicy : byte
{
	/// <summary>
	/// Stacks are aggregated by source, each owner has their own stack.
	/// </summary>
	AggregateBySource = 0,

	/// <summary>
	/// Stacks are aggregated by target, targets can have only one stack of a given effect at a time.
	/// </summary>
	AggregateByTarget = 1,
}
