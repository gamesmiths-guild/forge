// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Modifiers;

namespace Gamesmiths.Forge.Attributes;

/// <summary>
/// Represents a numeric property or characteristic of a gameplay object. Attributes can be used to model  a wide
/// variety of values such as health, strength, ammunition, resources, position, scale, bonuses, or any other measurable
/// aspect.
/// </summary>
public sealed class EntityAttribute
{
	private readonly ChannelData[] _channels;

	private readonly LinkedList<AttributeOverride> _attributeOverrides = [];

	/// <summary>
	/// Event triggered when this Attribute receives any changes in its <see cref="CurrentValue"/>.
	/// </summary>
	/// <remarks>
	/// <para>Some modifiers might not trigger this event in cases where the Attribute might be clamped by
	/// <see cref="Min"/> or <see cref="Max"/> values.</para>
	/// <para>And it may even trigger in case changes in the <see cref="Min"/> or <see cref="Max"/> values cause the
	/// <see cref="CurrentValue"/> to update.</para>
	/// </remarks>
	public event Action<EntityAttribute, int>? OnValueChanged;

	/// <summary>
	/// Gets the unique key identifying this attribute.
	/// </summary>
	public StringKey Key { get; internal set; }

	/// <summary>
	/// Gets the base value for this attribute.
	/// </summary>
	public int BaseValue { get; private set; }

	/// <summary>
	/// Gets the real total value for this attribute. It's always going to respect the <see cref="Min"/> and
	/// <see cref="Max"/> values regardless of the modifiers applied on it.
	/// </summary>
	public int CurrentValue { get; private set; }

	/// <summary>
	/// Gets the max value for this attribute.
	/// </summary>
	public int Max { get; private set; }

	/// <summary>
	/// Gets the min value for this attribute.
	/// </summary>
	public int Min { get; private set; }

	/// <summary>
	/// Gets the total modifier value kept so we can make Status Effect application concise.
	/// </summary>
	/// <remarks>
	/// Use <see cref="CurrentValue"/> to get the actual final value for the Attribute. This value could be clamped by
	/// <see cref="Min"/> and <see cref="Max"/> values.
	/// </remarks>
	public int Modifier { get; private set; }

	/// <summary>
	/// Gets the value overflown by the modifier bellow or above the <see cref="Min"/> and <see cref="Max"/> values.
	/// </summary>
	/// <remarks>
	/// Positive in case it's overflowing above <see cref="Max"/> and negative if it's overflowing bellow
	/// <see cref="Min"/>.
	/// </remarks>
	public int Overflow { get; private set; }

	/// <summary>
	/// Gets the adjusted modifier value that is not overflowing the <see cref="Min"/> and <see cref="Max"/> values.
	/// </summary>
	public int ValidModifier => Modifier - Overflow;

	internal int PendingValueChange { get; private set; }

	internal EntityAttribute(
		StringKey key,
		int defaultValue,
		int minValue,
		int maxValue,
		int channels)
	{
		Key = key;

		PendingValueChange = 0;

		Min = minValue;
		Max = maxValue;
		BaseValue = defaultValue;
		Modifier = 0;
		Overflow = 0;
		CurrentValue = BaseValue;

		_channels = new ChannelData[channels];

		if (Validation.Enabled)
		{
			ValidateData();
		}
	}

	internal void SetMinValue(int newMinValue)
	{
		Validation.Assert(newMinValue <= Max, "MinValue cannot be greater than MaxValue.");

		int oldValue = CurrentValue;

		Min = newMinValue;
		BaseValue = Math.Max(BaseValue, Min);

		RefreshValues(oldValue);
	}

	internal void SetMaxValue(int newMaxValue)
	{
		Validation.Assert(newMaxValue >= Min, "MaxValue cannot be lower than MinValue.");

		int oldValue = CurrentValue;

		Max = newMaxValue;
		BaseValue = Math.Min(BaseValue, Max);

		RefreshValues(oldValue);
	}

