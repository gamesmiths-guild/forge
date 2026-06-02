// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes.State;

public class TimerNodeTests
{
	[Fact]
	[Trait("Graph", "Timer")]
	public void Timer_node_stays_active_until_duration_elapses()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 2.0);

		TimerNode timer = CreateTimerNode("duration");

		graph.AddNode(timer);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		processor.GraphContext.IsActive.Should().BeTrue();

		// Not enough time has passed.
		processor.UpdateGraph(1.0);
		processor.GraphContext.IsActive.Should().BeTrue();

		// Still not enough.
		processor.UpdateGraph(0.5);
		processor.GraphContext.IsActive.Should().BeTrue();

		// Now it should deactivate (total: 1.0 + 0.5 + 0.5 = 2.0).
		processor.UpdateGraph(0.5);
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "Timer")]
	public void Timer_node_fires_on_deactivate_event_when_completed()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 1.0);

		TimerNode timer = CreateTimerNode("duration");
		var onDeactivateAction = new TrackingActionNode();

		graph.AddNode(timer);
		graph.AddNode(onDeactivateAction);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			timer.OutputPorts[TimerNode.OnDeactivatePort],
			onDeactivateAction.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		onDeactivateAction.ExecutionCount.Should().Be(0);

		processor.UpdateGraph(1.0);

		onDeactivateAction.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Timer")]
	public void Timer_node_fires_OnTimerEnd_when_duration_elapses_naturally()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 1.0);

		TimerNode timer = CreateTimerNode("duration");
		var onTimerEndAction = new TrackingActionNode();

		graph.AddNode(timer);
		graph.AddNode(onTimerEndAction);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			timer.OutputPorts[TimerNode.OnTimerEndPort],
			onTimerEndAction.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		onTimerEndAction.ExecutionCount.Should().Be(0);

		processor.UpdateGraph(1.0);

		onTimerEndAction.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Timer")]
	public void Timer_node_OnTimerEnd_can_still_resolve_property_backed_inputs_before_graph_completion()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 1.0);
		graph.VariableDefinitions.DefineProperty(
			"constant",
			new VariantResolver(new Variant128(1), typeof(int)));

		TimerNode timer = CreateTimerNode("duration");
		var readNode = new ReadPropertyNode<int>();
		readNode.BindInput(ReadPropertyNode<int>.ValueInput, "constant");

		graph.AddNode(timer);
		graph.AddNode(readNode);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			timer.OutputPorts[TimerNode.OnTimerEndPort],
			readNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		readNode.Found.Should().BeFalse();

		processor.UpdateGraph(1.0);

		readNode.Found.Should().BeTrue();
		readNode.LastReadValue.Should().Be(1);
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "Timer")]
	public void Timer_node_does_not_fire_OnTimerEnd_when_graph_stops_early()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 5.0);

		TimerNode timer = CreateTimerNode("duration");
		var onTimerEndAction = new TrackingActionNode();

		graph.AddNode(timer);
		graph.AddNode(onTimerEndAction);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			timer.OutputPorts[TimerNode.OnTimerEndPort],
			onTimerEndAction.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		processor.UpdateGraph(1.0);

		processor.StopGraph();

		onTimerEndAction.ExecutionCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "Timer")]
	public void Timer_node_does_not_fire_OnTimerEnd_when_deactivated_by_parent_subgraph()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("parentDuration", 0.5d);
		graph.VariableDefinitions.DefineVariable("childDuration", 5.0d);

		TimerNode parentTimer = CreateTimerNode("parentDuration");
		TimerNode childTimer = CreateTimerNode("childDuration");
		var onTimerEndAction = new TrackingActionNode();

		graph.AddNode(parentTimer);
		graph.AddNode(childTimer);
		graph.AddNode(onTimerEndAction);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			parentTimer.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			parentTimer.OutputPorts[StateNode<TimerNodeContext>.SubgraphPort],
			childTimer.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			childTimer.OutputPorts[TimerNode.OnTimerEndPort],
			onTimerEndAction.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		processor.UpdateGraph(0.5);

		onTimerEndAction.ExecutionCount.Should().Be(0);
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "Timer")]
	public void Timer_node_fires_on_activate_event_on_start()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 5.0);

		TimerNode timer = CreateTimerNode("duration");
		var onActivateAction = new TrackingActionNode();

		graph.AddNode(timer);
		graph.AddNode(onActivateAction);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			timer.OutputPorts[TimerNode.OnActivatePort],
			onActivateAction.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		onActivateAction.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Timer")]
	public void Two_processors_with_same_timer_graph_have_independent_elapsed_time()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 2.0);

		TimerNode timer = CreateTimerNode("duration");

		graph.AddNode(timer);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[ActionNode.InputPort]));

		var processor1 = new GraphProcessor(graph);
		var processor2 = new GraphProcessor(graph);

		processor1.StartGraph();
		processor2.StartGraph();

		processor1.UpdateGraph(2.0);
		processor2.UpdateGraph(1.0);

		processor1.GraphContext.IsActive.Should().BeFalse();
		processor2.GraphContext.IsActive.Should().BeTrue();

		processor2.UpdateGraph(1.0);
		processor2.GraphContext.IsActive.Should().BeFalse();
	}
}
