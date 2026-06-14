// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Nodes;

/// <summary>
/// The context for a <see cref="StateNode{T}"/>. This class holds the state of the node, such as whether it is active
/// or in the process of activating, as well as any deferred messages that need to be emitted after activation is
/// complete.
/// </summary>
public class StateNodeContext : INodeContext
{
	/// <summary>
	/// Gets a value indicating whether the node is active.
	/// </summary>
	public bool Active { get; internal set; }

	/// <summary>
	/// Gets a value indicating whether the in-progress deactivation was triggered by the Abort port. Reset on each
	/// activation; set just before deactivation when the node is aborted. Lets nodes distinguish a forced interruption
	/// from a natural shutdown (subgraph end or graph stop) inside <c>OnDeactivate</c>.
	/// </summary>
	public bool WasAborted { get; internal set; }

	internal bool Activating { get; set; }

	internal int[]? DeferredDeactivationEventPortIds { get; set; }

	internal List<int> DeferredEmitMessageData { get; set; } = [];
}
