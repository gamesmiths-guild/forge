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
	internal float CalculateMagnitude(
		in Effect effect,
		IForgeEntity target,
		int level,
		EffectEvaluatedData? effectEvaluatedData)
	{
		var baseMagnitude = MagnitudeCalculatorClass.CalculateBaseMagnitude(effect, target, effectEvaluatedData);

		var finalMagnitude = (Coefficient.GetValue(level) * (PreMultiplyAdditiveValue.GetValue(level) + baseMagnitude))
			+ PostMultiplyAdditiveValue.GetValue(level);

		if (LookupCurve is not null)
		{
			finalMagnitude = LookupCurve.Evaluate(finalMagnitude);
		}

		return finalMagnitude;
	}
}
