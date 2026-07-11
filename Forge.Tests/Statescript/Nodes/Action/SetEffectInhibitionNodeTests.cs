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

public class SetEffectInhibitionNodeTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string TargetAttribute = "TestAttributeSet.Attribute90";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Theory]
	[Trait("Graph", "SetEffectInhibition")]
	[InlineData(true, 90)]
	[InlineData(false, 95)]
	public void Set_effect_inhibition_node_toggles_the_effect_modifiers(bool inhibited, int expectedAttributeValue)
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		var effect = new Effect(CreateInfiniteEffectData(), new EffectOwnership(target, target));

		ActiveEffectHandle? handle = target.EffectsManager.ApplyEffect(effect);
		handle.Should().NotBeNull();
		target.Attributes[TargetAttribute].CurrentValue.Should().Be(95);

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("handle", handle!);
		graph.VariableDefinitions.DefineVariable("inhibited", inhibited);

		var inhibitionNode = new SetEffectInhibitionNode();
		inhibitionNode.BindInput(SetEffectInhibitionNode.HandleInput, "handle");
		inhibitionNode.BindInput(SetEffectInhibitionNode.InhibitedInput, "inhibited");

		graph.AddNode(inhibitionNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			inhibitionNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		handle!.IsInhibited.Should().Be(inhibited);
		target.Attributes[TargetAttribute].CurrentValue.Should().Be(expectedAttributeValue);
	}

	private static EffectData CreateInfiniteEffectData()
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
