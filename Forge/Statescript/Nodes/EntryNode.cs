// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript.Nodes;

/// <summary>
/// Node representing the entry point of a graph. It has a single output port that emits a message to start the graph
/// execution.
/// </summary>
public class EntryNode : Node
{
	/// <summary>
	/// Port index for the output port.
	/// </summary>
	public const byte OutputPort = 0;

	/// <summary>
	/// Starts the graph execution by emitting a message through the output port.
	/// </summary>
	/// <param name="graphContext">The graph context providing the runtime variables and execution state.</param>
	public void StartGraph(IGraphContext graphContext)
	{
		OutputPorts[OutputPort].EmitMessage(graphContext);
	}

	/// <summary>
	/// Stops the graph execution by emitting a disable message through the output port.
	/// </summary>
	/// <param name="graphContext">The graph context providing the runtime variables and execution state.</param>
	public void StopGraph(IGraphContext graphContext)
	{
		((SubgraphPort)OutputPorts[OutputPort]).EmitDisableSubgraphMessage(graphContext);
	}

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		outputPorts.Add(CreatePort<SubgraphPort>(OutputPort));
	}

	/// <inheritdoc/>
	protected override void HandleMessage(InputPort receiverPort, IGraphContext graphContext)
	{
		throw new NotImplementedException();
	}
}
