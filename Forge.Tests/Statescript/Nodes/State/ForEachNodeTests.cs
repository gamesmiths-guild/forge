// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes.State;

public class ForEachNodeTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = fixture.TagsManager;
	private readonly CuesManager _cuesManager = fixture.CuesManager;

	[Fact]
	[Trait("Graph", "ForEach")]
	public void For_each_walks_a_value_array_within_the_activation_frame()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineArrayVariable("source", 10, 20, 30);
		graph.VariableDefinitions.DefineVariable("element", 0);
		graph.VariableDefinitions.DefineVariable("index", -1);

		ForEachNode forEach = CreateForEach(graph);
		forEach.BindOutput(ForEachNode.ElementOutput, "element");
		forEach.BindOutput(ForEachNode.IndexOutput, "index");

		var elements = new RecordVariableNode<int>("element");
		var indices = new RecordVariableNode<int>("index");
		graph.AddNode(elements);
		graph.AddNode(indices);
		graph.AddConnection(new Connection(
			forEach.OutputPorts[ForEachNode.OnIterationPort],
			elements.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			forEach.OutputPorts[ForEachNode.OnIterationPort],
			indices.InputPorts[ActionNode.InputPort]));

		TrackingActionNode onFinished = ConnectFinishedTracker(graph, forEach);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		elements.ReadValues.Should().Equal(10, 20, 30);
		indices.ReadValues.Should().Equal(0, 1, 2);
		onFinished.ExecutionCount.Should().Be(1);
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "ForEach")]
	public void For_each_walks_an_object_array_through_the_element_variable_type()
	{
		var first = new TestEntity(_tagsManager, _cuesManager);
		var second = new TestEntity(_tagsManager, _cuesManager);

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectArrayVariable<IForgeEntity>("targets", first, second);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target");

		ForEachNode forEach = CreateForEach(graph, "targets");
		forEach.BindOutput(ForEachNode.ElementOutput, "target");

		var recorder = new RecordObjectVariableNode<IForgeEntity>("target");
		graph.AddNode(recorder);
		graph.AddConnection(new Connection(
			forEach.OutputPorts[ForEachNode.OnIterationPort],
			recorder.InputPorts[ActionNode.InputPort]));

		TrackingActionNode onFinished = ConnectFinishedTracker(graph, forEach);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		recorder.ReadValues.Should().Equal(first, second);
		onFinished.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "ForEach")]
	public void For_each_iterates_for_the_index_alone_when_no_element_variable_is_bound()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectArrayVariable<IForgeEntity>(
			"targets",
			new TestEntity(_tagsManager, _cuesManager),
			new TestEntity(_tagsManager, _cuesManager),
			new TestEntity(_tagsManager, _cuesManager));
		graph.VariableDefinitions.DefineVariable("index", -1);

		ForEachNode forEach = CreateForEach(graph, "targets");
		forEach.BindOutput(ForEachNode.IndexOutput, "index");

		var indices = new RecordVariableNode<int>("index");
		graph.AddNode(indices);
		graph.AddConnection(new Connection(
			forEach.OutputPorts[ForEachNode.OnIterationPort],
			indices.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		indices.ReadValues.Should().Equal(0, 1, 2);
	}

	[Fact]
	[Trait("Graph", "ForEach")]
	public void For_each_runs_no_iteration_when_the_element_variable_type_does_not_match_the_source()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectArrayVariable<IForgeEntity>(
			"targets",
			new TestEntity(_tagsManager, _cuesManager));
		graph.VariableDefinitions.DefineObjectVariable<Effect>("mismatched");

		ForEachNode forEach = CreateForEach(graph, "targets");
		forEach.BindOutput(ForEachNode.ElementOutput, "mismatched");

		(TrackingActionNode onIteration, TrackingActionNode onFinished) = ConnectTrackers(graph, forEach);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		// Nothing converts between element types, so a mismatch reads as an empty sequence rather than a bad write.
		onIteration.ExecutionCount.Should().Be(0);
		onFinished.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "ForEach")]
	public void For_each_runs_no_iteration_for_an_empty_or_unbound_source()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineArrayVariable<int>("source");

		ForEachNode forEach = CreateForEach(graph);
		(TrackingActionNode onIteration, TrackingActionNode onFinished) = ConnectTrackers(graph, forEach);

		var unboundGraph = new Graph();
		var unbound = new ForEachNode();
		unboundGraph.AddNode(unbound);
		unboundGraph.AddConnection(new Connection(
			unboundGraph.EntryNode.OutputPorts[EntryNode.OutputPort],
			unbound.InputPorts[ForEachNode.InputPort]));
		(TrackingActionNode unboundIteration, TrackingActionNode unboundFinished) =
			ConnectTrackers(unboundGraph, unbound);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		var unboundProcessor = new GraphProcessor(unboundGraph);
		unboundProcessor.StartGraph();

		onIteration.ExecutionCount.Should().Be(0);
		onFinished.ExecutionCount.Should().Be(1);
		unboundIteration.ExecutionCount.Should().Be(0);
		unboundFinished.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "ForEach")]
	public void For_each_spaces_the_iterations_after_the_first_by_the_interval()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineArrayVariable("source", 10, 20, 30);
		graph.VariableDefinitions.DefineVariable("element", 0);
		graph.VariableDefinitions.DefineVariable("interval", 0.5);

		ForEachNode forEach = CreateForEach(graph);
		forEach.BindInput(ForEachNode.IntervalInput, "interval");
		forEach.BindOutput(ForEachNode.ElementOutput, "element");

		var elements = new RecordVariableNode<int>("element");
		graph.AddNode(elements);
		graph.AddConnection(new Connection(
			forEach.OutputPorts[ForEachNode.OnIterationPort],
			elements.InputPorts[ActionNode.InputPort]));

		TrackingActionNode onFinished = ConnectFinishedTracker(graph, forEach);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		elements.ReadValues.Should().Equal(10);

		processor.UpdateGraph(0.5);
		elements.ReadValues.Should().Equal(10, 20);
		onFinished.ExecutionCount.Should().Be(0);
		processor.GraphContext.IsActive.Should().BeTrue();

		processor.UpdateGraph(0.5);
		elements.ReadValues.Should().Equal(10, 20, 30);
		onFinished.ExecutionCount.Should().Be(1);
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "ForEach")]
	public void For_each_keeps_iterating_the_array_it_snapshotted_on_activation()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineArrayVariable("source", 10, 20, 30);
		graph.VariableDefinitions.DefineVariable("element", 0);

		ForEachNode forEach = CreateForEach(graph);
		forEach.BindOutput(ForEachNode.ElementOutput, "element");

		var elements = new RecordVariableNode<int>("element");
		var replacer = new ReplaceIntArrayVariableNode("source", 99);
		graph.AddNode(elements);
		graph.AddNode(replacer);
		graph.AddConnection(new Connection(
			forEach.OutputPorts[ForEachNode.OnIterationPort],
			elements.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			elements.OutputPorts[ActionNode.OutputPort],
			replacer.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		// The first iteration replaces the source variable with a shorter array; the loop must not notice.
		elements.ReadValues.Should().Equal(10, 20, 30);
	}

	[Fact]
	[Trait("Graph", "ForEach")]
	public void For_each_ends_early_through_on_condition_failed_when_the_condition_stops_holding()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineArrayVariable("source", 10, 20, 30, 40);
		graph.VariableDefinitions.DefineVariable("keepGoing", true);

		ForEachNode forEach = CreateForEach(graph);
		forEach.BindInput(ForEachNode.ConditionInput, "keepGoing");

		(TrackingActionNode onIteration, TrackingActionNode onFinished) = ConnectTrackers(graph, forEach);
		TrackingActionNode onConditionFailed = ConnectTracker(graph, forEach, ForEachNode.OnConditionFailedPort);

		var stopper = new SetBoolVariableNode("keepGoing", false);
		graph.AddNode(stopper);
		graph.AddConnection(new Connection(
			onIteration.OutputPorts[ActionNode.OutputPort],
			stopper.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		// Walking off the end of the array and being cut short by the guard are distinct endings.
		onIteration.ExecutionCount.Should().Be(1);
		onConditionFailed.ExecutionCount.Should().Be(1);
		onFinished.ExecutionCount.Should().Be(0);
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "ForEach")]
	public void For_each_cannot_be_steered_by_an_iteration_writing_its_outputs_back()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineArrayVariable("source", 10, 20, 30);
		graph.VariableDefinitions.DefineVariable("element", 0);
		graph.VariableDefinitions.DefineVariable("index", -1);
		graph.VariableDefinitions.DefineVariable("runs", 0);

		// Safety guard: if the node ever started reading its own index output back, rewinding it every iteration
		// would loop forever. This bounds that regression to a failed assertion instead of a hung test.
		graph.VariableDefinitions.DefineProperty(
			"underSafetyLimit",
			new ComparisonResolver(
				new VariableResolver("runs", typeof(int)),
				ComparisonOperation.LessThan,
				new VariantResolver(new Variant128(50), typeof(int))));

		ForEachNode forEach = CreateForEach(graph);
		forEach.BindInput(ForEachNode.ConditionInput, "underSafetyLimit");
		forEach.BindOutput(ForEachNode.ElementOutput, "element");
		forEach.BindOutput(ForEachNode.IndexOutput, "index");

		(TrackingActionNode onIteration, TrackingActionNode onFinished) = ConnectTrackers(graph, forEach);

		var elements = new RecordVariableNode<int>("element");
		var safetyCounter = new IncrementCounterNode("runs");
		var indexRewinder = new SetIntVariableNode("index", 0);
		var elementRewinder = new SetIntVariableNode("element", 999);
		graph.AddNode(elements);
		graph.AddNode(safetyCounter);
		graph.AddNode(indexRewinder);
		graph.AddNode(elementRewinder);
		graph.AddConnection(new Connection(
			forEach.OutputPorts[ForEachNode.OnIterationPort],
			elements.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			elements.OutputPorts[ActionNode.OutputPort],
			safetyCounter.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			safetyCounter.OutputPorts[ActionNode.OutputPort],
			indexRewinder.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			indexRewinder.OutputPorts[ActionNode.OutputPort],
			elementRewinder.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		// Both outputs are write-only as far as the loop is concerned: position lives in the node context and the
		// elements come from the activation snapshot, so rewriting either changes nothing.
		onIteration.ExecutionCount.Should().Be(3);
		elements.ReadValues.Should().Equal(10, 20, 30);
		onFinished.ExecutionCount.Should().Be(1);
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "ForEach")]
	public void For_each_stops_iterating_when_an_iteration_stops_the_graph()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineArrayVariable("source", 10, 20, 30);

		ForEachNode forEach = CreateForEach(graph);
		(TrackingActionNode onIteration, TrackingActionNode onFinished) = ConnectTrackers(graph, forEach);

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

	private static ForEachNode CreateForEach(Graph graph, StringKey sourceName = default)
	{
		var forEach = new ForEachNode();
		forEach.BindInput(ForEachNode.ArrayInput, sourceName == StringKey.Empty ? "source" : sourceName);

		graph.AddNode(forEach);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			forEach.InputPorts[ForEachNode.InputPort]));

		return forEach;
	}

	private static TrackingActionNode ConnectTracker(Graph graph, ForEachNode forEach, byte outputPort)
	{
		var tracker = new TrackingActionNode();
		graph.AddNode(tracker);
		graph.AddConnection(new Connection(
			forEach.OutputPorts[outputPort],
			tracker.InputPorts[ActionNode.InputPort]));

		return tracker;
	}

	private static TrackingActionNode ConnectFinishedTracker(Graph graph, ForEachNode forEach)
	{
		return ConnectTracker(graph, forEach, ForEachNode.OnFinishedPort);
	}

	private static (TrackingActionNode OnIteration, TrackingActionNode OnFinished) ConnectTrackers(
		Graph graph,
		ForEachNode forEach)
	{
		return (
			ConnectTracker(graph, forEach, ForEachNode.OnIterationPort),
			ConnectTracker(graph, forEach, ForEachNode.OnFinishedPort));
	}
}
