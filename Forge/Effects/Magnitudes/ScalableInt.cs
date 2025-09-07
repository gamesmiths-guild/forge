// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Effects.Magnitudes;

/// <summary>
/// A scalable int is a magnitude value that associates an integer with a curve that can be evaluated and scaled over
/// time.
/// </summary>
/// <param name="BaseValue">The base value for this magnitude.</param>
/// <param name="ScalingCurve">The curve used for scaling.</param>
public readonly record struct ScalableInt(int BaseValue, ICurve? ScalingCurve = null)
{
	/// <summary>
	/// Gets an evaluated value for this scalable int at the given time.
	/// </summary>
	/// <param name="time">The time for evaluation.</param>
	/// <returns>The evaluated value at the given time.</returns>
	public readonly int GetValue(float time)
	{
		if (ScalingCurve is null)
		{
			return BaseValue;
		}

		var scalingFactor = ScalingCurve.Evaluate(time);
		return (int)(BaseValue * scalingFactor);
	}
}
