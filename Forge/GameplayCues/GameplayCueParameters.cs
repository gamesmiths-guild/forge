// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.GameplayCues;

/// <summary>
/// Contains parameters that can be passed to Gameplay Cues.
/// </summary>
/// <param name="magnitude">The magnitude for this cue.</param>
/// <param name="normalizedMagnitude">The normalized magnitude for this cue between 0 and 1.</param>
/// <param name="source">The source of the cue.</param>
/// <param name="customParameters">Additional custom parameters that can be passed to the cue.</param>
public readonly struct GameplayCueParameters(
	int magnitude,
	float normalizedMagnitude,
	IForgeEntity? source = null,
	Dictionary<StringKey, object>? customParameters = null)
{
	/// <summary>
	/// Gets the magnitude or strength of the effect.
	/// </summary>
	public readonly int Magnitude { get; } = magnitude;

	/// <summary>
	/// Gets the normalized magnitude (usually between 0 and 1).
	/// </summary>
	public readonly float NormalizedMagnitude { get; } = normalizedMagnitude;

	/// <summary>
	/// Gets optional source of the gameplay cue.
	/// </summary>
	public readonly IForgeEntity? Source { get; } = source;

	/// <summary>
	/// Gets additional custom parameters that can be passed to the gameplay cue.
	/// </summary>
	public readonly Dictionary<StringKey, object>? CustomParameters { get; } = customParameters;
}
