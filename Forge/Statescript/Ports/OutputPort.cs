// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Ports;

/// <summary>
/// Defines an output port that can emit messages to connected input ports in the Statescript system.
/// </summary>
public class OutputPort : Port
{
	/// <summary>
	/// Event triggered when a message is emitted from this output port.
	/// </summary>
	public event Action<Guid>? OnEmitMessage;

	/// <summary>
	/// Event triggered when a disable subgraph message is emitted from this output port.
	/// </summary>
	public event Action<Guid>? OnEmitMessageDisableSubgraphMessage;

	/// <summary>
	/// Gets the list of input ports connected to this output port.
	/// </summary>
	/// TODO: Convert to array.
	protected List<InputPort> ConnectedPorts { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="OutputPort"/> class.
	/// </summary>
	protected OutputPort()
	{
		ConnectedPorts = [];
	}

	/// <summary>
	/// Connects an input port to this output port.
	/// </summary>
	/// <param name="inputPort">The input port to connect.</param>
	public void Connect(InputPort inputPort)
	{
		ConnectedPorts.Add(inputPort);
	}

	internal void EmitMessage(IGraphContext graphContext)
	{
		foreach (InputPort inputPort in ConnectedPorts)
		{
			inputPort.ReceiveMessage(graphContext);
		}

		OnEmitMessage?.Invoke(PortID);
	}

	internal void InternalEmitDisableSubgraphMessage(IGraphContext graphContext)
	{
		foreach (InputPort inputPort in ConnectedPorts)
		{
			inputPort.ReceiveDisableSubgraphMessage(graphContext);
		}

		OnEmitMessageDisableSubgraphMessage?.Invoke(PortID);
	}
}
