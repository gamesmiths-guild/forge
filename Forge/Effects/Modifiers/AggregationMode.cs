// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects.Modifiers;

/// <summary>
/// Defines how a modifier combines with the other modifiers of its group.
/// </summary>
/// <remarks>
/// <para>Modifiers are grouped by attribute, channel, <see cref="ModifierOperation"/> and
/// <see cref="AggregationMode"/>. Each group contributes a single value to its channel, and those contributions are
/// then combined the usual way: flat contributions are added together, and percent contributions are added together
/// into a single multiplier.</para>
/// <para>This is what makes "strongest wins" mechanics declarative: mark every movement speed buff as
/// <see cref="Max"/> and only the strongest one is ever active, with the next strongest taking over automatically as
/// soon as it's removed.</para>
/// <para>Aggregation only applies to modifiers applied by active effects. Instant and periodic effects execute their
/// modifiers against the attribute's base value, permanently, so there's no group of active modifiers to aggregate.
/// </para>
/// </remarks>
public enum AggregationMode : byte
{
	/// <summary>
	/// All modifiers in the group contribute, summed together.
	/// </summary>
	/// <remarks>
	/// There's nothing to sum for <see cref="ModifierOperation.Override"/>, so this is the default
	/// last-applied-wins behavior.
	/// </remarks>
	Sum = 0,

	/// <summary>
	/// Only the highest valued modifier in the group contributes.
	/// </summary>
	Max = 1,

	/// <summary>
	/// Only the lowest valued modifier in the group contributes.
	/// </summary>
	Min = 2,
}
