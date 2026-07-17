// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="StateMachineNode"/>. Tracks the currently active state so the node can detect
/// selector transitions.
/// </summary>
public class StateMachineNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the currently active state index, or <see langword="null"/> when no state has been entered since
	/// activation.
	/// </summary>
	public int? CurrentState { get; set; }
}
