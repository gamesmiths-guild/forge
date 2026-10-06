// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Statescript.Providers;
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
		EffectNode node = GetEffectNode(processor);
		int effectEndMessages = 0;
		node.OutputPorts[EffectNode.OnEffectEndPort].OnEmitMessage += _ => effectEndMessages++;

		processor.StartGraph();

		processor.GraphContext.IsActive.Should().BeTrue();
		target.EffectsManager.GetEffectStackData(effectData).Should().ContainSingle();
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);

		processor.StopGraph();

		effectEndMessages.Should().Be(0);
		processor.GraphContext.IsActive.Should().BeFalse();
		target.EffectsManager.GetEffectStackData(effectData).Should().BeEmpty();
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

		processor.GraphContext.IsActive.Should().BeFalse();
		target.EffectsManager.GetEffectStackData(effectData).Should().BeEmpty();
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 11, 0, 0]);

		processor.StopGraph();

		target.EffectsManager.GetEffectStackData(effectData).Should().BeEmpty();
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 11, 0, 0]);
	}

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void Effect_node_deactivates_immediately_when_all_applied_effects_are_instant()
	{
		TestEntity target = CreateTestEntity();

		EffectData firstEffect = CreateFlatEffectData(
			"Instant A",
			"TestAttributeSet.Attribute1",
			10,
			DurationType.Instant);

		EffectData secondEffect = CreateFlatEffectData(
			"Instant B",
			"TestAttributeSet.Attribute2",
			5,
			DurationType.Instant);

		GraphProcessor processor = CreateProcessor(target, firstEffect, secondEffect);
		bool completed = false;
		EffectNode node = GetEffectNode(processor);
		int effectEndMessages = 0;
		node.OutputPorts[EffectNode.OnEffectEndPort].OnEmitMessage += _ => effectEndMessages++;
		processor.OnGraphCompleted = () => completed = true;

		processor.StartGraph();

		effectEndMessages.Should().Be(1);
		completed.Should().BeTrue();
		processor.GraphContext.IsActive.Should().BeFalse();
		target.EffectsManager.GetEffectStackData(firstEffect).Should().BeEmpty();
		target.EffectsManager.GetEffectStackData(secondEffect).Should().BeEmpty();
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 11, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [7, 7, 0, 0]);
	}

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void Effect_node_OnEffectEnd_can_still_resolve_property_backed_inputs_before_graph_completion()
	{
		TestEntity target = CreateTestEntity();

		EffectData effectData = CreateFlatEffectData(
			"Instant",
			"TestAttributeSet.Attribute1",
			10,
			DurationType.Instant);

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectProperty(
			"effect",
			new EffectFromDataResolver(effectData));
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);
		graph.VariableDefinitions.DefineProperty(
			"constant",
			new VariantResolver(new Variant128(2), typeof(int)));

		EffectNode node = CreateEffectNode("effect", "target");
		var readNode = new ReadPropertyNode<int>();
		readNode.BindInput(ReadPropertyNode<int>.ValueInput, "constant");

		graph.AddNode(node);
		graph.AddNode(readNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[EffectNode.OnEffectEndPort],
			readNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		readNode.Found.Should().BeTrue();
		readNode.LastReadValue.Should().Be(2);
		processor.GraphContext.IsActive.Should().BeFalse();
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

		target.EffectsManager.GetEffectStackData(effectData).Should().ContainSingle();
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);

		target.EffectsManager.UpdateEffects(1.0);
		target.EffectsManager.GetEffectStackData(effectData).Should().BeEmpty();
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);

		processor.StopGraph();

		target.EffectsManager.GetEffectStackData(effectData).Should().BeEmpty();
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
	}

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void Effect_node_deactivates_when_the_last_active_effect_expires()
	{
		TestEntity target = CreateTestEntity();
		EffectData effectData = CreateFlatEffectData(
			"Timed",
			"TestAttributeSet.Attribute1",
			10,
			DurationType.HasDuration,
			duration: 1.0f);
		GraphProcessor processor = CreateProcessor(target, effectData);
		bool completed = false;
		EffectNode node = GetEffectNode(processor);
		int effectEndMessages = 0;
		node.OutputPorts[EffectNode.OnEffectEndPort].OnEmitMessage += _ => effectEndMessages++;
		processor.OnGraphCompleted = () => completed = true;

		processor.StartGraph();

		processor.GraphContext.IsActive.Should().BeTrue();
		target.EffectsManager.GetEffectStackData(effectData).Should().ContainSingle();

		target.EffectsManager.UpdateEffects(1.0);
		processor.UpdateGraph(0.0);

		effectEndMessages.Should().Be(1);
		completed.Should().BeTrue();
		processor.GraphContext.IsActive.Should().BeFalse();
		target.EffectsManager.GetEffectStackData(effectData).Should().BeEmpty();
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
	}

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void Effect_node_stays_active_while_at_least_one_applied_effect_remains_active()
	{
		TestEntity target = CreateTestEntity();
		EffectData timedEffect = CreateFlatEffectData(
			"Timed",
			"TestAttributeSet.Attribute1",
			10,
			DurationType.HasDuration,
			duration: 1.0f);
		EffectData infiniteEffect = CreateFlatEffectData(
			"Infinite",
			"TestAttributeSet.Attribute2",
			5,
			DurationType.Infinite);
		GraphProcessor processor = CreateProcessor(target, timedEffect, infiniteEffect);
		bool completed = false;
		EffectNode node = GetEffectNode(processor);
		int effectEndMessages = 0;
		node.OutputPorts[EffectNode.OnEffectEndPort].OnEmitMessage += _ => effectEndMessages++;
		processor.OnGraphCompleted = () => completed = true;

		processor.StartGraph();

		processor.GraphContext.IsActive.Should().BeTrue();
		target.EffectsManager.GetEffectStackData(timedEffect).Should().ContainSingle();
		target.EffectsManager.GetEffectStackData(infiniteEffect).Should().ContainSingle();

		target.EffectsManager.UpdateEffects(1.0);
		processor.UpdateGraph(0.0);

		effectEndMessages.Should().Be(0);
		completed.Should().BeFalse();
		processor.GraphContext.IsActive.Should().BeTrue();
		target.EffectsManager.GetEffectStackData(timedEffect).Should().BeEmpty();
		target.EffectsManager.GetEffectStackData(infiniteEffect).Should().ContainSingle();
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [7, 2, 5, 0]);

		target.EffectsManager.RemoveEffectData(infiniteEffect);
		processor.UpdateGraph(0.0);

		effectEndMessages.Should().Be(1);
		completed.Should().BeTrue();
		processor.GraphContext.IsActive.Should().BeFalse();
		target.EffectsManager.GetEffectStackData(infiniteEffect).Should().BeEmpty();
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [2, 2, 0, 0]);
	}

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void Effect_node_does_not_emit_OnComplete_when_deactivated_by_parent_subgraph()
	{
		TestEntity target = CreateTestEntity();
		EffectData effectData = CreateFlatEffectData(
			"Infinite",
			"TestAttributeSet.Attribute1",
			10,
			DurationType.Infinite);
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 0.5d);
		graph.VariableDefinitions.DefineObjectProperty(
			"effect",
			new EffectFromDataResolver(effectData));
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", target);

		TimerNode timer = CreateTimerNode("duration");
		EffectNode effectNode = CreateEffectNode("effect", "entity");
		graph.AddNode(timer);
		graph.AddNode(effectNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			timer.OutputPorts[StateNode<TimerNodeContext>.SubgraphPort],
			effectNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		bool completed = false;
		int effectEndMessages = 0;
		effectNode.OutputPorts[EffectNode.OnEffectEndPort].OnEmitMessage += _ => effectEndMessages++;
		processor.OnGraphCompleted = () => completed = true;

		processor.StartGraph();

		processor.GraphContext.IsActive.Should().BeTrue();
		target.EffectsManager.GetEffectStackData(effectData).Should().ContainSingle();

		processor.UpdateGraph(0.5);

		effectEndMessages.Should().Be(0);
		completed.Should().BeTrue();
		processor.GraphContext.IsActive.Should().BeFalse();
		target.EffectsManager.GetEffectStackData(effectData).Should().BeEmpty();
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
	}

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void Effect_node_uses_level_and_ownership_configured_on_the_effect_resolver()
	{
		TestEntity ownershipOwner = CreateTestEntity();
		TestEntity ownershipSource = CreateTestEntity();
		TestEntity target = CreateTestEntity();
		var capture = new AppliedEffectCaptureComponent();
		EffectData effectData = CreateTrackingEffectData("Tracked", capture);
		var graph = new Graph();

		graph.VariableDefinitions.DefineVariable("level", 7);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("ownershipOwner", ownershipOwner);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("ownershipSource", ownershipSource);
		graph.VariableDefinitions.DefineObjectProperty(
			"effect",
			new EffectFromDataResolver(
				effectData,
				new VariableResolver("level", typeof(int)),
				new OwnershipResolver(
					new EntityVariableResolver("ownershipOwner"),
					new EntityVariableResolver("ownershipSource"))));
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", target);

		EffectNode node = CreateEffectNode("effect", "entity");
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
			new EffectFromDataResolver(CreateTrackingEffectData("Tracked", capture)));
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

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void Effect_node_writes_active_effect_output_on_activation()
	{
		TestEntity target = CreateTestEntity();
		EffectData effectData = CreateFlatEffectData(
			"Sustain",
			"TestAttributeSet.Attribute1",
			10,
			DurationType.Infinite);
		var graph = new Graph();

		graph.VariableDefinitions.DefineObjectProperty(
			"effect",
			new EffectFromDataResolver(effectData));
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", target);
		graph.VariableDefinitions.DefineObjectVariable<ActiveEffectHandle>("activeEffect");

		EffectNode node = CreateEffectNode("effect", "entity");
		node.BindOutput(EffectNode.ActiveEffectOutput, "activeEffect");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		processor.GraphContext.GraphVariables.TryGetObject("activeEffect", out ActiveEffectHandle? handle)
			.Should().BeTrue();
		handle.Should().NotBeNull();
		handle!.IsValid.Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void Effect_node_passes_provider_built_context_data_through_the_pipeline()
	{
		TestEntity target = CreateTestEntity();
		var capture = new ContextCaptureComponent();
		var graph = new Graph();

		graph.VariableDefinitions.DefineVariable("damage", 42);
		graph.VariableDefinitions.DefineObjectProperty(
			"effect",
			new EffectFromDataResolver(CreateTrackingEffectData("Tracked", capture)));
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", target);
		graph.VariableDefinitions.DefineObjectProperty(
			"context",
			new EffectContextDataResolver(new DamageContextProvider()));

		EffectNode node = CreateEffectNode("effect", "entity");
		node.BindInput(EffectNode.ContextDataInput, "context");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		capture.WasApplied.Should().BeTrue();
		capture.ReceivedContext.Should().BeTrue();
		capture.ReceivedDamage.Should().Be(42);
	}

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void Effect_node_applies_without_context_data_when_input_is_unbound()
	{
		TestEntity target = CreateTestEntity();
		var capture = new ContextCaptureComponent();
		var graph = new Graph();

		graph.VariableDefinitions.DefineObjectProperty(
			"effect",
			new EffectFromDataResolver(CreateTrackingEffectData("Tracked", capture)));
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", target);

		EffectNode node = CreateEffectNode("effect", "entity");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		capture.WasApplied.Should().BeTrue();
		capture.ReceivedContext.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void A_retriggered_effect_node_applies_its_effect_once_and_removes_it_all_by_default()
	{
		TestEntity target = CreateTestEntity();
		EffectData effect = CreateFlatEffectData(
			"Effect",
			"TestAttributeSet.Attribute1",
			10,
			DurationType.Infinite);

		Graph graph = CreateGraph(target, effect);
		EffectNode node = CreateEffectNode("effect", "target");
		AddFromEntry(graph, node);
		ConnectRetrigger(graph, node, 1.0);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		processor.UpdateGraph(1.0);

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);

		processor.StopGraph();

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
	}

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void A_restarting_effect_node_replaces_its_effect_with_a_fresh_application()
	{
		TestEntity target = CreateTestEntity();
		var counter = new ActiveEffectCounterComponent();
		EffectData effect = CreateTrackingEffectData("Tracked Effect", counter);

		Graph graph = CreateGraph(target, effect);
		var node = new EffectNode(restartOnRetrigger: true);
		node.BindInput(EffectNode.EffectInput, "effect");
		node.BindInput(EffectNode.TargetInput, "target");
		AddFromEntry(graph, node);
		ConnectRetrigger(graph, node, 1.0);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		processor.UpdateGraph(1.0);

		counter.AddedCount.Should().Be(2);
		counter.RemovedCount.Should().Be(1);
		processor.GraphContext.IsActive.Should().BeTrue();

		processor.StopGraph();

		counter.RemovedCount.Should().Be(2);
	}

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void A_restart_whose_removal_stops_the_graph_applies_nothing_more()
	{
		TestEntity target = CreateTestEntity();
		var counter = new ActiveEffectCounterComponent();
		var effect = new EffectData(
			"Tracked Effect",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10))),
			],
			effectComponents: [counter]);

		Graph graph = CreateGraph(target, effect);
		var node = new EffectNode(restartOnRetrigger: true);
		node.BindInput(EffectNode.EffectInput, "effect");
		node.BindInput(EffectNode.TargetInput, "target");
		AddFromEntry(graph, node);
		ConnectRetrigger(graph, node, 1.0);

		var processor = new GraphProcessor(graph);

		// Losing an effect can end the ability the graph runs for, which stops the graph from under the restart.
		counter.Removed = processor.StopGraph;
		processor.StartGraph();

		processor.Invoking(x => x.UpdateGraph(1.0)).Should().NotThrow();

		counter.AddedCount.Should().Be(1, "the restart ended with the graph instead of applying again");
		counter.RemovedCount.Should().Be(1, "the graph stopping does not remove the effect a second time");
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
	}

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void A_restart_whose_removal_starts_the_graph_over_leaves_the_new_run_its_own_effect()
	{
		TestEntity target = CreateTestEntity();
		var counter = new ActiveEffectCounterComponent();
		var effect = new EffectData(
			"Tracked Effect",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10))),
			],
			effectComponents: [counter]);

		Graph graph = CreateGraph(target, effect);
		var node = new EffectNode(restartOnRetrigger: true);
		node.BindInput(EffectNode.EffectInput, "effect");
		node.BindInput(EffectNode.TargetInput, "target");
		AddFromEntry(graph, node);
		ConnectRetrigger(graph, node, 1.0);

		var processor = new GraphProcessor(graph);

		// Losing the effect stops the graph, and the graph starts over before the restart it cut short returns.
		counter.Removed = processor.StopGraph;
		RestartOnFirstCompletion(processor);
		processor.StartGraph();
		processor.UpdateGraph(1.0);

		counter.AddedCount.Should().Be(2, "the new run applied the effect, and the restart it replaced did not");
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);

		processor.StopGraph();

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
	}

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void An_instant_effect_that_stops_the_graph_as_it_applies_ends_the_node_with_it()
	{
		TestEntity target = CreateTestEntity();
		var stopper = new StopsGraphComponent();
		GraphProcessor processor = CreateProcessor(target, CreateStoppingEffect(DurationType.Instant, stopper));

		// Applying an effect can end the ability the graph runs for, which stops the graph before the node holds it.
		stopper.Applied = processor.StopGraph;

		processor.Invoking(x => x.StartGraph()).Should().NotThrow();
		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void An_effect_that_stops_the_graph_as_it_applies_is_removed_with_it()
	{
		TestEntity target = CreateTestEntity();
		var stopper = new StopsGraphComponent();
		GraphProcessor processor = CreateProcessor(target, CreateStoppingEffect(DurationType.Infinite, stopper));
		stopper.Applied = processor.StopGraph;

		processor.StartGraph();

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
	}

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void The_effects_after_one_that_stops_the_graph_as_it_applies_go_unapplied()
	{
		TestEntity target = CreateTestEntity();
		var stopper = new StopsGraphComponent();
		EffectData later = CreateFlatEffectData("Later", "TestAttributeSet.Attribute2", 10, DurationType.Instant);
		GraphProcessor processor =
			CreateProcessor(target, CreateStoppingEffect(DurationType.Instant, stopper), later);
		stopper.Applied = processor.StopGraph;

		processor.StartGraph();

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [2, 2, 0, 0]);
	}

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void The_effects_after_one_that_ends_the_node_as_it_applies_go_unapplied()
	{
		TestEntity target = CreateTestEntity();
		var stopper = new StopsGraphComponent();
		EffectData later = CreateFlatEffectData("Later", "TestAttributeSet.Attribute2", 10, DurationType.Instant);
		GraphProcessor processor =
			CreateProcessor(target, CreateStoppingEffect(DurationType.Infinite, stopper), later);
		EffectNode node = GetEffectNode(processor);

		// Applying the first effect aborts the node, as a listener wired to its abort would.
		stopper.Applied = () =>
			node.InputPorts[StateNode<EffectNodeContext>.AbortPort].ReceiveMessage(processor.GraphContext);

		processor.StartGraph();

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [2, 2, 0, 0]);
	}

	[Fact]
	[Trait("Graph", "EffectNode")]
	public void A_graph_started_over_as_a_stacking_effect_applies_keeps_the_effect_of_its_new_run()
	{
		TestEntity target = CreateTestEntity();
		var stopper = new StopsGraphComponent();
		var effect = new EffectData(
			"Stacking Effect",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10))),
			],
			new StackingData(
				new ScalableInt(2),
				new ScalableInt(1),
				StackPolicy.AggregateByTarget,
				StackLevelPolicy.SegregateLevels,
				StackMagnitudePolicy.Sum,
				StackOverflowPolicy.AllowApplication,
				StackExpirationPolicy.ClearEntireStack,
				StackOwnerDenialPolicy.AlwaysAllow,
				StackOwnerOverridePolicy.Override,
				StackOwnerOverrideStackCountPolicy.IncreaseStacks),
			effectComponents: [stopper]);
		GraphProcessor processor = CreateProcessor(target, effect);

		// The first application ends the ability the graph runs for, once, and the graph is started over from there.
		// Applied while the stopped run still held its own, the new run's application would stack onto it and be
		// removed with it.
		bool stopped = false;
		stopper.Applied = () =>
		{
			if (!stopped)
			{
				stopped = true;
				processor.StopGraph();
			}
		};

		RestartOnFirstCompletion(processor);
		processor.StartGraph();
		processor.UpdateGraph(1.0);

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);
		processor.GraphContext.IsActive.Should().BeTrue("the new run still holds its effect");
	}

	// Adds 10 to Attribute1, and calls back as it applies.
	private static EffectData CreateStoppingEffect(DurationType durationType, StopsGraphComponent stopper)
	{
		return new EffectData(
			"Stopping Effect",
			new DurationData(durationType),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10))),
			],
			effectComponents: [stopper]);
	}

	private static Graph CreateGraph(TestEntity target, EffectData effect)
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectProperty("effect", new EffectFromDataResolver(effect));
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);

		return graph;
	}

	private static void AddFromEntry(Graph graph, EffectNode node)
	{
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));
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
		target.EffectsManager.GetEffectStackData(firstEffect).Should().HaveCount(shouldHaveFirstEffect ? 1 : 0);
		target.EffectsManager.GetEffectStackData(secondEffect).Should().HaveCount(shouldHaveSecondEffect ? 1 : 0);

		TestUtils.TestAttribute(
			target,
			"TestAttributeSet.Attribute1",
			shouldHaveFirstEffect ? [11, 1, 10, 0] : [1, 1, 0, 0]);
		TestUtils.TestAttribute(
			target,
			"TestAttributeSet.Attribute2",
			shouldHaveSecondEffect ? [7, 2, 5, 0] : [2, 2, 0, 0]);
	}

	private static GraphProcessor CreateProcessor(TestEntity target, params EffectData[] effectData)
	{
		var graph = new Graph();

		if (effectData.Length == 1)
		{
			graph.VariableDefinitions.DefineObjectProperty(
				"effect",
				new EffectFromDataResolver(effectData[0]));
		}
		else
		{
			graph.VariableDefinitions.DefineObjectArrayProperty(
				"effect",
				new EffectArrayFromDataResolver(effectData));
		}

		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", target);

		EffectNode node = CreateEffectNode("effect", "entity");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		return new GraphProcessor(graph);
	}

	private static EffectNode GetEffectNode(GraphProcessor processor)
	{
		return (EffectNode)processor.Graph.Nodes.Single(node => node is EffectNode);
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

	private sealed record DamageContext(int Damage);

	private sealed class DamageContextProvider : EffectContextDataProvider<DamageContext>
	{
		public override DamageContext CreateData(GraphContext graphContext, EffectContextDataInputs inputs)
		{
			graphContext.TryResolve("damage", out int damage);
			return new DamageContext(damage);
		}
	}

	private sealed class ActiveEffectCounterComponent : IEffectComponent
	{
		public int AddedCount { get; private set; }

		public int RemovedCount { get; private set; }

		public System.Action? Removed { get; set; }

		public bool OnActiveEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
		{
			AddedCount++;
			return true;
		}

		public void OnActiveEffectUnapplied(
			IForgeEntity target,
			in ActiveEffectEvaluatedData activeEffectEvaluatedData,
			bool removed,
			EffectRemovalReason reason)
		{
			if (removed)
			{
				RemovedCount++;
				Removed?.Invoke();
			}
		}
	}

	private sealed class StopsGraphComponent : IEffectComponent
	{
		public System.Action? Applied { get; set; }

		public void OnEffectApplied(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
		{
			Applied?.Invoke();
		}
	}

	private sealed class ContextCaptureComponent : IEffectComponent
	{
		public bool WasApplied { get; private set; }

		public bool ReceivedContext { get; private set; }

		public int ReceivedDamage { get; private set; }

		public void OnEffectApplied(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
		{
			WasApplied = true;

			if (effectEvaluatedData.TryGetContextData(out DamageContext? context))
			{
				ReceivedContext = true;
				ReceivedDamage = context.Damage;
			}
		}
	}
}
