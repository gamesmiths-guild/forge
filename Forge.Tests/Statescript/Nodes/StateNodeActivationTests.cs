// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Statescript.Ports;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes;

public class StateNodeActivationTests
{
	[Fact]
	[Trait("Graph", "Activation")]
	public void A_connection_that_stops_the_graph_ends_the_delivery_to_the_rest()
	{
		var graph = new Graph();
		var node = new TrackingStateNode();
		var exit = new ExitNode();
		var later = new TrackingStateNode();
		graph.AddNode(node);
		graph.AddNode(exit);
		graph.AddNode(later);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[TrackingStateNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[TrackingStateNode.OnActivatePort],
			exit.InputPorts[ExitNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[TrackingStateNode.OnActivatePort],
			later.InputPorts[TrackingStateNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		later.ActivateCount.Should().Be(0, "a node reached after the graph stopped would run in a graph that is gone");
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "Activation")]
	public void A_stop_reached_from_one_ending_port_ends_the_ones_after_it()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("interval", 1.0);
		graph.VariableDefinitions.DefineVariable("loops", 1);

		// Its last interval ends it through OnInterval and then OnFinished.
		var loopTimer = new LoopTimerNode();
		loopTimer.BindInput(LoopTimerNode.IntervalInput, "interval");
		loopTimer.BindInput(LoopTimerNode.LoopCountInput, "loops");
		var exit = new ExitNode();
		var later = new TrackingStateNode();
		graph.AddNode(loopTimer);
		graph.AddNode(exit);
		graph.AddNode(later);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			loopTimer.InputPorts[LoopTimerNode.InputPort]));
		graph.AddConnection(new Connection(
			loopTimer.OutputPorts[LoopTimerNode.OnIntervalPort],
			exit.InputPorts[ExitNode.InputPort]));
		graph.AddConnection(new Connection(
			loopTimer.OutputPorts[LoopTimerNode.OnFinishedPort],
			later.InputPorts[TrackingStateNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		processor.UpdateGraph(1.0);

		later.ActivateCount.Should().Be(0, "a node reached after the graph stopped would run in a graph that is gone");
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "Activation")]
	public void A_stop_reached_from_one_port_ends_the_ports_emitted_after_it()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("condition", false);

		// Becoming true emits OnBecameTrue and then the true subgraph.
		var monitor = new ConditionMonitorNode();
		monitor.BindInput(ConditionMonitorNode.ConditionInput, "condition");
		var exit = new ExitNode();
		var later = new TrackingStateNode();
		graph.AddNode(monitor);
		graph.AddNode(exit);
		graph.AddNode(later);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			monitor.InputPorts[ConditionMonitorNode.InputPort]));
		graph.AddConnection(new Connection(
			monitor.OutputPorts[ConditionMonitorNode.OnBecameTruePort],
			exit.InputPorts[ExitNode.InputPort]));
		graph.AddConnection(new Connection(
			monitor.OutputPorts[ConditionMonitorNode.TrueSubgraphPort],
			later.InputPorts[TrackingStateNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		processor.GraphContext.GraphVariables.SetVar("condition", true);
		processor.UpdateGraph(1.0);

		later.ActivateCount.Should().Be(0, "a node reached after the graph stopped would run in a graph that is gone");
		processor.GraphContext.IsActive.Should().BeFalse();
	}

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
	public void An_activation_whose_graph_starts_over_emits_nothing_into_the_new_run()
	{
		var graph = new Graph();
		var node = new DefersThenStopsGraphNode();
		var onActivate = new TrackingActionNode();
		var subgraph = new TrackingActionNode();
		var onEmitted = new TrackingActionNode();
		var onEnded = new TrackingActionNode();
		graph.AddNode(node);
		graph.AddNode(onActivate);
		graph.AddNode(subgraph);
		graph.AddNode(onEmitted);
		graph.AddNode(onEnded);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[DefersThenStopsGraphNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[DefersThenStopsGraphNode.OnActivatePort],
			onActivate.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[DefersThenStopsGraphNode.SubgraphPort],
			subgraph.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[DefersThenStopsGraphNode.OnEmittedPort],
			onEmitted.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[DefersThenStopsGraphNode.OnEndedPort],
			onEnded.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		node.Processor = processor;
		RestartOnFirstCompletion(processor);
		processor.StartGraph();

		// Only the new run's activation got as far as emitting, and what the stopped one deferred went with it.
		onActivate.ExecutionCount.Should().Be(1);
		subgraph.ExecutionCount.Should().Be(1);
		node.ActivatedCount.Should().Be(1);
		onEmitted.ExecutionCount.Should().Be(0);
		onEnded.ExecutionCount.Should().Be(0);
		IsActive(processor, node).Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "Activation")]
	public void A_deactivation_whose_graph_starts_over_leaves_the_new_run_running()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 1.0);

		// The timer's ending reaches an Exit before its subgraph is disabled.
		TimerNode timer = CreateTimerNode("duration");
		var child = new TrackingStateNode();
		var exit = new ExitNode();
		graph.AddNode(timer);
		graph.AddNode(child);
		graph.AddNode(exit);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[TimerNode.InputPort]));
		graph.AddConnection(new Connection(
			timer.OutputPorts[TimerNode.OnDeactivatePort],
			exit.InputPorts[ExitNode.InputPort]));
		graph.AddConnection(new Connection(
			timer.OutputPorts[TimerNode.SubgraphPort],
			child.InputPorts[TrackingStateNode.InputPort]));

		var processor = new GraphProcessor(graph);
		RestartOnFirstCompletion(processor);
		processor.StartGraph();
		processor.UpdateGraph(1.0);

		child.ActivateCount.Should().Be(2);
		child.DeactivateCount.Should().Be(1, "the stop ended the first run's child, and nothing ends the new one's");
	}

	[Fact]
	[Trait("Graph", "Activation")]
	public void An_abort_whose_graph_starts_over_leaves_the_new_run_running()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("abortDelay", 1.0);

		// The node's OnAbort reaches an Exit before the node itself is deactivated.
		var node = new TrackingStateNode();
		TimerNode abortTimer = CreateTimerNode("abortDelay");
		var exit = new ExitNode();
		graph.AddNode(node);
		graph.AddNode(abortTimer);
		graph.AddNode(exit);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[TrackingStateNode.InputPort]));
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			abortTimer.InputPorts[TimerNode.InputPort]));
		graph.AddConnection(new Connection(
			abortTimer.OutputPorts[TimerNode.OnTimerEndPort],
			node.InputPorts[TrackingStateNode.AbortPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[TrackingStateNode.OnAbortPort],
			exit.InputPorts[ExitNode.InputPort]));

		var processor = new GraphProcessor(graph);
		RestartOnFirstCompletion(processor);
		processor.StartGraph();
		processor.UpdateGraph(1.0);

		node.ActivateCount.Should().Be(2);
		node.DeactivateCount.Should().Be(1, "the stop ended the first run's node, and nothing ends the new one's");
	}

	[Fact]
	[Trait("Graph", "Activation")]
	public void A_subgraph_whose_disabling_starts_the_graph_over_leaves_the_new_run_running()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 1.0);

		// Disabling the timer's subgraph reaches a node that exits as it is disabled, and each of the two before it
		// has a sibling still to disable.
		TimerNode timer = CreateTimerNode("duration");
		var parent = new TrackingStateNode();
		var parentSibling = new TrackingStateNode();
		var exiting = new TrackingStateNode();
		var exitingSibling = new TrackingStateNode();
		var exit = new ExitNode();
		graph.AddNode(timer);
		graph.AddNode(parent);
		graph.AddNode(parentSibling);
		graph.AddNode(exiting);
		graph.AddNode(exitingSibling);
		graph.AddNode(exit);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[TimerNode.InputPort]));
		graph.AddConnection(new Connection(
			timer.OutputPorts[TimerNode.SubgraphPort],
			parent.InputPorts[TrackingStateNode.InputPort]));
		graph.AddConnection(new Connection(
			timer.OutputPorts[TimerNode.SubgraphPort],
			parentSibling.InputPorts[TrackingStateNode.InputPort]));
		graph.AddConnection(new Connection(
			parent.OutputPorts[TrackingStateNode.SubgraphPort],
			exiting.InputPorts[TrackingStateNode.InputPort]));
		graph.AddConnection(new Connection(
			parent.OutputPorts[TrackingStateNode.SubgraphPort],
			exitingSibling.InputPorts[TrackingStateNode.InputPort]));
		graph.AddConnection(new Connection(
			exiting.OutputPorts[TrackingStateNode.OnDeactivatePort],
			exit.InputPorts[ExitNode.InputPort]));

		var processor = new GraphProcessor(graph);
		RestartOnFirstCompletion(processor);
		processor.StartGraph();
		processor.UpdateGraph(1.0);

		IsActive(processor, parentSibling).Should().BeTrue("the first run's disabling ends none of the next run's");
		IsActive(processor, exitingSibling).Should().BeTrue("the first run's disabling ends none of the next run's");
	}

	[Fact]
	[Trait("Graph", "Activation")]
	public void A_node_whose_disabling_starts_the_graph_over_leaves_its_new_subgraph_running()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 1.0);

		// The timer's subgraph node exits as it is disabled, before its own subgraph is.
		TimerNode timer = CreateTimerNode("duration");
		var node = new TrackingStateNode();
		var child = new TrackingStateNode();
		var exit = new ExitNode();
		graph.AddNode(timer);
		graph.AddNode(node);
		graph.AddNode(child);
		graph.AddNode(exit);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[TimerNode.InputPort]));
		graph.AddConnection(new Connection(
			timer.OutputPorts[TimerNode.SubgraphPort],
			node.InputPorts[TrackingStateNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[TrackingStateNode.OnDeactivatePort],
			exit.InputPorts[ExitNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[TrackingStateNode.SubgraphPort],
			child.InputPorts[TrackingStateNode.InputPort]));

		var processor = new GraphProcessor(graph);
		RestartOnFirstCompletion(processor);
		processor.StartGraph();
		processor.UpdateGraph(1.0);

		IsActive(processor, child).Should().BeTrue("the first run's disabling ends none of the next run's");
	}

	[Fact]
	[Trait("Graph", "Activation")]
	public void A_node_started_by_one_being_disabled_emits_nothing_once_it_stops_the_graph()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 1.0);

		// The timer's subgraph node starts the stopping one as it is disabled.
		TimerNode timer = CreateTimerNode("duration");
		var disabled = new TrackingStateNode();
		var node = new StopsGraphNode(onActivate: true);
		var onActivate = new TrackingActionNode();
		graph.AddNode(timer);
		graph.AddNode(disabled);
		graph.AddNode(node);
		graph.AddNode(onActivate);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[TimerNode.InputPort]));
		graph.AddConnection(new Connection(
			timer.OutputPorts[TimerNode.SubgraphPort],
			disabled.InputPorts[TrackingStateNode.InputPort]));
		graph.AddConnection(new Connection(
			disabled.OutputPorts[TrackingStateNode.OnDeactivatePort],
			node.InputPorts[StopsGraphNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[StopsGraphNode.OnActivatePort],
			onActivate.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		node.Processor = processor;
		processor.StartGraph();
		processor.UpdateGraph(1.0);

		onActivate.ExecutionCount.Should().Be(0, "the graph stopped before the node finished activating");
	}

	[Fact]
	[Trait("Graph", "Activation")]
	public void A_node_started_from_outside_the_graph_emits_nothing_once_it_stops_the_graph()
	{
		var graph = new Graph();
		var keepAlive = new TrackingStateNode();
		var node = new StopsGraphNode(onActivate: true);
		var onActivate = new TrackingActionNode();
		graph.AddNode(keepAlive);
		graph.AddNode(node);
		graph.AddNode(onActivate);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			keepAlive.InputPorts[TrackingStateNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[StopsGraphNode.OnActivatePort],
			onActivate.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		node.Processor = processor;
		processor.StartGraph();

		// Nothing in the graph leads to the node, so the stop never reaches it, and the run ending is all it has left
		// to go by.
		node.InputPorts[StopsGraphNode.InputPort].ReceiveMessage(processor.GraphContext);

		onActivate.ExecutionCount.Should().Be(0, "the graph stopped before the node finished activating");
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

	private static bool IsActive(GraphProcessor processor, Node node)
	{
		return processor.GraphContext.GetNodeContext<StateNodeContext>(node.NodeID).Active;
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

	// Emits and asks to end from its activation, both of which wait for the activation to complete, and stops the graph
	// before it does. It does so once, as the ability it cancels is then gone.
	private sealed class DefersThenStopsGraphNode : StateNode<StateNodeContext>
	{
		public const byte OnEmittedPort = 4;

		public const byte OnEndedPort = 5;

		public GraphProcessor? Processor { get; set; }

		public int ActivatedCount { get; private set; }

		protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
		{
			base.DefinePorts(inputPorts, outputPorts);
			outputPorts.Add(CreatePort<EventPort>(OnEmittedPort, "OnEmitted"));
			outputPorts.Add(CreatePort<EventPort>(OnEndedPort, "OnEnded"));
		}

		protected override void OnActivate(GraphContext graphContext)
		{
			GraphProcessor? processor = Processor;

			if (processor is null)
			{
				return;
			}

			Processor = null;
			EmitMessage(graphContext, OnEmittedPort);
			DeactivateNodeAndEmitMessage(graphContext, OnEndedPort);
			processor.StopGraph();
		}

		protected override void OnActivated(GraphContext graphContext)
		{
			ActivatedCount++;
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
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
