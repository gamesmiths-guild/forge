// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes;

public class StateNodeActivationTests
{
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
