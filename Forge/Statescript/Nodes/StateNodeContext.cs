// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Nodes;

/// <summary>
/// The context for a <see cref="StateNode"/>. This class holds the state of the node, such as whether it is active or
/// in the process of activating, as well as any deferred messages that need to be emitted after activation is complete.
/// </summary>
public class StateNodeContext : INodeContext
{
	/// <summary>
	/// Gets a value indicating whether the node is active.
	/// </summary>
	public bool Active { get; internal set; }

	internal bool Activating { get; set; }

	internal int[]? DeferredDeactivationEventPortIds { get; set; }

	internal List<PortVariable> DeferredEmitMessageData { get; set; } = [];
}

internal record struct PortVariable(int PortId, Variables Variables);
