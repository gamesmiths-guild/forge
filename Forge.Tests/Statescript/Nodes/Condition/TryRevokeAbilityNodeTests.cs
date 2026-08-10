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
using Gamesmiths.Forge.Statescript.Nodes.Condition;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes.Condition;

public class TryRevokeAbilityNodeTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Graph", "TryRevokeAbility")]
	public void Try_revoke_ability_node_revokes_a_permanent_grant_and_routes_to_true()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);

		AbilityHandle handle = owner.Abilities.GrantAbilityPermanently(
			new AbilityData("Unlocked"),
			1,
			LevelComparison.None,
			sourceEntity: null);

		(TrackingActionNode onTrue, TrackingActionNode onFalse) = RunRevokeGraph(handle, out _);

		owner.Abilities.GrantedAbilities.Should().BeEmpty();
		handle.IsValid.Should().BeFalse();
		onTrue.ExecutionCount.Should().Be(1);
		onFalse.ExecutionCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "TryRevokeAbility")]
	public void Try_revoke_ability_node_routes_to_false_when_there_is_no_permanent_grant()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var abilityData = new AbilityData("Provided");

		// Granted only by an effect, so the default scope has nothing of its own to remove. This is the respec case:
		// the False port means "the entity never had this unlock", which no resolver can report.
		GrantThroughEffect(owner, abilityData).Should().NotBeNull();
		owner.Abilities.TryGetAbility(abilityData, out AbilityHandle? handle, owner).Should().BeTrue();

		(TrackingActionNode onTrue, TrackingActionNode onFalse) = RunRevokeGraph(handle!, out _);

		owner.Abilities.GrantedAbilities.Should().ContainSingle();
		handle!.IsValid.Should().BeTrue();
		onTrue.ExecutionCount.Should().Be(0);
		onFalse.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "TryRevokeAbility")]
	public void Try_revoke_ability_node_routes_to_false_for_an_invalid_handle()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);

		AbilityHandle handle = owner.Abilities.GrantAbilityPermanently(
			new AbilityData("Unlocked"),
			1,
			LevelComparison.None,
			sourceEntity: null);

		owner.Abilities.RevokeAbility(handle).Should().BeTrue();
		handle.IsValid.Should().BeFalse();

		(TrackingActionNode onTrue, TrackingActionNode onFalse) = RunRevokeGraph(handle, out GraphProcessor processor);

		onTrue.ExecutionCount.Should().Be(0);
		onFalse.ExecutionCount.Should().Be(1);
		processor.Should().NotBeNull();
	}

	[Fact]
	[Trait("Graph", "TryRevokeAbility")]
	public void Try_revoke_ability_node_attempts_every_handle_in_an_array()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var effectGrantedData = new AbilityData("Provided");

		AbilityHandle permanent = owner.Abilities.GrantAbilityPermanently(
			new AbilityData("Unlocked"),
			1,
			LevelComparison.None,
			sourceEntity: null);

		GrantThroughEffect(owner, effectGrantedData).Should().NotBeNull();
		owner.Abilities.TryGetAbility(effectGrantedData, out AbilityHandle? effectGranted, owner).Should().BeTrue();

		owner.Abilities.GrantedAbilities.Should().HaveCount(2);

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectArrayVariable("abilities", permanent, effectGranted!);

		var revokeNode = new TryRevokeAbilityNode();
		revokeNode.BindInput(TryRevokeAbilityNode.AbilityInput, "abilities");

		var onTrue = new TrackingActionNode();
		var onFalse = new TrackingActionNode();

		BuildGraph(graph, revokeNode, onTrue, onFalse);
		new GraphProcessor(graph).StartGraph();

		// The permanent grant goes, the effect's grant stays, and "any succeeded" routes to True.
		owner.Abilities.GrantedAbilities.Should().ContainSingle();
		permanent.IsValid.Should().BeFalse();
		effectGranted!.IsValid.Should().BeTrue();
		onTrue.ExecutionCount.Should().Be(1);
		onFalse.ExecutionCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "TryRevokeAbility")]
	public void Try_revoke_ability_node_in_all_grants_scope_clears_an_effect_grant()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var abilityData = new AbilityData("Provided");

		GrantThroughEffect(owner, abilityData).Should().NotBeNull();
		owner.Abilities.TryGetAbility(abilityData, out AbilityHandle? handle, owner).Should().BeTrue();

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("ability", handle!);

		var revokeNode = new TryRevokeAbilityNode(AbilityRevokeScope.AllGrants);
		revokeNode.BindInput(TryRevokeAbilityNode.AbilityInput, "ability");

		var onTrue = new TrackingActionNode();
		var onFalse = new TrackingActionNode();

		BuildGraph(graph, revokeNode, onTrue, onFalse);
		new GraphProcessor(graph).StartGraph();

		owner.Abilities.GrantedAbilities.Should().BeEmpty();
		handle!.IsValid.Should().BeFalse();
		onTrue.ExecutionCount.Should().Be(1);
		onFalse.ExecutionCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "TryRevokeAbility")]
	public void Try_revoke_ability_node_can_revoke_the_ability_driving_its_own_graph()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);

		// The behavior graph revokes the very ability running it, so the node tears down its own execution context
		// mid-message. The condition must still resolve without throwing, exactly like CancelAbility does.
		var behaviorGraph = new Graph();

		var revokeNode = new TryRevokeAbilityNode();
		var onTrue = new TrackingActionNode();
		var onFalse = new TrackingActionNode();

		BuildGraph(behaviorGraph, revokeNode, onTrue, onFalse);

		var abilityData = new AbilityData(
			"SelfRevoking",
			behaviorFactory: () => new GraphAbilityBehavior(behaviorGraph));

		AbilityHandle handle = owner.Abilities.GrantAbilityPermanently(
			abilityData,
			1,
			LevelComparison.None,
			sourceEntity: null);

		behaviorGraph.VariableDefinitions.DefineObjectProperty(
			"selfAbility",
			new GetAbilityHandleResolver(abilityData));
		revokeNode.BindInput(TryRevokeAbilityNode.AbilityInput, "selfAbility");

		FluentActions.Invoking(() => handle.TryActivate(out _)).Should().NotThrow();

		owner.Abilities.GrantedAbilities.Should().BeEmpty();
		handle.IsValid.Should().BeFalse();
		onTrue.ExecutionCount.Should().Be(1);
		onFalse.ExecutionCount.Should().Be(0);
	}

	private static void BuildGraph(
		Graph graph,
		TryRevokeAbilityNode revokeNode,
		TrackingActionNode onTrue,
		TrackingActionNode onFalse)
	{
		graph.AddNode(revokeNode);
		graph.AddNode(onTrue);
		graph.AddNode(onFalse);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			revokeNode.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			revokeNode.OutputPorts[ConditionNode.TruePort],
			onTrue.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			revokeNode.OutputPorts[ConditionNode.FalsePort],
			onFalse.InputPorts[ActionNode.InputPort]));
	}

	private static (TrackingActionNode OnTrue, TrackingActionNode OnFalse) RunRevokeGraph(
		AbilityHandle handle,
		out GraphProcessor processor)
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("ability", handle);

		var revokeNode = new TryRevokeAbilityNode();
		revokeNode.BindInput(TryRevokeAbilityNode.AbilityInput, "ability");

		var onTrue = new TrackingActionNode();
		var onFalse = new TrackingActionNode();

		BuildGraph(graph, revokeNode, onTrue, onFalse);

		processor = new GraphProcessor(graph);
		processor.StartGraph();

		return (onTrue, onFalse);
	}

	private static ActiveEffectHandle? GrantThroughEffect(TestEntity entity, AbilityData abilityData)
	{
		var effectData = new EffectData(
			"Grant Ability Effect",
			new DurationData(DurationType.Infinite),
			effectComponents:
			[
				new GrantAbilityEffectComponent(
				[
					new GrantAbilityConfig(
						abilityData,
						new ScalableInt(1),
						AbilityDeactivationPolicy.CancelImmediately,
						AbilityDeactivationPolicy.CancelImmediately)
				])
			]);

		return entity.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(entity, entity)));
	}
}
