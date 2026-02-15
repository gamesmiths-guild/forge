// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript.Nodes;

/// <summary>
/// Node representing an action in the graph. It has a single input port that triggers the execution of the action and a
/// single output port that emits a message after the action is executed.
/// </summary>
public abstract class ActionNode : Node
{
	/// <summary>
	/// Port index for the input port.
	/// </summary>
	public const byte InputPort = 0;

	/// <summary>
	/// Port index for the output port.
	/// </summary>
	public const byte OutputPort = 0;

	/// <summary>
	/// Executes the action associated with this node. This method is called when the input port receives a message.
	/// </summary>
	/// <param name="graphContext">The current graph context.</param>
	protected abstract void Execute(IGraphContext graphContext);

	/// <inheritdoc/>
#pragma warning disable SA1202 // Elements should be ordered by access
	internal override IEnumerable<int> GetReachableOutputPorts(byte inputPortIndex)
#pragma warning restore SA1202 // Elements should be ordered by access
	{
		yield return OutputPort;
	}

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		inputPorts.Add(CreatePort<InputPort>(InputPort));
		outputPorts.Add(CreatePort<EventPort>(OutputPort));
	}

	/// <inheritdoc/>
	protected override void HandleMessage(InputPort receiverPort, IGraphContext graphContext)
	{
		Execute(graphContext);
		OutputPorts[OutputPort].EmitMessage(graphContext);
	}
}
