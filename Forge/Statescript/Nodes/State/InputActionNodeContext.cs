// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// The context for an <see cref="InputActionNode"/>. Remembers whether the button was down, which is what turns a
/// per-tick read into the presses and releases the node reports.
/// </summary>
public class InputActionNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets whether the button was down on the last read. Null before the first one, which is what keeps a
	/// button already held when the node activated from counting as a press.
	/// </summary>
	public bool? LastPressed { get; set; }

	// The input and frame the last read came from, so a frame read on activation is not read again later in its tick.
	internal EntityInput? LastInput { get; set; }

	internal ulong LastFrame { get; set; }
}
