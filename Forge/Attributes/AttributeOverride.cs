// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects.Modifiers;

namespace Gamesmiths.Forge.Attributes;

/// <summary>
/// Represents the data for an attribute override.
/// </summary>
/// <param name="Magnitude">The magnitude of the override.</param>
/// <param name="Channel">The channel in which the override is applied.</param>
/// <param name="AggregationMode">How this override arbitrates against the other overrides in the same channel.</param>
public readonly record struct AttributeOverride(
	int Magnitude,
	int Channel,
	AggregationMode AggregationMode = AggregationMode.Sum);
