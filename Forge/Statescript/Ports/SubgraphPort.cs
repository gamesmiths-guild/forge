// Copyright © Gamesmiths Guild.

using System.Runtime.ExceptionServices;

namespace Gamesmiths.Forge.Statescript.Ports;

/// <summary>
/// Defines a subgraph output port that can emit disable subgraph messages to connected input ports.
/// </summary>
public class SubgraphPort : OutputPort
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SubgraphPort"/> class.
	/// </summary>
	public SubgraphPort()
	{
		PortID = Guid.NewGuid();
	}

	/// <summary>
	/// Emits a disable subgraph message to all connected input ports.
	/// </summary>
	/// <param name="graphContext">The graph context for the message.</param>
	public void EmitDisableSubgraphMessage(GraphContext graphContext)
	{
		InputPort[] ports = FinalizedConnectedPorts!;
		ulong run = graphContext.RunStamp;
		ExceptionDispatchInfo? failure = null;

		// A connection whose disabling ends the graph ends the rest of them too. One whose disabling throws still lets
		// the rest be disabled, so none is left running under what ended, and what threw first propagates after.
		for (int i = 0; i < ports.Length && graphContext.RunStamp == run; i++)
		{
			try
			{
				ports[i].ReceiveDisableSubgraphMessage(graphContext);
			}
			catch (Exception exception)
			{
				failure ??= ExceptionDispatchInfo.Capture(exception);
			}
		}

		failure?.Throw();
	}
}
