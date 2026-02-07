// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Ports;

/// <summary>
/// Defines a subgraph output port that can emit disable subgraph messages to connected input ports.
/// </summary>
[Serializable]
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
	/// <param name="graphVariables">The graph variables to include in the message.</param>
	/// <param name="graphContext">The graph context for the message.</param>
	public void EmitDisableSubgraphMessage(Variables graphVariables, IGraphContext graphContext)
	{
		foreach (InputPort inputPort in ConnectedPorts)
		{
			inputPort.ReceiveDisableSubgraphMessage(graphVariables, graphContext);
		}
	}
}
