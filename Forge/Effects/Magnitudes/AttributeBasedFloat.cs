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
	/// <param name="snapshotAttributes">The dictionary containing already captured snapshot attributes for this effect.
	/// </param>
	/// <returns>The calculated magnitude for this <see cref="AttributeBasedFloat"/>.</returns>
	public readonly float CalculateMagnitude(
		Effect effect,
		IForgeEntity target,
		int level,
		Dictionary<AttributeSnapshotKey, float> snapshotAttributes)
	{
		float magnitude = 0;

		switch (BackingAttribute.Source)
		{
			case AttributeCaptureSource.Source:
				magnitude = CaptureAttributeSnapshotAware(effect.Ownership.Owner, snapshotAttributes);
				break;

			case AttributeCaptureSource.Target:
				magnitude = CaptureAttributeSnapshotAware(target, snapshotAttributes);
				break;
		}

		var finalMagnitude = (Coefficient.GetValue(level) * (PreMultiplyAdditiveValue.GetValue(level) + magnitude))
			+ PostMultiplyAdditiveValue.GetValue(level);

		if (LookupCurve is not null)
		{
			finalMagnitude = LookupCurve.Evaluate(finalMagnitude);
		}

		return finalMagnitude;
	}

	private float CaptureAttributeSnapshotAware(
		IForgeEntity? sourceEntity,
		Dictionary<AttributeSnapshotKey, float> snapshotAttributes)
	{
		if (sourceEntity?.Attributes.ContainsAttribute(BackingAttribute.Attribute) != true)
		{
			return 0f;
		}

		EntityAttribute attribute = sourceEntity.Attributes[BackingAttribute.Attribute];

		if (!BackingAttribute.Snapshot)
		{
			return CaptureNow(attribute);
		}

		var key = new AttributeSnapshotKey(
			BackingAttribute.Attribute,
			BackingAttribute.Source,
			AttributeCalculationType,
			FinalChannel);

		if (snapshotAttributes.TryGetValue(key, out var cachedValue))
		{
			return cachedValue;
		}

		var currentValue = CaptureNow(attribute);
		snapshotAttributes[key] = currentValue;
		return currentValue;
	}

	private int CaptureNow(EntityAttribute attribute)
	{
		return AttributeCalculationType switch
		{
			AttributeCalculationType.CurrentValue => attribute.CurrentValue,
			AttributeCalculationType.BaseValue => attribute.BaseValue,
			AttributeCalculationType.Modifier => attribute.Modifier,
			AttributeCalculationType.Overflow => attribute.Overflow,
			AttributeCalculationType.ValidModifier => attribute.ValidModifier,
			AttributeCalculationType.Min => attribute.Min,
			AttributeCalculationType.Max => attribute.Max,
			AttributeCalculationType.MagnitudeEvaluatedUpToChannel =>
				(int)attribute.CalculateMagnitudeUpToChannel(FinalChannel),
			_ => 0,
		};
	}
}
