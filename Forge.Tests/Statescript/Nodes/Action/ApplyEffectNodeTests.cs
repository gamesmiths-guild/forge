// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;
using static Gamesmiths.Forge.Tests.Helpers.ResolverTestContextFactory;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes.Action;

public class ApplyEffectNodeTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Theory]
	[Trait("Graph", "ApplyEffect")]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public void Apply_effect_node_supports_all_scalar_and_array_combinations(bool useEffectArray, bool useEntityArray)
	{
		TestEntity primaryTarget = CreateTestEntity();
		TestEntity secondaryTarget = CreateTestEntity();

		EffectData firstEffect = CreateFlatEffectData(
			"First Effect",
			"TestAttributeSet.Attribute1",
			10,
			DurationType.Infinite);

		EffectData secondEffect = CreateFlatEffectData(
			"Second Effect",
			"TestAttributeSet.Attribute2",
			5,
			DurationType.Infinite);

		var graph = new Graph();

		ConfigureEffectInput(graph, useEffectArray, firstEffect, secondEffect);
		ConfigureEntityInput(graph, useEntityArray, primaryTarget, secondaryTarget);

		ApplyEffectNode node = CreateApplyEffectNode("effect", "entity");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		AssertAppliedEffects(
			primaryTarget,
			firstEffect,
			secondEffect,
			shouldHaveFirstEffect: true,
			shouldHaveSecondEffect: useEffectArray);

		AssertAppliedEffects(
			secondaryTarget,
			firstEffect,
			secondEffect,
			shouldHaveFirstEffect: useEntityArray,
			shouldHaveSecondEffect: useEffectArray && useEntityArray);
	}

	[Fact]
	[Trait("Graph", "ApplyEffect")]
	public void Apply_effect_node_defaults_level_and_ownership_from_ability_context()
	{
		TestEntity owner = CreateTestEntity();
		TestEntity source = CreateTestEntity();
		TestEntity target = CreateTestEntity();
		var capture = new AppliedEffectCaptureComponent();
		var graph = new Graph();

		graph.VariableDefinitions.DefineObjectProperty(
			"effect",
			new EffectFromDataResolver(CreateTrackingEffectData("Tracked", DurationType.Instant, capture)));
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", target);

		ApplyEffectNode node = CreateApplyEffectNode("effect", "entity");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		ExecuteAbilityGraph(graph, owner, target, source, level: 5);

		capture.LastLevel.Should().Be(5);
		capture.LastOwner.Should().BeSameAs(owner);
		capture.LastSource.Should().BeSameAs(source);
	}

	[Fact]
	[Trait("Graph", "ApplyEffect")]
	public void Apply_effect_node_uses_level_and_ownership_configured_on_the_effect_resolver()
	{
		TestEntity abilityOwner = CreateTestEntity();
		TestEntity abilitySource = CreateTestEntity();
		TestEntity explicitOwner = CreateTestEntity();
		TestEntity explicitSource = CreateTestEntity();
		TestEntity target = CreateTestEntity();
		var capture = new AppliedEffectCaptureComponent();
		var graph = new Graph();

		graph.VariableDefinitions.DefineVariable("level", 7);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("ownershipOwner", explicitOwner);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("ownershipSource", explicitSource);
		graph.VariableDefinitions.DefineObjectProperty(
			"effect",
			new EffectFromDataResolver(
				CreateTrackingEffectData("Tracked", DurationType.Instant, capture),
				new VariableResolver("level", typeof(int)),
				new OwnershipResolver(
					new EntityVariableResolver("ownershipOwner"),
					new EntityVariableResolver("ownershipSource"))));
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", target);

		ApplyEffectNode node = CreateApplyEffectNode("effect", "entity");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		ExecuteAbilityGraph(graph, abilityOwner, target, abilitySource, level: 3);

		capture.LastLevel.Should().Be(7);
		capture.LastOwner.Should().BeSameAs(explicitOwner);
		capture.LastSource.Should().BeSameAs(explicitSource);
	}

	private static void ConfigureEffectInput(
		Graph graph,
		bool useEffectArray,
		EffectData firstEffect,
		EffectData secondEffect)
	{
		if (useEffectArray)
		{
			graph.VariableDefinitions.DefineObjectArrayProperty(
				"effect",
				new EffectArrayFromDataResolver([firstEffect, secondEffect]));
			return;
		}

		graph.VariableDefinitions.DefineObjectProperty(
			"effect",
			new EffectFromDataResolver(firstEffect));
	}

	private static void ConfigureEntityInput(
		Graph graph,
		bool useEntityArray,
		TestEntity primaryTarget,
		TestEntity secondaryTarget)
	{
		if (useEntityArray)
		{
			graph.VariableDefinitions.DefineObjectArrayVariable<IForgeEntity>("entity", primaryTarget, secondaryTarget);
			return;
		}

		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", primaryTarget);
	}

	private static void AssertAppliedEffects(
		TestEntity target,
		EffectData firstEffect,
		EffectData secondEffect,
		bool shouldHaveFirstEffect,
		bool shouldHaveSecondEffect)
	{
		target.EffectsManager.GetEffectInfo(firstEffect).Should().HaveCount(shouldHaveFirstEffect ? 1 : 0);
		target.EffectsManager.GetEffectInfo(secondEffect).Should().HaveCount(shouldHaveSecondEffect ? 1 : 0);

		TestUtils.TestAttribute(
			target,
			"TestAttributeSet.Attribute1",
			shouldHaveFirstEffect ? [11, 1, 10, 0] : [1, 1, 0, 0]);
		TestUtils.TestAttribute(
			target,
			"TestAttributeSet.Attribute2",
			shouldHaveSecondEffect ? [7, 2, 5, 0] : [2, 2, 0, 0]);
	}

	private static EffectData CreateFlatEffectData(
		string name,
		string targetAttribute,
		float magnitude,
		DurationType durationType)
	{
		return new EffectData(
			name,
			new DurationData(durationType),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(magnitude)))
			]);
	}

	private static EffectData CreateTrackingEffectData(
		string name,
		DurationType durationType,
		IEffectComponent trackingComponent)
	{
		return new EffectData(
			name,
			new DurationData(durationType),
			effectComponents: [trackingComponent]);
	}

	private TestEntity CreateTestEntity()
	{
		return new TestEntity(_tagsManager, _cuesManager);
	}

	private sealed class AppliedEffectCaptureComponent : IEffectComponent
	{
		public int? LastLevel { get; private set; }

		public IForgeEntity? LastOwner { get; private set; }

		public IForgeEntity? LastSource { get; private set; }

		public void OnEffectApplied(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
		{
			LastLevel = effectEvaluatedData.Level;
			LastOwner = effectEvaluatedData.Effect.Ownership.Owner;
			LastSource = effectEvaluatedData.Effect.Ownership.Source;
		}
	}
}
