// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

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
	public int Max { get; }

	/// <summary>
	/// Gets the min value for this attribute.
	/// </summary>
	public int Min { get; }

	/// <summary>
	/// Gets the total modifier value kept so we can make Status Effect application consise.
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

		for (var i = 0; i < channels; i++)
		{
			_channels[i] = new ChannelData
			{
				Override = null,
				FlatModifier = 0,
				PercentModifier = 1,
			};
		}

		if (Validation.Enabled)
		{
			ValidateData();
		}
	}

	internal void SetMinValue(int newMinValue)
	{
		Validation.Assert(newMinValue <= Max, "MinValue cannot be lower than MaxValue.");

		var oldValue = CurrentValue;

		BaseValue = Math.Max(BaseValue, Min);

		UpdateCachedValues();

		if (CurrentValue != oldValue)
		{
			PendingValueChange += CurrentValue - oldValue;
		}
	}

	internal void SetMaxValue(int newMaxValue)
	{
		Validation.Assert(newMaxValue >= Min, "MaxValue cannot be lower than MinValue.");

		var oldValue = CurrentValue;

		BaseValue = Math.Min(BaseValue, Max);

		UpdateCachedValues();

		if (CurrentValue != oldValue)
		{
			PendingValueChange += CurrentValue - oldValue;
		}
	}

	internal void ExecuteOverride(int newValue)
	{
		var oldValue = CurrentValue;

		BaseValue = Math.Clamp(newValue, Min, Max);

		UpdateCachedValues();

		if (CurrentValue != oldValue)
		{
			PendingValueChange += CurrentValue - oldValue;
		}
	}

	internal void ExecuteFlatModifier(int value)
	{
		var oldValue = CurrentValue;

		BaseValue = Math.Clamp(BaseValue + value, Min, Max);

		UpdateCachedValues();

		if (CurrentValue != oldValue)
		{
			PendingValueChange += CurrentValue - oldValue;
		}
	}

	internal void ExecutePercentModifier(float value)
	{
		var oldValue = CurrentValue;

		BaseValue = Math.Clamp((int)(BaseValue * (1 + value)), Min, Max);

		UpdateCachedValues();

		if (CurrentValue != oldValue)
		{
			PendingValueChange += CurrentValue - oldValue;
		}
	}

	internal void AddOverride(AttributeOverride attributeOverrideData)
	{
		_attributeOverrides.AddFirst(attributeOverrideData);

		var oldValue = CurrentValue;

		ref ChannelData channelData = ref _channels[attributeOverrideData.Channel];
		channelData.Override = attributeOverrideData.Magnitude;

		UpdateCachedValues();

		if (CurrentValue != oldValue)
		{
			PendingValueChange += CurrentValue - oldValue;
		}
	}

	internal void ClearOverride(in AttributeOverride attributeOverride)
	{
		var oldValue = CurrentValue;
		var channel = attributeOverride.Channel;

		_attributeOverrides.Remove(attributeOverride);

		ref ChannelData channelData = ref _channels[channel];
		if (_attributeOverrides.Any(x => x.Channel == channel))
		{
			channelData.Override = _attributeOverrides.First(x => x.Channel == channel).Magnitude;
		}
		else
		{
			channelData.Override = null;
		}

		UpdateCachedValues();

		if (CurrentValue != oldValue)
		{
			PendingValueChange += CurrentValue - oldValue;
		}
	}

	internal void AddFlatModifier(int value, int channel)
	{
		var oldValue = CurrentValue;

		ref ChannelData channelData = ref _channels[channel];
		channelData.FlatModifier += value;

		UpdateCachedValues();

		if (CurrentValue != oldValue)
		{
			PendingValueChange += CurrentValue - oldValue;
		}
	}

	internal void AddPercentModifier(float value, int channel)
	{
		var oldValue = CurrentValue;

		ref ChannelData channelData = ref _channels[channel];
		channelData.PercentModifier += value;

		UpdateCachedValues();

		if (CurrentValue != oldValue)
		{
			PendingValueChange += CurrentValue - oldValue;
		}
	}

	internal float CalculateMagnitudeUpToChannel(int finalChanel)
	{
		var evaluatedValue = (float)BaseValue;

		for (var i = 0; i < finalChanel; i++)
		{
			var overrideValue = _channels[i].Override;

			if (overrideValue.HasValue)
			{
				evaluatedValue = overrideValue.Value;
				continue;
			}

			evaluatedValue = (evaluatedValue + _channels[i].FlatModifier) * _channels[i].PercentModifier;
		}

		return Math.Clamp((int)evaluatedValue, Min, Max);
	}

	internal void ApplyPendingValueChanges()
	{
		if (PendingValueChange != 0)
		{
			var cache = PendingValueChange;
			PendingValueChange = 0;
			OnValueChanged?.Invoke(this, cache);
		}
	}

	private void UpdateCachedValues()
	{
		var evaluatedValue = (float)BaseValue;

		foreach (ChannelData channel in _channels)
		{
			if (channel.Override.HasValue)
			{
				evaluatedValue = channel.Override.Value;
				continue;
			}

			evaluatedValue = (evaluatedValue + channel.FlatModifier) * channel.PercentModifier;
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
