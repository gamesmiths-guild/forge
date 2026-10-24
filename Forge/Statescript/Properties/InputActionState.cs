// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Which state of an input action an <see cref="InputActionResolver"/> reads.
/// </summary>
public enum InputActionState
{
	/// <summary>
	/// Whether the action is held. This is the one a condition wants: it reads the same on every tick the button is
	/// down.
	/// </summary>
	Held = 0,

	/// <summary>
	/// Whether the action was pressed since the last tick.
	/// </summary>
	Pressed = 1,

	/// <summary>
	/// Whether the action was released since the last tick.
	/// </summary>
	Released = 2,
}
