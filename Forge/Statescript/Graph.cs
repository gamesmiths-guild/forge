// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Represents a Statescript graph definition consisting of nodes and connections. A <see cref="Graph"/> instance is
/// constructed once and can then be shared across multiple <see cref="GraphProcessor"/> instances (Flyweight pattern).
/// Each runner pairs the shared graph with its own <see cref="IGraphContext"/>, which holds all mutable runtime state.
/// </summary>
/// <remarks>
/// <para>The Flyweight boundary is at the <see cref="Graph"/> level, not the individual <see cref="Node"/> level.
/// Nodes own their ports, and ports store connection data, so a given <see cref="Node"/> instance is bound to the
/// graph that wired it. Do not add the same <see cref="Node"/> instance to multiple graphs.</para>
/// <para>All mutable runtime state (variable values, node contexts, activation status) lives in
/// <see cref="IGraphContext"/>.</para>
/// </remarks>
public class Graph
{
	/// <summary>
	/// Sentinel value indicating the node was entered via disable-subgraph, not a specific input port.
	/// </summary>
	private const byte DisableSubgraphEntry = byte.MaxValue;

	/// <summary>
	/// Gets the entry node of the graph.
	/// </summary>
	public EntryNode EntryNode { get; }

	/// <summary>
	/// Gets the list of nodes in the graph.
	/// </summary>
	public List<Node> Nodes { get; }

	/// <summary>
	/// Gets the list of connections between nodes in the graph.
	/// </summary>
	public List<Connection> Connections { get; }

