// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects.Modifiers;

namespace Gamesmiths.Forge.Attributes;

/// <summary>
/// Channel configuration for Attributes.
/// </summary>
/// <remarks>
/// <para>Attributes have channels for calculating its modifiers. Multiple channels can be used to calculate in
/// sequence, granting it's possible to have various kinds of formulas combining flat and percentage modifiers.</para>
/// <para>Within a channel, modifiers are grouped by operation and <see cref="AggregationMode"/>. Each group
/// contributes a single value, and those contributions are added together.</para>
/// </remarks>
internal struct ChannelData
{
	private List<int>? _flatMaxGroup;
	private List<int>? _flatMinGroup;
	private List<double>? _percentMaxGroup;
	private List<double>? _percentMinGroup;

	private int _flatSum;
	private double _percentSum;
	private double _percentTotal;

	/// <summary>
	/// Gets or sets an override value at this channel.
	/// </summary>
	/// <remarks>
	/// Can be null in case there are no overrides.
	/// </remarks>
	public int? Override { get; set; }

	/// <summary>
	/// Gets the aggregated flat value modifier for this channel.
	/// </summary>
	public int FlatModifier { get; private set; }

	/// <summary>
	/// Gets the aggregated percent modifier for this channel, as a multiplier.
	/// </summary>
	public readonly double PercentModifier => 1 + _percentTotal;

	/// <summary>
	/// Adds a flat modifier to its aggregation group.
	/// </summary>
	/// <param name="value">The value of the modifier.</param>
	/// <param name="aggregationMode">The aggregation group the modifier belongs to.</param>
	public void AddFlatModifier(int value, AggregationMode aggregationMode)
	{
		switch (aggregationMode)
		{
			case AggregationMode.Max:
				_flatMaxGroup ??= [];
				_flatMaxGroup.Add(value);
				break;

			case AggregationMode.Min:
				_flatMinGroup ??= [];
				_flatMinGroup.Add(value);
				break;

			default:
				_flatSum += value;
				break;
		}

		UpdateFlatTotal();
	}

	/// <summary>
	/// Removes a previously added flat modifier from its aggregation group.
	/// </summary>
	/// <param name="value">The value of the modifier.</param>
	/// <param name="aggregationMode">The aggregation group the modifier belongs to.</param>
	public void RemoveFlatModifier(int value, AggregationMode aggregationMode)
	{
		switch (aggregationMode)
		{
			case AggregationMode.Max:
				_flatMaxGroup?.Remove(value);
				break;

			case AggregationMode.Min:
				_flatMinGroup?.Remove(value);
				break;

			default:
				_flatSum -= value;
				break;
		}

		UpdateFlatTotal();
	}

	/// <summary>
	/// Adds a percent modifier to its aggregation group.
	/// </summary>
	/// <param name="value">The value of the modifier.</param>
	/// <param name="aggregationMode">The aggregation group the modifier belongs to.</param>
	public void AddPercentModifier(double value, AggregationMode aggregationMode)
	{
		switch (aggregationMode)
		{
			case AggregationMode.Max:
				_percentMaxGroup ??= [];
				_percentMaxGroup.Add(value);
				break;

			case AggregationMode.Min:
				_percentMinGroup ??= [];
				_percentMinGroup.Add(value);
				break;

			default:
				_percentSum += value;
				break;
		}

		UpdatePercentTotal();
	}

	/// <summary>
	/// Removes a previously added percent modifier from its aggregation group.
	/// </summary>
	/// <param name="value">The value of the modifier.</param>
	/// <param name="aggregationMode">The aggregation group the modifier belongs to.</param>
	public void RemovePercentModifier(double value, AggregationMode aggregationMode)
	{
		switch (aggregationMode)
		{
			case AggregationMode.Max:
				_percentMaxGroup?.Remove(value);
				break;

			case AggregationMode.Min:
				_percentMinGroup?.Remove(value);
				break;

			default:
				_percentSum -= value;
				break;
		}

		UpdatePercentTotal();
	}

	/// <summary>
	/// Calculates what <see cref="FlatModifier"/> would be with the given not yet applied modifiers included.
	/// </summary>
	/// <param name="pendingModifiers">The flat modifiers about to be applied to this channel.</param>
	/// <returns>The resulting flat value modifier.</returns>
	public readonly int EvaluateFlatModifier(in PendingChannelModifiers pendingModifiers)
	{
		int? pendingMax = pendingModifiers.MaxGroup.HasValue ? (int)pendingModifiers.MaxGroup.Value : null;
		int? pendingMin = pendingModifiers.MinGroup.HasValue ? (int)pendingModifiers.MinGroup.Value : null;

		return _flatSum
			+ (int)pendingModifiers.SumGroup
			+ Extreme(_flatMaxGroup, pendingMax, AggregationMode.Max)
			+ Extreme(_flatMinGroup, pendingMin, AggregationMode.Min);
	}

	/// <summary>
	/// Calculates what <see cref="PercentModifier"/> would be with the given not yet applied modifiers included.
	/// </summary>
	/// <param name="pendingModifiers">The percent modifiers about to be applied to this channel.</param>
	/// <returns>The resulting percent multiplier.</returns>
	public readonly double EvaluatePercentModifier(in PendingChannelModifiers pendingModifiers)
	{
		return 1
			+ _percentSum
			+ pendingModifiers.SumGroup
			+ Extreme(_percentMaxGroup, pendingModifiers.MaxGroup, AggregationMode.Max)
			+ Extreme(_percentMinGroup, pendingModifiers.MinGroup, AggregationMode.Min);
	}

	/// <summary>
	/// Gets the value an aggregation group contributes to the channel.
	/// </summary>
	/// <typeparam name="T">The type of the aggregated values.</typeparam>
	/// <param name="groupValues">The values currently active in the group.</param>
	/// <param name="pendingValue">An optional value about to be added to the group.</param>
	/// <param name="aggregationMode">Which extreme the group contributes.</param>
	/// <returns>The group's extreme value, or <see langword="default"/> when the group is empty.</returns>
	private static T Extreme<T>(List<T>? groupValues, T? pendingValue, AggregationMode aggregationMode)
		where T : struct
	{
		Comparer<T> comparer = Comparer<T>.Default;
		T extreme = default;
		bool hasValue = false;

		if (groupValues is not null)
		{
			foreach (T value in groupValues)
			{
				if (!hasValue || IsMoreExtreme(comparer, value, extreme, aggregationMode))
				{
					extreme = value;
					hasValue = true;
				}
			}
		}

		if (pendingValue.HasValue &&
			(!hasValue || IsMoreExtreme(comparer, pendingValue.Value, extreme, aggregationMode)))
		{
			extreme = pendingValue.Value;
		}

		return extreme;
	}

	private static bool IsMoreExtreme<T>(Comparer<T> comparer, T value, T current, AggregationMode aggregationMode)
	{
		int comparison = comparer.Compare(value, current);

		return aggregationMode == AggregationMode.Max ? comparison > 0 : comparison < 0;
	}

	private void UpdateFlatTotal()
	{
		FlatModifier = _flatSum
			+ Extreme(_flatMaxGroup, null, AggregationMode.Max)
			+ Extreme(_flatMinGroup, null, AggregationMode.Min);
	}

	private void UpdatePercentTotal()
	{
		_percentTotal = _percentSum
			+ Extreme(_percentMaxGroup, null, AggregationMode.Max)
			+ Extreme(_percentMinGroup, null, AggregationMode.Min);
	}
}
