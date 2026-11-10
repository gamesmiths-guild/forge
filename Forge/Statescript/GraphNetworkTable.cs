// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Statescript;

// What a networked game needs of a graph, worked out once from how it's built: the kind each node has in this graph,
// the states and actions a client runs as the server says, numbered for the wire, and the variables that travel.
internal sealed class GraphNetworkTable
{
	private readonly Dictionary<Node, NodeNetworkKind> _kinds = [];
	private readonly Dictionary<Node, int> _stateIndices = [];
	private readonly Dictionary<Node, int> _actionIndices = [];

	// The states a client starts and ends as the server says, by their number on the wire.
	public Node[] States { get; }

	// The actions a client executes when the server says they ran.
	public Node[] Actions { get; }

	// The graph's single value variables and value arrays, which a client gets as they change, by their number on the
	// wire. Object variables stay on each machine.
	public StringKey[] ValueVariables { get; }

	public StringKey[] ArrayVariables { get; }

	public bool RunsClientOnlyNodes { get; }

	// A client predicting the graph's entity leaves these out, so its run can't count activations as the server's does.
	public bool RunsServerOnlyNodes { get; }

	// A graph a client runs nothing of has nothing to send.
	public bool RunsOnClients => States.Length > 0 || Actions.Length > 0 || RunsClientOnlyNodes;

	public GraphNetworkTable(Graph graph)
	{
		GraphVariableDefinitions definitions = graph.VariableDefinitions;
		var made = new HashSet<StringKey>();

		_kinds[graph.EntryNode] = graph.EntryNode.NetworkKind;

		foreach (Node node in graph.Nodes)
		{
			_kinds[node] = node.NetworkKind;

			if (node.NetworkKind == NodeNetworkKind.Presentation)
			{
				AddMade(node, made);
			}
		}

		// A node acting on what a presentation node made is presentation in this graph, and so is one acting on what
		// that node made in turn.
		bool tainted;

		do
		{
			tainted = false;

			foreach (Node node in graph.Nodes)
			{
				if (_kinds[node] == NodeNetworkKind.Simulation && ReadsAny(node, made, definitions))
				{
					_kinds[node] = NodeNetworkKind.Presentation;
					AddMade(node, made);
					tainted = true;
				}
			}
		}
		while (tainted);

		var states = new List<Node>();
		var actions = new List<Node>();

		foreach (Node node in graph.Nodes)
		{
			RunsClientOnlyNodes |= node.Region == NodeRegion.ClientOnly;
			RunsServerOnlyNodes |= node.Region == NodeRegion.ServerOnly;

			if (node.Region != NodeRegion.None || _kinds[node] != NodeNetworkKind.Presentation)
			{
				continue;
			}

			if (node.IsStateNode)
			{
				_stateIndices[node] = states.Count;
				states.Add(node);
			}
			else if (node is ActionNode)
			{
				_actionIndices[node] = actions.Count;
				actions.Add(node);
			}
		}

		States = [.. states];
		Actions = [.. actions];
		ValueVariables = [.. definitions.VariableDefinitions.Select(x => x.Name)];
		ArrayVariables = [.. definitions.ArrayVariableDefinitions.Select(x => x.Name)];
	}

	public NodeNetworkKind KindOf(Node node)
	{
		return _kinds.TryGetValue(node, out NodeNetworkKind kind) ? kind : node.NetworkKind;
	}

	public int StateIndexOf(Node node)
	{
		return _stateIndices.TryGetValue(node, out int index) ? index : -1;
	}

	public int ActionIndexOf(Node node)
	{
		return _actionIndices.TryGetValue(node, out int index) ? index : -1;
	}

	// What a node made is something a client has too, unless the node never runs on one.
	private static void AddMade(Node node, HashSet<StringKey> made)
	{
		if (node.Region == NodeRegion.ServerOnly)
		{
			return;
		}

		foreach (OutputVariable output in node.OutputVariables)
		{
			if (output.BoundName != StringKey.Empty && output.Scope == VariableScope.Graph)
			{
				made.Add(output.BoundName);
			}
		}
	}

	private static bool ReadsAny(Node node, HashSet<StringKey> made, GraphVariableDefinitions definitions)
	{
		return Array.Exists(
			node.InputProperties,
			x => x.BoundName != StringKey.Empty && made.Contains(VariableBehind(x.BoundName, definitions)));
	}

	// The graph variable an input reads: the one it's bound to, or the one the property it's bound to reads as it is.
	// A variable read deeper inside a property isn't followed.
	private static StringKey VariableBehind(StringKey name, GraphVariableDefinitions definitions)
	{
		object? resolver = null;

		if (definitions.PropertiesByName.TryGetValue(name, out PropertyDefinition property))
		{
			resolver = property.Resolver;
		}
		else if (definitions.ObjectPropertiesByName.TryGetValue(name, out ObjectPropertyDefinition objectProperty))
		{
			resolver = objectProperty.Resolver;
		}
		else if (definitions.ArrayPropertiesByName.TryGetValue(name, out ArrayPropertyDefinition arrayProperty))
		{
			resolver = arrayProperty.Resolver;
		}
		else if (definitions.ObjectArrayPropertiesByName.TryGetValue(
			name,
			out ObjectArrayPropertyDefinition objectArrayProperty))
		{
			resolver = objectArrayProperty.Resolver;
		}

		return resolver is IVariableReader { Scope: VariableScope.Graph } reader ? reader.VariableName : name;
	}
}
