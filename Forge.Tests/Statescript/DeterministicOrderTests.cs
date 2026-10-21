// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Tests.Statescript;

/// <summary>
/// Covers the order a graph's updates walk its active state nodes in: graph order, whatever order they activated in.
/// </summary>
public class DeterministicOrderTests
{
	[Fact]
	[Trait("Graph", "Order")]
	public void Nodes_are_indexed_in_the_order_they_were_added_once_the_graph_starts()
	{
		var log = new List<string>();
		var first = new LoggingStateNode("first", log);
		var second = new LoggingStateNode("second", log);

		var graph = new Graph();
		graph.AddNode(first);
		graph.AddNode(second);

		new GraphProcessor(graph).StartGraph();

		graph.EntryNode.Index.Should().Be(0);
		first.Index.Should().Be(1);
		second.Index.Should().Be(2);
	}

	[Fact]
	[Trait("Graph", "Order")]
	public void Fixed_updates_walk_active_nodes_in_graph_order_not_activation_order()
	{
		var log = new List<string>();
		GraphProcessor processor = StartInReverseActivationOrder(log);
		log.Should().Equal("activate c", "activate b", "activate a");
		log.Clear();

		processor.FixedUpdateGraph(1.0 / 60.0);

		log.Should().Equal("fixed a", "fixed b", "fixed c");
	}

	[Fact]
	[Trait("Graph", "Order")]
	public void Frame_updates_walk_active_nodes_in_graph_order_not_activation_order()
	{
		var log = new List<string>();
		GraphProcessor processor = StartInReverseActivationOrder(log);
		log.Clear();

		processor.UpdateGraph(0.016);

		log.Should().Equal("frame a", "frame b", "frame c");
	}

	// Adds a, b, c in that order but wires the entry to c first, so the nodes activate c, b, a.
	private static GraphProcessor StartInReverseActivationOrder(List<string> log)
	{
		var a = new LoggingStateNode("a", log);
		var b = new LoggingStateNode("b", log);
		var c = new LoggingStateNode("c", log);

		var graph = new Graph();
		graph.AddNode(a);
		graph.AddNode(b);
		graph.AddNode(c);

		OutputPort entry = graph.EntryNode.OutputPorts[EntryNode.OutputPort];
		graph.AddConnection(new Connection(entry, c.InputPorts[StateNode<StateNodeContext>.InputPort]));
		graph.AddConnection(new Connection(entry, b.InputPorts[StateNode<StateNodeContext>.InputPort]));
		graph.AddConnection(new Connection(entry, a.InputPorts[StateNode<StateNodeContext>.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		return processor;
	}

	private sealed class LoggingStateNode(string name, List<string> log) : StateNode<StateNodeContext>
	{
		private readonly string _name = name;

		private readonly List<string> _log = log;

		protected override void OnActivate(GraphContext graphContext)
		{
			_log.Add($"activate {_name}");
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
		}

		protected override void OnUpdate(double deltaTime, GraphContext graphContext)
		{
			_log.Add($"frame {_name}");
		}

		protected override void OnFixedUpdate(double deltaTime, GraphContext graphContext)
		{
			_log.Add($"fixed {_name}");
		}
	}
}
