// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Effects.Magnitudes;

/// <summary>
/// An attribute based float is a magnitude value that's associates an attribute from an entity. It's used in the
/// general form of:
/// <code>
/// finalValue = (coefficient * (attributeMagnitude + preMultiply)) + postMultiply;
/// </code>
/// considering some other minor configurations.
/// <para>The captured attribute can also be a snapshot at the moment of creation or updated whenever the target
/// attribute updates its value.</para>
/// </summary>
/// <param name="BackingAttribute">Information about which and attribute should be captured and how.</param>
/// <param name="AttributeCalculationType">How the magnitude is going to be extracted from the given attribute.</param>
/// <param name="Coefficient">Value to be multiplied with the calculated magnitude.</param>
/// <param name="PreMultiplyAdditiveValue">Value to be added to the magnitude before multiplying the coefficient.
/// </param>
/// <param name="PostMultiplyAdditiveValue">Value to be added to the magnitude after multiplying the coefficient.
/// </param>
/// <param name="FinalChannel">In case <paramref name="AttributeCalculationType"/> ==
/// <see cref="AttributeCalculationType.MagnitudeEvaluatedUpToChannel"/> a final channel for the calculation
/// must be provided.</param>
/// <param name="LookupCurve">If provided, the final evaluated magnitude will be used as a lookup into this curve.
/// </param>
public readonly record struct AttributeBasedFloat(
	AttributeCaptureDefinition BackingAttribute,
	AttributeCalculationType AttributeCalculationType,
	ScalableFloat Coefficient,
	ScalableFloat PreMultiplyAdditiveValue,
	ScalableFloat PostMultiplyAdditiveValue,
	int FinalChannel = 0,
	ICurve? LookupCurve = null)
{
	/// <summary>
	/// Calculates the final magnitude based on the AttributeBasedFloat configurations.
	/// </summary>
	/// <param name="effect">The source effect that will be used to capture source attributes from.</param>
	/// <param name="target">The target entity that will be used to capture source attributes from.</param>
	/// <param name="level">Level to use in the magnitude calculation.</param>
	/// <returns>The calculated magnitude for this <see cref="AttributeBasedFloat"/>.</returns>
	public readonly float CalculateMagnitude(Effect effect, IForgeEntity target, int level)
	{
		EntityAttribute? attribute = null;

		switch (BackingAttribute.Source)
		{
			case AttributeCaptureSource.Source:

				if (effect.Ownership.Owner?.Attributes.ContainsAttribute(BackingAttribute.Attribute) != true)
				{
					break;
				}

				attribute = effect.Ownership.Owner.Attributes[BackingAttribute.Attribute];
				break;

			case AttributeCaptureSource.Target:

				if (!target.Attributes.ContainsAttribute(BackingAttribute.Attribute))
				{
					break;
				}

				attribute = target.Attributes[BackingAttribute.Attribute];
				break;
		}

		float magnitude = 0;

		if (attribute is not null)
		{
			switch (AttributeCalculationType)
			{
				case AttributeCalculationType.CurrentValue:
					magnitude = attribute.CurrentValue;
					break;

				case AttributeCalculationType.BaseValue:
					magnitude = attribute.BaseValue;
					break;

				case AttributeCalculationType.Modifier:
					magnitude = attribute.Modifier;
					break;

				case AttributeCalculationType.Overflow:
					magnitude = attribute.Overflow;
					break;

				case AttributeCalculationType.ValidModifier:
					magnitude = attribute.ValidModifier;
					break;

				case AttributeCalculationType.Min:
					magnitude = attribute.Min;
					break;

				case AttributeCalculationType.Max:
					magnitude = attribute.Max;
					break;

				case AttributeCalculationType.MagnitudeEvaluatedUpToChannel:
					magnitude = attribute.CalculateMagnitudeUpToChannel(FinalChannel);
					break;
			}
		}

		var finalMagnitude = (Coefficient.GetValue(level) * (PreMultiplyAdditiveValue.GetValue(level) + magnitude))
			+ PostMultiplyAdditiveValue.GetValue(level);

		if (LookupCurve is not null)
		{
			finalMagnitude = LookupCurve.Evaluate(finalMagnitude);
		}

		return finalMagnitude;
	}
}
