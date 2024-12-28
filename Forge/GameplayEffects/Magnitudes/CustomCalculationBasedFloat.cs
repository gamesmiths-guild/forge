// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.GameplayEffects.Magnitudes;

/// <summary>
/// A custom calculation based float is a magnitude which supports a custom programmable class to be used to calculate
/// the final magnitude.
/// </summary>
/// <remarks>
/// After aquiring the custom calculated magnitude, it's still processed through the formula:
/// <code>
/// finalValue = (coefficient * (calculatedMagnitude + preMultiply)) + postMultiply;
/// </code>
/// </remarks>
/// <param name="magnitudeCalculatorClass">The magnitude calculator class to be used for magnitude calculation.</param>
/// <param name="coefficient">Value to be multiplied with the calculcuated magnitude.</param>
/// <param name="preMultiplyAdditiveValue">Value to be added to the magnitude before multiplying the coeficient.</param>
/// <param name="postMultiplyAdditiveValue">Value to be added to the magnitude after multiplying the coeficient.</param>
/// <param name="lookupCurve">If provided, the final evaluated magnitude will be used as a lookup into this curve.
/// </param>
public readonly struct CustomCalculationBasedFloat(
	IMagnitudeCalculator magnitudeCalculatorClass,
	ScalableFloat coefficient,
	ScalableFloat preMultiplyAdditiveValue,
	ScalableFloat postMultiplyAdditiveValue,
	Curve? lookupCurve = null) : IEquatable<CustomCalculationBasedFloat>
{
	/// <summary>
	/// Gets the magnitude calculator class used to calculate the magnitude.
	/// </summary>
	public IMagnitudeCalculator MagnitudeCalculatorClass { get; } = magnitudeCalculatorClass;

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
	/// Gets the curve entry to use as a lookup instead of directly using the evaluated magnitude.
	/// </summary>
	public Curve? LookupCurve { get; } = lookupCurve;

	/// <summary>
	/// Calculates the final magnitude based on the CustomCalculationBasedFloat configurations.
	/// </summary>
	/// <param name="effect">The source effect that will be used for calculating this magnitude.</param>
	/// <param name="level">Level to use in the final magnitude calculation.</param>
	/// <returns>The calculated magnitude for this <see cref="CustomCalculationBasedFloat"/>.</returns>
	public float CalculateMagnitude(in GameplayEffect effect, int level)
	{
		var baseMagnitude = MagnitudeCalculatorClass.CalculateBaseMagnitude(effect);

		var finalMagnitude = (Coefficient.GetValue(level) * (PreMultiplyAdditiveValue.GetValue(level) + baseMagnitude))
			+ PostMultiplyAdditiveValue.GetValue(level);

		if (LookupCurve.HasValue)
		{
			finalMagnitude = LookupCurve.Value.Evaluate(finalMagnitude);
		}

		return finalMagnitude;
	}

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		var hash = default(HashCode);
		hash.Add(MagnitudeCalculatorClass);
		hash.Add(Coefficient);
		hash.Add(PreMultiplyAdditiveValue);
		hash.Add(PostMultiplyAdditiveValue);

		return hash.ToHashCode();
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		if (obj is CustomCalculationBasedFloat other)
		{
			return Equals(other);
		}

		return false;
	}

	/// <inheritdoc/>
	public bool Equals(CustomCalculationBasedFloat other)
	{
		return MagnitudeCalculatorClass.Equals(other.MagnitudeCalculatorClass)
			&& Coefficient.Equals(other.Coefficient)
			&& PreMultiplyAdditiveValue.Equals(other.PreMultiplyAdditiveValue)
			&& PostMultiplyAdditiveValue.Equals(other.PostMultiplyAdditiveValue);
	}

	/// <summary>
	/// Determines if two <see cref="CustomCalculationBasedFloat"/> objects are equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="CustomCalculationBasedFloat"/> to compare.</param>
	/// <param name="rhs">The second <see cref="CustomCalculationBasedFloat"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are equal;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(CustomCalculationBasedFloat lhs, CustomCalculationBasedFloat rhs)
	{
		return lhs.Equals(rhs);
	}

	/// <summary>
	/// Determines if two <see cref="CustomCalculationBasedFloat"/> objects are not equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="CustomCalculationBasedFloat"/> to compare.</param>
	/// <param name="rhs">The second <see cref="CustomCalculationBasedFloat"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are not
	/// equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(CustomCalculationBasedFloat lhs, CustomCalculationBasedFloat rhs)
	{
		return !(lhs == rhs);
	}
}
