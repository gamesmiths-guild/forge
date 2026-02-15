// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript.Nodes;

/// <summary>
/// Node representing a condition in the graph. It has a single input port that triggers the evaluation of the condition
/// and two output ports: one for the true result and one for the false result.
/// </summary>
public abstract class ConditionNode : Node
{
	/// <summary>
	/// Port index for the input port.
	/// </summary>
	public const byte InputPort = 0;

	/// <summary>
	/// Port index for the true output port.
	/// </summary>
	public const byte TruePort = 0;

	/// <summary>
	/// Port index for the false output port.
	/// </summary>
	public const byte FalsePort = 1;

	/// <summary>
	/// Tests the condition and returns true or false. The result determines which output port will emit a message.
	/// </summary>
	/// <param name="graphContext">The current graph context.</param>
	/// <returns><see langword="true"/> if the condition is met; otherwise, <see langword="false"/>.</returns>
	protected abstract bool Test(IGraphContext graphContext);

	/// <inheritdoc/>
#pragma warning disable SA1202 // Elements should be ordered by access
	internal override IEnumerable<int> GetReachableOutputPorts(byte inputPortIndex)
#pragma warning restore SA1202 // Elements should be ordered by access
	{
		yield return TruePort;
		yield return FalsePort;
	}

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		inputPorts.Add(CreatePort<InputPort>(InputPort));
		outputPorts.Add(CreatePort<EventPort>(TruePort));
		outputPorts.Add(CreatePort<EventPort>(FalsePort));
	}

	/// <inheritdoc/>
	protected sealed override void HandleMessage(InputPort receiverPort, IGraphContext graphContext)
	{
		if (Test(graphContext))
		{
			OutputPorts[TruePort].EmitMessage(graphContext);
		}
		else
		{
			OutputPorts[FalsePort].EmitMessage(graphContext);
		}
	}
}
