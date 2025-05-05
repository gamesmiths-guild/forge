// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Represents a key into a <see cref="Curve"/>.
/// </summary>
/// <param name="time">The time of the curve key.</param>
/// <param name="value">The value of the curve key.</param>
public readonly struct CurveKey(float time, float value) : IEquatable<CurveKey>
{
	/// <summary>
	/// Gets the time of this curve key.
	/// </summary>
	public float Time { get; } = time;

	/// <summary>
	/// Gets the value of this curve key.
	/// </summary>
	public float Value { get; } = value;

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		var hash = default(HashCode);
		hash.Add(Time);
		hash.Add(Value);
		return hash.ToHashCode();
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		if (obj is CurveKey other)
		{
			return Equals(other);
		}

		return false;
	}

	/// <inheritdoc/>
	public bool Equals(CurveKey other)
	{
#pragma warning disable S1244 // Floating point numbers should not be tested for equality
		return Time == other.Time
			&& Value.Equals(other.Value);
#pragma warning restore S1244 // Floating point numbers should not be tested for equality
	}

	/// <summary>
	/// Determines if two <see cref="CurveKey"/> objects are equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="CurveKey"/> to compare.</param>
	/// <param name="rhs">The second <see cref="CurveKey"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are equal;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(CurveKey lhs, CurveKey rhs)
	{
		return lhs.Equals(rhs);
	}

	/// <summary>
	/// Determines if two <see cref="CurveKey"/> objects are not equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="CurveKey"/> to compare.</param>
	/// <param name="rhs">The second <see cref="CurveKey"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are not
	/// equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(CurveKey lhs, CurveKey rhs)
	{
		return !(lhs == rhs);
	}
}
