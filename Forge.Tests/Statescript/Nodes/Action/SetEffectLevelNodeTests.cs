// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes.Action;

public class SetEffectLevelNodeTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string TargetAttribute = "TestAttributeSet.Attribute90";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Graph", "EffectLevel")]
	public void Set_effect_level_node_levels_up_effects_by_default()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		var effect = new Effect(CreateEffectData(), new EffectOwnership(target, target));

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("effect", effect);

		var levelNode = new SetEffectLevelNode();
		levelNode.BindInput(SetEffectLevelNode.EffectInput, "effect");

		graph.AddNode(levelNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			levelNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		effect.Level.Should().Be(2);
	}

	[Fact]
	[Trait("Graph", "EffectLevel")]
	public void Set_effect_level_node_sets_the_resolved_level()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		var effect = new Effect(CreateEffectData(), new EffectOwnership(target, target));

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("effect", effect);
		graph.VariableDefinitions.DefineVariable("level", 5);

		var levelNode = new SetEffectLevelNode(SetEffectLevelOperation.SetLevel);
		levelNode.BindInput(SetEffectLevelNode.EffectInput, "effect");
		levelNode.BindInput(SetEffectLevelNode.LevelInput, "level");

		graph.AddNode(levelNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			levelNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		effect.Level.Should().Be(5);
	}

	private static EffectData CreateEffectData()
	{
		return new EffectData(
			"Buff",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					TargetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(5)))
			]);
	}
}
