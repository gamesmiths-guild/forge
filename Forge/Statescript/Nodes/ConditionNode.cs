// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript.Nodes;

/// <summary>
/// Node representing a condition in the graph. It has a single input port that triggers the evaluation of the condition
/// and two output ports: one for the true result and one for the false result.
/// </summary>
public abstract class ConditionNode : Node
{
	private const byte InputPort = 0;
	private const byte TruePort = 0;
	private const byte FalsePort = 1;

	/// <summary>
	/// Tests the condition and returns true or false. The result determines which output port will emit a message.
	/// </summary>
	/// <param name="graphVariables">The current graph variables.</param>
	/// <param name="graphContext">The current graph context.</param>
	/// <returns><see langword="true"/> if the condition is met; otherwise, <see langword="false"/>.</returns>
	protected abstract bool Test(Variables graphVariables, IGraphContext graphContext);

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		inputPorts.Add(CreatePort<InputPort>(InputPort));
		outputPorts.Add(CreatePort<EventPort>(TruePort));
		outputPorts.Add(CreatePort<EventPort>(FalsePort));
	}

	/// <inheritdoc/>
	protected sealed override void HandleMessage(InputPort receiverPort, Variables graphVariables, IGraphContext graphContext)
	{
		if (Test(graphVariables, graphContext))
		{
			OutputPorts[TruePort].EmitMessage(graphVariables, graphContext);
		}
		else
		{
			OutputPorts[FalsePort].EmitMessage(graphVariables, graphContext);
		}
	}
}
