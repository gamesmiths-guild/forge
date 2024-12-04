// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.GameplayEffects.Magnitudes;

/// <summary>
/// A scalable int is a magnitude value that associates an integer with a curve that can be evaluated and scaled over
/// time.
/// </summary>
/// <param name="baseValue">The base value for this magnitude.</param>
/// <param name="curve">The curve used for scaling.</param>
public readonly struct ScalableInt(int baseValue, Curve curve)
{
	/// <summary>
	/// Gets the base value for this scalable int.
	/// </summary>
	public int BaseValue { get; } = baseValue;

	/// <summary>
	/// Gets the curve fort this scalable int.
	/// </summary>
	public Curve ScalingCurve { get; } = curve;

	/// <summary>
	/// Gets an evaluated value for this scalable int at the given time.
	/// </summary>
	/// <param name="time">The time for evaluation.</param>
	/// <returns>The evaluated value at the given time.</returns>
	public readonly int GetValue(float time)
	{
		var scalingFactor = ScalingCurve.Evaluate(time);
		return (int)(BaseValue * scalingFactor);
	}
}