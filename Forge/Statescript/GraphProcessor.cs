// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Provides functionality to execute and manage the lifecycle of a graph within a specified context.
/// </summary>
/// <remarks>
/// <para>The <see cref="GraphProcessor"/> class pairs a shared, immutable <see cref="Graph"/> definition with a
/// per-execution <see cref="GraphContext"/> that holds all mutable runtime state (variable values, node contexts,
/// activation flags). Multiple processors can share the same <see cref="Graph"/> instance, each with its own context
/// (Flyweight pattern).</para>
/// <para>A <see cref="GraphProcessor"/> is reusable: after a graph completes naturally or is explicitly stopped,
/// it can be started again with a fresh execution cycle.</para>
/// </remarks>
public class GraphProcessor
{
	private readonly List<Node> _updateBuffer = [];

	private bool _updating;

	/// <summary>
	/// Gets the graph that this processor is responsible for executing.
	/// </summary>
	public Graph Graph { get; }

	/// <summary>
	/// Gets the context in which the graph is executed. The context holds all mutable runtime state including variable
	/// values, node contexts, and activation status.
	/// </summary>
	public GraphContext GraphContext { get; }

	/// <summary>
	/// Gets or sets an optional callback that is invoked when the graph completes naturally (i.e., all state nodes
	/// have deactivated) or when the graph is explicitly stopped. This allows external systems to react to graph
	/// completion without polling.
	/// </summary>
	public Action? OnGraphCompleted { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="GraphProcessor"/> class.
	/// </summary>
	/// <param name="graph">The graph to be executed by this processor.</param>
	/// <param name="sharedVariables">Optional shared variables for this graph execution. When set, scope-aware variable
	/// resolvers such as <see cref="Properties.VariableResolver"/> can read entity-level shared state.</param>
	public GraphProcessor(Graph graph, Variables? sharedVariables = null)
	{
		Graph = graph;
		GraphContext = new GraphContext { SharedVariables = sharedVariables };
	}

	/// <summary>
	/// Starts the execution of the graph. This method initializes the context's runtime variables from the graph's
	/// variable definitions, ensuring that each execution instance has independent state, and then initiates the
	/// graph's entry node to begin processing.
	/// </summary>
	/// <param name="variableOverrides">An optional callback invoked after variables are initialized from definitions
	/// but before the graph's entry node begins processing. Use this to overwrite specific variable values with
	/// runtime data (e.g., activation context from an ability).</param>
	public void StartGraph(Action<Variables>? variableOverrides = null)
	{
		GraphContext.Processor = this;
		GraphContext.HasStarted = true;
		GraphContext.FinalizationDeferralCount = 0;
		GraphContext.GraphVariables.InitializeFrom(Graph.VariableDefinitions);
		variableOverrides?.Invoke(GraphContext.GraphVariables);
		Graph.FinalizeConnections();
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
		if (!BufferActiveNodes())
		{
			return;
		}

		try
		{
			for (int i = 0; i < _updateBuffer.Count; i++)
			{
				_updateBuffer[i].Update(deltaTime, GraphContext);
			}
		}
		finally
		{
			_updating = false;
		}
	}

	/// <summary>
	/// Updates all active state nodes on the host's fixed step. Call this from the game's fixed callback - a physics
	/// step, or a network tick - alongside <see cref="UpdateGraph"/> in its frame callback.
	/// </summary>
	/// <remarks>
	/// A host with no fixed step of its own simply never calls this, and the nodes that need one stop running rather
	/// than running at the frame rate. Nothing in the graph requires both to be driven.
	/// </remarks>
	/// <param name="deltaTime">The length of the fixed step, in seconds.</param>
	public void FixedUpdateGraph(double deltaTime)
	{
		if (!BufferActiveNodes())
		{
			return;
		}

		try
		{
			for (int i = 0; i < _updateBuffer.Count; i++)
			{
				_updateBuffer[i].FixedUpdate(deltaTime, GraphContext);
			}
		}
		finally
		{
			_updating = false;
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
		if (GraphContext.Processor != this || !GraphContext.HasStarted)
		{
			return;
		}

		// Clear HasStarted first so the disable cascade is re-entrancy safe (e.g. an ExitNode triggering StopGraph, or
		// a state node reaching FinalizeGraph) without nulling Processor yet. Keeping Processor set throughout the
		// cascade lets action nodes on OnDeactivate paths still resolve property-backed inputs.
		GraphContext.HasStarted = false;
		Graph.EntryNode.StopGraph(GraphContext);
		GraphContext.Processor = null;
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

	// Snapshots the active nodes before either update walks them, because a node updated part way through can
	// deactivate itself or another and modify the set being walked. Stamping here rather than in each caller is what
	// makes the frame and fixed updates count as the separate passes they are.
	private bool BufferActiveNodes()
	{
		if (!GraphContext.HasStarted)
		{
			return false;
		}

		// The buffer is one list reused per call, so a nested pass would clear and refill the list the outer walk is
		// still indexing: some nodes updated twice, others skipped, silently and depending on set order. Nesting is
		// never useful here - updates cascade downward through messages, not by re-driving the graph - so it is
		// refused outright rather than made to work. Refusing also keeps a validation-disabled build safe: it drops
		// the nested pass instead of miscounting everyone's time.
		if (_updating)
		{
			Validation.Fail(
				"A graph update was re-entered while one was already running, which would corrupt the walk in " +
				"progress. Drive UpdateGraph and FixedUpdateGraph from the host's own callbacks, never from inside " +
				"a node or an ability behavior.");

			return false;
		}

		_updating = true;

		GraphContext.UpdateStamp++;

		_updateBuffer.Clear();
		_updateBuffer.AddRange(GraphContext.ActiveStateNodes);

		return true;
	}
}
