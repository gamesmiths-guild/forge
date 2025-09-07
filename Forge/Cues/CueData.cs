// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Cues;

/// <summary>
/// Configuration data for a cue.
/// </summary>
/// <param name="CueTags">The container with all identifier tags for this cue.</param>
/// <param name="MinValue">The minimum value used for this cue's normalized magnitude calculation.</param>
/// <param name="MaxValue">The maximum value used for this cue's normalized magnitude calculation.</param>
/// <param name="MagnitudeType">The type of magnitude to be used for this cue.</param>
/// <param name="MagnitudeAttribute">The modified attribute to be used as magnitude. If <see langword="null"/> the
/// effect level will be used instead.
/// </param>
/// <param name="FinalChannel">In case <paramref name="MagnitudeType"/> ==
/// <see cref="CueMagnitudeType.AttributeMagnitudeEvaluatedUpToChannel"/> a final channel for the calculation
/// must be provided.</param>
public readonly record struct CueData(
	TagContainer? CueTags,
	int MinValue,
	int MaxValue,
	CueMagnitudeType MagnitudeType,
	string? MagnitudeAttribute = null,
	int FinalChannel = 0)
{
	/// <summary>
	/// Calculates and gets the normalized magnitude for the given value.
	/// </summary>
	/// <param name="magnitude">The magnitude to be normalized.</param>
	/// <returns>The normalized magnitude, a value between 0f and 1f.</returns>
	public float NormalizedMagnitude(int magnitude)
	{
		var range = MaxValue - MinValue;
		if (range <= 0.01f)
		{
			return 1;
		}

		return Math.Clamp((magnitude - MinValue) / (float)range, 0f, 1f);
	}
}
