// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="TimerNode"/>. Tracks elapsed time since activation so the node can determine when
/// its configured duration has been reached.
/// </summary>
public class TimerNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the elapsed time in seconds since the timer was activated.
	/// </summary>
	public double ElapsedTime { get; set; }
}