	internal void ExecuteOverride(int newValue)
	{
		int oldValue = CurrentValue;

		BaseValue = Math.Clamp(newValue, Min, Max);

		RefreshValues(oldValue);
	}

	internal void ExecuteFlatModifier(int value)
	{
		int oldValue = CurrentValue;

		BaseValue = Math.Clamp(BaseValue + value, Min, Max);

		RefreshValues(oldValue);
	}

	internal void ExecutePercentModifier(float value)
	{
		int oldValue = CurrentValue;

		BaseValue = Math.Clamp((int)(BaseValue * Math.Round(1 + value, 6)), Min, Max);

		RefreshValues(oldValue);
	}

	internal void AddOverride(AttributeOverride attributeOverrideData)
	{
		int oldValue = CurrentValue;

		_attributeOverrides.AddFirst(attributeOverrideData);

		ref ChannelData channelData = ref _channels[attributeOverrideData.Channel];
		channelData.Override = ResolveOverride(attributeOverrideData.Channel);

		RefreshValues(oldValue);
	}

	internal void ClearOverride(in AttributeOverride attributeOverride)
	{
		int oldValue = CurrentValue;
		int channel = attributeOverride.Channel;

		_attributeOverrides.Remove(attributeOverride);

		ref ChannelData channelData = ref _channels[channel];
		channelData.Override = ResolveOverride(channel);

		RefreshValues(oldValue);
	}

	internal void AddFlatModifier(int value, int channel, AggregationMode aggregationMode)
	{
		int oldValue = CurrentValue;

		ref ChannelData channelData = ref _channels[channel];
		channelData.AddFlatModifier(value, aggregationMode);

		RefreshValues(oldValue);
	}

	internal void RemoveFlatModifier(int value, int channel, AggregationMode aggregationMode)
	{
		int oldValue = CurrentValue;

		ref ChannelData channelData = ref _channels[channel];
		channelData.RemoveFlatModifier(value, aggregationMode);

		RefreshValues(oldValue);
	}

	internal void AddPercentModifier(float value, int channel, AggregationMode aggregationMode)
	{
		int oldValue = CurrentValue;

		ref ChannelData channelData = ref _channels[channel];
		channelData.AddPercentModifier(Math.Round(value, 6), aggregationMode);

		RefreshValues(oldValue);
	}

	internal void RemovePercentModifier(float value, int channel, AggregationMode aggregationMode)
	{
		int oldValue = CurrentValue;

		ref ChannelData channelData = ref _channels[channel];
		channelData.RemovePercentModifier(Math.Round(value, 6), aggregationMode);

		RefreshValues(oldValue);
	}

	internal float CalculateMagnitudeUpToChannel(int finalChanel)
	{
		float evaluatedValue = BaseValue;

		for (int i = 0; i < finalChanel; i++)
		{
			int? overrideValue = _channels[i].Override;

			if (overrideValue.HasValue)
			{
				evaluatedValue = overrideValue.Value;
				continue;
			}

			evaluatedValue = (float)((evaluatedValue + _channels[i].FlatModifier) * _channels[i].PercentModifier);
		}

		return Math.Clamp((int)evaluatedValue, Min, Max);
	}

	internal float CalculateValueWithPendingModifiers(
		Dictionary<int, PendingChannelModifiers>? pendingFlatBonusByChannel,
		Dictionary<int, PendingChannelModifiers>? pendingPercentBonusByChannel,
		Dictionary<int, AttributeOverride>? pendingOverrideByChannel)
	{
		float evaluatedValue = BaseValue;

		for (int i = 0; i < _channels.Length; i++)
		{
			AttributeOverride? pendingOverride = null;
			if (pendingOverrideByChannel is not null &&
				pendingOverrideByChannel.TryGetValue(i, out AttributeOverride channelPendingOverride))
			{
				pendingOverride = channelPendingOverride;
			}

			int? channelOverride = pendingOverride.HasValue
				? ResolveOverride(i, pendingOverride)
				: _channels[i].Override;

			if (channelOverride.HasValue)
			{
				evaluatedValue = channelOverride.Value;
				continue;
			}

			PendingChannelModifiers pendingFlat = default;
			pendingFlatBonusByChannel?.TryGetValue(i, out pendingFlat);

			PendingChannelModifiers pendingPercent = default;
			pendingPercentBonusByChannel?.TryGetValue(i, out pendingPercent);

			evaluatedValue = (float)((evaluatedValue + _channels[i].EvaluateFlatModifier(pendingFlat))
				* _channels[i].EvaluatePercentModifier(pendingPercent));
		}

		return Math.Clamp((int)evaluatedValue, Min, Max);
	}

