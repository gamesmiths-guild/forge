// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes.State;

public class RepeatNodeTests
{
	[Fact]
	[Trait("Graph", "Repeat")]
	public void Repeat_runs_every_iteration_on_the_activation_frame()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("count", 3);

		RepeatNode repeat = CreateRepeat(graph);
		(TrackingActionNode onIteration, TrackingActionNode onFinished) = ConnectTrackers(graph, repeat);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		// No UpdateGraph call: the whole loop belongs to the activation frame.
		onIteration.ExecutionCount.Should().Be(3);
		onFinished.ExecutionCount.Should().Be(1);
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "Repeat")]
	public void Repeat_publishes_the_iteration_index_before_each_iteration()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("count", 4);
		graph.VariableDefinitions.DefineVariable("index", -1);

		RepeatNode repeat = CreateRepeat(graph);
		repeat.BindOutput(RepeatNode.IndexOutput, "index");

		var recorder = new RecordVariableNode<int>("index");
		graph.AddNode(recorder);
		graph.AddConnection(new Connection(
			repeat.OutputPorts[RepeatNode.OnIterationPort],
			recorder.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		recorder.ReadValues.Should().Equal(0, 1, 2, 3);
	}

	[Fact]
	[Trait("Graph", "Repeat")]
	public void Repeat_spaces_the_iterations_after_the_first_by_the_interval()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("count", 3);
		graph.VariableDefinitions.DefineVariable("interval", 0.2);

		RepeatNode repeat = CreateRepeat(graph);
		repeat.BindInput(RepeatNode.IntervalInput, "interval");
		(TrackingActionNode onIteration, TrackingActionNode onFinished) = ConnectTrackers(graph, repeat);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		// The first iteration still lands on the activation frame; the interval only spaces out the ones after it.
		onIteration.ExecutionCount.Should().Be(1);
		onFinished.ExecutionCount.Should().Be(0);

		processor.UpdateGraph(0.2);
		onIteration.ExecutionCount.Should().Be(2);
		onFinished.ExecutionCount.Should().Be(0);
		processor.GraphContext.IsActive.Should().BeTrue();

		processor.UpdateGraph(0.2);
		onIteration.ExecutionCount.Should().Be(3);
		onFinished.ExecutionCount.Should().Be(1);
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "Repeat")]
	public void Repeat_catches_up_when_several_intervals_elapse_in_one_update()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("count", 4);
		graph.VariableDefinitions.DefineVariable("interval", 1.0);

		RepeatNode repeat = CreateRepeat(graph);
		repeat.BindInput(RepeatNode.IntervalInput, "interval");
		(TrackingActionNode onIteration, TrackingActionNode onFinished) = ConnectTrackers(graph, repeat);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		processor.UpdateGraph(3.5);

		onIteration.ExecutionCount.Should().Be(4);
		onFinished.ExecutionCount.Should().Be(1);
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "Repeat")]
	public void Repeat_ends_early_through_on_condition_failed_when_the_condition_stops_holding()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("count", 5);
		graph.VariableDefinitions.DefineVariable("counter", 0);
		graph.VariableDefinitions.DefineProperty(
			"keepGoing",
			new ComparisonResolver(
				new VariableResolver("counter", typeof(int)),
				ComparisonOperation.LessThan,
				new VariantResolver(new Variant128(2), typeof(int))));

		RepeatNode repeat = CreateRepeat(graph);
		repeat.BindInput(RepeatNode.ConditionInput, "keepGoing");
		(TrackingActionNode onIteration, TrackingActionNode onFinished) = ConnectTrackers(graph, repeat);
		TrackingActionNode onConditionFailed = ConnectTracker(graph, repeat, RepeatNode.OnConditionFailedPort);

		var counter = new IncrementCounterNode("counter");
		graph.AddNode(counter);
		graph.AddConnection(new Connection(
			repeat.OutputPorts[RepeatNode.OnIterationPort],
			counter.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		// The condition is re-evaluated before every iteration, so the loop stops well short of its count. That is a
		// distinct ending from running out of iterations, so OnFinished must stay silent.
		onIteration.ExecutionCount.Should().Be(2);
		onConditionFailed.ExecutionCount.Should().Be(1);
		onFinished.ExecutionCount.Should().Be(0);
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "Repeat")]
	public void Repeat_runs_no_iteration_when_the_condition_never_holds()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("count", 3);
		graph.VariableDefinitions.DefineVariable("keepGoing", false);

		RepeatNode repeat = CreateRepeat(graph);
		repeat.BindInput(RepeatNode.ConditionInput, "keepGoing");
		(TrackingActionNode onIteration, TrackingActionNode onFinished) = ConnectTrackers(graph, repeat);
		TrackingActionNode onConditionFailed = ConnectTracker(graph, repeat, RepeatNode.OnConditionFailedPort);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		// A condition that never holds reports itself as the reason, even though no iteration ever ran.
		onIteration.ExecutionCount.Should().Be(0);
		onConditionFailed.ExecutionCount.Should().Be(1);
		onFinished.ExecutionCount.Should().Be(0);
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "Repeat")]
	public void Repeat_reports_a_completed_loop_through_on_finished_only()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("count", 2);
		graph.VariableDefinitions.DefineVariable("keepGoing", true);

		RepeatNode repeat = CreateRepeat(graph);
		repeat.BindInput(RepeatNode.ConditionInput, "keepGoing");
		(TrackingActionNode onIteration, TrackingActionNode onFinished) = ConnectTrackers(graph, repeat);
		TrackingActionNode onConditionFailed = ConnectTracker(graph, repeat, RepeatNode.OnConditionFailedPort);
		TrackingActionNode onDeactivate = ConnectTracker(graph, repeat, RepeatNode.OnDeactivatePort);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		// The three endings are mutually exclusive; OnDeactivate is the one that fires for all of them.
		onIteration.ExecutionCount.Should().Be(2);
		onFinished.ExecutionCount.Should().Be(1);
		onConditionFailed.ExecutionCount.Should().Be(0);
		onDeactivate.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Repeat")]
	public void Repeat_cannot_be_steered_by_an_iteration_writing_the_index_variable_back()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("count", 3);
		graph.VariableDefinitions.DefineVariable("index", -1);
		graph.VariableDefinitions.DefineVariable("runs", 0);

		// Safety guard: if the node ever started reading its own index output back, resetting it every iteration
		// would loop forever. This bounds that regression to a failed assertion instead of a hung test.
		graph.VariableDefinitions.DefineProperty(
			"underSafetyLimit",
			new ComparisonResolver(
				new VariableResolver("runs", typeof(int)),
				ComparisonOperation.LessThan,
				new VariantResolver(new Variant128(50), typeof(int))));

		RepeatNode repeat = CreateRepeat(graph);
		repeat.BindInput(RepeatNode.ConditionInput, "underSafetyLimit");
		repeat.BindOutput(RepeatNode.IndexOutput, "index");
		(TrackingActionNode onIteration, TrackingActionNode onFinished) = ConnectTrackers(graph, repeat);

		var recorder = new RecordVariableNode<int>("index");
		var safetyCounter = new IncrementCounterNode("runs");
		var rewinder = new SetIntVariableNode("index", 0);
		graph.AddNode(recorder);
		graph.AddNode(safetyCounter);
		graph.AddNode(rewinder);
		graph.AddConnection(new Connection(
			repeat.OutputPorts[RepeatNode.OnIterationPort],
			recorder.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			recorder.OutputPorts[ActionNode.OutputPort],
			safetyCounter.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			safetyCounter.OutputPorts[ActionNode.OutputPort],
			rewinder.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		// How far the loop has walked lives in the node context, never read back from the variable, so rewinding the
		// variable every iteration changes nothing: the loop still runs exactly its count and each iteration still
		// observes its own index.
		onIteration.ExecutionCount.Should().Be(3);
		recorder.ReadValues.Should().Equal(0, 1, 2);
		onFinished.ExecutionCount.Should().Be(1);
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Theory]
	[Trait("Graph", "Repeat")]
	[InlineData(0)]
	[InlineData(-3)]
	public void Repeat_runs_no_iteration_for_a_non_positive_count(int count)
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("count", count);

		RepeatNode repeat = CreateRepeat(graph);
		(TrackingActionNode onIteration, TrackingActionNode onFinished) = ConnectTrackers(graph, repeat);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		onIteration.ExecutionCount.Should().Be(0);
		onFinished.ExecutionCount.Should().Be(1);
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "Repeat")]
	public void Repeat_runs_no_iteration_when_the_count_is_unbound()
	{
		var graph = new Graph();

		var repeat = new RepeatNode();
		graph.AddNode(repeat);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			repeat.InputPorts[RepeatNode.InputPort]));

		(TrackingActionNode onIteration, TrackingActionNode onFinished) = ConnectTrackers(graph, repeat);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		// An unbound count must never mean "forever": the whole loop can run within a single frame.
		onIteration.ExecutionCount.Should().Be(0);
		onFinished.ExecutionCount.Should().Be(1);
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "Repeat")]
	public void Repeat_stops_iterating_when_an_iteration_stops_the_graph()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("count", 5);

		RepeatNode repeat = CreateRepeat(graph);
		(TrackingActionNode onIteration, TrackingActionNode onFinished) = ConnectTrackers(graph, repeat);

		var exit = new ExitNode();
		graph.AddNode(exit);
		graph.AddConnection(new Connection(
			onIteration.OutputPorts[ActionNode.OutputPort],
			exit.InputPorts[ExitNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		onIteration.ExecutionCount.Should().Be(1);
		onFinished.ExecutionCount.Should().Be(0);
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "Repeat")]
	public void Repeat_stops_iterating_when_an_iteration_aborts_it()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("count", 5);

		RepeatNode repeat = CreateRepeat(graph);
		(TrackingActionNode onIteration, TrackingActionNode onFinished) = ConnectTrackers(graph, repeat);

		var onAbort = new TrackingActionNode();
		graph.AddNode(onAbort);
		graph.AddConnection(new Connection(
			repeat.OutputPorts[RepeatNode.OnAbortPort],
			onAbort.InputPorts[ActionNode.InputPort]));

		var abortTrigger = new FixedConditionNode(true);
		graph.AddNode(abortTrigger);
		graph.AddConnection(new Connection(
			onIteration.OutputPorts[ActionNode.OutputPort],
			abortTrigger.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			abortTrigger.OutputPorts[ConditionNode.TruePort],
			repeat.InputPorts[RepeatNode.AbortPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		onIteration.ExecutionCount.Should().Be(1);
		onAbort.ExecutionCount.Should().Be(1);
		onFinished.ExecutionCount.Should().Be(0);
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	private static RepeatNode CreateRepeat(Graph graph)
	{
		var repeat = new RepeatNode();
		repeat.BindInput(RepeatNode.CountInput, "count");

		graph.AddNode(repeat);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			repeat.InputPorts[RepeatNode.InputPort]));

		return repeat;
	}

	private static TrackingActionNode ConnectTracker(Graph graph, RepeatNode repeat, byte outputPort)
	{
		var tracker = new TrackingActionNode();
		graph.AddNode(tracker);
		graph.AddConnection(new Connection(
			repeat.OutputPorts[outputPort],
			tracker.InputPorts[ActionNode.InputPort]));

		return tracker;
	}

	private static (TrackingActionNode OnIteration, TrackingActionNode OnFinished) ConnectTrackers(
		Graph graph,
		RepeatNode repeat)
	{
		return (
			ConnectTracker(graph, repeat, RepeatNode.OnIterationPort),
			ConnectTracker(graph, repeat, RepeatNode.OnFinishedPort));
	}
}
