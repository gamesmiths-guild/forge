// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript;

public class StateNodeTests
{
	[Fact]
	[Trait("Graph", "Timer")]
	public void Timer_node_stays_active_until_duration_elapses()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 2.0);

		var timer = new TimerNode("duration");

		graph.AddNode(timer);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[ActionNode.InputPort]));

		var context = new GraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		context.IsActive.Should().BeTrue();

		// Not enough time has passed.
		processor.UpdateGraph(1.0);
		context.IsActive.Should().BeTrue();

		// Still not enough.
		processor.UpdateGraph(0.5);
		context.IsActive.Should().BeTrue();

		// Now it should deactivate (total: 1.0 + 0.5 + 0.5 = 2.0).
		processor.UpdateGraph(0.5);
		context.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "Timer")]
	public void Timer_node_fires_on_deactivate_event_when_completed()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 1.0);

		var timer = new TimerNode("duration");
		var onDeactivateAction = new TrackingActionNode();

		graph.AddNode(timer);
		graph.AddNode(onDeactivateAction);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			timer.OutputPorts[TimerNode.OnDeactivatePort],
			onDeactivateAction.InputPorts[ActionNode.InputPort]));

		var context = new GraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		onDeactivateAction.ExecutionCount.Should().Be(0);

		processor.UpdateGraph(1.0);

		onDeactivateAction.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Timer")]
	public void Timer_node_fires_on_activate_event_on_start()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 5.0);

		var timer = new TimerNode("duration");
		var onActivateAction = new TrackingActionNode();

		graph.AddNode(timer);
		graph.AddNode(onActivateAction);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			timer.OutputPorts[TimerNode.OnActivatePort],
			onActivateAction.InputPorts[ActionNode.InputPort]));

		var context = new GraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		onActivateAction.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Timer")]
	public void Two_processors_with_same_timer_graph_have_independent_elapsed_time()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 2.0);

		var timer = new TimerNode("duration");

		graph.AddNode(timer);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[ActionNode.InputPort]));

		var context1 = new GraphContext();
		var processor1 = new GraphProcessor(graph, context1);

		var context2 = new GraphContext();
		var processor2 = new GraphProcessor(graph, context2);

		processor1.StartGraph();
		processor2.StartGraph();

		processor1.UpdateGraph(2.0);
		processor2.UpdateGraph(1.0);

		context1.IsActive.Should().BeFalse();
		context2.IsActive.Should().BeTrue();

		processor2.UpdateGraph(1.0);
		context2.IsActive.Should().BeFalse();
	}
}