	internal void ApplyPendingValueChanges()
	{
		if (PendingValueChange != 0)
		{
			int cache = PendingValueChange;
			PendingValueChange = 0;
			OnValueChanged?.Invoke(this, cache);
		}
	}

	/// <summary>
	/// Resolves which of the active overrides of a channel is the one in effect.
	/// </summary>
	/// <remarks>
	/// The most recently applied override decides the policy: with <see cref="AggregationMode.Sum"/> it simply wins,
	/// while <see cref="AggregationMode.Max"/> and <see cref="AggregationMode.Min"/> hand the channel over to the
	/// extreme override of that same mode.
	/// </remarks>
	/// <param name="channel">The channel being resolved.</param>
	/// <param name="pendingOverride">An override about to be applied, which counts as the most recent one.</param>
	/// <returns>The value overriding the channel, or <see langword="null"/> if it isn't being overridden.</returns>
	private int? ResolveOverride(int channel, AttributeOverride? pendingOverride = null)
	{
		AttributeOverride? mostRecentOverride = pendingOverride;

		if (!mostRecentOverride.HasValue)
		{
			foreach (AttributeOverride activeOverride in _attributeOverrides)
			{
				if (activeOverride.Channel == channel)
				{
					mostRecentOverride = activeOverride;
					break;
				}
			}
		}

		if (!mostRecentOverride.HasValue)
		{
			return null;
		}

		AggregationMode aggregationMode = mostRecentOverride.Value.AggregationMode;
		int overrideValue = mostRecentOverride.Value.Magnitude;

		if (aggregationMode == AggregationMode.Sum)
		{
			return overrideValue;
		}

		foreach (AttributeOverride activeOverride in _attributeOverrides)
		{
			if (activeOverride.Channel != channel || activeOverride.AggregationMode != aggregationMode)
			{
				continue;
			}

			if (aggregationMode == AggregationMode.Max
				? activeOverride.Magnitude > overrideValue
				: activeOverride.Magnitude < overrideValue)
			{
				overrideValue = activeOverride.Magnitude;
			}
		}

		return overrideValue;
	}

	private void RefreshValues(int oldValue)
	{
		UpdateCachedValues();

		if (CurrentValue != oldValue)
		{
			PendingValueChange += CurrentValue - oldValue;
		}
	}

	private void UpdateCachedValues()
	{
		float evaluatedValue = BaseValue;

		foreach (ChannelData channel in _channels)
		{
			if (channel.Override.HasValue)
			{
				evaluatedValue = channel.Override.Value;
				continue;
			}

			evaluatedValue = (float)((evaluatedValue + channel.FlatModifier) * channel.PercentModifier);
		}

		CurrentValue = Math.Clamp((int)evaluatedValue, Min, Max);
		Modifier = (int)evaluatedValue - BaseValue;

		if (evaluatedValue > Max)
		{
			Overflow = (int)evaluatedValue - Max;
		}
		else if (evaluatedValue < Min)
		{
			Overflow = (int)evaluatedValue - Min;
		}
		else
		{
			Overflow = 0;
		}
	}

	private void ValidateData()
	{
		Validation.Assert(
			Min <= Max,
			"MinValue cannot be greater than MaxValue.");

		Validation.Assert(
			BaseValue >= Min && BaseValue <= Max,
			"DefaultValue should be withing MinValue and MaxValue.");

		Validation.Assert(
			_channels.Length > 0,
			"There should be at least one channel.");
	}
}
