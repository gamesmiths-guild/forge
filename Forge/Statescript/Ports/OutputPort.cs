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
	/// Gets the number of input ports connected to this output port.
	/// </summary>
	internal int ConnectionCount => FinalizedConnectedPorts?.Length ?? PendingConnectedPorts.Count;

	/// <summary>
	/// Gets the finalized array of connected input ports. Only available after <see cref="FinalizeConnections"/>
	/// has been called.
	/// </summary>
	protected internal InputPort[]? FinalizedConnectedPorts { get; private set; }

	private List<InputPort> PendingConnectedPorts { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="OutputPort"/> class.
	/// </summary>
	protected OutputPort()
	{
		PendingConnectedPorts = [];
	}

	/// <summary>
	/// Connects an input port to this output port.
	/// </summary>
	/// <param name="inputPort">The input port to connect.</param>
	public void Connect(InputPort inputPort)
	{
		PendingConnectedPorts.Add(inputPort);
	}

	/// <summary>
	/// Gets the connected input port at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the connected port.</param>
	/// <returns>The connected input port.</returns>
	internal InputPort GetConnectedPort(int index)
	{
		return PendingConnectedPorts[index];
	}

	/// <summary>
	/// Disconnects an input port from this output port.
	/// </summary>
	/// <param name="inputPort">The input port to disconnect.</param>
	/// <returns><see langword="true"/> if the port was found and removed; <see langword="false"/> otherwise.</returns>
	internal bool Disconnect(InputPort inputPort)
	{
		return PendingConnectedPorts.Remove(inputPort);
	}

	/// <summary>
	/// Finalizes the connected ports list into a fixed array for optimal iteration performance.
	/// Should be called after all connections have been established.
	/// </summary>
	internal void FinalizeConnections()
	{
		FinalizedConnectedPorts = [.. PendingConnectedPorts];
	}

	internal void EmitMessage(IGraphContext graphContext)
	{
		InputPort[] ports = FinalizedConnectedPorts!;

		for (var i = 0; i < ports.Length; i++)
		{
			ports[i].ReceiveMessage(graphContext);
		}

		OnEmitMessage?.Invoke(PortID);
	}

	internal void InternalEmitDisableSubgraphMessage(IGraphContext graphContext)
	{
		InputPort[] ports = FinalizedConnectedPorts!;

		for (var i = 0; i < ports.Length; i++)
		{
			ports[i].ReceiveDisableSubgraphMessage(graphContext);
		}

		OnEmitMessageDisableSubgraphMessage?.Invoke(PortID);
	}
}
