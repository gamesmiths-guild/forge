// Copyright © 2024 Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Initializes a new instance of the <see cref="Curve"/> class.
/// </summary>
/// <param name="keys">The keys for the curve.</param>
public readonly struct Curve(CurveKey[] keys) : IEquatable<Curve>
{
	private readonly CurveKey[] _keys = [.. keys.OrderBy(x => x.Time)];

	/// <summary>
	/// Evaluates the curve at a given time.
	/// </summary>
	/// <param name="time">The time for evaluation.</param>
	/// <returns>The evaluated value.</returns>
	public float Evaluate(float time)
	{
		if (_keys.Length == 0)
		{
			return 1.0f; // Default scaling factor if no keys are defined
		}

		if (time <= _keys[0].Time)
		{
			return _keys[0].Value;
		}

		if (time >= _keys[^1].Time)
		{
			return _keys[^1].Value;
		}

		for (var i = 0; i < _keys.Length - 1; i++)
		{
			if (time >= _keys[i].Time && time <= _keys[i + 1].Time)
			{
				var t = (time - _keys[i].Time) / (_keys[i + 1].Time - _keys[i].Time);
				return _keys[i].Value + (t * (_keys[i + 1].Value - _keys[i].Value));
			}
		}

		return 1.0f; // Fallback
	}

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		var hash = default(HashCode);
		foreach (CurveKey key in _keys)
		{
			hash.Add(key);
		}

		return hash.ToHashCode();
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		if (obj is Curve other)
		{
			return Equals(other);
		}

		return false;
	}

	/// <inheritdoc/>
	public bool Equals(Curve other)
	{
		return _keys.SequenceEqual(other._keys);
	}

	/// <summary>
	/// Determines if two <see cref="Curve"/> objects are equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="Curve"/> to compare.</param>
	/// <param name="rhs">The second <see cref="Curve"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are equal;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(Curve lhs, Curve rhs)
	{
		return lhs.Equals(rhs);
	}

	/// <summary>
	/// Determines if two <see cref="Curve"/> objects are not equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="Curve"/> to compare.</param>
	/// <param name="rhs">The second <see cref="Curve"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are not
	/// equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(Curve lhs, Curve rhs)
	{
		return !(lhs == rhs);
	}
}
