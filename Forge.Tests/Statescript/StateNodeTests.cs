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

		var processor = new GraphProcessor(graph);
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

		var timer = new TimerNode("duration");

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
