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
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;
using static Gamesmiths.Forge.Tests.Helpers.ResolverTestContextFactory;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes.State;

public class EffectNodeTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Theory]
	[Trait("Graph", "EffectNode")]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public void Effect_node_supports_all_scalar_and_array_combinations_and_removes_active_effects(
		bool useEffectArray,
		bool useTargetArray)
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
		ConfigureTargetInput(graph, useTargetArray, primaryTarget, secondaryTarget);

		EffectNode node = CreateEffectNode("effect", "target");
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
			shouldHaveFirstEffect: useTargetArray,
			shouldHaveSecondEffect: useEffectArray && useTargetArray);

		processor.StopGraph();

		AssertAppliedEffects(
			primaryTarget,
			firstEffect,
			secondEffect,
			shouldHaveFirstEffect: false,
			shouldHaveSecondEffect: false);

		AssertAppliedEffects(
			secondaryTarget,
			firstEffect,
			secondEffect,
			shouldHaveFirstEffect: false,
			shouldHaveSecondEffect: false);
	}

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void Effect_node_removes_active_effects_when_graph_stops()
	{
		TestEntity target = CreateTestEntity();

		EffectData effectData = CreateFlatEffectData(
			"Sustain",
			"TestAttributeSet.Attribute1",
			10,
			DurationType.Infinite);

		GraphProcessor processor = CreateProcessor(target, effectData);

		processor.StartGraph();

		target.EffectsManager.GetEffectInfo(effectData).Should().ContainSingle();
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);

		processor.StopGraph();

		target.EffectsManager.GetEffectInfo(effectData).Should().BeEmpty();
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
	}

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void Effect_node_does_not_try_to_remove_instant_effects()
	{
		TestEntity target = CreateTestEntity();

		EffectData effectData = CreateFlatEffectData(
			"Instant",
			"TestAttributeSet.Attribute1",
			10,
			DurationType.Instant);

		GraphProcessor processor = CreateProcessor(target, effectData);

		processor.StartGraph();
		target.EffectsManager.GetEffectInfo(effectData).Should().BeEmpty();
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 11, 0, 0]);

		processor.StopGraph();

		target.EffectsManager.GetEffectInfo(effectData).Should().BeEmpty();
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 11, 0, 0]);
	}

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void Effect_node_ignores_effects_that_expired_before_deactivation()
	{
		TestEntity target = CreateTestEntity();
		EffectData effectData = CreateFlatEffectData(
			"Timed",
			"TestAttributeSet.Attribute1",
			10,
			DurationType.HasDuration,
			duration: 1.0f);
		GraphProcessor processor = CreateProcessor(target, effectData);

		processor.StartGraph();

		target.EffectsManager.GetEffectInfo(effectData).Should().ContainSingle();
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);

		target.EffectsManager.UpdateEffects(1.0);
		target.EffectsManager.GetEffectInfo(effectData).Should().BeEmpty();
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);

		processor.StopGraph();

		target.EffectsManager.GetEffectInfo(effectData).Should().BeEmpty();
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
	}

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void Effect_node_uses_bound_level_and_ownership_when_present()
	{
		TestEntity ownershipOwner = CreateTestEntity();
		TestEntity ownershipSource = CreateTestEntity();
		TestEntity target = CreateTestEntity();
		var capture = new AppliedEffectCaptureComponent();
		EffectData effectData = CreateTrackingEffectData("Tracked", capture);
		var graph = new Graph();

		graph.VariableDefinitions.DefineObjectProperty("effect", new EffectDataResolver(effectData));
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", target);
		graph.VariableDefinitions.DefineVariable("level", 7);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("ownershipOwner", ownershipOwner);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("ownershipSource", ownershipSource);
		graph.VariableDefinitions.DefineObjectProperty(
			"ownership",
			new OwnershipResolver(
				new EntityVariableResolver("ownershipOwner"),
				new EntityVariableResolver("ownershipSource")));

		EffectNode node = CreateEffectNode("effect", "entity", "level", "ownership");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		capture.LastLevel.Should().Be(7);
		capture.LastOwner.Should().BeSameAs(ownershipOwner);
		capture.LastSource.Should().BeSameAs(ownershipSource);

		processor.StopGraph();
	}

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void Effect_node_defaults_level_and_ownership_from_ability_context()
	{
		TestEntity owner = CreateTestEntity();
		TestEntity source = CreateTestEntity();
		TestEntity target = CreateTestEntity();
		var capture = new AppliedEffectCaptureComponent();
		var graph = new Graph();

		graph.VariableDefinitions.DefineObjectProperty(
			"effect",
			new EffectDataResolver(CreateTrackingEffectData("Tracked", capture)));
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", target);

		EffectNode node = CreateEffectNode("effect", "entity");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		ExecuteAbilityGraph(graph, owner, target, source, level: 5);

		capture.LastLevel.Should().Be(5);
		capture.LastOwner.Should().BeSameAs(owner);
		capture.LastSource.Should().BeSameAs(source);
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
				new EffectDataArrayResolver(firstEffect, secondEffect));
			return;
		}

		graph.VariableDefinitions.DefineObjectProperty("effect", new EffectDataResolver(firstEffect));
	}

	private static void ConfigureTargetInput(
		Graph graph,
		bool useTargetArray,
		TestEntity primaryTarget,
		TestEntity secondaryTarget)
	{
		if (useTargetArray)
		{
			graph.VariableDefinitions.DefineObjectArrayVariable<IForgeEntity>("target", primaryTarget, secondaryTarget);
			return;
		}

		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", primaryTarget);
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

	private static GraphProcessor CreateProcessor(TestEntity target, EffectData effectData)
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectProperty("effect", new EffectDataResolver(effectData));
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", target);

		EffectNode node = CreateEffectNode("effect", "entity");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		return new GraphProcessor(graph);
	}

	private static EffectData CreateFlatEffectData(
		string name,
		string targetAttribute,
		float magnitude,
		DurationType durationType,
		float duration = 0f)
	{
		DurationData durationData = durationType == DurationType.HasDuration
			? new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(
					MagnitudeCalculationType.ScalableFloat,
					new ScalableFloat(duration)))
			: new DurationData(durationType);

		return new EffectData(
			name,
			durationData,
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(magnitude)))
			]);
	}

	private static EffectData CreateTrackingEffectData(string name, IEffectComponent trackingComponent)
	{
		return new EffectData(
			name,
			new DurationData(DurationType.Infinite),
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
