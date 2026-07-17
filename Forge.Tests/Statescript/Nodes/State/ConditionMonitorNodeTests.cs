// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes.State;

public class ConditionMonitorNodeTests
{
	[Fact]
	[Trait("Graph", "ConditionMonitor")]
	public void Condition_monitor_fires_transition_events_and_routes_subgraphs()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("condition", true);
		graph.VariableDefinitions.DefineVariable("childDuration", 100.0);

		var monitor = new ConditionMonitorNode();
		monitor.BindInput(ConditionMonitorNode.ConditionInput, "condition");

		TimerNode trueChild = CreateTimerNode("childDuration");
		var trueChildDeactivated = new TrackingActionNode();
		var becameTrue = new TrackingActionNode();
		var becameFalse = new TrackingActionNode();
		var falseChild = new TrackingActionNode();

		graph.AddNode(monitor);
		graph.AddNode(trueChild);
		graph.AddNode(trueChildDeactivated);
		graph.AddNode(becameTrue);
		graph.AddNode(becameFalse);
		graph.AddNode(falseChild);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			monitor.InputPorts[StateNode<ConditionMonitorNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			monitor.OutputPorts[ConditionMonitorNode.OnBecameTruePort],
			becameTrue.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			monitor.OutputPorts[ConditionMonitorNode.OnBecameFalsePort],
			becameFalse.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			monitor.OutputPorts[ConditionMonitorNode.TrueSubgraphPort],
			trueChild.InputPorts[StateNode<TimerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			trueChild.OutputPorts[StateNode<TimerNodeContext>.OnDeactivatePort],
			trueChildDeactivated.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			monitor.OutputPorts[ConditionMonitorNode.FalseSubgraphPort],
			falseChild.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		// Initial evaluation: condition is true.
		becameTrue.ExecutionCount.Should().Be(1);
		becameFalse.ExecutionCount.Should().Be(0);
		trueChildDeactivated.ExecutionCount.Should().Be(0);

		// Flip to false: the true subgraph is disabled and the false subgraph activates.
		processor.GraphContext.GraphVariables.SetVar("condition", false);
		processor.UpdateGraph(0.1);

		becameFalse.ExecutionCount.Should().Be(1);
		trueChildDeactivated.ExecutionCount.Should().Be(1);
		falseChild.ExecutionCount.Should().Be(1);

		// Flip back to true: the true subgraph re-activates.
		processor.GraphContext.GraphVariables.SetVar("condition", true);
		processor.UpdateGraph(0.1);

		becameTrue.ExecutionCount.Should().Be(2);
		trueChildDeactivated.ExecutionCount.Should().Be(1);
		processor.GraphContext.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "ConditionMonitor")]
	public void Condition_monitor_does_not_refire_without_a_transition()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("condition", true);

		var monitor = new ConditionMonitorNode();
		monitor.BindInput(ConditionMonitorNode.ConditionInput, "condition");

		var becameTrue = new TrackingActionNode();

		graph.AddNode(monitor);
		graph.AddNode(becameTrue);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			monitor.InputPorts[StateNode<ConditionMonitorNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			monitor.OutputPorts[ConditionMonitorNode.OnBecameTruePort],
			becameTrue.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		processor.UpdateGraph(0.1);
		processor.UpdateGraph(0.1);

		becameTrue.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "ConditionMonitor")]
	public void Condition_monitor_deactivates_when_condition_becomes_true_in_wait_until_mode()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("condition", false);

		var monitor = new ConditionMonitorNode(deactivateWhenTrue: true);
		monitor.BindInput(ConditionMonitorNode.ConditionInput, "condition");

		var becameTrue = new TrackingActionNode();

		graph.AddNode(monitor);
		graph.AddNode(becameTrue);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			monitor.InputPorts[StateNode<ConditionMonitorNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			monitor.OutputPorts[ConditionMonitorNode.OnBecameTruePort],
			becameTrue.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		processor.GraphContext.IsActive.Should().BeTrue();
		becameTrue.ExecutionCount.Should().Be(0);

		processor.GraphContext.GraphVariables.SetVar("condition", true);
		processor.UpdateGraph(0.1);

		becameTrue.ExecutionCount.Should().Be(1);
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "ConditionMonitor")]
	public void Condition_monitor_deactivates_immediately_when_wait_until_condition_starts_true()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("condition", true);

		var monitor = new ConditionMonitorNode(deactivateWhenTrue: true);
		monitor.BindInput(ConditionMonitorNode.ConditionInput, "condition");

		graph.AddNode(monitor);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			monitor.InputPorts[StateNode<ConditionMonitorNodeContext>.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "ConditionMonitor")]
	public void Condition_monitor_skips_the_initial_check_when_configured()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("condition", true);

		var monitor = new ConditionMonitorNode(initialCheckOnActivate: false);
		monitor.BindInput(ConditionMonitorNode.ConditionInput, "condition");

		var becameTrue = new TrackingActionNode();

		graph.AddNode(monitor);
		graph.AddNode(becameTrue);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			monitor.InputPorts[StateNode<ConditionMonitorNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			monitor.OutputPorts[ConditionMonitorNode.OnBecameTruePort],
			becameTrue.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		becameTrue.ExecutionCount.Should().Be(0);

		processor.UpdateGraph(0.1);

		becameTrue.ExecutionCount.Should().Be(1);
	}
}
