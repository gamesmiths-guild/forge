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
using Gamesmiths.Forge.Statescript.Nodes.Condition;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes.Condition;

public class TryCommitAbilityNodeTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string CostAttribute = "TestAttributeSet.Attribute90";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Theory]
	[Trait("Graph", "TryCommitAbility")]
	[InlineData(CommitAbilityOperation.CostAndCooldown, 85, true)]
	[InlineData(CommitAbilityOperation.CooldownOnly, 90, true)]
	[InlineData(CommitAbilityOperation.CostOnly, 85, false)]
	public void Try_commit_ability_node_commits_the_configured_operation(
		CommitAbilityOperation operation,
		int expectedAttributeValue,
		bool expectCooldown)
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var cooldownTag = Tag.RequestTag(_tagsManager, "simple.tag");

		var graph = new Graph();
		var commitNode = new TryCommitAbilityNode(operation);
		var committedAction = new TrackingActionNode();
		var failedAction = new TrackingActionNode();
		graph.AddNode(commitNode);
		graph.AddNode(committedAction);
		graph.AddNode(failedAction);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			commitNode.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			commitNode.OutputPorts[ConditionNode.TruePort],
			committedAction.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			commitNode.OutputPorts[ConditionNode.FalsePort],
			failedAction.InputPorts[ActionNode.InputPort]));

		AbilityData abilityData = CreateAbilityData(graph, cooldownTag);

		AbilityHandle handle = owner.Abilities.GrantAbilityPermanently(
			abilityData,
			1,
			LevelComparison.None,
			sourceEntity: null);

		handle.TryActivate(out AbilityActivationFailures failures).Should().BeTrue();
		failures.Should().Be(AbilityActivationFailures.None);

		committedAction.ExecutionCount.Should().Be(1);
		failedAction.ExecutionCount.Should().Be(0);
		owner.Attributes[CostAttribute].CurrentValue.Should().Be(expectedAttributeValue);
		(handle.GetRemainingCooldownTime(cooldownTag) > 0).Should().Be(expectCooldown);
	}

	[Fact]
	[Trait("Graph", "TryCommitAbility")]
	public void Try_commit_ability_node_routes_to_false_when_the_cooldown_is_already_running()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var cooldownTag = Tag.RequestTag(_tagsManager, "simple.tag");

		var graph = new Graph();
		var firstCommit = new TryCommitAbilityNode(CommitAbilityOperation.CooldownOnly);
		var secondCommit = new TryCommitAbilityNode(CommitAbilityOperation.CooldownOnly);
		var committedAction = new TrackingActionNode();
		var failedAction = new TrackingActionNode();
		graph.AddNode(firstCommit);
		graph.AddNode(secondCommit);
		graph.AddNode(committedAction);
		graph.AddNode(failedAction);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			firstCommit.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			firstCommit.OutputPorts[ConditionNode.TruePort],
			secondCommit.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			secondCommit.OutputPorts[ConditionNode.TruePort],
			committedAction.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			secondCommit.OutputPorts[ConditionNode.FalsePort],
			failedAction.InputPorts[ActionNode.InputPort]));

		AbilityData abilityData = CreateAbilityData(graph, cooldownTag);

		AbilityHandle handle = owner.Abilities.GrantAbilityPermanently(
			abilityData,
			1,
			LevelComparison.None,
			sourceEntity: null);

		handle.TryActivate(out AbilityActivationFailures failures).Should().BeTrue();
		failures.Should().Be(AbilityActivationFailures.None);

		// The first commit started the cooldown, so the second one has nothing left to pay.
		committedAction.ExecutionCount.Should().Be(0);
		failedAction.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "TryCommitAbility")]
	public void Try_commit_ability_node_routes_to_false_when_the_cost_is_no_longer_affordable()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var cooldownTag = Tag.RequestTag(_tagsManager, "simple.tag");

		var graph = new Graph();
		var firstCommit = new TryCommitAbilityNode(CommitAbilityOperation.CostOnly);
		var secondCommit = new TryCommitAbilityNode(CommitAbilityOperation.CostOnly);
		var committedAction = new TrackingActionNode();
		var failedAction = new TrackingActionNode();
		graph.AddNode(firstCommit);
		graph.AddNode(secondCommit);
		graph.AddNode(committedAction);
		graph.AddNode(failedAction);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			firstCommit.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			firstCommit.OutputPorts[ConditionNode.TruePort],
			secondCommit.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			secondCommit.OutputPorts[ConditionNode.TruePort],
			committedAction.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			secondCommit.OutputPorts[ConditionNode.FalsePort],
			failedAction.InputPorts[ActionNode.InputPort]));

		// A 50 point cost against the attribute's starting value of 90: affordable once, never twice.
		AbilityData abilityData = CreateAbilityData(graph, cooldownTag, costMagnitude: -50);

		AbilityHandle handle = owner.Abilities.GrantAbilityPermanently(
			abilityData,
			1,
			LevelComparison.None,
			sourceEntity: null);

		handle.TryActivate(out AbilityActivationFailures failures).Should().BeTrue();
		failures.Should().Be(AbilityActivationFailures.None);

		committedAction.ExecutionCount.Should().Be(0);
		failedAction.ExecutionCount.Should().Be(1);
		owner.Attributes[CostAttribute].CurrentValue.Should().Be(40);
	}

	[Fact]
	[Trait("Graph", "TryCommitAbility")]
	public void Try_commit_ability_node_pays_nothing_when_only_one_of_cost_and_cooldown_can_be_committed()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var cooldownTag = Tag.RequestTag(_tagsManager, "simple.tag");

		var graph = new Graph();
		var costCommit = new TryCommitAbilityNode(CommitAbilityOperation.CostOnly);
		var fullCommit = new TryCommitAbilityNode(CommitAbilityOperation.CostAndCooldown);
		var committedAction = new TrackingActionNode();
		var failedAction = new TrackingActionNode();
		graph.AddNode(costCommit);
		graph.AddNode(fullCommit);
		graph.AddNode(committedAction);
		graph.AddNode(failedAction);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			costCommit.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			costCommit.OutputPorts[ConditionNode.TruePort],
			fullCommit.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			fullCommit.OutputPorts[ConditionNode.TruePort],
			committedAction.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			fullCommit.OutputPorts[ConditionNode.FalsePort],
			failedAction.InputPorts[ActionNode.InputPort]));

		AbilityData abilityData = CreateAbilityData(graph, cooldownTag, costMagnitude: -50);

		AbilityHandle handle = owner.Abilities.GrantAbilityPermanently(
			abilityData,
			1,
			LevelComparison.None,
			sourceEntity: null);

		handle.TryActivate(out AbilityActivationFailures failures).Should().BeTrue();
		failures.Should().Be(AbilityActivationFailures.None);

		// The cooldown was free to start, but the cost was not affordable, so neither was applied.
		committedAction.ExecutionCount.Should().Be(0);
		failedAction.ExecutionCount.Should().Be(1);
		owner.Attributes[CostAttribute].CurrentValue.Should().Be(40);
		handle.GetRemainingCooldownTime(cooldownTag).Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "TryCommitAbility")]
	public void Try_commit_ability_node_routes_to_false_without_an_ability_context()
	{
		var graph = new Graph();
		var commitNode = new TryCommitAbilityNode();
		var committedAction = new TrackingActionNode();
		var failedAction = new TrackingActionNode();
		graph.AddNode(commitNode);
		graph.AddNode(committedAction);
		graph.AddNode(failedAction);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			commitNode.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			commitNode.OutputPorts[ConditionNode.TruePort],
			committedAction.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			commitNode.OutputPorts[ConditionNode.FalsePort],
			failedAction.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);

		FluentActions.Invoking(() => processor.StartGraph()).Should().NotThrow();

		committedAction.ExecutionCount.Should().Be(0);
		failedAction.ExecutionCount.Should().Be(1);
	}

	private static AbilityData CreateAbilityData(Graph graph, Tag cooldownTag, float costMagnitude = -5)
	{
		var costEffect = new EffectData(
			"Cost",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					CostAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(costMagnitude)))
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
