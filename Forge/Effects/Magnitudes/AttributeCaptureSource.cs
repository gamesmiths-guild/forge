// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects.Magnitudes;

/// <summary>
/// The types of sources an attribute can be captured from.
/// </summary>
public enum AttributeCaptureSource : byte
{
	/// <summary>
	/// The source owner of the effect.
	/// </summary>
	Source = 0,

	/// <summary>
	/// The target entity of the effect.
	/// </summary>
	Target = 1,
}
