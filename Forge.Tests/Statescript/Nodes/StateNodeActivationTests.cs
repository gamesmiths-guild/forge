// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes;

public class StateNodeActivationTests
{
	[Fact]
	[Trait("Graph", "Activation")]
	public void An_activation_that_stops_the_graph_emits_nothing_after_it()
	{
		var graph = new Graph();
		var node = new StopsGraphNode(onActivate: true);
		TrackingActionNode onActivate = ConnectOnActivateTracker(graph, node);

		var processor = new GraphProcessor(graph);
		node.Processor = processor;
		processor.StartGraph();

		onActivate.ExecutionCount.Should().Be(0, "the graph stopped before the node finished activating");
	}

	[Fact]
	[Trait("Graph", "Activation")]
	public void A_restart_that_stops_the_graph_emits_nothing_after_it()
	{
		var graph = new Graph();
		var node = new StopsGraphNode(onActivate: false);
		TrackingActionNode onActivate = ConnectOnActivateTracker(graph, node);
		ConnectRetrigger(graph, node, 1.0);

		var processor = new GraphProcessor(graph);
		node.Processor = processor;
		processor.StartGraph();
		processor.UpdateGraph(1.0);

		onActivate.ExecutionCount.Should().Be(1, "the graph stopped before the restart finished");
	}

	[Fact]
	[Trait("Graph", "Activation")]
	public void An_abort_reached_from_OnActivate_leaves_the_subgraph_unstarted()
	{
		var graph = new Graph();
		var parent = new TrackingStateNode();
		var child = new TrackingStateNode();
		graph.AddNode(parent);
		graph.AddNode(child);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			parent.InputPorts[TrackingStateNode.InputPort]));
		graph.AddConnection(new Connection(
			parent.OutputPorts[TrackingStateNode.OnActivatePort],
			parent.InputPorts[TrackingStateNode.AbortPort]));
		graph.AddConnection(new Connection(
			parent.OutputPorts[TrackingStateNode.SubgraphPort],
			child.InputPorts[TrackingStateNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		parent.DeactivateCount.Should().Be(1);
		child.ActivateCount.Should().Be(0, "a subgraph started under an aborted node would have nothing to end it");
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "Activation")]
	public void An_abort_reached_from_OnActivate_replaces_the_ending_the_activation_deferred()
	{
		var graph = new Graph();
		var node = new EndsOnActivateNode();
		var keepAlive = new TrackingStateNode();
		TrackingActionNode onEnded = ConnectAbortedEndingNode(graph, node);
		graph.AddNode(keepAlive);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			keepAlive.InputPorts[TrackingStateNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		onEnded.ExecutionCount.Should().Be(0, "the node was aborted, so it did not end on its own");
	}

	[Fact]
	[Trait("Graph", "Activation")]
	public void An_abort_that_completes_the_graph_during_activation_drops_the_deferred_ending()
	{
		var graph = new Graph();
		var node = new EndsOnActivateNode();
		TrackingActionNode onEnded = ConnectAbortedEndingNode(graph, node);

		var processor = new GraphProcessor(graph);

		processor.Invoking(x => x.StartGraph()).Should().NotThrow(
			"the abort discarded the node's context along with the rest of the graph");
		onEnded.ExecutionCount.Should().Be(0);
	}

	private static TrackingActionNode ConnectOnActivateTracker(Graph graph, StopsGraphNode node)
	{
		var onActivate = new TrackingActionNode();
		graph.AddNode(node);
		graph.AddNode(onActivate);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[StopsGraphNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[StopsGraphNode.OnActivatePort],
			onActivate.InputPorts[ActionNode.InputPort]));

		return onActivate;
	}

	// The node asks to end during its activation, which is deferred until the activation completes, and its own
	// OnActivate port aborts it before then.
	private static TrackingActionNode ConnectAbortedEndingNode(Graph graph, EndsOnActivateNode node)
	{
		var onEnded = new TrackingActionNode();
		graph.AddNode(node);
		graph.AddNode(onEnded);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[EndsOnActivateNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[EndsOnActivateNode.OnActivatePort],
			node.InputPorts[EndsOnActivateNode.AbortPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[EndsOnActivateNode.OnEndedPort],
			onEnded.InputPorts[ActionNode.InputPort]));

		return onEnded;
	}

	// Stops the graph from its own activation or restart, as an effect it applies might by cancelling the ability the
	// graph runs for.
	private sealed class StopsGraphNode(bool onActivate) : StateNode<StateNodeContext>(restartOnRetrigger: true)
	{
		public GraphProcessor? Processor { get; set; }

		protected override void OnActivate(GraphContext graphContext)
		{
			if (onActivate)
			{
				Processor!.StopGraph();
			}
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
		}

		protected override void OnRestart(GraphContext graphContext)
		{
			Processor!.StopGraph();
		}
	}

	private sealed class EndsOnActivateNode : StateNode<StateNodeContext>
	{
		public const byte OnEndedPort = 4;

		protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
		{
			base.DefinePorts(inputPorts, outputPorts);
			outputPorts.Add(CreatePort<EventPort>(OnEndedPort, "OnEnded"));
		}

		protected override void OnActivate(GraphContext graphContext)
		{
			DeactivateNodeAndEmitMessage(graphContext, OnEndedPort);
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
		}
	}
}
