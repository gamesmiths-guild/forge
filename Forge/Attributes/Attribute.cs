// Copyright © 2024 Gamesmiths Guild.

using System.Diagnostics;

namespace Gamesmiths.Forge.Attributes;

/// <summary>
/// Represents a numeric property or characteristic of a gameplay object. Attributes can be used to model  a wide
/// variety of values such as health, strength, ammunition, resources, position, scale, bonuses, or any other measurable
/// aspect.
/// </summary>
public sealed class Attribute
{
	private readonly List<ChannelData> _channels = [];

	/// <summary>
	/// Event triggered when this Attribute receives any changes in its <see cref="CurrentValue"/>.
	/// </summary>
	/// <remarks>
	/// <para>Some modifiers might not trigger this event in cases where the Attribute might be clamped by
	/// <see cref="Min"/> or <see cref="Max"/> values.</para>
	/// <para>And it may even trigger in case changes in the <see cref="Min"/> or <see cref="Max"/> values cause the
	/// <see cref="CurrentValue"/> to update.</para>
	/// </remarks>
	public event Action<Attribute, int>? OnValueChanged;

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

	internal Attribute(
		int defaultValue,
		int minValue,
		int maxValue,
		int channels)
	{
		Debug.Assert(
			minValue <= maxValue,
			"MinValue cannot be greater than MaxValue.");

		Debug.Assert(
			defaultValue >= minValue && defaultValue <= maxValue,
			"DefaultValue should be withing MinValue and MaxValue.");

		Debug.Assert(
			channels > 0,
			"There should be at least one channel.");

		Min = minValue;
		Max = maxValue;
		BaseValue = defaultValue;
		Modifier = 0;
		Overflow = 0;
		CurrentValue = BaseValue;

		for (var i = 0; i < channels - 1; i++)
		{
			_channels.Add(new ChannelData
			{
				Override = null,
				FlatModifier = 0,
				PercentModifier = 1,
			});
		}
	}

	internal void SetMinValue(int newMinValue)
	{
		Debug.Assert(newMinValue <= Max, "MinValue cannot be lower than MaxValue.");

		var oldValue = CurrentValue;

		BaseValue = Math.Max(BaseValue, Min);

		UpdateCachedValues();

		if (CurrentValue != oldValue)
		{
			OnValueChanged?.Invoke(this, CurrentValue - oldValue);
		}
	}

	internal void SetMaxValue(int newMaxValue)
	{
		Debug.Assert(newMaxValue >= Min, "MaxValue cannot be lower than MinValue.");

		var oldValue = CurrentValue;

		BaseValue = Math.Min(BaseValue, Max);

		UpdateCachedValues();

		if (CurrentValue != oldValue)
		{
			OnValueChanged?.Invoke(this, CurrentValue - oldValue);
		}
	}

	internal void ExecuteOverride(int newValue)
	{
		var oldValue = CurrentValue;

		BaseValue = Math.Clamp(newValue, Min, Max);

		UpdateCachedValues();

		if (CurrentValue != oldValue)
		{
			OnValueChanged?.Invoke(this, CurrentValue - oldValue);
		}
	}

	internal void ExecuteFlatModifier(int value)
	{
		var oldValue = CurrentValue;

		BaseValue = Math.Clamp(BaseValue + value, Min, Max);

		UpdateCachedValues();

		if (CurrentValue != oldValue)
		{
			OnValueChanged?.Invoke(this, CurrentValue - oldValue);
		}
	}

	internal void ExecutePercentModifier(float value)
	{
		var oldValue = CurrentValue;

		BaseValue = Math.Clamp((int)(BaseValue * (1 + value)), Min, Max);

		UpdateCachedValues();

		if (CurrentValue != oldValue)
		{
			OnValueChanged?.Invoke(this, CurrentValue - oldValue);
		}
	}

	internal void AddOverride(int value, int channel)
	{
		var oldValue = CurrentValue;

		_channels[channel].Override = value;

		UpdateCachedValues();

		if (CurrentValue != oldValue)
		{
			OnValueChanged?.Invoke(this, CurrentValue - oldValue);
		}
	}

	internal void ClearOverride(int channel)
	{
		var oldValue = CurrentValue;

		_channels[channel].Override = null;

		UpdateCachedValues();

		if (CurrentValue != oldValue)
		{
			OnValueChanged?.Invoke(this, CurrentValue - oldValue);
		}
	}

	internal void AddFlatModifier(int value, int channel)
	{
		var oldValue = CurrentValue;

		_channels[channel].FlatModifier += value;

		UpdateCachedValues();

		if (CurrentValue != oldValue)
		{
			OnValueChanged?.Invoke(this, CurrentValue - oldValue);
		}
	}

	internal void AddPercentModifier(float value, int channel)
	{
		var oldValue = CurrentValue;

		_channels[channel].PercentModifier += value;

		UpdateCachedValues();

		if (CurrentValue != oldValue)
		{
			OnValueChanged?.Invoke(this, CurrentValue - oldValue);
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
			Overflow = Min - (int)evaluatedValue;
		}
		else
		{
			Overflow = 0;
		}
	}
}
