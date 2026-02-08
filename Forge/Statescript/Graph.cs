// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Nodes;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Represents a Statescript graph consisting of nodes and connections.
/// </summary>
public class Graph
{
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
	/// Gets the variables associated with the graph.
	/// </summary>
	public Variables GraphVariables { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="Graph"/> class.
	/// </summary>
	public Graph()
	{
		EntryNode = new EntryNode();
		Nodes = [];
		Connections = [];
		GraphVariables = new Variables();
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
	}
}
