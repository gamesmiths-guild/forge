// Copyright © 2024 Gamesmiths Guild.

namespace Gamesmiths.Forge.GameplayEffects.Magnitudes;

/// <summary>
/// The types of sources an attribute can be captured from.
/// </summary>
public enum AttributeCaptureSource : byte
{
	/// <summary>
	/// The source owner of the gameplay effect.
	/// </summary>
	Source = 0,

	/// <summary>
	/// The target entity of the gameplay effect.
	/// </summary>
	Target = 1,
}
