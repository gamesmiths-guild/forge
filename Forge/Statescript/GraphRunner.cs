// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Provides functionality to execute and manage the lifecycle of a graph within a specified context.
/// </summary>
/// <remarks>The <see cref="GraphRunner"/> class encapsulates a graph and its associated execution context, allowing for
/// starting and stopping the graph's execution. It maintains the graph's variables and ensures proper initialization
/// and cleanup when running or halting the graph.</remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="GraphRunner"/> class with the specified graph and graph context.
/// </remarks>
/// <param name="graph">The graph to be executed by this runner.</param>
/// <param name="graphContext">The context in which the graph will be executed, providing necessary information and
/// services for graph processing.</param>
public class GraphRunner(Graph graph, IGraphContext graphContext)
{
	/// <summary>
	/// Gets the graph that this runner is responsible for executing.
	/// </summary>
	public Graph Graph { get; } = graph;

	/// <summary>
	/// Gets the variables associated with the graph during execution. These variables are cloned from the graph's
	/// original variables to ensure that each execution instance has its own set of variables, allowing for independent
	/// graph runs without interference.
	/// </summary>
	public Variables? GraphVariables { get; private set; }

	/// <summary>
	/// Gets the context in which the graph is executed. The context provides necessary information and services for
	/// graph processing.
	/// </summary>
	public IGraphContext GraphContext { get; } = graphContext;

	/// <summary>
	/// Starts the execution of the graph. This method clones the graph's variables to ensure that each execution
	/// instance has its own set of variables, and then initiates the graph's entry node to begin processing the graph.
	/// The entry node will use the cloned variables and the provided graph context to execute the graph's logic.
	/// </summary>
	public void StartGraph()
	{
		GraphVariables = (Variables)Graph.GraphVariables.Clone();
		Graph.EntryNode.StartGraph(GraphVariables, GraphContext);
	}

	/// <summary>
	/// Stops the execution of the graph. This method calls the entry node's stop method to halt the graph's processing
	/// and then removes all node contexts from the graph context to clean up any state associated with the graph's
	/// execution. This ensures that the graph is properly reset and ready for future executions without any lingering
	/// state from previous runs.
	/// </summary>
	public void StopGraph()
	{
		if (GraphVariables is not null)
		{
			Graph.EntryNode.StopGraph(GraphVariables, GraphContext);
			GraphContext.RemoveAllNodeContext();
		}
	}
}
