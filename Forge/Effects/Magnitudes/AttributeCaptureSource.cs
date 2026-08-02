// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects.Magnitudes;

/// <summary>
/// Which entity involved in an effect an attribute is captured from.
/// </summary>
/// <remarks>
/// The naming matches <see cref="Components.EffectApplicationTarget"/> and
/// <see cref="Components.OwnershipEntity"/>: each member that names an ownership entity resolves to the matching
/// <see cref="EffectOwnership"/> property.
/// </remarks>
public enum AttributeCaptureSource : byte
{
	/// <summary>
	/// The target entity of the effect.
	/// </summary>
	Target = 0,

	/// <summary>
	/// <see cref="EffectOwnership.Source"/> — what actually caused the effect.
	/// </summary>
	Source = 1,

	/// <summary>
	/// <see cref="EffectOwnership.Owner"/> — who triggered the action that caused the effect.
	/// </summary>
	Owner = 2,
}
