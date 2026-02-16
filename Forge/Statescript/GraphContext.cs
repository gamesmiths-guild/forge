// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;
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
	/// Gets or sets the optional shared variables for this graph execution. When the graph is driven by an ability,
	/// this is set to the owner entity's <see cref="IForgeEntity.SharedVariables"/>, allowing property resolvers and
	/// nodes to access entity-level shared state. For standalone graphs this may be <see langword="null"/> or set to
	/// a custom <see cref="Variables"/> instance.
	/// </summary>
	public Variables? SharedVariables { get; set; }

	/// <summary>
	/// Gets or sets optional activation context data for this graph execution. This provides a generic extensibility
	/// point that allows external systems to pass contextual data into the graph without coupling
	/// <see cref="GraphContext"/> to specific subsystems. For example, when a graph is driven by an ability,
	/// <see cref="Abilities.AbilityBehaviorContext"/> is stored here so that ability-aware nodes can access the ability
	/// handle for operations like committing cooldowns or costs.
	/// </summary>
	public object? ActivationContext { get; set; }

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
	/// Attempts to retrieve the <see cref="ActivationContext"/> as a specific type. This is the recommended way for
	/// nodes to access activation context data, providing a safe pattern that gracefully handles both missing and
	/// mismatched data.
	/// </summary>
	/// <typeparam name="T">The expected type of the activation context.</typeparam>
	/// <param name="data">When this method returns <see langword="true"/>, contains the activation context cast to
	/// <typeparamref name="T"/>; otherwise, <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if <see cref="ActivationContext"/> is not <see langword="null"/> and is of type
	/// <typeparamref name="T"/>; otherwise, <see langword="false"/>.</returns>
	public bool TryGetActivationContext<T>([NotNullWhen(true)] out T? data)
		where T : class
	{
		if (ActivationContext is T typed)
		{
			data = typed;
			return true;
		}

		data = null;
		return false;
	}

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
