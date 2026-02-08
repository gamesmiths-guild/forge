// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript.Nodes;

/// <summary>
/// Node representing the entry point of a graph. It has a single output port that emits a message to start the graph
/// execution.
/// </summary>
public class EntryNode : Node
{
	private const byte InputPort = 0;

	/// <summary>
	/// 2 ideas:
	/// 1. We can move GraphVariables for inside the EntryNode, and now we don't need
	/// to send it as a parameter every time we fire a node
	/// or
	/// 2. We can create a simplified version of graph instances that only contains graphVariables
	/// and we use the same Graph to execute based on the graphVariables passed. (I like this idea, it's
	/// kind of like the "Flyweight" pattern that we thought at the beginning)
	/// possible problems with this approach would be with the internal timer variable for the WaitNode for
	/// example, but if we move that to the GraphVariables then I think it would work fine.
	/// </summary>
	/// <param name="graphVariables">The graph variables to be used when starting the graph.</param>
	/// <param name="graphContext">The graph context providing information about the graph's execution state.</param>
	public void StartGraph(Variables graphVariables, IGraphContext graphContext)
	{
		graphVariables.SaveVariableValues();
		OutputPorts[InputPort].EmitMessage(graphVariables, graphContext);
	}

	/// <summary>
	/// Stops the graph execution by emitting a disable message through the output port and loading the previous
	/// variable values.
	/// </summary>
	/// <param name="graphVariables">The graph variables to be used when stopping the graph.</param>
	/// <param name="graphContext">The graph context providing information about the graph's execution state.</param>
	public void StopGraph(Variables graphVariables, IGraphContext graphContext)
	{
		((SubgraphPort)OutputPorts[InputPort]).EmitDisableSubgraphMessage(graphVariables, graphContext);
		graphVariables.LoadVariableValues();
	}

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		outputPorts.Add(CreatePort<SubgraphPort>(InputPort));
	}

	/// <inheritdoc/>
	protected override void HandleMessage(InputPort receiverPort, Variables graphVariables, IGraphContext graphContext)
	{
		throw new NotImplementedException();
	}
}
