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
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes.Action;

public class CommitAbilityNodeTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string CostAttribute = "TestAttributeSet.Attribute90";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Theory]
	[Trait("Graph", "CommitAbility")]
	[InlineData(CommitAbilityOperation.CostAndCooldown, 85, true)]
	[InlineData(CommitAbilityOperation.CooldownOnly, 90, true)]
	[InlineData(CommitAbilityOperation.CostOnly, 85, false)]
	public void Commit_ability_node_commits_the_configured_operation(
		CommitAbilityOperation operation,
		int expectedAttributeValue,
		bool expectCooldown)
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var cooldownTag = Tag.RequestTag(_tagsManager, "simple.tag");

		var graph = new Graph();
		var commitNode = new CommitAbilityNode(operation);
		graph.AddNode(commitNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			commitNode.InputPorts[ActionNode.InputPort]));

		AbilityData abilityData = CreateAbilityData(graph, cooldownTag);

		AbilityHandle handle = owner.Abilities.GrantAbilityPermanently(
			abilityData,
			1,
			LevelComparison.None,
			sourceEntity: null);

		handle.Activate(out AbilityActivationFailures failures).Should().BeTrue();
		failures.Should().Be(AbilityActivationFailures.None);

		owner.Attributes[CostAttribute].CurrentValue.Should().Be(expectedAttributeValue);
		(handle.GetRemainingCooldownTime(cooldownTag) > 0).Should().Be(expectCooldown);
	}

	[Fact]
	[Trait("Graph", "CommitAbility")]
	public void Commit_ability_node_does_nothing_without_an_ability_context()
	{
		var graph = new Graph();
		var commitNode = new CommitAbilityNode();
		graph.AddNode(commitNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			commitNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);

		FluentActions.Invoking(() => processor.StartGraph()).Should().NotThrow();
	}

	private static AbilityData CreateAbilityData(Graph graph, Tag cooldownTag)
	{
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

		return new AbilityData(
			"Commit Test",
			costEffect: costEffect,
			cooldownEffects: [cooldownEffect],
			behaviorFactory: () => new GraphAbilityBehavior(graph));
	}
}
