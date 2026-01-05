// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;

namespace Gamesmiths.Forge.Effects.Calculator;

/// <summary>
/// Abstract class for implementing custom logic for calculating attribute modifiers.
/// </summary>
public abstract class CustomCalculator
{
	/// <summary>
	/// Gets a list of the relevant attributes to be captured and used by this calculator.
	/// </summary>
	public List<AttributeCaptureDefinition> AttributesToCapture { get; } = [];

	/// <summary>
	/// Gets a dictionary of custom cue parameters.
	/// </summary>
	public Dictionary<StringKey, object> CustomCueParameters { get; } = [];

	/// <summary>
	/// Captures the current value of a given <see cref="AttributeCaptureDefinition"/>.
	/// </summary>
	/// <param name="capturedAttribute">Definition for the attribute to be captured.</param>
	/// <param name="effect">The effect which makes use of this custom calculator.</param>
	/// <param name="target">The target of the effect.</param>
	/// <param name="effectEvaluatedData">The evaluated data for the effect.</param>
	/// <param name="calculationType">Which type of calculation to use to capture the magnitude.</param>
	/// <param name="finalChannel">In case <paramref name="calculationType"/> ==
	/// <see cref="AttributeCalculationType.MagnitudeEvaluatedUpToChannel"/> a final channel for the calculation
	/// must be provided.</param>
	/// <returns>The current total value of the captured attribute.</returns>
	protected static int CaptureAttributeMagnitude(
		AttributeCaptureDefinition capturedAttribute,
		Effect effect,
		IForgeEntity? target,
		EffectEvaluatedData? effectEvaluatedData,
		AttributeCalculationType calculationType = AttributeCalculationType.CurrentValue,
		int finalChannel = 0)
	{
		IForgeEntity? captureTarget = capturedAttribute.Source switch
		{
			AttributeCaptureSource.Source => effect.Ownership.Owner,
			AttributeCaptureSource.Target => target,
			_ => null,
		};

		if (captureTarget is null)
		{
			return 0;
		}

		var capturedValue = (int)CaptureAttributeSnapshotAware(
					capturedAttribute,
					calculationType,
					finalChannel,
					captureTarget,
					effectEvaluatedData);

		capturedValue += GetPendingModifierContribution(
			capturedAttribute.Attribute,
			capturedValue,
			effectEvaluatedData);

		return capturedValue;
	}

	private static int GetPendingModifierContribution(
		StringKey attribute,
		int currentValue,
		EffectEvaluatedData? effectEvaluatedData)
	{
		var flatBonus = 0f;
		var percentBonus = 0f;

		if (effectEvaluatedData is null || effectEvaluatedData.ModifiersEvaluatedData is null)
		{
			return 0;
		}

		foreach (ModifierEvaluatedData modifier in effectEvaluatedData.ModifiersEvaluatedData)
		{
			if (modifier.Attribute.Key != attribute)
			{
				continue;
			}

			switch (modifier.ModifierOperation)
			{
				case ModifierOperation.FlatBonus:
					flatBonus += modifier.Magnitude;
					break;
				case ModifierOperation.PercentBonus:
					percentBonus += modifier.Magnitude;
					break;
				case ModifierOperation.Override:
					return (int)modifier.Magnitude - currentValue;
			}
		}

		// Apply flat first, then percent (matching the attribute calculation order)
		var withFlat = currentValue + flatBonus;
		var withPercent = withFlat * (1 + percentBonus);

		return (int)(withPercent - currentValue);
	}

	private static float CaptureAttributeSnapshotAware(
		AttributeCaptureDefinition capturedAttribute,
		AttributeCalculationType calculationType,
		int finalChannel,
		IForgeEntity? sourceEntity,
		EffectEvaluatedData? effectEvaluatedData)
	{
		if (sourceEntity?.Attributes.ContainsAttribute(capturedAttribute.Attribute) != true)
		{
			return 0f;
		}

		EntityAttribute attribute = sourceEntity.Attributes[capturedAttribute.Attribute];

		if (!capturedAttribute.Snapshot || effectEvaluatedData is null)
		{
			return CaptureMagnitudeValue(attribute, calculationType, finalChannel);
		}

		var key = new AttributeSnapshotKey(
			capturedAttribute.Attribute,
			capturedAttribute.Source,
			calculationType,
			finalChannel);

		if (effectEvaluatedData.SnapshotAttributes.TryGetValue(key, out var cachedValue))
		{
			return cachedValue;
		}

		var currentValue = CaptureMagnitudeValue(attribute, calculationType, finalChannel);
		effectEvaluatedData.SnapshotAttributes[key] = currentValue;
		return currentValue;
	}

	private static int CaptureMagnitudeValue(
		EntityAttribute attribute,
		AttributeCalculationType calculationType,
		int finalChannel)
	{
		return calculationType switch
		{
			AttributeCalculationType.CurrentValue => attribute.CurrentValue,
			AttributeCalculationType.BaseValue => attribute.BaseValue,
			AttributeCalculationType.Modifier => attribute.Modifier,
			AttributeCalculationType.Overflow => attribute.Overflow,
			AttributeCalculationType.ValidModifier => attribute.ValidModifier,
			AttributeCalculationType.Min => attribute.Min,
			AttributeCalculationType.Max => attribute.Max,
			AttributeCalculationType.MagnitudeEvaluatedUpToChannel =>
				(int)attribute.CalculateMagnitudeUpToChannel(finalChannel),
			_ => 0,
		};
	}
}
