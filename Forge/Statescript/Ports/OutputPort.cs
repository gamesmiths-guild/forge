// Copyright © Gamesmiths Guild.

using System.Runtime.ExceptionServices;

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
	public event Action<Guid>? OnEmitDisableSubgraphMessage;

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

	internal void EmitMessage(GraphContext graphContext)
	{
		InputPort[] ports = FinalizedConnectedPorts!;
		ulong run = graphContext.RunStamp;
		ExceptionDispatchInfo? failure = null;

		// The run cannot finish part way through a message, since a connection it has yet to reach can start a node
		// that keeps it going. A connection that ends the graph - an Exit, an ability ended on the way - ends the
		// delivery with it, rather than reaching the rest in a graph that has been torn down or started over.
		graphContext.FinalizationDeferralCount++;

		try
		{
			for (int i = 0; i < ports.Length && graphContext.RunStamp == run; i++)
			{
				ports[i].ReceiveMessage(graphContext);
			}

			OnEmitMessage?.Invoke(PortID);
		}
		catch (Exception exception)
		{
			failure = ExceptionDispatchInfo.Capture(exception);
		}

		// A run that ended along the way took its count with it, and the next one keeps its own. One still going
		// completes once nothing is left running, even if a delivery threw, whose exception then propagates rather than
		// the completion's.
		if (graphContext.RunStamp == run)
		{
			graphContext.FinalizationDeferralCount--;
			ExceptionDispatchInfo? completion = graphContext.FinalizeIfIdle();
			failure ??= completion;
		}

		failure?.Throw();
	}

	internal void InternalEmitDisableSubgraphMessage(GraphContext graphContext)
	{
		InputPort[] ports = FinalizedConnectedPorts!;
		ulong run = graphContext.RunStamp;

		// A connection whose disabling ends the graph ends the rest of them too.
		for (int i = 0; i < ports.Length && graphContext.RunStamp == run; i++)
		{
			ports[i].ReceiveDisableSubgraphMessage(graphContext);
		}

		OnEmitDisableSubgraphMessage?.Invoke(PortID);
	}
}
