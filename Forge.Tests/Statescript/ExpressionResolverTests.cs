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

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;

namespace Gamesmiths.Forge.Tests.Statescript;

public class ExpressionResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Graph", "Expression")]
	public void Comparison_resolver_greater_than_returns_true_when_left_exceeds_right()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineProperty(
			"isAboveThreshold",
			new ComparisonResolver(
				new VariantResolver(new Variant128(15.0), typeof(double)),
				ComparisonOperation.GreaterThan,
				new VariantResolver(new Variant128(10.0), typeof(double))));

		ExpressionNode condition = CreateExpressionNode("isAboveThreshold");
		var trueAction = new TrackingActionNode();
		var falseAction = new TrackingActionNode();

		graph.AddNode(condition);
		graph.AddNode(trueAction);
		graph.AddNode(falseAction);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			condition.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.TruePort],
			trueAction.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.FalsePort],
			falseAction.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		trueAction.ExecutionCount.Should().Be(1);
		falseAction.ExecutionCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "Expression")]
	public void Comparison_resolver_greater_than_returns_false_when_left_is_less()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineProperty(
			"isAboveThreshold",
			new ComparisonResolver(
				new VariantResolver(new Variant128(5.0), typeof(double)),
				ComparisonOperation.GreaterThan,
				new VariantResolver(new Variant128(10.0), typeof(double))));

		ExpressionNode condition = CreateExpressionNode("isAboveThreshold");
		var trueAction = new TrackingActionNode();
		var falseAction = new TrackingActionNode();

		graph.AddNode(condition);
		graph.AddNode(trueAction);
		graph.AddNode(falseAction);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			condition.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.TruePort],
			trueAction.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.FalsePort],
			falseAction.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		trueAction.ExecutionCount.Should().Be(0);
		falseAction.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Expression")]
	public void Comparison_resolver_equal_returns_true_for_matching_values()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineProperty(
			"isEqual",
			new ComparisonResolver(
				new VariantResolver(new Variant128(42.0), typeof(double)),
				ComparisonOperation.Equal,
				new VariantResolver(new Variant128(42.0), typeof(double))));

		ExpressionNode condition = CreateExpressionNode("isEqual");
		var trueAction = new TrackingActionNode();
		var falseAction = new TrackingActionNode();

		graph.AddNode(condition);
		graph.AddNode(trueAction);
		graph.AddNode(falseAction);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			condition.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.TruePort],
			trueAction.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.FalsePort],
			falseAction.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		trueAction.ExecutionCount.Should().Be(1);
		falseAction.ExecutionCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "Expression")]
	public void Comparison_resolver_compares_two_graph_variables()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("health", 25.0);
		graph.VariableDefinitions.DefineVariable("threshold", 50.0);

		graph.VariableDefinitions.DefineProperty(
			"isHealthAboveThreshold",
			new ComparisonResolver(
				new VariableResolver("health", typeof(double)),
				ComparisonOperation.GreaterThan,
				new VariableResolver("threshold", typeof(double))));

		ExpressionNode condition = CreateExpressionNode("isHealthAboveThreshold");
		var trueAction = new TrackingActionNode();
		var falseAction = new TrackingActionNode();

		graph.AddNode(condition);
		graph.AddNode(trueAction);
		graph.AddNode(falseAction);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			condition.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.TruePort],
			trueAction.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.FalsePort],
			falseAction.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		trueAction.ExecutionCount.Should().Be(0, "health (25) is NOT above threshold (50)");
		falseAction.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Expression")]
	public void Comparison_resolver_less_than_or_equal_at_boundary()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineProperty(
			"atBoundary",
			new ComparisonResolver(
				new VariantResolver(new Variant128(10.0), typeof(double)),
				ComparisonOperation.LessThanOrEqual,
				new VariantResolver(new Variant128(10.0), typeof(double))));

		ExpressionNode condition = CreateExpressionNode("atBoundary");
		var trueAction = new TrackingActionNode();
		var falseAction = new TrackingActionNode();

		graph.AddNode(condition);
		graph.AddNode(trueAction);
		graph.AddNode(falseAction);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			condition.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.TruePort],
			trueAction.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.FalsePort],
			falseAction.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		trueAction.ExecutionCount.Should().Be(1, "10 <= 10 is true");
		falseAction.ExecutionCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "Expression")]
	public void Comparison_resolver_not_equal_returns_true_for_different_values()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineProperty(
			"isDifferent",
			new ComparisonResolver(
				new VariantResolver(new Variant128(1.0), typeof(double)),
				ComparisonOperation.NotEqual,
				new VariantResolver(new Variant128(2.0), typeof(double))));

		ExpressionNode condition = CreateExpressionNode("isDifferent");
		var trueAction = new TrackingActionNode();
		var falseAction = new TrackingActionNode();

		graph.AddNode(condition);
		graph.AddNode(trueAction);
		graph.AddNode(falseAction);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			condition.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.TruePort],
			trueAction.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.FalsePort],
			falseAction.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		trueAction.ExecutionCount.Should().Be(1);
		falseAction.ExecutionCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "Expression")]
	public void Expression_condition_node_returns_false_for_missing_property()
	{
		var graph = new Graph();

		ExpressionNode condition = CreateExpressionNode("nonexistent");
		var trueAction = new TrackingActionNode();
		var falseAction = new TrackingActionNode();

		graph.AddNode(condition);
		graph.AddNode(trueAction);
		graph.AddNode(falseAction);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			condition.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.TruePort],
			trueAction.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.FalsePort],
			falseAction.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		trueAction.ExecutionCount.Should().Be(0);
		falseAction.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Expression")]
	public void Comparison_resolver_works_with_int_operands()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("score", 100);
		graph.VariableDefinitions.DefineVariable("requiredScore", 50);

		graph.VariableDefinitions.DefineProperty(
			"hasEnoughScore",
			new ComparisonResolver(
				new VariableResolver("score", typeof(int)),
				ComparisonOperation.GreaterThanOrEqual,
				new VariableResolver("requiredScore", typeof(int))));

		ExpressionNode condition = CreateExpressionNode("hasEnoughScore");
		var trueAction = new TrackingActionNode();
		var falseAction = new TrackingActionNode();

		graph.AddNode(condition);
		graph.AddNode(trueAction);
		graph.AddNode(falseAction);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			condition.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.TruePort],
			trueAction.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.FalsePort],
			falseAction.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		trueAction.ExecutionCount.Should().Be(1, "score (100) >= requiredScore (50)");
		falseAction.ExecutionCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "Expression")]
	public void Comparison_resolver_works_with_attribute_resolver()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);

		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("required", 3);

		graph.VariableDefinitions.DefineProperty(
			"hasEnoughAttribute",
			new ComparisonResolver(
				new AttributeResolver("TestAttributeSet.Attribute5"),
				ComparisonOperation.GreaterThanOrEqual,
				new VariableResolver("required", typeof(int))));

		ExpressionNode condition = CreateExpressionNode("hasEnoughAttribute");
		var trueAction = new TrackingActionNode();
		var falseAction = new TrackingActionNode();

		graph.AddNode(condition);
		graph.AddNode(trueAction);
		graph.AddNode(falseAction);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			condition.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.TruePort],
			trueAction.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.FalsePort],
			falseAction.InputPorts[ActionNode.InputPort]));

		var behavior = new GraphAbilityBehavior(graph);

		var abilityData = new AbilityData("AttrResolverTest", behaviorFactory: () => behavior);

		var grantConfig = new GrantAbilityConfig(
			abilityData,
			new ScalableInt(1),
			AbilityDeactivationPolicy.CancelImmediately,
			AbilityDeactivationPolicy.CancelImmediately,
			false,
			false,
			LevelComparison.Higher);

		var grantEffectData = new EffectData(
			"Grant",
			new DurationData(DurationType.Infinite),
			effectComponents: [new GrantAbilityEffectComponent([grantConfig])]);

		var grantEffect = new Effect(grantEffectData, new EffectOwnership(null, null));
		_ = entity.EffectsManager.ApplyEffect(grantEffect);
		entity.Abilities.TryGetAbility(abilityData, out AbilityHandle? handle);
		handle!.Activate(out _);

		trueAction.ExecutionCount.Should().Be(1);
		falseAction.ExecutionCount.Should().Be(0);
	}
}
