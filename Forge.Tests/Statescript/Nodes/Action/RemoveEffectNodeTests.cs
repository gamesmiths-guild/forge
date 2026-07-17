// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
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

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes.Action;

public class RemoveEffectNodeTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string TargetAttribute = "TestAttributeSet.Attribute90";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Graph", "RemoveEffect")]
	public void Remove_effect_node_removes_applied_effects_through_their_handles()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		var effect = new Effect(CreateInfiniteEffectData(), new EffectOwnership(target, target));

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("effect", effect);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);
		graph.VariableDefinitions.DefineObjectVariable<ActiveEffectHandle>("handle");

		ApplyEffectNode applyNode = CreateApplyEffectNode("effect", "target");
		applyNode.BindOutput(ApplyEffectNode.ActiveEffectOutput, "handle");

		var removeNode = new RemoveEffectNode();
		removeNode.BindInput(RemoveEffectNode.HandleInput, "handle");

		graph.AddNode(applyNode);
		graph.AddNode(removeNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			applyNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			applyNode.OutputPorts[ActionNode.OutputPort],
			removeNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		target.Attributes[TargetAttribute].CurrentValue.Should().Be(90);
		target.EffectsManager.GetActiveEffects().Should().BeEmpty();
	}

	[Fact]
	[Trait("Graph", "RemoveEffect")]
	public void Remove_effect_node_skips_invalid_handles()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		var effect = new Effect(CreateInfiniteEffectData(), new EffectOwnership(target, target));

		ActiveEffectHandle? handle = target.EffectsManager.ApplyEffect(effect);
		handle.Should().NotBeNull();
		target.EffectsManager.RemoveEffect(handle!, forceRemoval: true);
		handle!.IsValid.Should().BeFalse();

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("handle", handle);

		var removeNode = new RemoveEffectNode();
		removeNode.BindInput(RemoveEffectNode.HandleInput, "handle");

		graph.AddNode(removeNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			removeNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);

		FluentActions.Invoking(() => processor.StartGraph()).Should().NotThrow();
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
