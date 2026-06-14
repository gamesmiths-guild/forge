// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes.Action;

public class UpdateCueNodeTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;

	[Theory]
	[Trait("Graph", "UpdateCue")]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public void Update_cue_node_supports_all_scalar_and_array_combinations(bool useTagArray, bool useTargetArray)
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

		graph.VariableDefinitions.DefineVariable("magnitude", 3);

		UpdateCueNode node = CreateUpdateCueNode("cueTag", "target");
		node.BindInput(UpdateCueNode.MagnitudeInput, "magnitude");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		int expectedPerTag = useTargetArray ? 2 : 1;
		firstHandler.UpdateCount.Should().Be(expectedPerTag);
		secondHandler.UpdateCount.Should().Be(useTagArray ? expectedPerTag : 0);
	}

	[Fact]
	[Trait("Graph", "UpdateCue")]
	public void Update_cue_node_passes_resolved_parameters_to_the_handler()
	{
		var cuesManager = new CuesManager();
		var handler = new RecordingCueHandler();
		var cue = Tag.RequestTag(_tagsManager, "test.cue1");
		cuesManager.RegisterCue(cue, handler);
		var target = new TestEntity(_tagsManager, cuesManager);

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("cueTag", cue);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);
		graph.VariableDefinitions.DefineVariable("magnitude", 9);

		UpdateCueNode node = CreateUpdateCueNode("cueTag", "target");
		node.BindInput(UpdateCueNode.MagnitudeInput, "magnitude");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		handler.UpdateCount.Should().Be(1);
		handler.LastTarget.Should().BeSameAs(target);
		handler.LastParameters.Should().NotBeNull();
		handler.LastParameters!.Value.Magnitude.Should().Be(9);
	}
}
