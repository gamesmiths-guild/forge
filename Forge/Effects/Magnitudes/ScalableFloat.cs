// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Effects.Magnitudes;

/// <summary>
/// A scalable float is a magnitude value that associates a float with a curve that can be evaluated and scaled over
/// time.
/// </summary>
/// <param name="BaseValue">The base value for this magnitude.</param>
/// <param name="ScalingCurve">The curve used for scaling.</param>
public readonly record struct ScalableFloat(float BaseValue, ICurve? ScalingCurve = null)
{
	/// <summary>
	/// Gets an evaluated value for this scalable float at the given time.
	/// </summary>
	/// <param name="time">The time for evaluation.</param>
	/// <returns>The evaluated value at the given time.</returns>
	public float GetValue(float time)
	{
		if (ScalingCurve is null)
		{
			return BaseValue;
		}

		var scalingFactor = ScalingCurve.Evaluate(time);
		return BaseValue * scalingFactor;
	}
}
