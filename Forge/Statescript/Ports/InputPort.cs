// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Ports;

/// <summary>
/// Defines an input port that can receive messages in the Statescript system.
/// </summary>
public class InputPort : Port
{
	/// <summary>
	/// Gets the node that owns this input port.
	/// </summary>
	internal Node? OwnerNode { get; private set; }

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
		OwnerNode = ownerNode;
	}

	/// <summary>
	/// Receives a message and notifies the owner node.
	/// </summary>
	/// <param name="graphContext">The graph context for the message.</param>
	public void ReceiveMessage(IGraphContext graphContext)
	{
		OwnerNode?.OnMessageReceived(this, graphContext);
	}

	/// <summary>
	/// Receives a disable subgraph message and notifies the owner node.
	/// </summary>
	/// <param name="graphContext">The graph context for the message.</param>
	public void ReceiveDisableSubgraphMessage(IGraphContext graphContext)
	{
		OwnerNode?.OnSubgraphDisabledMessageReceived(graphContext);
	}
}
