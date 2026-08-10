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
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes.Action;

public class RevokeAbilityNodeTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Graph", "RevokeAbility")]
	public void Revoke_ability_node_revokes_a_permanent_grant_through_its_handle()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var abilityData = new AbilityData("Unlocked");

		AbilityHandle handle = owner.Abilities.GrantAbilityPermanently(
			abilityData,
			1,
			LevelComparison.None,
			sourceEntity: null);

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("ability", handle);

		var revokeNode = new RevokeAbilityNode();
		revokeNode.BindInput(RevokeAbilityNode.AbilityInput, "ability");

		graph.AddNode(revokeNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			revokeNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		owner.Abilities.GrantedAbilities.Should().BeEmpty();
		handle.IsValid.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "RevokeAbility")]
	public void Revoke_ability_node_revokes_every_handle_in_an_array()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);

		AbilityHandle first = owner.Abilities.GrantAbilityPermanently(
			new AbilityData("First"),
			1,
			LevelComparison.None,
			sourceEntity: null);

		AbilityHandle second = owner.Abilities.GrantAbilityPermanently(
			new AbilityData("Second"),
			1,
			LevelComparison.None,
			sourceEntity: null);

		owner.Abilities.GrantedAbilities.Should().HaveCount(2);

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectArrayVariable("abilities", first, second);

		var revokeNode = new RevokeAbilityNode();
		revokeNode.BindInput(RevokeAbilityNode.AbilityInput, "abilities");

		graph.AddNode(revokeNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			revokeNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		owner.Abilities.GrantedAbilities.Should().BeEmpty();
		first.IsValid.Should().BeFalse();
		second.IsValid.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "RevokeAbility")]
	public void Revoke_ability_node_leaves_an_effect_grant_alone_in_the_default_scope()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var abilityData = new AbilityData("Provided");

		ActiveEffectHandle? effectHandle = GrantThroughEffect(owner, abilityData);
		effectHandle.Should().NotBeNull();

		owner.Abilities.TryGetAbility(abilityData, out AbilityHandle? handle, owner).Should().BeTrue();

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("ability", handle!);

		var revokeNode = new RevokeAbilityNode();
		revokeNode.BindInput(RevokeAbilityNode.AbilityInput, "ability");

		graph.AddNode(revokeNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			revokeNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		owner.Abilities.GrantedAbilities.Should().ContainSingle();
		handle!.IsValid.Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "RevokeAbility")]
	public void Revoke_ability_node_in_all_grants_scope_clears_an_effect_grant()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var abilityData = new AbilityData("Provided");

		GrantThroughEffect(owner, abilityData).Should().NotBeNull();
		owner.Abilities.TryGetAbility(abilityData, out AbilityHandle? handle, owner).Should().BeTrue();

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("ability", handle!);

		var revokeNode = new RevokeAbilityNode(AbilityRevokeScope.AllGrants);
		revokeNode.BindInput(RevokeAbilityNode.AbilityInput, "ability");

		graph.AddNode(revokeNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			revokeNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		owner.Abilities.GrantedAbilities.Should().BeEmpty();
		handle!.IsValid.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "RevokeAbility")]
	public void Revoke_ability_node_skips_invalid_handles()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);

		AbilityHandle handle = owner.Abilities.GrantAbilityPermanently(
			new AbilityData("Unlocked"),
			1,
			LevelComparison.None,
			sourceEntity: null);

		owner.Abilities.RevokeAbility(handle).Should().BeTrue();
		handle.IsValid.Should().BeFalse();

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("ability", handle);

		var revokeNode = new RevokeAbilityNode();
		revokeNode.BindInput(RevokeAbilityNode.AbilityInput, "ability");

		var afterNode = new TrackingActionNode();

		graph.AddNode(revokeNode);
		graph.AddNode(afterNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			revokeNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			revokeNode.OutputPorts[ActionNode.OutputPort],
			afterNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);

		FluentActions.Invoking(() => processor.StartGraph()).Should().NotThrow();

		// Skipping the handle is silent, so the action still passes the message on.
		afterNode.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "RevokeAbility")]
	public void Revoke_ability_node_can_revoke_the_ability_driving_its_own_graph()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);

		// The behavior graph revokes the very ability running it, so the node tears down its own execution context
		// mid-message. The action must still complete without throwing, exactly like CancelAbility does.
		var behaviorGraph = new Graph();

		var revokeNode = new RevokeAbilityNode();
		var afterNode = new TrackingActionNode();

		behaviorGraph.AddNode(revokeNode);
		behaviorGraph.AddNode(afterNode);
		behaviorGraph.AddConnection(new Connection(
			behaviorGraph.EntryNode.OutputPorts[EntryNode.OutputPort],
			revokeNode.InputPorts[ActionNode.InputPort]));
		behaviorGraph.AddConnection(new Connection(
			revokeNode.OutputPorts[ActionNode.OutputPort],
			afterNode.InputPorts[ActionNode.InputPort]));

		var abilityData = new AbilityData(
			"SelfRevoking",
			behaviorFactory: () => new GraphAbilityBehavior(behaviorGraph));

		AbilityHandle handle = owner.Abilities.GrantAbilityPermanently(
			abilityData,
			1,
			LevelComparison.None,
			sourceEntity: null);

		// The graph reads its own handle from the ability context through the standard resolver.
		behaviorGraph.VariableDefinitions.DefineObjectProperty(
			"selfAbility",
			new GetAbilityHandleResolver(abilityData));
		revokeNode.BindInput(RevokeAbilityNode.AbilityInput, "selfAbility");

		FluentActions.Invoking(() => handle.TryActivate(out _)).Should().NotThrow();

		owner.Abilities.GrantedAbilities.Should().BeEmpty();
		handle.IsValid.Should().BeFalse();
		afterNode.ExecutionCount.Should().Be(1);
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
