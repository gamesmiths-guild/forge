// Copyright © 2024 Gamesmiths Guild.

namespace Gamesmiths.Forge.GameplayEffects.Magnitudes;

/// <summary>
/// The possible types of calculation for an <see cref="AttributeBasedFloat"/>.
/// </summary>
public enum AttributeBasedFloatCalculationType : byte
{
	/// <summary>
	/// Use the attribute's total magnitude (CurrentValue).
	/// </summary>
	AttributeMagnitude = 0,

	/// <summary>
	/// Use the attribute's base value.
	/// </summary>
	AttributeBaseValue = 1,

	/// <summary>
	/// Use the attribute's valid modifier value.
	/// </summary>
	AttributeModifierMagnitude = 2,

	/// <summary>
	/// Use the attribute's total magnitude calculated up to a specific channel.
	/// </summary>
	AttributeMagnitudeEvaluatedUpToChannel = 3,
}
