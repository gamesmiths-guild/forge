// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript;

public class GraphProcessorTests
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
	public void Graph_processor_initializes_variables_on_start()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("health", 100);

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);

		processor.StartGraph();

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
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			actionNode.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

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

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			action1.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			action1.OutputPorts[ActionNode.OutputPort],
			action2.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			action2.OutputPorts[ActionNode.OutputPort],
			action3.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

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

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			condition.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.TruePort],
			trueAction.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.FalsePort],
			falseAction.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

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

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			condition.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.TruePort],
			trueAction.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.FalsePort],
			falseAction.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

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

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			incrementNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			incrementNode.OutputPorts[ActionNode.OutputPort],
			readNode.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

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

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			condition.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.TruePort],
			aboveAction.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.FalsePort],
			belowAction.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

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

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			action1.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			action2.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

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
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			incrementNode.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		context.GraphVariables.TryGetVar("counter", out int valueAfterStart).Should().BeTrue();
		valueAfterStart.Should().Be(1);

		// StopGraph cleans up node contexts - verify it doesn't throw.
		processor.StopGraph();
	}

	[Fact]
	[Trait("Graph", "Lifecycle")]
	public void Stopping_graph_removes_all_node_contexts()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 5.0);

		var timer = new TimerNode("duration");
		graph.AddNode(timer);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		context.InternalNodeActivationStatus.Should().NotBeEmpty();
		context.NodeContextCount.Should().BePositive();

		processor.StopGraph();

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

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			incrementNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			incrementNode.OutputPorts[ActionNode.OutputPort],
			condition.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.TruePort],
			trackA.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.FalsePort],
			trackB.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

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

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			connectedAction.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		connectedAction.ExecutionCount.Should().Be(1);
		disconnectedAction.ExecutionCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "Node")]
	public void Each_graph_processor_has_independent_variable_state()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("counter", 0);

		var incrementNode = new IncrementCounterNode("counter");
		graph.AddNode(incrementNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			incrementNode.InputPorts[ActionNode.InputPort]));

		var context1 = new TestGraphContext();
		var processor1 = new GraphProcessor(graph, context1);

		var context2 = new TestGraphContext();
		var processor2 = new GraphProcessor(graph, context2);

		processor1.StartGraph();
		processor2.StartGraph();

		context1.GraphVariables.TryGetVar("counter", out int value1);
		context2.GraphVariables.TryGetVar("counter", out int value2);

		value1.Should().Be(1);
		value2.Should().Be(1);
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
	[Trait("Graph", "ExitNode")]
	public void Exit_node_stops_graph_execution()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 5.0);

		var timer = new TimerNode("duration");
		var exitNode = new ExitNode();

		graph.AddNode(timer);
		graph.AddNode(exitNode);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			timer.OutputPorts[StateNode<TimerNodeContext>.OnDeactivatePort],
			exitNode.InputPorts[ExitNode.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		context.IsActive.Should().BeTrue();

		processor.UpdateGraph(5.0);

		context.IsActive.Should().BeFalse();
		context.NodeContextCount.Should().Be(0);
		context.Processor.Should().BeNull();
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

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			actionNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			actionNode.OutputPorts[ActionNode.OutputPort],
			exitNode.InputPorts[ExitNode.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		actionNode.ExecutionCount.Should().Be(1);
		context.NodeContextCount.Should().Be(0);
		context.Processor.Should().BeNull();
	}

	[Fact]
	[Trait("Graph", "ExitNode")]
	public void Exit_node_stops_all_active_state_nodes()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("shortDuration", 1.0);
		graph.VariableDefinitions.DefineVariable("longDuration", 10.0);

		var shortTimer = new TimerNode("shortDuration");
		var longTimer = new TimerNode("longDuration");
		var exitNode = new ExitNode();

		graph.AddNode(shortTimer);
		graph.AddNode(longTimer);
		graph.AddNode(exitNode);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			shortTimer.InputPorts[StateNode<TimerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			longTimer.InputPorts[StateNode<TimerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			shortTimer.OutputPorts[StateNode<TimerNodeContext>.OnDeactivatePort],
			exitNode.InputPorts[ExitNode.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		context.ActiveStateNodes.Should().HaveCount(2);

		processor.UpdateGraph(1.0);

		context.IsActive.Should().BeFalse();
		context.ActiveStateNodes.Should().BeEmpty();
		context.NodeContextCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "Lifecycle")]
	public void Processor_reference_is_set_on_start_and_cleared_on_stop()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 5.0);

		var timer = new TimerNode("duration");
		graph.AddNode(timer);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);

		context.Processor.Should().BeNull();

		processor.StartGraph();
		context.Processor.Should().Be(processor);

		processor.StopGraph();
		context.Processor.Should().BeNull();
	}

	[Fact]
	[Trait("Graph", "Lifecycle")]
	public void Active_state_nodes_set_tracks_active_nodes()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 2.0);

		var timer = new TimerNode("duration");
		graph.AddNode(timer);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		context.ActiveStateNodes.Should().ContainSingle().Which.Should().Be(timer);

		processor.UpdateGraph(2.0);

		context.ActiveStateNodes.Should().BeEmpty();
	}

	[Fact]
	[Trait("Graph", "Lifecycle")]
	public void Action_only_graph_finalizes_immediately_after_start()
	{
		var graph = new Graph();
		var actionNode = new TrackingActionNode();

		graph.AddNode(actionNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			actionNode.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		actionNode.ExecutionCount.Should().Be(1);
		context.Processor.Should().BeNull();
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

		var timer = new TimerNode("duration");
		graph.AddNode(timer);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		context.HasStarted.Should().BeTrue();
		context.Processor.Should().Be(processor);

		processor.UpdateGraph(2.0);

		context.IsActive.Should().BeFalse();
		context.HasStarted.Should().BeFalse();
		context.Processor.Should().BeNull();
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

		var shortTimer = new TimerNode("shortDuration");
		var longTimer = new TimerNode("longDuration");

		graph.AddNode(shortTimer);
		graph.AddNode(longTimer);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			shortTimer.InputPorts[StateNode<TimerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			longTimer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		context.ActiveStateNodes.Should().HaveCount(2);
		context.HasStarted.Should().BeTrue();

		processor.UpdateGraph(1.0);
		context.ActiveStateNodes.Should().ContainSingle();
		context.HasStarted.Should().BeTrue();
		context.Processor.Should().Be(processor);

		processor.UpdateGraph(2.0);
		context.IsActive.Should().BeFalse();
		context.HasStarted.Should().BeFalse();
		context.Processor.Should().BeNull();
		context.NodeContextCount.Should().Be(0);
		context.InternalNodeActivationStatus.Should().BeEmpty();
	}

	[Fact]
	[Trait("Graph", "Lifecycle")]
	public void Update_graph_does_nothing_after_finalization()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 1.0);

		var timer = new TimerNode("duration");
		graph.AddNode(timer);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		processor.UpdateGraph(1.0);
		context.HasStarted.Should().BeFalse();

		// Subsequent updates should be no-ops and not throw.
		processor.UpdateGraph(1.0);
		processor.UpdateGraph(1.0);
		context.HasStarted.Should().BeFalse();
		context.Processor.Should().BeNull();
	}

	[Fact]
	[Trait("Graph", "Lifecycle")]
	public void Empty_graph_finalizes_immediately()
	{
		var graph = new Graph();
		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);

		processor.StartGraph();

		context.HasStarted.Should().BeFalse();
		context.Processor.Should().BeNull();
	}

	[Fact]
	[Trait("Graph", "ArrayVariables")]
	public void Array_variable_is_initialized_from_definition()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineArrayVariable("targets", 10, 20, 30);

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		context.GraphVariables.GetArrayLength("targets").Should().Be(3);
		context.GraphVariables.TryGetArrayElement("targets", 0, out int v0).Should().BeTrue();
		context.GraphVariables.TryGetArrayElement("targets", 1, out int v1).Should().BeTrue();
		context.GraphVariables.TryGetArrayElement("targets", 2, out int v2).Should().BeTrue();
		v0.Should().Be(10);
		v1.Should().Be(20);
		v2.Should().Be(30);
	}

	[Fact]
	[Trait("Graph", "ArrayVariables")]
	public void Array_variable_has_independent_state_per_processor()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineArrayVariable("ids", 1, 2, 3);

		var context1 = new TestGraphContext();
		var processor1 = new GraphProcessor(graph, context1);
		processor1.StartGraph();

		var context2 = new TestGraphContext();
		var processor2 = new GraphProcessor(graph, context2);
		processor2.StartGraph();

		context1.GraphVariables.SetArrayElement("ids", 0, 99);

		context1.GraphVariables.TryGetArrayElement("ids", 0, out int val1);
		context2.GraphVariables.TryGetArrayElement("ids", 0, out int val2);

		val1.Should().Be(99);
		val2.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "ArrayVariables")]
	public void Array_variable_returns_negative_length_for_nonexistent_variable()
	{
		var graph = new Graph();

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		context.GraphVariables.GetArrayLength("nonexistent").Should().Be(-1);
	}

	[Fact]
	[Trait("Graph", "ArrayVariables")]
	public void Array_variable_try_get_returns_false_for_out_of_range_index()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineArrayVariable("data", 1.0, 2.0);

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		context.GraphVariables.TryGetArrayElement("data", 5, out double _).Should().BeFalse();
		context.GraphVariables.TryGetArrayElement("data", -1, out double _).Should().BeFalse();
	}
}
