// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects.Stacking;

/// <summary>
/// Defines how to handle stacks when effects of different level are applied on the same target.
/// </summary>
public enum StackLevelPolicy : byte
{
	/// <summary>
	/// Aggregate all levels on the same stack.
	/// </summary>
	AggregateLevels = 0,

	/// <summary>
	/// Segregate each level on its own stacks.
	/// </summary>
	SegregateLevels = 1,
}
