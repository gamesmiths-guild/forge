// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;

namespace Gamesmiths.Forge.Tests.Statescript;

public class FixedUpdateTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = fixture.TagsManager;
	private readonly CuesManager _cuesManager = fixture.CuesManager;

	[Fact]
	[Trait("Graph", "FixedUpdate")]
	public void Frame_update_reaches_only_the_frame_hook()
	{
		var node = new TrackingStateNode();
		GraphProcessor processor = StartGraphWith(node);

		processor.UpdateGraph(0.25);

		node.UpdateCount.Should().Be(1);
		node.FixedUpdateCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "FixedUpdate")]
	public void Fixed_update_reaches_only_the_fixed_hook()
	{
		var node = new TrackingStateNode();
		GraphProcessor processor = StartGraphWith(node);

		processor.FixedUpdateGraph(0.25);

		node.FixedUpdateCount.Should().Be(1);
		node.UpdateCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "FixedUpdate")]
	public void Each_rail_carries_its_own_delta()
	{
		var node = new TrackingStateNode();
		GraphProcessor processor = StartGraphWith(node);

		processor.UpdateGraph(0.008);
		processor.FixedUpdateGraph(1.0 / 60.0);

		node.LastUpdateDelta.Should().Be(0.008);
		node.LastFixedUpdateDelta.Should().Be(1.0 / 60.0);
	}

	[Fact]
	[Trait("Graph", "FixedUpdate")]
	public void Both_rails_can_drive_the_same_graph_in_one_tick()
	{
		var node = new TrackingStateNode();
		GraphProcessor processor = StartGraphWith(node);

		processor.UpdateGraph(0.016);
		processor.FixedUpdateGraph(0.016);
		processor.UpdateGraph(0.016);

		node.UpdateCount.Should().Be(2);
		node.FixedUpdateCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "FixedUpdate")]
	public void A_timer_does_not_advance_on_fixed_updates()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 1.0);

		TimerNode timer = CreateTimerNode("duration");
		graph.AddNode(timer);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		processor.FixedUpdateGraph(5.0);

		processor.GraphContext.IsActive.Should().BeTrue(
			"a timer counts wall-clock time and is not driven by the fixed step");

		processor.UpdateGraph(1.0);

		processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "FixedUpdate")]
	public void Fixed_update_skips_a_node_that_has_been_aborted()
	{
		var node = new TrackingStateNode();
		GraphProcessor processor = StartGraphWith(node);

		node.InputPorts[StateNode<StateNodeContext>.AbortPort].ReceiveMessage(processor.GraphContext);
		node.DeactivateCount.Should().Be(1);

		processor.FixedUpdateGraph(0.016);

		node.FixedUpdateCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "FixedUpdate")]
	public void Fixed_update_does_nothing_before_the_graph_starts()
	{
		var node = new TrackingStateNode();

		var graph = new Graph();
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[StateNode<StateNodeContext>.InputPort]));

		var processor = new GraphProcessor(graph);

		processor.FixedUpdateGraph(0.016);

		node.ActivateCount.Should().Be(0);
		node.FixedUpdateCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "FixedUpdate")]
	public void Fixed_update_does_nothing_after_the_graph_stops()
	{
		var node = new TrackingStateNode();
		GraphProcessor processor = StartGraphWith(node);

		processor.StopGraph();
		processor.FixedUpdateGraph(0.016);

		node.FixedUpdateCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "FixedUpdate")]
	public void Fixed_update_survives_a_node_deactivating_another_mid_walk()
	{
		var target = new TrackingStateNode();
		var deactivator = new DeactivateOnFixedUpdateNode(target);

		var graph = new Graph();
		graph.AddNode(deactivator);
		graph.AddNode(target);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			deactivator.InputPorts[StateNode<StateNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			target.InputPorts[StateNode<StateNodeContext>.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		target.ActivateCount.Should().Be(1);

		// The walk runs off a snapshot, so removing a node from the active set part way through it neither throws nor
		// skips the rest of the set.
		processor.Invoking(x => x.FixedUpdateGraph(0.016)).Should().NotThrow();

		target.DeactivateCount.Should().Be(1);

		// Whether the target was reached before the abort is down to the order the active set happens to be in, which
		// nothing promises. What is promised is that no walk after the abort reaches it.
		int updatesBeforeAbort = target.FixedUpdateCount;

		processor.FixedUpdateGraph(0.016);
		processor.FixedUpdateGraph(0.016);

		target.FixedUpdateCount.Should().Be(updatesBeforeAbort);
	}

	[Fact]
	[Trait("GraphBehavior", "FixedUpdate")]
	public void Fixed_update_abilities_drives_the_graph_behind_an_ability()
	{
		var node = new TrackingStateNode();

		var graph = new Graph();
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[StateNode<StateNodeContext>.InputPort]));

		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new GraphAbilityBehavior(graph);

		AbilityHandle handle = GrantAndActivate(entity, behavior);
		handle.IsActive.Should().BeTrue();

		entity.Abilities.FixedUpdateAbilities(1.0 / 60.0);

		node.FixedUpdateCount.Should().Be(1);
		node.UpdateCount.Should().Be(0);

		entity.Abilities.UpdateAbilities(0.016);

		node.FixedUpdateCount.Should().Be(1);
		node.UpdateCount.Should().Be(1);
	}

	[Fact]
	[Trait("GraphBehavior", "FixedUpdate")]
	public void A_behavior_with_no_fixed_half_is_left_alone()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new FrameOnlyBehavior();

		AbilityHandle handle = GrantAndActivate(entity, behavior);
		handle.Should().NotBeNull();

		entity.Abilities.FixedUpdateAbilities(1.0 / 60.0);

		behavior.UpdateCount.Should().Be(0, "the default fixed hook does nothing");

		entity.Abilities.UpdateAbilities(0.016);

		behavior.UpdateCount.Should().Be(1);
	}

	[Fact]
	[Trait("GraphBehavior", "FixedUpdate")]
	public void A_behavior_may_end_its_own_instance_from_a_fixed_update()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new SelfEndingBehavior(endOnFixedUpdate: true);

		AbilityHandle handle = GrantAndActivate(entity, behavior);
		handle.IsActive.Should().BeTrue();

		// Ending an instance removes the behavior from the dictionary being walked. A graph completing on the fixed
		// step does exactly this, so it is the ordinary case rather than an exotic one.
		entity.Abilities.Invoking(x => x.FixedUpdateAbilities(1.0 / 60.0)).Should().NotThrow();

		handle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("GraphBehavior", "FixedUpdate")]
	public void A_behavior_may_clear_its_own_ability_from_a_fixed_update()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new SelfClearingBehavior(entity, clearOnFixedUpdate: true);

		AbilityHandle handle = GrantAndActivate(entity, behavior);
		handle.IsActive.Should().BeTrue();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		// Clearing removes the handle from the granted set being walked.
		entity.Abilities.Invoking(x => x.FixedUpdateAbilities(1.0 / 60.0)).Should().NotThrow();

		entity.Abilities.GrantedAbilities.Should().BeEmpty("the clear has to actually remove, or this proves nothing");
	}

	[Fact]
	[Trait("GraphBehavior", "FixedUpdate")]
	public void A_behavior_may_grant_another_ability_from_a_fixed_update()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var granted = new AbilityData("GrantedDuringFixedUpdate");
		var behavior = new GrantingBehavior(entity, granted, grantOnFixedUpdate: true);

		AbilityHandle handle = GrantAndActivate(entity, behavior);
		handle.IsActive.Should().BeTrue();

		// Adding to the granted set is what still invalidates an enumerator - a graph node granting an ability is the
		// ordinary way to reach it.
		entity.Abilities.Invoking(x => x.FixedUpdateAbilities(1.0 / 60.0)).Should().NotThrow();

		entity.Abilities.GrantedAbilities.Should().HaveCount(2);
	}

	[Fact]
	[Trait("GraphBehavior", "Update")]
	public void A_behavior_may_grant_another_ability_from_a_frame_update()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var granted = new AbilityData("GrantedDuringFrameUpdate");
		var behavior = new GrantingBehavior(entity, granted, grantOnFixedUpdate: false);

		AbilityHandle handle = GrantAndActivate(entity, behavior);
		handle.IsActive.Should().BeTrue();

		// The frame rail has always had the same hazard; it was simply never covered.
		entity.Abilities.Invoking(x => x.UpdateAbilities(0.016)).Should().NotThrow();

		entity.Abilities.GrantedAbilities.Should().HaveCount(2);
	}

	[Fact]
	[Trait("GraphBehavior", "Update")]
	public void A_behavior_may_end_its_own_instance_from_a_frame_update()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new SelfEndingBehavior(endOnFixedUpdate: false);

		AbilityHandle handle = GrantAndActivate(entity, behavior);
		handle.IsActive.Should().BeTrue();

		entity.Abilities.Invoking(x => x.UpdateAbilities(0.016)).Should().NotThrow();

		handle.IsActive.Should().BeFalse();
	}

	private static GraphProcessor StartGraphWith(TrackingStateNode node)
	{
		var graph = new Graph();
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[StateNode<StateNodeContext>.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		node.ActivateCount.Should().Be(1);

		return processor;
	}

	private static AbilityHandle GrantAndActivate(TestEntity entity, IAbilityBehavior behavior)
	{
		var abilityData = new AbilityData("FixedGraph", behaviorFactory: () => behavior);

		var grantConfig = new GrantAbilityConfig(
			abilityData,
			new ScalableInt(1),
			AbilityDeactivationPolicy.CancelImmediately,
			AbilityDeactivationPolicy.CancelImmediately,
			false,
			false,
			LevelComparison.Higher);

		var effectData = new EffectData(
			"GrantFixedGraph",
			new DurationData(DurationType.Infinite),
			effectComponents: [new GrantAbilityEffectComponent([grantConfig])]);

		entity.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(entity, entity)));

		AbilityHandle? handle = entity.Abilities.GrantedAbilities.First();
		handle.Should().NotBeNull();
		handle.TryActivate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);

		return handle;
	}
}