	/// <summary>
	/// Gets the variable and property definitions for the graph. These define the schema (names, initial values, and
	/// property resolvers) that will be used to initialize runtime <see cref="Variables"/> instances when a graph
	/// execution starts.
	/// </summary>
	public GraphVariableDefinitions VariableDefinitions { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="Graph"/> class.
	/// </summary>
	public Graph()
	{
		EntryNode = new EntryNode();
		Nodes = [];
		Connections = [];
		VariableDefinitions = new GraphVariableDefinitions();
	}

	/// <summary>
	/// Adds a node to the graph.
	/// </summary>
	/// <param name="node">The node to add.</param>
	public virtual void AddNode(Node node)
	{
		Nodes.Add(node);
	}

	/// <summary>
	/// Adds a connection between nodes in the graph.
	/// </summary>
	/// <param name="connection">The connection to add.</param>
	public virtual void AddConnection(Connection connection)
	{
		Connections.Add(connection);

		connection.OutputPort.Connect(connection.InputPort);

		if (Validation.Enabled)
		{
			ValidateNoLoops(connection);
		}
	}

	internal void FinalizeConnections()
	{
		FinalizeNodePorts(EntryNode);

		for (var i = 0; i < Nodes.Count; i++)
		{
			FinalizeNodePorts(Nodes[i]);
		}
	}

	private static void FinalizeNodePorts(Node node)
	{
		for (var i = 0; i < node.OutputPorts.Length; i++)
		{
			node.OutputPorts[i].FinalizeConnections();
		}
	}

	private static bool IsLoopDetected(Node nextNode, byte nextEntryMode, Node targetNode, byte targetInputIndex)
	{
		return nextNode.NodeID == targetNode.NodeID && nextEntryMode == targetInputIndex;
	}

	private void ValidateNoLoops(Connection newConnection)
	{
		Node? targetNode = newConnection.InputPort.OwnerNode;

		if (targetNode is null)
		{
			return;
		}

		var targetInputIndex = newConnection.InputPort.Index;

		// The visited set tracks (NodeId, EntryMode) where EntryMode is either a specific input port index (for message
		// propagation) or DisableSubgraphEntry (for disable-subgraph cascades).
		var visited = new HashSet<NodeEntryKey>();
		var stack = new Stack<NodeEntryState>();

		stack.Push(new NodeEntryState(targetNode, targetInputIndex));

		while (stack.Count > 0)
		{
			NodeEntryState entry = stack.Pop();

			if (!visited.Add(new NodeEntryKey(entry.Node.NodeID, entry.EntryMode)))
			{
				continue;
			}

			if (entry.EntryMode == DisableSubgraphEntry)
			{
				// Disable-subgraph entry: the node's BeforeDisable may fire regular messages on some ports, then ALL
				// output ports propagate the disable-subgraph signal downstream.
				EnqueueMessagePortEdges(
					entry.Node.GetMessagePortsOnDisable(),
					entry.Node,
					stack,
					targetNode,
					targetInputIndex,
					newConnection);

				EnqueueDisableSubgraphEdges(
					entry.Node,
					stack,
					targetNode,
					targetInputIndex,
					newConnection);
			}
			else
			{
				// Message entry on a specific input port: follow the declared reachable output ports.
				EnqueueReachablePortEdges(
					entry.EntryMode,
					entry.Node,
					stack,
					targetNode,
					targetInputIndex,
					newConnection);
			}
		}
	}

	private void EnqueueReachablePortEdges(
		byte inputIndex,
		Node current,
		Stack<NodeEntryState> stack,
		Node targetNode,
		byte targetInputIndex,
		Connection newConnection)
	{
		foreach (var outputIndex in current.GetReachableOutputPorts(inputIndex))
		{
			if (outputIndex < 0 || outputIndex >= current.OutputPorts.Length)
			{
				continue;
			}

			OutputPort outputPort = current.OutputPorts[outputIndex];

			for (var connectionIndex = 0; connectionIndex < outputPort.ConnectionCount; connectionIndex++)
			{
				InputPort connectedInput = outputPort.GetConnectedPort(connectionIndex);
				Node? nextNode = connectedInput.OwnerNode;

				if (nextNode is null)
				{
					continue;
				}

				// SubgraphPorts that appear in GetReachableOutputPorts use EmitMessage (regular message), not
				// EmitDisableSubgraphMessage. The caller (HandleMessage) calls OutputPorts[SubgraphPort].EmitMessage()
				// which triggers ReceiveMessage on the target.
				var nextEntryMode = connectedInput.Index;

				if (IsLoopDetected(nextNode, nextEntryMode, targetNode, targetInputIndex))
				{
					RejectConnection(newConnection, current, outputIndex, targetNode, targetInputIndex);
					return;
				}

				stack.Push(new NodeEntryState(nextNode, nextEntryMode));
			}
		}
	}

	private void EnqueueMessagePortEdges(
		IEnumerable<int> messagePortIndices,
		Node current,
		Stack<NodeEntryState> stack,
		Node targetNode,
		byte targetInputIndex,
		Connection newConnection)
	{
		foreach (var outputIndex in messagePortIndices)
		{
			if (outputIndex < 0 || outputIndex >= current.OutputPorts.Length)
			{
				continue;
			}

			OutputPort outputPort = current.OutputPorts[outputIndex];

			for (var connectionIndex = 0; connectionIndex < outputPort.ConnectionCount; connectionIndex++)
			{
				InputPort connectedInput = outputPort.GetConnectedPort(connectionIndex);
				Node? nextNode = connectedInput.OwnerNode;

				if (nextNode is null)
				{
					continue;
				}

				var nextEntryMode = connectedInput.Index;

				if (IsLoopDetected(nextNode, nextEntryMode, targetNode, targetInputIndex))
				{
					RejectConnection(newConnection, current, outputIndex, targetNode, targetInputIndex);
					return;
				}

				stack.Push(new NodeEntryState(nextNode, nextEntryMode));
			}
		}
	}

	private void EnqueueDisableSubgraphEdges(
		Node current,
		Stack<NodeEntryState> stack,
		Node targetNode,
		byte targetInputIndex,
		Connection newConnection)
	{
		for (var outputIndex = 0; outputIndex < current.OutputPorts.Length; outputIndex++)
		{
			OutputPort outputPort = current.OutputPorts[outputIndex];

			for (var connectionIndex = 0; connectionIndex < outputPort.ConnectionCount; connectionIndex++)
			{
				InputPort connectedInput = outputPort.GetConnectedPort(connectionIndex);
				Node? nextNode = connectedInput.OwnerNode;

				if (nextNode is null)
				{
					continue;
				}

				if (IsLoopDetected(nextNode, DisableSubgraphEntry, targetNode, targetInputIndex))
				{
					RejectConnection(newConnection, current, outputIndex, targetNode, targetInputIndex);
					return;
				}

				stack.Push(new NodeEntryState(nextNode, DisableSubgraphEntry));
			}
		}
	}

	private void RejectConnection(
		Connection connection,
		Node throughNode,
		int outputIndex,
		Node targetNode,
		byte targetInputIndex)
	{
		Connections.Remove(connection);
		connection.OutputPort.Disconnect(connection.InputPort);
		Validation.Fail(
			$"Adding this connection creates a loop: the message path from node '{targetNode.GetType().Name}' (input " +
			$"port {targetInputIndex}) reaches back to itself through node '{throughNode.GetType().Name}' (output " +
			$"port {outputIndex}).");
	}

	private readonly record struct NodeEntryKey(Guid NodeId, byte EntryMode);

	private readonly record struct NodeEntryState(Node Node, byte EntryMode);
}
