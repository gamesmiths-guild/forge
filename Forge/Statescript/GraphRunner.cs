// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Provides functionality to execute and manage the lifecycle of a graph within a specified context.
/// </summary>
/// <remarks>
/// <para>The <see cref="GraphRunner"/> class encapsulates a graph and its associated execution context, allowing for
/// starting, updating, and stopping the graph's execution. It ensures proper initialization and cleanup when running
/// or halting the graph.</para>
/// <para>The graph definition (nodes, connections, default variables) is immutable and shared. All mutable runtime
/// state — including variable values, node contexts, and activation status — lives in the <see cref="IGraphContext"/>.
/// This allows a single <see cref="Graph"/> to be executed concurrently by multiple runners, each with its own
/// context (Flyweight pattern).</para>
/// </remarks>
/// <param name="graph">The graph to be executed by this runner.</param>
/// <param name="graphContext">The context in which the graph will be executed, providing runtime state for this
/// execution instance.</param>
public class GraphRunner(Graph graph, IGraphContext graphContext)
{
	/// <summary>
	/// Gets the graph that this runner is responsible for executing.
	/// </summary>
	public Graph Graph { get; } = graph;

	/// <summary>
	/// Gets the context in which the graph is executed. The context holds all mutable runtime state including
	/// variable values, node contexts, and activation status.
	/// </summary>
	public IGraphContext GraphContext { get; } = graphContext;

	/// <summary>
	/// Starts the execution of the graph. This method clones the graph's default variables into the context
	/// to ensure that each execution instance has independent state, and then initiates the graph's entry node
	/// to begin processing.
	/// </summary>
	public void StartGraph()
	{
		GraphContext.GraphVariables.LoadFrom(Graph.GraphVariables);
		Graph.EntryNode.StartGraph(GraphContext);
	}

	/// <summary>
	/// Updates all active state nodes in the graph with the given delta time. Call this method in your game loop
	/// to drive time-dependent state node logic such as timers, animations, or continuous evaluation.
	/// </summary>
	/// <param name="deltaTime">The time elapsed since the last update, in seconds.</param>
	public void UpdateGraph(double deltaTime)
	{
		foreach (Node node in Graph.Nodes)
		{
			node.Update(deltaTime, GraphContext);
		}
	}

	/// <summary>
	/// Stops the execution of the graph. This method calls the entry node's stop method to halt the graph's processing
	/// and then removes all node contexts from the graph context to clean up any state associated with the graph's
	/// execution.
	/// </summary>
	public void StopGraph()
	{
		Graph.EntryNode.StopGraph(GraphContext);
		GraphContext.RemoveAllNodeContext();
	}
}
