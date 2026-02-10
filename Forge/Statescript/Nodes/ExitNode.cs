// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript.Nodes;

/// <summary>
/// Node representing the exit point of a graph. It has a single input port that receives a message to stop the graph
/// execution.
/// </summary>
public class ExitNode : Node
{
	/// <summary>
	/// Port index for the input port.
	/// </summary>
	public const byte InputPort = 0;

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		inputPorts.Add(CreatePort<InputPort>(InputPort));
	}

	/// <inheritdoc/>
	protected override void HandleMessage(InputPort receiverPort, IGraphContext graphContext)
	{
		// TODO: Implement the logic to stop the graph execution when a message is received on the input port.
		throw new NotImplementedException();
	}
}
