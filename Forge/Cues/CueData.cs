// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Cues;

/// <summary>
/// Configuration data for a cue.
/// </summary>
/// <param name="cueTags">The container with all identifier tags for this cue.</param>
/// <param name="minValue">The minimum value used for this cue's normalized magnitude calculation.</param>
/// <param name="maxValue">The maximum value used for this cue's normalized magnitude calculation.</param>
/// <param name="magnitudeType">The type of magnitude to be used for this cue.</param>
/// <param name="magnitudeAttribute">The modified attribute to be used as magnitude. If <see langword="null"/> the
/// effect level will be used instead.
/// </param>
public readonly struct CueData(
	TagContainer? cueTags,
	int minValue,
	int maxValue,
	CueMagnitudeType magnitudeType,
	string? magnitudeAttribute = null)
{
	/// <summary>
	/// Gets a container with all the identifier tags for this cue.
	/// </summary>
	public TagContainer? CueTags { get; } = cueTags;

	/// <summary>
	/// Gets the minimum value used for this cue's normalized magnitude calculation.
	/// </summary>
	public int MinValue { get; } = minValue;

	/// <summary>
	/// Gets the maximum value used for this cue's normalized magnitude calculation.
	/// </summary>
	public int MaxValue { get; } = maxValue;

	/// <summary>
	/// Gets the type of magnitude to be used for this cue.
	/// </summary>
	public CueMagnitudeType MagnitudeType { get; } = magnitudeType;

	/// <summary>
	/// Gets the attribute to be used as magnitude modifier. If <see langword="null"/> the effect level will be used
	/// instead.
	/// </summary>
	public string? MagnitudeAttribute { get; } = magnitudeAttribute;

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
