// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Cues;

/// <summary>
/// Contains parameters that can be passed to Cues.
/// </summary>
/// <param name="Magnitude">The magnitude for this cue.</param>
/// <param name="NormalizedMagnitude">The normalized magnitude for this cue between 0 and 1.</param>
/// <param name="Source">The source of the cue.</param>
/// <param name="CustomParameters">Additional custom parameters that can be passed to the cue.</param>
public readonly record struct CueParameters(
	int Magnitude,
	float NormalizedMagnitude,
	IForgeEntity? Source = null,
	Dictionary<StringKey, object>? CustomParameters = null);
