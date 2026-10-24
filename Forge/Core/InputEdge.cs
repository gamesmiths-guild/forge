// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Which change of an input action something reacts to.
/// </summary>
public enum InputEdge : byte
{
	/// <summary>
	/// The action was pressed since the last tick.
	/// </summary>
	Pressed = 0,

	/// <summary>
	/// The action was released since the last tick.
	/// </summary>
	Released = 1,
}
