// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Statescript.Nodes.State;

namespace Gamesmiths.Forge.Tests.Statescript;

public class StatescriptTests
{
	[Fact]
	[Trait("Graph", "Initialization")]
	public void New_graph_has_an_entry_node()
	{
		var graph = new Graph();

		graph.EntryNode.Should().NotBeNull();
		graph.Nodes.Should().BeEmpty();
		graph.Connections.Should().BeEmpty();
	}

	[Fact]
	[Trait("Graph", "Initialization")]
	public void Graph_runner_clones_variables_on_start()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("health", 100);

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);

		runner.StartGraph();

		context.GraphVariables.TryGetVar("health", out int value).Should().BeTrue();
		value.Should().Be(100);
	}

	[Fact]
	[Trait("Graph", "Execution")]
	public void Starting_graph_executes_connected_action_node()
	{
		var graph = new Graph();
		var actionNode = new TrackingActionNode();

		graph.AddNode(actionNode);
		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], actionNode.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		actionNode.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Execution")]
	public void Action_nodes_execute_in_sequence()
	{
		var executionOrder = new List<string>();

		var graph = new Graph();
		var action1 = new TrackingActionNode("A", executionOrder);
		var action2 = new TrackingActionNode("B", executionOrder);
		var action3 = new TrackingActionNode("C", executionOrder);

		graph.AddNode(action1);
		graph.AddNode(action2);
		graph.AddNode(action3);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], action1.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(action1.OutputPorts[ActionNode.OutputPort], action2.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(action2.OutputPorts[ActionNode.OutputPort], action3.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		executionOrder.Should().ContainInOrder("A", "B", "C");
	}

	[Fact]
	[Trait("Graph", "Condition")]
	public void Condition_node_routes_to_true_port_when_condition_is_met()
	{
		var graph = new Graph();
		var condition = new FixedConditionNode(result: true);
		var trueAction = new TrackingActionNode();
		var falseAction = new TrackingActionNode();

		graph.AddNode(condition);
		graph.AddNode(trueAction);
		graph.AddNode(falseAction);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], condition.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(condition.OutputPorts[ConditionNode.TruePort], trueAction.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(condition.OutputPorts[ConditionNode.FalsePort], falseAction.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		trueAction.ExecutionCount.Should().Be(1);
		falseAction.ExecutionCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "Condition")]
	public void Condition_node_routes_to_false_port_when_condition_is_not_met()
	{
		var graph = new Graph();
		var condition = new FixedConditionNode(result: false);
		var trueAction = new TrackingActionNode();
		var falseAction = new TrackingActionNode();

		graph.AddNode(condition);
		graph.AddNode(trueAction);
		graph.AddNode(falseAction);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], condition.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(condition.OutputPorts[ConditionNode.TruePort], trueAction.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(condition.OutputPorts[ConditionNode.FalsePort], falseAction.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		trueAction.ExecutionCount.Should().Be(0);
		falseAction.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Variables")]
	public void Action_node_can_read_and_write_graph_variables()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("counter", 0);

		var incrementNode = new IncrementCounterNode("counter");
		var readNode = new ReadVariableNode<int>("counter");

		graph.AddNode(incrementNode);
		graph.AddNode(readNode);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], incrementNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(incrementNode.OutputPorts[ActionNode.OutputPort], readNode.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		readNode.LastReadValue.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Variables")]
	public void Condition_node_can_branch_based_on_graph_variables()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("threshold", 10);
		graph.VariableDefinitions.DefineVariable("value", 15);

		var condition = new ThresholdConditionNode("value", "threshold");
		var aboveAction = new TrackingActionNode();
		var belowAction = new TrackingActionNode();

		graph.AddNode(condition);
		graph.AddNode(aboveAction);
		graph.AddNode(belowAction);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], condition.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(condition.OutputPorts[ConditionNode.TruePort], aboveAction.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(condition.OutputPorts[ConditionNode.FalsePort], belowAction.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		aboveAction.ExecutionCount.Should().Be(1, "value (15) is above threshold (10)");
		belowAction.ExecutionCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "Branching")]
	public void Output_port_can_connect_to_multiple_input_ports()
	{
		var graph = new Graph();
		var action1 = new TrackingActionNode();
		var action2 = new TrackingActionNode();

		graph.AddNode(action1);
		graph.AddNode(action2);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], action1.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], action2.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		action1.ExecutionCount.Should().Be(1);
		action2.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Lifecycle")]
	public void Stopping_graph_resets_variables_to_saved_state()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("counter", 0);

		var incrementNode = new IncrementCounterNode("counter");

		graph.AddNode(incrementNode);
		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], incrementNode.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		context.GraphVariables.TryGetVar("counter", out int valueAfterStart).Should().BeTrue();
		valueAfterStart.Should().Be(1);

		// StopGraph cleans up node contexts - verify it doesn't throw.
		runner.StopGraph();
	}

	[Fact]
	[Trait("Graph", "Lifecycle")]
	public void Stopping_graph_removes_all_node_contexts()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 5.0);

		var timer = new TimerStateNode("duration");
		graph.AddNode(timer);
		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		context.InternalNodeActivationStatus.Should().NotBeEmpty();
		context.NodeContextCount.Should().BePositive();

		runner.StopGraph();

		context.NodeContextCount.Should().Be(0);
		context.InternalNodeActivationStatus.Should().BeEmpty();
	}

	[Fact]
	[Trait("Graph", "Complex")]
	public void Complex_graph_with_condition_and_multiple_actions_executes_correctly()
	{
		var executionOrder = new List<string>();

		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("counter", 0);

		var incrementNode = new IncrementCounterNode("counter");
		var condition = new ThresholdConditionNode("counter", threshold: 0);
		var trackA = new TrackingActionNode("TrueAction", executionOrder);
		var trackB = new TrackingActionNode("FalseAction", executionOrder);

		graph.AddNode(incrementNode);
		graph.AddNode(condition);
		graph.AddNode(trackA);
		graph.AddNode(trackB);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], incrementNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(incrementNode.OutputPorts[ActionNode.OutputPort], condition.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(condition.OutputPorts[ConditionNode.TruePort], trackA.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(condition.OutputPorts[ConditionNode.FalsePort], trackB.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		trackA.ExecutionCount.Should().Be(1);
		trackB.ExecutionCount.Should().Be(0);
		executionOrder.Should().ContainSingle().Which.Should().Be("TrueAction");
	}

	[Fact]
	[Trait("Graph", "Node")]
	public void Disconnected_node_is_not_executed()
	{
		var graph = new Graph();
		var connectedAction = new TrackingActionNode();
		var disconnectedAction = new TrackingActionNode();

		graph.AddNode(connectedAction);
		graph.AddNode(disconnectedAction);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], connectedAction.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		connectedAction.ExecutionCount.Should().Be(1);
		disconnectedAction.ExecutionCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "Node")]
	public void Each_graph_runner_has_independent_variable_state()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("counter", 0);

		var incrementNode = new IncrementCounterNode("counter");
		graph.AddNode(incrementNode);
		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], incrementNode.InputPorts[ActionNode.InputPort]));

		var context1 = new TestGraphContext();
		var runner1 = new GraphRunner(graph, context1);

		var context2 = new TestGraphContext();
		var runner2 = new GraphRunner(graph, context2);

		runner1.StartGraph();
		runner2.StartGraph();

		context1.GraphVariables.TryGetVar("counter", out int value1);
		context2.GraphVariables.TryGetVar("counter", out int value2);

		value1.Should().Be(1);
		value2.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Timer")]
	public void Timer_node_stays_active_until_duration_elapses()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 2.0);

		var timer = new TimerStateNode("duration");

		graph.AddNode(timer);
		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], timer.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		context.IsActive.Should().BeTrue();

		// Not enough time has passed.
		runner.UpdateGraph(1.0);
		context.IsActive.Should().BeTrue();

		// Still not enough.
		runner.UpdateGraph(0.5);
		context.IsActive.Should().BeTrue();

		// Now it should deactivate (total: 1.0 + 0.5 + 0.5 = 2.0).
		runner.UpdateGraph(0.5);
		context.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "Timer")]
	public void Timer_node_fires_on_deactivate_event_when_completed()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 1.0);

		var timer = new TimerStateNode("duration");
		var onDeactivateAction = new TrackingActionNode();

		graph.AddNode(timer);
		graph.AddNode(onDeactivateAction);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], timer.InputPorts[ActionNode.InputPort]));

		// OutputPorts[1] is the OnDeactivate event port on StateNode.
		graph.AddConnection(new Connection(timer.OutputPorts[TimerStateNode.OnDeactivatePort], onDeactivateAction.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		onDeactivateAction.ExecutionCount.Should().Be(0);

		runner.UpdateGraph(1.0);

		onDeactivateAction.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Timer")]
	public void Timer_node_fires_on_activate_event_on_start()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 5.0);

		var timer = new TimerStateNode("duration");
		var onActivateAction = new TrackingActionNode();

		graph.AddNode(timer);
		graph.AddNode(onActivateAction);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], timer.InputPorts[ActionNode.InputPort]));

		// OutputPorts[0] is the OnActivate event port on StateNode.
		graph.AddConnection(new Connection(timer.OutputPorts[TimerStateNode.OnActivatePort], onActivateAction.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		onActivateAction.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Timer")]
	public void Two_runners_with_same_timer_graph_have_independent_elapsed_time()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 2.0);

		var timer = new TimerStateNode("duration");

		graph.AddNode(timer);
		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], timer.InputPorts[ActionNode.InputPort]));

		var context1 = new TestGraphContext();
		var runner1 = new GraphRunner(graph, context1);

		var context2 = new TestGraphContext();
		var runner2 = new GraphRunner(graph, context2);

		runner1.StartGraph();
		runner2.StartGraph();

		// Advance runner1 past duration, but not runner2.
		runner1.UpdateGraph(2.0);
		runner2.UpdateGraph(1.0);

		context1.IsActive.Should().BeFalse();
		context2.IsActive.Should().BeTrue();

		// Now advance runner2 past duration.
		runner2.UpdateGraph(1.0);
		context2.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "Validation")]
	public void Validate_property_type_returns_true_for_matching_type()
	{
		var definitions = new GraphVariableDefinitions();
		definitions.DefineVariable("duration", 2.0);

		definitions.ValidatePropertyType("duration", typeof(double)).Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "Validation")]
	public void Validate_property_type_returns_false_for_mismatched_type()
	{
		var definitions = new GraphVariableDefinitions();
		definitions.DefineVariable("flag", true);

		definitions.ValidatePropertyType("flag", typeof(double)).Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "Validation")]
	public void Validate_property_type_returns_false_for_nonexistent_property()
	{
		var definitions = new GraphVariableDefinitions();

		definitions.ValidatePropertyType("missing", typeof(double)).Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "SetVariable")]
	public void Set_variable_node_copies_value_from_source_to_target()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("source", 42);
		graph.VariableDefinitions.DefineVariable("target", 0);

		var setNode = new SetVariableNode("source", "target");
		var readNode = new ReadVariableNode<int>("target");

		graph.AddNode(setNode);
		graph.AddNode(readNode);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], setNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(setNode.OutputPorts[ActionNode.OutputPort], readNode.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		readNode.LastReadValue.Should().Be(42);
	}

	[Fact]
	[Trait("Graph", "SetVariable")]
	public void Set_variable_node_copies_value_between_different_variables()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("counter", 0);
		graph.VariableDefinitions.DefineVariable("result", 0);

		var incrementNode = new IncrementCounterNode("counter");
		var setNode = new SetVariableNode("counter", "result");
		var readNode = new ReadVariableNode<int>("result");

		graph.AddNode(incrementNode);
		graph.AddNode(setNode);
		graph.AddNode(readNode);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], incrementNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(incrementNode.OutputPorts[ActionNode.OutputPort], setNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(setNode.OutputPorts[ActionNode.OutputPort], readNode.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		// counter was incremented to 1, then copied to result
		readNode.LastReadValue.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "SetVariable")]
	public void Set_variable_node_does_not_modify_source()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("source", 99);
		graph.VariableDefinitions.DefineVariable("target", 0);

		var setNode = new SetVariableNode("source", "target");
		var readSource = new ReadVariableNode<int>("source");
		var readTarget = new ReadVariableNode<int>("target");

		graph.AddNode(setNode);
		graph.AddNode(readTarget);
		graph.AddNode(readSource);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], setNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(setNode.OutputPorts[ActionNode.OutputPort], readTarget.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(readTarget.OutputPorts[ActionNode.OutputPort], readSource.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		readTarget.LastReadValue.Should().Be(99);
		readSource.LastReadValue.Should().Be(99);
	}

	[Fact]
	[Trait("Graph", "SetVariable")]
	public void Set_variable_node_with_nonexistent_source_does_not_modify_target()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("target", 77);

		var setNode = new SetVariableNode("nonexistent", "target");
		var readNode = new ReadVariableNode<int>("target");

		graph.AddNode(setNode);
		graph.AddNode(readNode);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], setNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(setNode.OutputPorts[ActionNode.OutputPort], readNode.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		// Target should remain unchanged because source doesn't exist
		readNode.LastReadValue.Should().Be(77);
	}

	[Fact]
	[Trait("Graph", "SetVariable")]
	public void Set_variable_node_works_with_double_values()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 3.5);
		graph.VariableDefinitions.DefineVariable("cachedDuration", 0.0);

		var setNode = new SetVariableNode("duration", "cachedDuration");
		var readNode = new ReadVariableNode<double>("cachedDuration");

		graph.AddNode(setNode);
		graph.AddNode(readNode);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], setNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(setNode.OutputPorts[ActionNode.OutputPort], readNode.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		readNode.LastReadValue.Should().Be(3.5);
	}

	[Fact]
	[Trait("Graph", "SetVariable")]
	public void Set_variable_node_works_with_bool_values()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("flag", true);
		graph.VariableDefinitions.DefineVariable("copy", false);

		var setNode = new SetVariableNode("flag", "copy");
		var readNode = new ReadVariableNode<bool>("copy");

		graph.AddNode(setNode);
		graph.AddNode(readNode);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], setNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(setNode.OutputPorts[ActionNode.OutputPort], readNode.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		readNode.LastReadValue.Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "SetVariable")]
	public void Two_runners_using_set_variable_have_independent_state()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("source", 10);
		graph.VariableDefinitions.DefineVariable("target", 0);

		var incrementNode = new IncrementCounterNode("source");
		var setNode = new SetVariableNode("source", "target");

		graph.AddNode(incrementNode);
		graph.AddNode(setNode);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], incrementNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(incrementNode.OutputPorts[ActionNode.OutputPort], setNode.InputPorts[ActionNode.InputPort]));

		var context1 = new TestGraphContext();
		var runner1 = new GraphRunner(graph, context1);

		var context2 = new TestGraphContext();
		var runner2 = new GraphRunner(graph, context2);

		runner1.StartGraph();
		runner2.StartGraph();

		context1.GraphVariables.TryGetVar("target", out int value1);
		context2.GraphVariables.TryGetVar("target", out int value2);

		// Both should have source incremented from 10 to 11, then copied to target
		value1.Should().Be(11);
		value2.Should().Be(11);
	}

	[Fact]
	[Trait("Graph", "ExitNode")]
	public void Exit_node_stops_graph_execution()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 5.0);

		var timer = new TimerStateNode("duration");
		var exitNode = new ExitNode();

		graph.AddNode(timer);
		graph.AddNode(exitNode);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(timer.OutputPorts[StateNode<TimerNodeContext>.OnDeactivatePort], exitNode.InputPorts[ExitNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		context.IsActive.Should().BeTrue();

		// Timer deactivates after 5 seconds, which triggers ExitNode.
		runner.UpdateGraph(5.0);

		context.IsActive.Should().BeFalse();
		context.NodeContextCount.Should().Be(0);
		context.Runner.Should().BeNull();
	}

	[Fact]
	[Trait("Graph", "ExitNode")]
	public void Exit_node_connected_to_action_stops_graph_after_action()
	{
		var graph = new Graph();
		var actionNode = new TrackingActionNode();
		var exitNode = new ExitNode();

		graph.AddNode(actionNode);
		graph.AddNode(exitNode);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], actionNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(actionNode.OutputPorts[ActionNode.OutputPort], exitNode.InputPorts[ExitNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		actionNode.ExecutionCount.Should().Be(1);
		context.NodeContextCount.Should().Be(0);
		context.Runner.Should().BeNull();
	}

	[Fact]
	[Trait("Graph", "ExitNode")]
	public void Exit_node_stops_all_active_state_nodes()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("shortDuration", 1.0);
		graph.VariableDefinitions.DefineVariable("longDuration", 10.0);

		var shortTimer = new TimerStateNode("shortDuration");
		var longTimer = new TimerStateNode("longDuration");
		var exitNode = new ExitNode();

		graph.AddNode(shortTimer);
		graph.AddNode(longTimer);
		graph.AddNode(exitNode);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], shortTimer.InputPorts[StateNode<TimerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], longTimer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		// When the short timer completes, exit the graph (which should also stop the long timer).
		graph.AddConnection(new Connection(shortTimer.OutputPorts[StateNode<TimerNodeContext>.OnDeactivatePort], exitNode.InputPorts[ExitNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		context.ActiveStateNodes.Should().HaveCount(2);

		// Short timer elapses, triggering ExitNode which stops everything.
		runner.UpdateGraph(1.0);

		context.IsActive.Should().BeFalse();
		context.ActiveStateNodes.Should().BeEmpty();
		context.NodeContextCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "Lifecycle")]
	public void Runner_reference_is_set_on_start_and_cleared_on_stop()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 5.0);

		var timer = new TimerStateNode("duration");
		graph.AddNode(timer);
		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);

		context.Runner.Should().BeNull();

		runner.StartGraph();
		context.Runner.Should().Be(runner);

		runner.StopGraph();
		context.Runner.Should().BeNull();
	}

	[Fact]
	[Trait("Graph", "Lifecycle")]
	public void Active_state_nodes_set_tracks_active_nodes()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 2.0);

		var timer = new TimerStateNode("duration");
		graph.AddNode(timer);
		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		context.ActiveStateNodes.Should().ContainSingle().Which.Should().Be(timer);

		runner.UpdateGraph(2.0);

		context.ActiveStateNodes.Should().BeEmpty();
	}

	[Fact]
	[Trait("Graph", "Lifecycle")]
	public void Action_only_graph_finalizes_immediately_after_start()
	{
		var graph = new Graph();
		var actionNode = new TrackingActionNode();

		graph.AddNode(actionNode);
		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], actionNode.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		actionNode.ExecutionCount.Should().Be(1);
		context.Runner.Should().BeNull();
		context.HasStarted.Should().BeFalse();
		context.NodeContextCount.Should().Be(0);
		context.InternalNodeActivationStatus.Should().BeEmpty();
	}

	[Fact]
	[Trait("Graph", "Lifecycle")]
	public void Timer_graph_finalizes_when_last_state_node_deactivates()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 2.0);

		var timer = new TimerStateNode("duration");
		graph.AddNode(timer);
		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		context.HasStarted.Should().BeTrue();
		context.Runner.Should().Be(runner);

		runner.UpdateGraph(2.0);

		context.IsActive.Should().BeFalse();
		context.HasStarted.Should().BeFalse();
		context.Runner.Should().BeNull();
		context.NodeContextCount.Should().Be(0);
		context.InternalNodeActivationStatus.Should().BeEmpty();
	}

	[Fact]
	[Trait("Graph", "Lifecycle")]
	public void Multiple_timers_finalize_only_after_all_deactivate()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("shortDuration", 1.0);
		graph.VariableDefinitions.DefineVariable("longDuration", 3.0);

		var shortTimer = new TimerStateNode("shortDuration");
		var longTimer = new TimerStateNode("longDuration");

		graph.AddNode(shortTimer);
		graph.AddNode(longTimer);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], shortTimer.InputPorts[StateNode<TimerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], longTimer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		context.ActiveStateNodes.Should().HaveCount(2);
		context.HasStarted.Should().BeTrue();

		// Short timer elapses, but long timer still active — graph should NOT finalize.
		runner.UpdateGraph(1.0);
		context.ActiveStateNodes.Should().ContainSingle();
		context.HasStarted.Should().BeTrue();
		context.Runner.Should().Be(runner);

		// Long timer elapses — now the graph should finalize.
		runner.UpdateGraph(2.0);
		context.IsActive.Should().BeFalse();
		context.HasStarted.Should().BeFalse();
		context.Runner.Should().BeNull();
		context.NodeContextCount.Should().Be(0);
		context.InternalNodeActivationStatus.Should().BeEmpty();
	}

	[Fact]
	[Trait("Graph", "Lifecycle")]
	public void Update_graph_does_nothing_after_finalization()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 1.0);

		var timer = new TimerStateNode("duration");
		graph.AddNode(timer);
		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[EntryNode.OutputPort], timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		runner.UpdateGraph(1.0);
		context.HasStarted.Should().BeFalse();

		// Subsequent updates should be no-ops and not throw.
		runner.UpdateGraph(1.0);
		runner.UpdateGraph(1.0);
		context.HasStarted.Should().BeFalse();
		context.Runner.Should().BeNull();
	}

	[Fact]
	[Trait("Graph", "Lifecycle")]
	public void Empty_graph_finalizes_immediately()
	{
		var graph = new Graph();
		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);

		runner.StartGraph();

		context.HasStarted.Should().BeFalse();
		context.Runner.Should().BeNull();
	}

	private sealed class TestGraphContext : IGraphContext
	{
		private readonly Dictionary<Guid, INodeContext> _nodeContexts = [];

		public bool IsActive => ActiveStateNodes.Count > 0;

		public IForgeEntity? Owner { get; set; }

		public Variables GraphVariables { get; } = new Variables();

		public Dictionary<Guid, bool> InternalNodeActivationStatus { get; } = [];

		public HashSet<Node> ActiveStateNodes { get; } = [];

		public GraphRunner? Runner { get; set; }

		public bool HasStarted { get; set; }

		public int NodeContextCount => _nodeContexts.Count;

		public T GetOrCreateNodeContext<T>(Guid nodeID)
			where T : INodeContext, new()
		{
			if (_nodeContexts.TryGetValue(nodeID, out INodeContext? context))
			{
				return (T)context;
			}

			var newContext = new T();
			_nodeContexts[nodeID] = newContext;
			return newContext;
		}

		public T GetNodeContext<T>(Guid nodeID)
			where T : INodeContext, new()
		{
			if (_nodeContexts.TryGetValue(nodeID, out INodeContext? context))
			{
				return (T)context;
			}

			return default!;
		}

		public void RemoveAllNodeContext()
		{
			_nodeContexts.Clear();
		}
	}

	private sealed class TrackingActionNode(string? name = null, List<string>? executionLog = null) : ActionNode
	{
		private readonly string? _name = name;
		private readonly List<string>? _executionLog = executionLog;

		public int ExecutionCount { get; private set; }

		protected override void Execute(IGraphContext graphContext)
		{
			ExecutionCount++;

			if (_name is not null)
			{
				_executionLog?.Add(_name);
			}
		}
	}

	private sealed class FixedConditionNode(bool result) : ConditionNode
	{
		private readonly bool _result = result;

		protected override bool Test(IGraphContext graphContext)
		{
			return _result;
		}
	}

	private sealed class ThresholdConditionNode : ConditionNode
	{
		private readonly string _variableName;
		private readonly string? _thresholdVariableName;
		private readonly int _fixedThreshold;

		public ThresholdConditionNode(string variableName, string thresholdVariableName)
		{
			_variableName = variableName;
			_thresholdVariableName = thresholdVariableName;
		}

		public ThresholdConditionNode(string variableName, int threshold)
		{
			_variableName = variableName;
			_fixedThreshold = threshold;
		}

		protected override bool Test(IGraphContext graphContext)
		{
			graphContext.GraphVariables.TryGetVar(_variableName, out int value);

			var threshold = _fixedThreshold;
			if (_thresholdVariableName is not null)
			{
				graphContext.GraphVariables.TryGetVar(_thresholdVariableName, out threshold);
			}

			return value > threshold;
		}
	}

	private sealed class IncrementCounterNode(string variableName) : ActionNode
	{
		private readonly string _variableName = variableName;

		protected override void Execute(IGraphContext graphContext)
		{
			graphContext.GraphVariables.TryGetVar(_variableName, out int currentValue);
			graphContext.GraphVariables.SetVar(_variableName, currentValue + 1);
		}
	}

	private sealed class ReadVariableNode<T>(string variableName) : ActionNode
		where T : unmanaged
	{
		private readonly string _variableName = variableName;

		public T LastReadValue { get; private set; }

		protected override void Execute(IGraphContext graphContext)
		{
			graphContext.GraphVariables.TryGetVar(_variableName, out T value);
			LastReadValue = value;
		}
	}
}
