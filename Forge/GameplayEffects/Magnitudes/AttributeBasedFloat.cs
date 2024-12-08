// Copyright © 2024 Gamesmiths Guild.

// Copyright © 2024 Gamesmiths Guild.
using System.Diagnostics;
using Gamesmiths.Forge.Core;
using Attribute = Gamesmiths.Forge.Attributes.Attribute;

namespace Gamesmiths.Forge.GameplayEffects.Magnitudes;

/// <summary>
/// An attribute based float is a magnitude value thats associates an attribute from an entity. It's used in the general
/// form of:
/// <code>
/// finalValue = (coefficient * (attributeMagnitude + preMultiply)) + postMultiply;
/// </code>
/// considering some other minor configurations.
/// <para>The captured attribute can also be a snapshot at the moment of creation or updated whenever the target
/// attribute updates its value.</para>
/// </summary>
/// <param name="backingAttribute">Information about which and attribute should be captured and how.</param>
/// <param name="attributeCalculationType">How the magnitude is going to be extracted from the given attribute.</param>
/// <param name="coefficient">Value to be multiplied with the calculcuated magnitude.</param>
/// <param name="preMultiplyAdditiveValue">Value to be added to the magnitude before multiplying the coeficient.</param>
/// <param name="postMultiplyAdditiveValue">Value to be added to the magnitude after multiplying the coeficient.</param>
/// <param name="finalChannel">In case <paramref name="attributeCalculationType"/> ==
/// <see cref="AttributeBasedFloatCalculationType.AttributeMagnitudeEvaluatedUpToChannel"/> a final channel for the
/// calculation must be provided.</param>
public readonly struct AttributeBasedFloat(
	AttributeCaptureDefinition backingAttribute,
	AttributeBasedFloatCalculationType attributeCalculationType,
	ScalableFloat coefficient,
	ScalableFloat preMultiplyAdditiveValue,
	ScalableFloat postMultiplyAdditiveValue,
	int? finalChannel = null) : IEquatable<AttributeBasedFloat>
{
	/// <summary>
	/// Gets the capture definition for the backing attribute.
	/// </summary>
	public AttributeCaptureDefinition BackingAttribute { get; } = backingAttribute;

	/// <summary>
	/// Gets the calculation type used to capture the base magnitude.
	/// </summary>
	public AttributeBasedFloatCalculationType AttributeCalculationType { get; } = attributeCalculationType;

	/// <summary>
	/// Gets the coeficient to be multiplied with the captured magnitude.
	/// </summary>
	public ScalableFloat Coefficient { get; } = coefficient;

	/// <summary>
	/// Gets the value to be added to the captured magnitude before multiplying the coefficient.
	/// </summary>
	public ScalableFloat PreMultiplyAdditiveValue { get; } = preMultiplyAdditiveValue;

	/// <summary>
	/// Gets the value to be added to the captured magnitude after multiplying the coefficient.
	/// </summary>
	public ScalableFloat PostMultiplyAdditiveValue { get; } = postMultiplyAdditiveValue;

	/// <summary>
	/// Gets a value for the final channel when capturing the magnitude using
	/// <see cref="AttributeBasedFloatCalculationType.AttributeMagnitudeEvaluatedUpToChannel"/>.
	/// </summary>
	public int? FinalChannel { get; } = finalChannel;

	/// <summary>
	/// Calculates the final magnitude based on the AttributeBasedFloat configurations.
	/// </summary>
	/// <param name="effect">The source effect that will be used to capture source attributes from.</param>
	/// <param name="target">The target enity that will be used to capture source attributes from.</param>
	/// <param name="level">Level to use in the magnitude calculation.</param>
	/// <returns>The calculated magnitude for this <see cref="AttributeBasedFloat"/>.</returns>
	public readonly float CalculateMagnitude(GameplayEffect effect, IForgeEntity target, int level)
	{
		Attribute? attribute = null;

		switch (BackingAttribute.Source)
		{
			case AttributeCaptureSource.Source:
				attribute = effect.Ownership.Owner.Attributes[BackingAttribute.Attribute];
				break;

			case AttributeCaptureSource.Target:
				attribute = target.Attributes[BackingAttribute.Attribute];
				break;
		}

		if (attribute is null)
		{
			return 0f;
		}

		float magnitude = 0;

		switch (AttributeCalculationType)
		{
			case AttributeBasedFloatCalculationType.AttributeMagnitude:
				magnitude = attribute.CurrentValue;
				break;

			case AttributeBasedFloatCalculationType.AttributeBaseValue:
				magnitude = attribute.BaseValue;
				break;

			case AttributeBasedFloatCalculationType.AttributeModifierMagnitude:
				magnitude = attribute.Modifier - attribute.Overflow;
				break;

			case AttributeBasedFloatCalculationType.AttributeMagnitudeEvaluatedUpToChannel:
				Debug.Assert(
					FinalChannel.HasValue,
					"FinalChannel must be defined when AttributeCalculationType == AttributeMagnitudeEvaluatedUpToChannel");
				magnitude = attribute.CalculateMagnitudeUpToChannel(FinalChannel.Value);
				break;
		}

		return (Coefficient.GetValue(level) * (PreMultiplyAdditiveValue.GetValue(level) + magnitude))
			+ PostMultiplyAdditiveValue.GetValue(level);
	}

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		var hash = default(HashCode);
		hash.Add(BackingAttribute);
		hash.Add(AttributeCalculationType);
		hash.Add(Coefficient);
		hash.Add(PreMultiplyAdditiveValue);
		hash.Add(PostMultiplyAdditiveValue);
		hash.Add(FinalChannel);

		return hash.ToHashCode();
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		if (obj is AttributeBasedFloat other)
		{
			return Equals(other);
		}

		return false;
	}

	/// <inheritdoc/>
	public bool Equals(AttributeBasedFloat other)
	{
		return BackingAttribute.Equals(other.BackingAttribute)
			&& AttributeCalculationType.Equals(other.AttributeCalculationType)
			&& Coefficient.Equals(other.Coefficient)
			&& PreMultiplyAdditiveValue.Equals(other.PreMultiplyAdditiveValue)
			&& PostMultiplyAdditiveValue.Equals(other.PostMultiplyAdditiveValue)
			&& Nullable.Equals(FinalChannel, other.FinalChannel);
	}

	/// <summary>
	/// Determines if two <see cref="AttributeBasedFloat"/> objects are equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="AttributeBasedFloat"/> to compare.</param>
	/// <param name="rhs">The second <see cref="AttributeBasedFloat"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are equal;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(AttributeBasedFloat lhs, AttributeBasedFloat rhs)
	{
		return lhs.Equals(rhs);
	}

	/// <summary>
	/// Determines if two <see cref="AttributeBasedFloat"/> objects are not equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="AttributeBasedFloat"/> to compare.</param>
	/// <param name="rhs">The second <see cref="AttributeBasedFloat"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are not
	/// equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(AttributeBasedFloat lhs, AttributeBasedFloat rhs)
	{
		return !(lhs == rhs);
	}
}
