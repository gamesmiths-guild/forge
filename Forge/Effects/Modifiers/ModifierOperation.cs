// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects.Modifiers;

/// <summary>
/// Possible types of modifier operations.
/// </summary>
public enum ModifierOperation : byte
{
	/// <summary>
	/// A flat operation, adding or subtracing a value.
	/// </summary>
	FlatBonus = 0,

	/// <summary>
	/// A percentage bonus operation, adding or subtracting a percentage of the current value.
	/// </summary>
	PercentBonus = 1,

	/// <summary>
	/// An override operation, completly overriding the current value.
	/// </summary>
	Override = 2,
}
