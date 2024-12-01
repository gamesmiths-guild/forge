// Copyright © 2024 Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Represents a key into a <see cref="Curve"/>.
/// </summary>
/// <param name="time">The time of the curve key.</param>
/// <param name="value">The value of the curve key.</param>
public readonly struct CurveKey(float time, float value)
{
	/// <summary>
	/// Gets the time of this curve key.
	/// </summary>
	public float Time { get; } = time;

	/// <summary>
	/// Gets the value of this curve key.
	/// </summary>
	public float Value { get; } = value;
}
