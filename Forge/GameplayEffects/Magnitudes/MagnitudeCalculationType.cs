// Copyright © 2024 Gamesmiths Guild.

namespace Gamesmiths.Forge.GameplayEffects.Magnitudes;

/// <summary>
/// The supported types of magnitude calculation.
/// </summary>
public enum MagnitudeCalculationType : byte
{
	/// <summary>
	/// Use <see cref="Magnitudes.ScalableFloat"/> to calculate the magnitude. Scales with effect level.
	/// </summary>
	ScalableFloat = 0,

	/// <summary>
	/// Use <see cref="AttributeBasedFloat"/> to calculate the magnitude.
	/// </summary>
	AttributeBased = 1,

	/// <summary>
	/// Use a <see cref="IMagnitudeCalculator"/> for calculating the magnitude.
	/// </summary>
	CustomCalculatorClass = 2,

	/// <summary>
	/// Use <see cref="SetByCallerFloat"/> to let the caller set a custom magnitude.
	/// </summary>
	SetByCaller = 3,
}
