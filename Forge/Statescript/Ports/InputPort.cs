// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Ports;

/// <summary>
/// Defines an input port that can receive messages in the Statescript system.
/// </summary>
public class InputPort : Port
{
	private Node? _ownerNode;

	/// <summary>
	/// Initializes a new instance of the <see cref="InputPort"/> class.
	/// </summary>
	public InputPort()
	{
		PortID = Guid.NewGuid();
	}

	/// <summary>
	/// Sets the owner node of this input port.
	/// </summary>
	/// <param name="ownerNode">The owner node to set.</param>
	public void SetOwnerNode(Node ownerNode)
	{
		_ownerNode = ownerNode;
	}

	/// <summary>
	/// Receives a message and notifies the owner node.
	/// </summary>
	/// <param name="graphVariables">The graph variables to include in the message.</param>
	/// <param name="graphContext">The graph context for the message.</param>
	public void ReceiveMessage(Variables graphVariables, IGraphContext graphContext)
	{
		_ownerNode?.OnMessageReceived(this, graphVariables, graphContext);
	}

	/// <summary>
	/// Receives a disable subgraph message and notifies the owner node.
	/// </summary>
	/// <param name="graphVariables">The graph variables to include in the message.</param>
	/// <param name="graphContext">The graph context for the message.</param>
	public void ReceiveDisableSubgraphMessage(Variables graphVariables, IGraphContext graphContext)
	{
		_ownerNode?.OnSubgraphDisabledMessageReceived(graphVariables, graphContext);
	}
}
