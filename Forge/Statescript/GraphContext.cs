// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Holds all mutable runtime state for a single graph execution instance. Each <see cref="GraphProcessor"/> owns one
/// <see cref="GraphContext"/>, providing independent state (variables, node contexts, activation flags) so that
/// multiple processors can share the same immutable <see cref="Graph"/> definition (Flyweight pattern).
/// </summary>
public sealed class GraphContext
{
	private readonly Dictionary<Guid, INodeContext> _nodeContexts = [];

	/// <summary>
	/// Gets a value indicating whether the graph is currently active. A graph is considered active if it has at least
	/// one active state node.
	/// </summary>
	public bool IsActive => ActiveStateNodes.Count > 0;

	/// <summary>
	/// Gets or sets the optional owner entity for this graph execution. The owner provides access to entity attributes,
	/// tags, and other systems that property resolvers can use to compute derived values. May be
	/// <see langword="null"/> if the graph does not require an owner entity.
	/// </summary>
	public IForgeEntity? Owner { get; set; }

	/// <summary>
	/// Gets the runtime variables for this graph execution instance. These are initialized from the graph's variable
	/// definitions when the graph starts, providing each execution with independent state.
	/// </summary>
	public Variables GraphVariables { get; } = new Variables();

	internal Dictionary<Guid, bool> InternalNodeActivationStatus { get; } = [];

	internal HashSet<Node> ActiveStateNodes { get; } = [];

	internal GraphProcessor? Processor { get; set; }

	internal bool HasStarted { get; set; }

	internal int NodeContextCount => _nodeContexts.Count;

	/// <summary>
	/// Gets the node context of type T for the specified node ID. The context is guaranteed to exist because the
	/// framework creates it automatically when the node first receives a message, before any user code runs.
	/// </summary>
	/// <typeparam name="T">The type of the node context to get. Must implement <see cref="INodeContext"/> and have a
	/// parameterless constructor.</typeparam>
	/// <param name="nodeID">The unique identifier of the node for which to get the context.</param>
	/// <returns>The node context associated with the specified node ID.</returns>
	/// <exception cref="InvalidOperationException">Thrown if no context exists for the given node ID. This indicates
	/// a framework bug, as contexts are created automatically during node activation.</exception>
	public T GetNodeContext<T>(Guid nodeID)
		where T : class, INodeContext, new()
	{
		if (_nodeContexts.TryGetValue(nodeID, out INodeContext? context))
		{
			return (T)context;
		}

		throw new InvalidOperationException(
			$"Node context of type {typeof(T).Name} not found for node ID {nodeID}. " +
			"This should never happen as contexts are created automatically by the framework.");
	}

	internal T GetOrCreateNodeContext<T>(Guid nodeID)
		where T : INodeContext, new()
	{
		if (_nodeContexts.TryGetValue(nodeID, out INodeContext? context))
		{
			return (T)context;
		}

		var newContext = new T();
		_nodeContexts[nodeID] = newContext;
		return newContext;
	}

	internal bool HasNodeContext(Guid nodeID)
	{
		return _nodeContexts.ContainsKey(nodeID);
	}

	internal void RemoveAllNodeContext()
	{
		_nodeContexts.Clear();
	}
}
