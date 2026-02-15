// Copyright © Gamesmiths Guild.

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
	public void EmitDisableSubgraphMessage(IGraphContext graphContext)
	{
		InputPort[] ports = FinalizedConnectedPorts!;

		for (var i = 0; i < ports.Length; i++)
		{
			ports[i].ReceiveDisableSubgraphMessage(graphContext);
		}
	}
}
