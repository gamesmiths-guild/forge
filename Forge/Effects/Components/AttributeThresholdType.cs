// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// How the bounds of an <see cref="AttributeRequirement"/> are interpreted.
/// </summary>
public enum AttributeThresholdType : byte
{
	/// <summary>
	/// The bounds are raw attribute values, compared directly against the resolved value.
	/// </summary>
	Absolute = 0,

	/// <summary>
	/// The bounds are percentages of the attribute's <c>Max</c>, from 0 to 100. An attribute whose <c>Max</c> is zero
	/// or negative resolves to 0%.
	/// </summary>
	PercentOfMax = 1,
}
