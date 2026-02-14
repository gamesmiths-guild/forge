// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Nodes;

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
	}
}
