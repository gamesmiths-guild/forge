// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects.Modifiers;

namespace Gamesmiths.Forge.Attributes;

/// <summary>
/// The modifiers of a single operation that are about to be applied to a channel, grouped by aggregation mode, so an
/// attribute value can be evaluated as if they were already there.
/// </summary>
/// <remarks>
/// Used by custom calculators capturing an attribute that the very same effect is about to modify.
/// </remarks>
/// <param name="SumGroup">The sum of the pending <see cref="AggregationMode.Sum"/> modifiers.</param>
/// <param name="MaxGroup">The highest pending <see cref="AggregationMode.Max"/> modifier, if any.</param>
/// <param name="MinGroup">The lowest pending <see cref="AggregationMode.Min"/> modifier, if any.</param>
internal readonly record struct PendingChannelModifiers(
	double SumGroup = 0,
	double? MaxGroup = null,
	double? MinGroup = null)
{
	/// <summary>
	/// Accumulates a pending modifier into its aggregation group.
	/// </summary>
	/// <param name="value">The value of the modifier.</param>
	/// <param name="aggregationMode">The aggregation group the modifier belongs to.</param>
	/// <returns>The resulting pending modifiers.</returns>
	public PendingChannelModifiers Add(double value, AggregationMode aggregationMode)
	{
		return aggregationMode switch
		{
			AggregationMode.Max => this with
			{
				MaxGroup = MaxGroup.HasValue ? Math.Max(MaxGroup.Value, value) : value,
			},
			AggregationMode.Min => this with
			{
				MinGroup = MinGroup.HasValue ? Math.Min(MinGroup.Value, value) : value,
			},
			_ => this with { SumGroup = SumGroup + value },
		};
	}
}
