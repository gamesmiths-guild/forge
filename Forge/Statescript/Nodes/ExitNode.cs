// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript.Nodes;

/// <summary>
/// Node representing the exit point of a graph. When a message reaches this node, the entire graph execution is
/// stopped, all active state nodes are disabled, node contexts are removed, and the execution is finalized.
/// </summary>
/// <remarks>
/// <para>Place an <see cref="ExitNode"/> at any point in the graph where you want to force the execution to end. This
/// has the same effect as calling <see cref="GraphProcessor.StopGraph"/> externally.</para>
/// </remarks>
public class ExitNode : Node
{
	/// <summary>
	/// Port index for the input port.
	/// </summary>
	public const byte InputPort = 0;

	/// <inheritdoc/>
	internal override IEnumerable<int> GetReachableOutputPorts(byte inputPortIndex)
	{
		return [];
	}

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		inputPorts.Add(CreatePort<InputPort>(InputPort));
	}

	/// <inheritdoc/>
	protected override void HandleMessage(InputPort receiverPort, GraphContext graphContext)
	{
		graphContext.Processor?.StopGraph();
	}
}
