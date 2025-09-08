// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Calculator;

namespace Gamesmiths.Forge.Effects.Magnitudes;

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
/// <param name="MagnitudeCalculatorClass">The magnitude calculator class to be used for magnitude calculation.</param>
/// <param name="Coefficient">Value to be multiplied with the calculcuated magnitude.</param>
/// <param name="PreMultiplyAdditiveValue">Value to be added to the magnitude before multiplying the coeficient.</param>
/// <param name="PostMultiplyAdditiveValue">Value to be added to the magnitude after multiplying the coeficient.</param>
/// <param name="LookupCurve">If provided, the final evaluated magnitude will be used as a lookup into this curve.
/// </param>
public readonly record struct CustomCalculationBasedFloat(
	CustomModifierMagnitudeCalculator MagnitudeCalculatorClass,
	ScalableFloat Coefficient,
	ScalableFloat PreMultiplyAdditiveValue,
	ScalableFloat PostMultiplyAdditiveValue,
	ICurve? LookupCurve = null)
{
	/// <summary>
	/// Calculates the final magnitude based on the CustomCalculationBasedFloat configurations.
	/// </summary>
	/// <param name="effect">The source effect that will be used for calculating this magnitude.</param>
	/// <param name="target">The target of the effect to be used for calcuilating this magnitude.</param>
	/// <param name="level">Level to use in the final magnitude calculation.</param>
	/// <returns>The calculated magnitude for this <see cref="CustomCalculationBasedFloat"/>.</returns>
	public float CalculateMagnitude(in Effect effect, IForgeEntity target, int level)
	{
		var baseMagnitude = MagnitudeCalculatorClass.CalculateBaseMagnitude(effect, target);

		var finalMagnitude = (Coefficient.GetValue(level) * (PreMultiplyAdditiveValue.GetValue(level) + baseMagnitude))
			+ PostMultiplyAdditiveValue.GetValue(level);

		if (LookupCurve is not null)
		{
			finalMagnitude = LookupCurve.Evaluate(finalMagnitude);
		}

		return finalMagnitude;
	}
}
