// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects.Magnitudes;

/// <summary>
/// The possible types of calculation for an <see cref="AttributeBasedFloat"/>.
/// </summary>
public enum AttributeCalculationType : byte
{
	/// <summary>
	/// Use the attribute's total magnitude (CurrentValue).
	/// </summary>
	CurrentValue = 0,

	/// <summary>
	/// Use the attribute's base value.
	/// </summary>
	BaseValue = 1,

	/// <summary>
	/// Use the attribute's modifier value.
	/// </summary>
	Modifier = 2,

	/// <summary>
	/// Use the attribute's overflow value.
	/// </summary>
	Overflow = 3,

	/// <summary>
	/// Use the attribute's valid modifier value.
	/// </summary>
	ValidModifier = 4,

	/// <summary>
	/// Use the attribute's minimum value.
	/// </summary>
	Min = 5,

	/// <summary>
	/// Use the attribute's maximum value.
	/// </summary>
	Max = 6,

	/// <summary>
	/// Use the attribute's total magnitude calculated up to a specific channel.
	/// </summary>
	MagnitudeEvaluatedUpToChannel = 7,
}
