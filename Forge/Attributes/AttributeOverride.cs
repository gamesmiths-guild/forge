// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Attributes;

/// <summary>
/// Represents the data for an attribute override.
/// </summary>
/// <param name="Magnitude">The magnitude of the override.</param>
/// <param name="Channel">The channel in which the override is applied.</param>
public readonly record struct AttributeOverride(int Magnitude, int Channel);
