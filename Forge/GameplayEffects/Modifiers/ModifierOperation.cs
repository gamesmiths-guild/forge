// Copyright © 2024 Gamesmiths Guild.

namespace Gamesmiths.Forge.GameplayEffects.Modifiers;

/// <summary>
/// Possible types of modifier operations.
/// </summary>
public enum ModifierOperation : byte
{
	/// <summary>
	/// A flat operation, adding or subtracing a value.
	/// </summary>
	Flat = 0,

	/// <summary>
	/// A percentage operation to be multiplied to the value.
	/// </summary>
	Percent = 1,

	/// <summary>
	/// An override operation, completly overriding the current value.
	/// </summary>
	Override = 2,
}
