// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects.Stacking;

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
