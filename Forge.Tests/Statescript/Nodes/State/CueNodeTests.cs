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

	private (GraphProcessor Processor, RecordingCueHandler Handler) BuildSingleCueGraph(bool abortOnStart = false)
	{
		var cuesManager = new CuesManager();
		var handler = new RecordingCueHandler();
		var cue = Tag.RequestTag(_tagsManager, "test.cue1");
		cuesManager.RegisterCue(cue, handler);
		var target = new TestEntity(_tagsManager, cuesManager);

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("cueTag", cue);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);

		CueNode node = CreateCueNode("cueTag", "target");
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

		return (new GraphProcessor(graph), handler);
	}
}
