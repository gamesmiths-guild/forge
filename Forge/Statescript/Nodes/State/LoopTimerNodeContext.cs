// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="LoopTimerNode"/>. Tracks time within the current interval and the number of
/// completed loops.
/// </summary>
public class LoopTimerNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the elapsed time in seconds within the current interval.
	/// </summary>
	public double ElapsedTime { get; set; }

	/// <summary>
	/// Gets or sets the number of intervals completed since activation.
	/// </summary>
	public int CompletedLoops { get; set; }
}
