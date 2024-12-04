// Copyright © 2024 Gamesmiths Guild.

namespace Gamesmiths.Forge.GameplayEffects.Stacking;

/// <summary>
/// Defines how the magnitude of the effects are handled.
/// </summary>
public enum StackMagnitudePolicy : byte
{
	/// <summary>
	/// Don't change magnitudes at all.
	/// </summary>
	DontStack = 0,

	/// <summary>
	/// Sum the magnitudes of each stack.
	/// </summary>
	Sum = 1,
}