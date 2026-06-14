// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes.Action;

public class ExecuteCueNodeTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;

	[Theory]
	[Trait("Graph", "ExecuteCue")]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public void Execute_cue_node_supports_all_scalar_and_array_combinations(bool useTagArray, bool useTargetArray)
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
		ConfigureCueTagInput(graph, useTagArray, firstCue, secondCue);
		ConfigureTargetInput(graph, useTargetArray, primaryTarget, secondaryTarget);
		graph.VariableDefinitions.DefineVariable("magnitude", 7);

		ExecuteCueNode node = CreateExecuteCueNode("cueTag", "target");
		node.BindInput(ExecuteCueNode.MagnitudeInput, "magnitude");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		int expectedPerTag = useTargetArray ? 2 : 1;
		firstHandler.ExecuteCount.Should().Be(expectedPerTag);
		secondHandler.ExecuteCount.Should().Be(useTagArray ? expectedPerTag : 0);
	}

	[Fact]
	[Trait("Graph", "ExecuteCue")]
	public void Execute_cue_node_passes_resolved_parameters_to_the_handler()
	{
		var cuesManager = new CuesManager();
		var handler = new RecordingCueHandler();
		var cue = Tag.RequestTag(_tagsManager, "test.cue1");
		cuesManager.RegisterCue(cue, handler);
		var target = new TestEntity(_tagsManager, cuesManager);

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("cueTag", cue);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);
		graph.VariableDefinitions.DefineVariable("magnitude", 12);
		graph.VariableDefinitions.DefineVariable("normalized", 0.75f);

		ExecuteCueNode node = CreateExecuteCueNode("cueTag", "target");
		node.BindInput(ExecuteCueNode.MagnitudeInput, "magnitude");
		node.BindInput(ExecuteCueNode.NormalizedMagnitudeInput, "normalized");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		handler.ExecuteCount.Should().Be(1);
		handler.LastTarget.Should().BeSameAs(target);
		handler.LastParameters.Should().NotBeNull();
		handler.LastParameters!.Value.Magnitude.Should().Be(12);
		handler.LastParameters.Value.NormalizedMagnitude.Should().Be(0.75f);
	}

	[Fact]
	[Trait("Graph", "ExecuteCue")]
	public void Execute_cue_node_passes_null_parameters_when_no_parameter_inputs_are_bound()
	{
		var cuesManager = new CuesManager();
		var handler = new RecordingCueHandler();
		var cue = Tag.RequestTag(_tagsManager, "test.cue1");
		cuesManager.RegisterCue(cue, handler);
		var target = new TestEntity(_tagsManager, cuesManager);

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("cueTag", cue);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);

		ExecuteCueNode node = CreateExecuteCueNode("cueTag", "target");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		handler.ExecuteCount.Should().Be(1);
		handler.LastParameters.Should().BeNull();
	}

	[Fact]
	[Trait("Graph", "ExecuteCue")]
	public void Execute_cue_node_passes_provider_authored_custom_parameters_to_the_handler()
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

		ExecuteCueNode node = CreateExecuteCueNode("cueTag", "target");
		node.BindInput(ExecuteCueNode.CustomParametersInput, "customParams");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		handler.ExecuteCount.Should().Be(1);
		handler.LastParameters.Should().NotBeNull();
		handler.LastParameters!.Value.CustomParameters.Should().NotBeNull();
		handler.LastParameters.Value.CustomParameters![TestCueCustomParametersProvider.PowerKey]
			.Should().Be(TestCueCustomParametersProvider.PowerValue);
	}

	private static void ConfigureCueTagInput(Graph graph, bool useArray, Tag firstCue, Tag secondCue)
	{
		if (useArray)
		{
			graph.VariableDefinitions.DefineObjectArrayVariable("cueTag", firstCue, secondCue);
			return;
		}

		graph.VariableDefinitions.DefineObjectVariable("cueTag", firstCue);
	}

	private static void ConfigureTargetInput(Graph graph, bool useArray, TestEntity primary, TestEntity secondary)
	{
		if (useArray)
		{
			graph.VariableDefinitions.DefineObjectArrayVariable<IForgeEntity>("target", primary, secondary);
			return;
		}

		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", primary);
	}
}
