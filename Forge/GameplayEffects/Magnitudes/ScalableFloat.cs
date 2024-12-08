// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.GameplayEffects.Magnitudes;

/// <summary>
/// A scalable float is a magnitude value that associates a float with a curve that can be evaluated and scaled over
/// time.
/// </summary>
/// <param name="baseValue">The base value for this magnitude.</param>
/// <param name="curve">The curve used for scaling.</param>
public readonly struct ScalableFloat(float baseValue, Curve curve) : IEquatable<ScalableFloat>
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
		if (obj is ScalableFloat other)
		{
			return Equals(other);
		}

		return false;
	}

	/// <inheritdoc/>
	public bool Equals(ScalableFloat other)
	{
#pragma warning disable S1244 // Floating point numbers should not be tested for equality
		return BaseValue == other.BaseValue
			&& ScalingCurve.Equals(other.ScalingCurve);
#pragma warning restore S1244 // Floating point numbers should not be tested for equality
	}

	/// <summary>
	/// Determines if two <see cref="ScalableFloat"/> objects are equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="ScalableFloat"/> to compare.</param>
	/// <param name="rhs">The second <see cref="ScalableFloat"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are equal;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(ScalableFloat lhs, ScalableFloat rhs)
	{
		return lhs.Equals(rhs);
	}

	/// <summary>
	/// Determines if two <see cref="ScalableFloat"/> objects are not equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="ScalableFloat"/> to compare.</param>
	/// <param name="rhs">The second <see cref="ScalableFloat"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are not
	/// equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(ScalableFloat lhs, ScalableFloat rhs)
	{
		return !(lhs == rhs);
	}
}
