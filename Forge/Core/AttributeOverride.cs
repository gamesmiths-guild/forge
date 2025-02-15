// Copyright © 2024 Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Represents the data for an attribute override.
/// </summary>
/// <param name="magnitude">The magnitude of the override.</param>
/// <param name="channel">The channel in which the override is applied.</param>
public class AttributeOverride(int magnitude, int channel)
{
	/// <summary>
	/// Gets the override magnitude.
	/// </summary>
	public int Magnitude { get; } = magnitude;

	/// <summary>
	/// Gets the target channel for the override.
	/// </summary>
	public int Channel { get; } = channel;
}
