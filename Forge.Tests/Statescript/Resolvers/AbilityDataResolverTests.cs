// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class AbilityDataResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string CostAttribute = "TestAttributeSet.Attribute90";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "AbilityCooldown")]
	public void Ability_cooldown_resolver_reads_the_committed_cooldown()
	{
		(GraphContext context, AbilityHandle handle, TestEntity owner) = CreateActivatedAbility();

		new AbilityCooldownResolver(AbilityCooldownDataType.RemainingTime)
			.Resolve(context).Get<float>().Should().Be(0f);
		new AbilityCooldownResolver(AbilityCooldownDataType.TotalTime)
			.Resolve(context).Get<float>().Should().BeApproximately(5f, 0.001f);

		handle.CommitCooldown();

		new AbilityCooldownResolver(AbilityCooldownDataType.RemainingTime)
			.Resolve(context).Get<float>().Should().BeApproximately(5f, 0.001f);
		new AbilityCooldownResolver(AbilityCooldownDataType.RemainingFraction)
			.Resolve(context).Get<float>().Should().BeApproximately(1f, 0.001f);

		owner.EffectsManager.UpdateEffects(2);

		new AbilityCooldownResolver(AbilityCooldownDataType.RemainingTime)
			.Resolve(context).Get<float>().Should().BeApproximately(3f, 0.001f);
		new AbilityCooldownResolver(AbilityCooldownDataType.RemainingFraction)
			.Resolve(context).Get<float>().Should().BeApproximately(0.6f, 0.001f);
	}

	[Fact]
	[Trait("Resolver", "AbilityCooldown")]
	public void Ability_cooldown_resolver_filters_by_tag()
	{
		(GraphContext context, AbilityHandle handle, _) = CreateActivatedAbility();

		handle.CommitCooldown();

		var cooldownTag = Tag.RequestTag(_tagsManager, "simple.tag");
		var unrelatedTag = Tag.RequestTag(_tagsManager, "color.red");

		new AbilityCooldownResolver(AbilityCooldownDataType.RemainingTime, cooldownTag)
			.Resolve(context).Get<float>().Should().BeApproximately(5f, 0.001f);
		new AbilityCooldownResolver(AbilityCooldownDataType.RemainingTime, unrelatedTag)
			.Resolve(context).Get<float>().Should().Be(0f);
	}

	[Fact]
	[Trait("Resolver", "AbilityCost")]
	public void Ability_cost_resolver_reads_the_evaluated_cost()
	{
		(GraphContext context, _, _) = CreateActivatedAbility();

		new AbilityCostResolver(CostAttribute).Resolve(context).Get<int>().Should().Be(-5);
		new AbilityCostResolver("Invalid.Attribute").Resolve(context).Get<int>().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "CanActivateAbility")]
	public void Can_activate_ability_resolver_reflects_cooldown_state()
	{
		(GraphContext context, AbilityHandle handle, _) = CreateActivatedAbility();

		new CanActivateAbilityResolver().Resolve(context).AsBool().Should().BeTrue();

		handle.CommitCooldown();

		new CanActivateAbilityResolver().Resolve(context).AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "AbilityState")]
	public void Ability_state_resolver_reads_handle_flags()
	{
		(GraphContext context, _, _) = CreateActivatedAbility();

		new AbilityStateResolver(AbilityStateType.IsValid).Resolve(context).AsBool().Should().BeTrue();
		new AbilityStateResolver(AbilityStateType.IsInhibited).Resolve(context).AsBool().Should().BeFalse();

		// The capture graph has no state nodes, so the instance ended right after activation.
		new AbilityStateResolver(AbilityStateType.IsActive).Resolve(context).AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "GetAbilityHandle")]
	public void Get_ability_handle_resolver_finds_granted_abilities()
	{
		(GraphContext context, AbilityHandle handle, TestEntity owner) = CreateActivatedAbility(
			out AbilityData abilityData);

		AbilityHandle? resolvedHandle = new GetAbilityHandleResolver(abilityData).Resolve(context);
		resolvedHandle.Should().Be(handle);

		// Cross-ability query: the resolved handle feeds other ability resolvers.
		handle.CommitCooldown();
		new AbilityCooldownResolver(
				AbilityCooldownDataType.RemainingTime,
				handleResolver: new GetAbilityHandleResolver(abilityData))
			.Resolve(context).Get<float>().Should().BeApproximately(5f, 0.001f);

		var ungrantedData = new AbilityData("Ungranted");
		new GetAbilityHandleResolver(ungrantedData).Resolve(context).Should().BeNull();

		owner.Abilities.GrantedAbilities.Should().Contain(handle);
	}

	[Fact]
	[Trait("Resolver", "AbilityData")]
	public void Ability_resolvers_return_defaults_without_an_ability_context()
	{
		var context = new GraphContext();

		new AbilityCooldownResolver().Resolve(context).Get<float>().Should().Be(0f);
		new AbilityCostResolver(CostAttribute).Resolve(context).Get<int>().Should().Be(0);
		new CanActivateAbilityResolver().Resolve(context).AsBool().Should().BeFalse();
		new AbilityStateResolver(AbilityStateType.IsValid).Resolve(context).AsBool().Should().BeFalse();
	}

	private (GraphContext Context, AbilityHandle Handle, TestEntity Owner) CreateActivatedAbility()
	{
		return CreateActivatedAbility(out _);
	}

	private (GraphContext Context, AbilityHandle Handle, TestEntity Owner) CreateActivatedAbility(
		out AbilityData abilityData)
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var cooldownTag = Tag.RequestTag(_tagsManager, "simple.tag");

		var graph = new Graph();
		var captureNode = new CaptureGraphContextNode();
		graph.AddNode(captureNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			captureNode.InputPorts[ActionNode.InputPort]));

		var costEffect = new EffectData(
			"Cost",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					CostAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-5)))
			]);

		var cooldownEffect = new EffectData(
			"Cooldown",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(5))),
			effectComponents: [new ModifierTagsEffectComponent(cooldownTag.GetSingleTagContainer()!)]);

		abilityData = new AbilityData(
			"Ability Data Resolver Test",
			costEffect: costEffect,
			cooldownEffects: [cooldownEffect],
			behaviorFactory: () => new GraphAbilityBehavior(graph));

		AbilityHandle handle = owner.Abilities.GrantAbilityPermanently(
			abilityData,
			1,
			LevelComparison.None,
			sourceEntity: null);

		handle.Activate(out _).Should().BeTrue();
		captureNode.CapturedGraphContext.Should().NotBeNull();

		return (captureNode.CapturedGraphContext!, handle, owner);
	}
}
