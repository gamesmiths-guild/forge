// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.GameplayEffects.Magnitudes;

/// <summary>
/// A scalable int is a magnitude value that associates an integer with a curve that can be evaluated and scaled over
/// time.
/// </summary>
/// <param name="baseValue">The base value for this magnitude.</param>
/// <param name="curve">The curve used for scaling.</param>
public readonly struct ScalableInt(int baseValue, Curve curve = default) : IEquatable<ScalableInt>
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

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		var hash = default(HashCode);
		hash.Add(BaseValue);
		hash.Add(ScalingCurve);
		return hash.ToHashCode();
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		if (obj is ScalableInt other)
		{
			return Equals(other);
		}

		return false;
	}

	/// <inheritdoc/>
	public bool Equals(ScalableInt other)
	{
		return BaseValue == other.BaseValue
			&& ScalingCurve.Equals(other.ScalingCurve);
	}

	/// <summary>
	/// Determines if two <see cref="ScalableInt"/> objects are equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="ScalableInt"/> to compare.</param>
	/// <param name="rhs">The second <see cref="ScalableInt"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are equal;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(ScalableInt lhs, ScalableInt rhs)
	{
		return lhs.Equals(rhs);
	}

	/// <summary>
	/// Determines if two <see cref="ScalableInt"/> objects are not equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="ScalableInt"/> to compare.</param>
	/// <param name="rhs">The second <see cref="ScalableInt"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are not
	/// equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(ScalableInt lhs, ScalableInt rhs)
	{
		return !(lhs == rhs);
	}
}
