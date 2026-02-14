// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Provides functionality to execute and manage the lifecycle of a graph within a specified context.
/// </summary>
/// <remarks>
/// <para>The <see cref="GraphProcessor"/> class pairs a shared, immutable <see cref="Graph"/> definition with a
/// per-execution <see cref="IGraphContext"/> that holds all mutable runtime state (variable values, node contexts,
/// activation flags). Multiple processors can share the same <see cref="Graph"/> instance, each with its own context
/// (Flyweight pattern).</para>
/// <para>A <see cref="GraphProcessor"/> is reusable: after a graph completes naturally or is explicitly stopped,
/// it can be started again with a fresh execution cycle.</para>
/// </remarks>
/// <param name="graph">The graph to be executed by this processor.</param>
/// <param name="graphContext">The context in which the graph will be executed, providing runtime state for this
/// execution instance.</param>
public class GraphProcessor(Graph graph, IGraphContext graphContext)
{
	private readonly List<Node> _updateBuffer = [];

	/// <summary>
	/// Gets the graph that this processor is responsible for executing.
	/// </summary>
	public Graph Graph { get; } = graph;

	/// <summary>
	/// Gets the context in which the graph is executed. The context holds all mutable runtime state including variable
	/// values, node contexts, and activation status.
	/// </summary>
	public IGraphContext GraphContext { get; } = graphContext;

	/// <summary>
	/// Gets or sets an optional callback that is invoked when the graph completes naturally (i.e., all state nodes
	/// have deactivated) or when the graph is explicitly stopped. This allows external systems to react to graph
	/// completion without polling.
	/// </summary>
	public Action? OnGraphCompleted { get; set; }

	/// <summary>
	/// Starts the execution of the graph. This method initializes the context's runtime variables from the graph's
	/// variable definitions, ensuring that each execution instance has independent state, and then initiates the
	/// graph's entry node to begin processing.
	/// </summary>
	public void StartGraph()
	{
		GraphContext.Processor = this;
		GraphContext.HasStarted = true;
		GraphContext.GraphVariables.InitializeFrom(Graph.VariableDefinitions);
		Graph.EntryNode.StartGraph(GraphContext);

		// If no state nodes were activated during the initial message propagation (e.g., action-only graphs), the graph
		// is already complete.
		if (GraphContext.HasStarted && !GraphContext.IsActive)
		{
			FinalizeGraph();
		}
	}

	/// <summary>
	/// Updates all active state nodes in the graph with the given delta time. Only state nodes that are currently
	/// active are updated, avoiding unnecessary iteration over inactive nodes. Call this method in your game loop to
	/// drive time-dependent state node logic such as timers, animations, or continuous evaluation.
	/// </summary>
	/// <param name="deltaTime">The time elapsed since the last update, in seconds.</param>
	public void UpdateGraph(double deltaTime)
	{
		if (!GraphContext.HasStarted)
		{
			return;
		}

		_updateBuffer.Clear();
		_updateBuffer.AddRange(GraphContext.ActiveStateNodes);

		for (var i = 0; i < _updateBuffer.Count; i++)
		{
			_updateBuffer[i].Update(deltaTime, GraphContext);
		}
	}

	/// <summary>
	/// Stops the execution of the graph. This method calls the entry node's stop method to halt the graph's processing
	/// and then removes all node contexts from the graph context to clean up any state associated with the graph's
	/// execution. This method is safe to call re-entrantly (e.g., from an <see cref="Nodes.ExitNode"/> triggered
	/// during the disable cascade).
	/// </summary>
	public void StopGraph()
	{
		if (GraphContext.Processor != this)
		{
			return;
		}

		GraphContext.Processor = null;
		GraphContext.HasStarted = false;
		Graph.EntryNode.StopGraph(GraphContext);
		GraphContext.ActiveStateNodes.Clear();
		GraphContext.InternalNodeActivationStatus.Clear();
		GraphContext.RemoveAllNodeContext();
		OnGraphCompleted?.Invoke();
	}

	/// <summary>
	/// Finalizes the graph execution after all state nodes have naturally deactivated. Unlike <see cref="StopGraph"/>,
	/// this does not propagate disable messages through the entry node since all nodes have already deactivated through
	/// their normal lifecycle. This method clears remaining runtime state (node contexts, activation status) so the GC
	/// can reclaim memory.
	/// </summary>
	internal void FinalizeGraph()
	{
		if (!GraphContext.HasStarted)
		{
			return;
		}

		GraphContext.HasStarted = false;
		GraphContext.Processor = null;
		GraphContext.InternalNodeActivationStatus.Clear();
		GraphContext.RemoveAllNodeContext();
		OnGraphCompleted?.Invoke();
	}
}
