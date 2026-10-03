// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes.State;

public class CueNodeTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;

	[Theory]
	[Trait("Graph", "CueNode")]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public void Cue_node_applies_all_scalar_and_array_combinations_on_activation(bool useTagArray, bool useTargetArray)
	{
		var cuesManager = new CuesManager();
		var firstHandler = new RecordingCueHandler();
		var secondHandler = new RecordingCueHandler();
		var firstCue = Tag.RequestTag(_tagsManager, "test.cue1");
		var secondCue = Tag.RequestTag(_tagsManager, "test.cue2");
		cuesManager.RegisterCue(firstCue, firstHandler);
		cuesManager.RegisterCue(secondCue, secondHandler);

		var primaryTarget = new TestEntity(_tagsManager, cuesManager);
		var secondaryTarget = new TestEntity(_tagsManager, cuesManager);

		var graph = new Graph();

		if (useTagArray)
		{
			graph.VariableDefinitions.DefineObjectArrayVariable("cueTag", firstCue, secondCue);
		}
		else
		{
			graph.VariableDefinitions.DefineObjectVariable("cueTag", firstCue);
		}

		if (useTargetArray)
		{
			graph.VariableDefinitions.DefineObjectArrayVariable<IForgeEntity>("target", primaryTarget, secondaryTarget);
		}
		else
		{
			graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", primaryTarget);
		}

		CueNode node = CreateCueNode("cueTag", "target");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[StateNode<CueNodeContext>.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		int expectedPerTag = useTargetArray ? 2 : 1;
		firstHandler.ApplyCount.Should().Be(expectedPerTag);
		secondHandler.ApplyCount.Should().Be(useTagArray ? expectedPerTag : 0);
		firstHandler.IsApplied.Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "CueNode")]
	public void Cue_node_removes_applied_cues_on_deactivation_without_interruption_by_default()
	{
		(GraphProcessor processor, RecordingCueHandler handler) = BuildSingleCueGraph();

		processor.StartGraph();
		handler.ApplyCount.Should().Be(1);
		handler.IsApplied.Should().BeTrue();

		processor.StopGraph();

		handler.RemoveCount.Should().Be(1);
		handler.IsApplied.Should().BeFalse();
		handler.LastInterrupted.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "CueNode")]
	public void Cue_node_marks_removal_interrupted_when_deactivated_through_the_abort_port()
	{
		// Aborting on start fires after activation (synchronous fan-out in connection order), so the cues are applied
		// and then removed as an interruption in the same StartGraph call.
		(GraphProcessor processor, RecordingCueHandler handler) = BuildSingleCueGraph(abortOnStart: true);

		processor.StartGraph();

		handler.ApplyCount.Should().Be(1);
		handler.RemoveCount.Should().Be(1);
		handler.IsApplied.Should().BeFalse();
		handler.LastInterrupted.Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "CueNode")]
	public void Cue_node_passes_provider_authored_custom_parameters_to_the_handler_on_apply()
	{
		var cuesManager = new CuesManager();
		var handler = new RecordingCueHandler();
		var cue = Tag.RequestTag(_tagsManager, "test.cue1");
		cuesManager.RegisterCue(cue, handler);
		var target = new TestEntity(_tagsManager, cuesManager);

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("cueTag", cue);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);
		graph.VariableDefinitions.DefineObjectProperty(
			"customParams",
			new CueCustomParametersResolver(new TestCueCustomParametersProvider()));

		CueNode node = CreateCueNode("cueTag", "target");
		node.BindInput(CueNode.CustomParametersInput, "customParams");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[StateNode<CueNodeContext>.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		handler.ApplyCount.Should().Be(1);
		handler.LastParameters.Should().NotBeNull();
		handler.LastParameters!.Value.CustomParameters.Should().NotBeNull();
		handler.LastParameters.Value.CustomParameters![TestCueCustomParametersProvider.PowerKey]
			.Should().Be(TestCueCustomParametersProvider.PowerValue);
	}

	[Fact]
	[Trait("Graph", "CueNode")]
	public void A_retriggered_cue_node_applies_its_cue_once_by_default()
	{
		(GraphProcessor processor, RecordingCueHandler handler) = BuildSingleCueGraph(retriggerAt: 1.0);

		processor.StartGraph();
		processor.UpdateGraph(1.0);

		handler.ApplyCount.Should().Be(1);
		handler.RemoveCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "CueNode")]
	public void A_restarting_cue_node_cuts_its_cue_short_and_applies_it_again()
	{
		(GraphProcessor processor, RecordingCueHandler handler) =
			BuildSingleCueGraph(retriggerAt: 1.0, restartOnRetrigger: true);

		processor.StartGraph();
		processor.UpdateGraph(1.0);

		handler.ApplyCount.Should().Be(2);
		handler.RemoveCount.Should().Be(1);
		handler.LastInterrupted.Should().BeTrue();
		handler.IsApplied.Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "CueNode")]
	public void A_restart_whose_removal_stops_the_graph_applies_nothing_more()
	{
		(GraphProcessor processor, RecordingCueHandler handler) =
			BuildSingleCueGraph(retriggerAt: 1.0, restartOnRetrigger: true);

		// A handler's removal can end the ability the graph runs for, which stops the graph from under the restart.
		handler.Removed = processor.StopGraph;
		processor.StartGraph();

		processor.Invoking(x => x.UpdateGraph(1.0)).Should().NotThrow();

		handler.ApplyCount.Should().Be(1, "the restart ended with the graph instead of applying again");
		handler.RemoveCount.Should().Be(1, "the graph stopping does not remove the cue a second time");
	}

	[Fact]
	[Trait("Graph", "CueNode")]
	public void A_restart_whose_removal_starts_the_graph_over_leaves_the_new_run_its_own_cue()
	{
		(GraphProcessor processor, RecordingCueHandler handler) =
			BuildSingleCueGraph(retriggerAt: 1.0, restartOnRetrigger: true);

		// A handler's removal stops the graph, and the graph starts over before the restart it cut short returns.
		handler.Removed = processor.StopGraph;
		RestartOnFirstCompletion(processor);
		processor.StartGraph();
		processor.UpdateGraph(1.0);

		handler.ApplyCount.Should().Be(2, "the new run applied the cue, and the restart it replaced did not");

		processor.StopGraph();

		handler.RemoveCount.Should().Be(2, "every cue applied was removed");
	}

	[Fact]
	[Trait("Graph", "CueNode")]
	public void A_cue_that_stops_the_graph_as_it_applies_is_removed_with_the_rest()
	{
		(GraphProcessor processor, RecordingCueHandler firstHandler, RecordingCueHandler secondHandler) =
			BuildTwoCueGraph();

		// The second cue's handler ends the ability the graph runs for as it applies, which stops the graph before the
		// node holds that cue.
		secondHandler.Applied = processor.StopGraph;
		processor.StartGraph();

		firstHandler.RemoveCount.Should().Be(1, "the stop removed the cue the node already held");
		secondHandler.RemoveCount.Should().Be(1, "the cue the node did not hold yet is removed once it does");
	}

	[Fact]
	[Trait("Graph", "CueNode")]
	public void The_cues_after_one_that_stops_the_graph_as_it_applies_go_unapplied()
	{
		(GraphProcessor processor, RecordingCueHandler firstHandler, RecordingCueHandler secondHandler) =
			BuildTwoCueGraph();

		// The first cue's handler ends the ability the graph runs for as it applies.
		firstHandler.Applied = processor.StopGraph;
		processor.StartGraph();

		firstHandler.RemoveCount.Should().Be(1, "the cue the node did not hold yet is removed once it does");
		secondHandler.ApplyCount.Should().Be(0, "nothing is applied for a graph that is gone");
	}

	[Fact]
	[Trait("Graph", "CueNode")]
	public void The_cues_after_one_that_ends_the_node_as_it_applies_go_unapplied()
	{
		(GraphProcessor processor, RecordingCueHandler firstHandler, RecordingCueHandler secondHandler) =
			BuildTwoCueGraph();
		Node node = processor.Graph.Nodes.Single(x => x is CueNode);

		// The first cue's handler aborts the node as it applies, as a listener wired to its abort would.
		firstHandler.Applied = () =>
			node.InputPorts[StateNode<CueNodeContext>.AbortPort].ReceiveMessage(processor.GraphContext);
		processor.StartGraph();

		firstHandler.RemoveCount.Should().Be(1, "the cue the node did not hold yet is removed once it does");
		secondHandler.ApplyCount.Should().Be(0, "nothing is applied for a node that has ended");
	}

	private (GraphProcessor Processor, RecordingCueHandler FirstHandler, RecordingCueHandler SecondHandler)
		BuildTwoCueGraph()
	{
		var cuesManager = new CuesManager();
		var firstHandler = new RecordingCueHandler();
		var secondHandler = new RecordingCueHandler();
		var firstCue = Tag.RequestTag(_tagsManager, "test.cue1");
		var secondCue = Tag.RequestTag(_tagsManager, "test.cue2");
		cuesManager.RegisterCue(firstCue, firstHandler);
		cuesManager.RegisterCue(secondCue, secondHandler);
		var target = new TestEntity(_tagsManager, cuesManager);

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectArrayVariable("cueTag", firstCue, secondCue);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);

		var node = new CueNode();
		node.BindInput(CueNode.CueTagInput, "cueTag");
		node.BindInput(CueNode.TargetInput, "target");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[StateNode<CueNodeContext>.InputPort]));

		return (new GraphProcessor(graph), firstHandler, secondHandler);
	}

	private (GraphProcessor Processor, RecordingCueHandler Handler) BuildSingleCueGraph(
		bool abortOnStart = false,
		double? retriggerAt = null,
		bool restartOnRetrigger = false)
	{
		var cuesManager = new CuesManager();
		var handler = new RecordingCueHandler();
		var cue = Tag.RequestTag(_tagsManager, "test.cue1");
		cuesManager.RegisterCue(cue, handler);
		var target = new TestEntity(_tagsManager, cuesManager);

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("cueTag", cue);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);

		var node = new CueNode(restartOnRetrigger);
		node.BindInput(CueNode.CueTagInput, "cueTag");
		node.BindInput(CueNode.TargetInput, "target");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[StateNode<CueNodeContext>.InputPort]));

		if (abortOnStart)
		{
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				node.InputPorts[StateNode<CueNodeContext>.AbortPort]));
		}

		if (retriggerAt is double delay)
		{
			ConnectRetrigger(graph, node, delay);
		}

		return (new GraphProcessor(graph), handler);
	}
}
