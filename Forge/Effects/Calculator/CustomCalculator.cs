// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Magnitudes;

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
	/// <param name="calculationType">Which type of calculation to use to capture the magnitude.</param>
	/// <param name="finalChannel">In case <paramref name="calculationType"/> ==
	/// <see cref="AttributeCalculationType.MagnitudeEvaluatedUpToChannel"/> a final channel for the calculation
	/// must be provided.</param>
	/// <returns>The current total value of the captured attribute.</returns>
	protected static int CaptureAttributeMagnitude(
		AttributeCaptureDefinition capturedAttribute,
		Effect effect,
		IForgeEntity? target,
		AttributeCalculationType calculationType = AttributeCalculationType.CurrentValue,
		int finalChannel = 0)
	{
		switch (capturedAttribute.Source)
		{
			case AttributeCaptureSource.Source:

				if (effect.Ownership.Owner?.Attributes.ContainsAttribute(capturedAttribute.Attribute) != true)
				{
					return 0;
				}

				return CaptureMagnitudeValue(
					effect.Ownership.Owner.Attributes[capturedAttribute.Attribute],
					calculationType,
					finalChannel);

			case AttributeCaptureSource.Target:

				if (target?.Attributes.ContainsAttribute(capturedAttribute.Attribute) != true)
				{
					return 0;
				}

				return CaptureMagnitudeValue(
					target.Attributes[capturedAttribute.Attribute],
					calculationType,
					finalChannel);
		}

		return 0;
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
			_ => throw new ArgumentOutOfRangeException(nameof(calculationType), calculationType, null),
		};
	}
}
