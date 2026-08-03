// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes.State;

public class LoopTimerNodeTests
{
	[Fact]
	[Trait("Graph", "LoopTimer")]
	public void Loop_timer_fires_on_interval_and_finishes_after_the_configured_loops()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("interval", 1.0);
		graph.VariableDefinitions.DefineVariable("loops", 3);

		LoopTimerNode loopTimer = CreateLoopTimer(graph, bindLoopCount: true);
		(TrackingActionNode onInterval, TrackingActionNode onFinished) = ConnectTrackers(graph, loopTimer);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		processor.UpdateGraph(1.0);
		onInterval.ExecutionCount.Should().Be(1);
		onFinished.ExecutionCount.Should().Be(0);
		processor.GraphContext.IsActive.Should().BeTrue();

		processor.UpdateGraph(1.0);
		onInterval.ExecutionCount.Should().Be(2);

		processor.UpdateGraph(1.0);
		onInterval.ExecutionCount.Should().Be(3);
		onFinished.ExecutionCount.Should().Be(1);
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "LoopTimer")]
	public void Loop_timer_handles_multiple_intervals_in_a_single_update()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("interval", 1.0);
		graph.VariableDefinitions.DefineVariable("loops", 3);

		LoopTimerNode loopTimer = CreateLoopTimer(graph, bindLoopCount: true);
		(TrackingActionNode onInterval, TrackingActionNode onFinished) = ConnectTrackers(graph, loopTimer);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		processor.UpdateGraph(3.5);

		onInterval.ExecutionCount.Should().Be(3);
		onFinished.ExecutionCount.Should().Be(1);
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "LoopTimer")]
	public void Loop_timer_loops_forever_when_no_loop_count_is_bound()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("interval", 1.0);

		LoopTimerNode loopTimer = CreateLoopTimer(graph, bindLoopCount: false);
		(TrackingActionNode onInterval, TrackingActionNode onFinished) = ConnectTrackers(graph, loopTimer);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		processor.UpdateGraph(5.0);

		onInterval.ExecutionCount.Should().Be(5);
		onFinished.ExecutionCount.Should().Be(0);
		processor.GraphContext.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "LoopTimer")]
	public void Loop_timer_stops_iterating_when_an_interval_handler_stops_the_graph()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("interval", 1.0);
		graph.VariableDefinitions.DefineVariable("loops", 3);

		LoopTimerNode loopTimer = CreateLoopTimer(graph, bindLoopCount: true);
		(TrackingActionNode onInterval, TrackingActionNode onFinished) = ConnectTrackers(graph, loopTimer);

		var exit = new ExitNode();
		graph.AddNode(exit);
		graph.AddConnection(new Connection(
			onInterval.OutputPorts[ActionNode.OutputPort],
			exit.InputPorts[ExitNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		// Three intervals elapse at once, but the first one stops the graph: the remaining ones must not run.
		processor.UpdateGraph(3.5);

		onInterval.ExecutionCount.Should().Be(1);
		onFinished.ExecutionCount.Should().Be(0);
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	private static LoopTimerNode CreateLoopTimer(Graph graph, bool bindLoopCount)
	{
		var loopTimer = new LoopTimerNode();
		loopTimer.BindInput(LoopTimerNode.IntervalInput, "interval");

		if (bindLoopCount)
		{
			loopTimer.BindInput(LoopTimerNode.LoopCountInput, "loops");
		}

		graph.AddNode(loopTimer);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			loopTimer.InputPorts[StateNode<LoopTimerNodeContext>.InputPort]));

		return loopTimer;
	}

	private static (TrackingActionNode OnInterval, TrackingActionNode OnFinished) ConnectTrackers(
		Graph graph,
		LoopTimerNode loopTimer)
	{
		var onInterval = new TrackingActionNode();
		var onFinished = new TrackingActionNode();

		graph.AddNode(onInterval);
		graph.AddNode(onFinished);
		graph.AddConnection(new Connection(
			loopTimer.OutputPorts[LoopTimerNode.OnIntervalPort],
			onInterval.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			loopTimer.OutputPorts[LoopTimerNode.OnFinishedPort],
			onFinished.InputPorts[ActionNode.InputPort]));

		return (onInterval, onFinished);
	}
}
