// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Interface representing the context of a graph during execution, providing access to necessary information and
/// services for graph processing.
public interface IGraphContext
{
	/// <summary>
	/// Gets or sets the count of active state nodes in the graph. This property is used to track how many state nodes
	/// are currently active during graph execution.
	/// </summary>
	int ActiveStateNodeCount { get; set; }

	/// <summary>
	/// Gets a value indicating whether the graph is currently active. A graph is considered active if it has at least
	/// one active state node.
	/// </summary>
	bool IsActive { get; }

	/// <summary>
	/// Gets a dictionary mapping node IDs to their activation status. This allows tracking which nodes in the graph are
	/// currently active or inactive during execution.
	/// </summary>
	Dictionary<Guid, bool> InternalNodeActivationStatus { get; }

	/// <summary>
	/// Gets or creates a node context of type T for the specified node ID. If a context for the given node ID already
	/// exists, it returns the existing context; otherwise, it creates a new instance of T and associates it with the
	/// node ID.
	/// </summary>
	/// <typeparam name="T">The type of the node context to get or create. Must implement INodeContext and have a
	/// parameterless constructor.</typeparam>
	/// <param name="nodeID">The unique identifier of the node for which to get or create the context.</param>
	/// <returns>The node context associated with the specified node ID.</returns>
	T GetOrCreateNodeContext<T>(Guid nodeID)
		where T : INodeContext, new();

	/// <summary>
	/// Gets the node context of type T for the specified node ID. If no context exists for the given node ID, it
	/// returns null.
	/// </summary>
	/// <typeparam name="T">The type of the node context to get. Must implement INodeContext.</typeparam>
	/// <param name="nodeID">The unique identifier of the node for which to get the context.</param>
	/// <returns>The node context associated with the specified node ID, or null if no context exists.</returns>
	T GetNodeContext<T>(Guid nodeID)
		where T : INodeContext, new();

	/// <summary>
	/// Removes all node contexts from the graph context. This method is typically called when resetting the graph or
	/// when the graph is no longer needed.
	/// </summary>
	void RemoveAllNodeContext();
}
