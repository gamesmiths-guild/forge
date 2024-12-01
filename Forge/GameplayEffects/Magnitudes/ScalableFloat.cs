// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.GameplayEffects.Magnitudes;

/// <summary>
/// A scalable float is a magnitude value that associates a float with a curve that can be evaluated and scaled over
/// time.
/// </summary>
/// <param name="baseValue">The base value for this magnitude.</param>
/// <param name="curve">The curve used for scaling.</param>
public readonly struct ScalableFloat(float baseValue, Curve curve)
{
	/// <summary>
	/// Gets the base value for this scalable float.
	/// </summary>
	public float BaseValue { get; } = baseValue;

	/// <summary>
	/// Gets the curve fort this scalable float.
	/// </summary>
	public Curve ScalingCurve { get; } = curve;

	/// <summary>
	/// Gets an evaluated value for this scalable float at the given time.
	/// </summary>
	/// <param name="time">The time for evaluation.</param>
	/// <returns>The evaluated value at the given time.</returns>
	public float GetValue(float time)
	{
		var scalingFactor = ScalingCurve.Evaluate(time);
		return BaseValue * scalingFactor;
	}
}
