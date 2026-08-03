// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// The context for an <see cref="IterationNode{T}"/>. Tracks how far the loop has walked and, when the loop is paced
/// by an interval, the time accumulated toward the next iteration.
/// </summary>
public class IterationNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the zero-based index of the next iteration to run.
	/// </summary>
	public int NextIndex { get; set; }

	/// <summary>
	/// Gets or sets the elapsed time in seconds accumulated toward the next iteration. Unused while the loop runs
	/// synchronously.
	/// </summary>
	public double ElapsedTime { get; set; }
}
