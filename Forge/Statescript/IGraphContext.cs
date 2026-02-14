// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Interface representing the context of a graph during execution, providing access to necessary information and
/// services for graph processing.
/// </summary>
public interface IGraphContext
{
	/// <summary>
	/// Gets a value indicating whether the graph is currently active. A graph is considered active if it has at least
	/// one active state node.
	/// </summary>
	bool IsActive { get; }

	/// <summary>
	/// Gets the optional owner entity for this graph execution. The owner provides access to entity attributes, tags,
	/// and other systems that property resolvers can use to compute derived values. May be <see langword="null"/> if
	/// the graph does not require an owner entity.
	/// </summary>
	IForgeEntity? Owner { get; }

	/// <summary>
	/// Gets the runtime variables for this graph execution instance. These are initialized from the graph's variable
	/// definitions when the graph starts, providing each execution with independent state.
	/// </summary>
	Variables GraphVariables { get; }

	/// <summary>
	/// Gets a dictionary mapping node IDs to their activation status. This allows tracking which nodes in the graph are
	/// currently active or inactive during execution.
	/// </summary>
	Dictionary<Guid, bool> InternalNodeActivationStatus { get; }

	/// <summary>
	/// Gets the set of state nodes that are currently active during this graph execution. Only active state nodes
	/// are updated each tick, avoiding unnecessary iteration over inactive nodes.
	/// </summary>
	HashSet<Node> ActiveStateNodes { get; }

	/// <summary>
	/// Gets or sets the <see cref="GraphRunner"/> currently executing this context. This reference allows nodes
	/// (such as <see cref="Nodes.ExitNode"/>) to trigger graph-level operations like stopping execution. Set
	/// automatically by <see cref="GraphRunner.StartGraph"/> and cleared by <see cref="GraphRunner.StopGraph"/>.
	/// </summary>
	GraphRunner? Runner { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the graph has been started and is awaiting completion. This flag is
	/// set to <see langword="true"/> when the graph starts executing and is cleared when the graph completes or is
	/// explicitly stopped. It is used to distinguish between a graph that was never started and one that has finished
	/// naturally (i.e., all state nodes have deactivated).
	/// </summary>
	bool HasStarted { get; set; }

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
