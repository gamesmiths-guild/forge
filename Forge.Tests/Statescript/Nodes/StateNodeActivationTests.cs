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
	public void An_activation_ended_and_started_again_from_inside_it_leaves_the_new_one_to_itself()
	{
		var graph = new Graph();
		var node = new StartsAgainNode();
		var onActivate = new TrackingActionNode();
		var onEmitted = new TrackingActionNode();
		graph.AddNode(node);
		graph.AddNode(onActivate);
		graph.AddNode(onEmitted);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[StartsAgainNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[StartsAgainNode.OnActivatePort],
			onActivate.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[StartsAgainNode.OnEmittedPort],
			onEmitted.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		// Only the activation that is still running emits, and what the ended one deferred went with it.
		onActivate.ExecutionCount.Should().Be(1);
		onEmitted.ExecutionCount.Should().Be(1);
		node.ActivatedCount.Should().Be(1);
		IsActive(processor, node).Should().BeTrue();
	}

	[Theory]
	[Trait("Graph", "Activation")]
	[InlineData(false)]
	[InlineData(true)]
	public void A_node_ended_and_started_again_from_its_update_leaves_the_new_activation_to_itself(bool fixedStep)
	{
		var graph = new Graph();
		var keepAlive = new TrackingStateNode();
		var node = new StartsAgainOnUpdateNode();
		var onUpdated = new TrackingActionNode();
		graph.AddNode(keepAlive);
		graph.AddNode(node);
		graph.AddNode(onUpdated);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			keepAlive.InputPorts[TrackingStateNode.InputPort]));
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[StartsAgainOnUpdateNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[StartsAgainOnUpdateNode.OnUpdatedPort],
			onUpdated.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		if (fixedStep)
		{
			processor.FixedUpdateGraph(1.0);
		}
		else
		{
			processor.UpdateGraph(1.0);
		}

		onUpdated.ExecutionCount.Should().Be(0, "the update that would report belongs to the activation that ended");
		IsActive(processor, node).Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "Activation")]
	public void A_node_ended_and_started_again_by_one_port_emits_none_after_it()
	{
		var graph = new Graph();
		var keepAlive = new TrackingStateNode();
		var node = new ReportsTwiceNode();
		var startAgain = new EndsAndStartsNodeOnceNode(node);
		var onSecond = new TrackingActionNode();
		graph.AddNode(keepAlive);
		graph.AddNode(node);
		graph.AddNode(startAgain);
		graph.AddNode(onSecond);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			keepAlive.InputPorts[TrackingStateNode.InputPort]));
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ReportsTwiceNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[ReportsTwiceNode.OnFirstPort],
			startAgain.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[ReportsTwiceNode.OnSecondPort],
			onSecond.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		// Reported from outside the graph's own calls, as an event handler does.
		node.Report(processor.GraphContext);

		onSecond.ExecutionCount.Should().Be(0, "the report belongs to the activation that ended");
		IsActive(processor, node).Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "Activation")]
	public void A_node_started_again_once_its_last_activation_is_over_keeps_its_context()
	{
		var graph = new Graph();
		var keepAlive = new TrackingStateNode();
		var node = new TrackingStateNode();
		graph.AddNode(keepAlive);
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			keepAlive.InputPorts[TrackingStateNode.InputPort]));
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[TrackingStateNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		StateNodeContext context = processor.GraphContext.GetNodeContext<StateNodeContext>(node.NodeID);

		node.InputPorts[TrackingStateNode.AbortPort].ReceiveMessage(processor.GraphContext);
		node.InputPorts[TrackingStateNode.InputPort].ReceiveMessage(processor.GraphContext);

		processor.GraphContext.GetNodeContext<StateNodeContext>(node.NodeID).Should().BeSameAs(context);
		context.Active.Should().BeTrue();
		context.WasAborted.Should().BeFalse("the abort ended the activation before this one");
	}

	[Fact]
	[Trait("Graph", "Activation")]
	public void A_node_ended_and_started_again_from_its_abort_leaves_the_new_activation_running()
	{
		var graph = new Graph();
		var keepAlive = new TrackingStateNode();
		var node = new TrackingStateNode();
		var startAgain = new EndsAndStartsNodeOnceNode(node);
		graph.AddNode(keepAlive);
		graph.AddNode(node);
		graph.AddNode(startAgain);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			keepAlive.InputPorts[TrackingStateNode.InputPort]));
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[TrackingStateNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[TrackingStateNode.OnAbortPort],
			startAgain.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		node.InputPorts[TrackingStateNode.AbortPort].ReceiveMessage(processor.GraphContext);

		node.ActivateCount.Should().Be(2);
		node.DeactivateCount.Should().Be(1, "the abort ends only the activation it reached");
		IsActive(processor, node).Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "Activation")]
	public void A_message_reaching_a_node_that_is_still_deactivating_is_ignored()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 1.0);

		// The timer's OnDeactivate starts it again from outside the graph's connections, before its subgraph is
		// disabled.
		TimerNode timer = CreateTimerNode("duration");
		var child = new TrackingStateNode();
		var startAgain = new StartsNodeOnceNode(timer);
		graph.AddNode(timer);
		graph.AddNode(child);
		graph.AddNode(startAgain);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[TimerNode.InputPort]));
		graph.AddConnection(new Connection(
			timer.OutputPorts[TimerNode.OnDeactivatePort],
			startAgain.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			timer.OutputPorts[TimerNode.SubgraphPort],
			child.InputPorts[TrackingStateNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		processor.UpdateGraph(1.0);

		child.DeactivateCount.Should().Be(1);
		processor.GraphContext.IsActive.Should().BeFalse("the timer ended along with its subgraph");
	}

	[Fact]
	[Trait("Graph", "Activation")]
	public void A_message_set_off_by_a_node_cleaning_up_after_itself_is_ignored()
	{
		var graph = new Graph();
		var node = new StartsItselfOnDeactivateNode();
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[StartsItselfOnDeactivateNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		node.InputPorts[StartsItselfOnDeactivateNode.AbortPort].ReceiveMessage(processor.GraphContext);

		node.ActivateCount.Should().Be(1);
		processor.GraphContext.IsActive.Should().BeFalse("the node had not finished deactivating");
	}

	[Fact]
	[Trait("Graph", "Activation")]
	public void An_abort_reaching_a_node_that_never_started_is_ignored()
	{
		var graph = new Graph();
		var keepAlive = new TrackingStateNode();
		var node = new TrackingStateNode();
		var onAbort = new TrackingActionNode();
		graph.AddNode(keepAlive);
		graph.AddNode(node);
		graph.AddNode(onAbort);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			keepAlive.InputPorts[TrackingStateNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[TrackingStateNode.OnAbortPort],
			onAbort.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		node.InputPorts[TrackingStateNode.AbortPort].ReceiveMessage(processor.GraphContext);

		onAbort.ExecutionCount.Should().Be(0, "a node that is not running has nothing to abort");
	}

	[Fact]
	[Trait("Graph", "Activation")]
	public void An_abort_reaching_a_node_ending_by_itself_is_ignored()
	{
		var graph = new Graph();
		var keepAlive = new TrackingStateNode();
		var node = new EndsOnDemandNode();
		var abort = new AbortsNodeOnceNode(node);
		var onAbort = new TrackingActionNode();
		graph.AddNode(keepAlive);
		graph.AddNode(node);
		graph.AddNode(abort);
		graph.AddNode(onAbort);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			keepAlive.InputPorts[TrackingStateNode.InputPort]));
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[EndsOnDemandNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[EndsOnDemandNode.OnDeactivatePort],
			abort.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[EndsOnDemandNode.OnAbortPort],
			onAbort.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		node.End(processor.GraphContext);

		node.EndedAborted.Should().BeFalse("the node ended by itself before the abort reached it");
		onAbort.ExecutionCount.Should().Be(0);
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
	public void A_node_started_from_outside_the_graph_ends_when_the_graph_stops()
	{
		var graph = new Graph();
		var keepAlive = new TrackingStateNode();
		var node = new TrackingStateNode();
		graph.AddNode(keepAlive);
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			keepAlive.InputPorts[TrackingStateNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		// Nothing in the graph leads to the node, so the stop has no way to reach it.
		node.InputPorts[TrackingStateNode.InputPort].ReceiveMessage(processor.GraphContext);
		processor.StopGraph();

		node.DeactivateCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Activation")]
	public void A_message_reaching_a_node_while_the_graph_stops_starts_nothing()
	{
		var graph = new Graph();
		var keepAlive = new TrackingStateNode();
		var ending = new TrackingStateNode();
		var started = new TrackingStateNode();
		var start = new StartsNodeOnceNode(started);
		graph.AddNode(keepAlive);
		graph.AddNode(ending);
		graph.AddNode(started);
		graph.AddNode(start);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			keepAlive.InputPorts[TrackingStateNode.InputPort]));
		graph.AddConnection(new Connection(
			ending.OutputPorts[TrackingStateNode.OnDeactivatePort],
			start.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		// Nothing in the graph leads to either node, so the stop ends the first by itself, which starts the second as it
		// ends.
		ending.InputPorts[TrackingStateNode.InputPort].ReceiveMessage(processor.GraphContext);
		processor.StopGraph();

		ending.DeactivateCount.Should().Be(1);
		started.ActivateCount.Should().Be(0, "nothing starts in a graph that is stopping");
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

	// Emits from its activation, which waits for the activation to complete, then is aborted and started again before
	// it does, as an effect it applies might through an event that both aborts and retriggers it. It does so once.
	private sealed class StartsAgainNode : StateNode<StateNodeContext>
	{
		public const byte OnEmittedPort = 4;

		private bool _startedAgain;

		public int ActivatedCount { get; private set; }

		protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
		{
			base.DefinePorts(inputPorts, outputPorts);
			outputPorts.Add(CreatePort<EventPort>(OnEmittedPort, "OnEmitted"));
		}

		protected override void OnActivate(GraphContext graphContext)
		{
			EmitMessage(graphContext, OnEmittedPort);

			if (_startedAgain)
			{
				return;
			}

			_startedAgain = true;
			InputPorts[AbortPort].ReceiveMessage(graphContext);
			InputPorts[InputPort].ReceiveMessage(graphContext);
		}

		protected override void OnActivated(GraphContext graphContext)
		{
			ActivatedCount++;
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
		}
	}

	// Is aborted and started again from its own update, once, as an event the update sets off might, and reports the
	// update after that only if the activation it ran for is still going.
	private sealed class StartsAgainOnUpdateNode : StateNode<StateNodeContext>
	{
		public const byte OnUpdatedPort = 4;

		private bool _startedAgain;

		protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
		{
			base.DefinePorts(inputPorts, outputPorts);
			outputPorts.Add(CreatePort<EventPort>(OnUpdatedPort, "OnUpdated"));
		}

		protected override void OnActivate(GraphContext graphContext)
		{
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
		}

		protected override void OnUpdate(double deltaTime, GraphContext graphContext)
		{
			StartAgainThenReport(graphContext);
		}

		protected override void OnFixedUpdate(double deltaTime, GraphContext graphContext)
		{
			StartAgainThenReport(graphContext);
		}

		private void StartAgainThenReport(GraphContext graphContext)
		{
			if (_startedAgain)
			{
				return;
			}

			_startedAgain = true;
			StateNodeContext nodeContext = graphContext.GetNodeContext<StateNodeContext>(NodeID);
			InputPorts[AbortPort].ReceiveMessage(graphContext);
			InputPorts[InputPort].ReceiveMessage(graphContext);

			if (nodeContext.Active)
			{
				EmitMessage(graphContext, OnUpdatedPort);
			}
		}
	}

	private sealed class ReportsTwiceNode : StateNode<StateNodeContext>
	{
		public const byte OnFirstPort = 4;

		public const byte OnSecondPort = 5;

		public void Report(GraphContext graphContext)
		{
			EmitMessage(graphContext, OnFirstPort, OnSecondPort);
		}

		protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
		{
			base.DefinePorts(inputPorts, outputPorts);
			outputPorts.Add(CreatePort<EventPort>(OnFirstPort, "OnFirst"));
			outputPorts.Add(CreatePort<EventPort>(OnSecondPort, "OnSecond"));
		}

		protected override void OnActivate(GraphContext graphContext)
		{
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
		}
	}

	// Ends when told to, as a timer running out does, and records whether its ending was an abort.
	private sealed class EndsOnDemandNode : StateNode<StateNodeContext>
	{
		public bool EndedAborted { get; private set; }

		public void End(GraphContext graphContext)
		{
			DeactivateNode(graphContext);
		}

		protected override void OnActivate(GraphContext graphContext)
		{
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
			EndedAborted = graphContext.GetNodeContext<StateNodeContext>(NodeID).WasAborted;
		}
	}

	// Sends its own input a message as it cleans up, once, as an event that releasing what it holds might.
	private sealed class StartsItselfOnDeactivateNode : StateNode<StateNodeContext>
	{
		private bool _sent;

		public int ActivateCount { get; private set; }

		protected override void OnActivate(GraphContext graphContext)
		{
			ActivateCount++;
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
			if (!_sent)
			{
				_sent = true;
				InputPorts[InputPort].ReceiveMessage(graphContext);
			}
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

	// Sends its target's input a message from outside the graph's connections, once, as an event set off by the
	// target's own deactivation might.
	private sealed class StartsNodeOnceNode(Node target) : ActionNode
	{
		private bool _sent;

		protected override void Execute(GraphContext graphContext)
		{
			if (!_sent)
			{
				_sent = true;
				target.InputPorts[0].ReceiveMessage(graphContext);
			}
		}
	}

	// Aborts its target and starts it again from outside the graph's connections, once, as an event that does both
	// might.
	private sealed class EndsAndStartsNodeOnceNode(Node target) : ActionNode
	{
		private bool _sent;

		protected override void Execute(GraphContext graphContext)
		{
			if (!_sent)
			{
				_sent = true;
				target.InputPorts[StateNode<StateNodeContext>.AbortPort].ReceiveMessage(graphContext);
				target.InputPorts[StateNode<StateNodeContext>.InputPort].ReceiveMessage(graphContext);
			}
		}
	}

	// Aborts its target from outside the graph's connections, once, as an event set off by the target's own ending
	// might.
	private sealed class AbortsNodeOnceNode(Node target) : ActionNode
	{
		private bool _sent;

		protected override void Execute(GraphContext graphContext)
		{
			if (!_sent)
			{
				_sent = true;
				target.InputPorts[StateNode<StateNodeContext>.AbortPort].ReceiveMessage(graphContext);
			}
		}
	}
}
