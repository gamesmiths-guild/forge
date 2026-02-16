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
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript;

public class PropertyResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "Attribute")]
	public void Attribute_resolver_returns_current_value_of_existing_attribute()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var resolver = new AttributeResolver("TestAttributeSet.Attribute5");

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsInt().Should().Be(5);
	}

	[Fact]
	[Trait("Resolver", "Attribute")]
	public void Attribute_resolver_returns_default_for_missing_attribute()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var resolver = new AttributeResolver("TestAttributeSet.NonExistent");

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Attribute")]
	public void Attribute_resolver_returns_default_when_no_activation_context()
	{
		var resolver = new AttributeResolver("TestAttributeSet.Attribute5");

		var context = new GraphContext();

		Variant128 result = resolver.Resolve(context);

		result.AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Attribute")]
	public void Attribute_resolver_value_type_is_int()
	{
		var resolver = new AttributeResolver("TestAttributeSet.Attribute5");

		resolver.ValueType.Should().Be(typeof(int));
	}

	[Fact]
	[Trait("Resolver", "Attribute")]
	public void Attribute_resolver_reads_different_attributes()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var resolver1 = new AttributeResolver("TestAttributeSet.Attribute1");
		var resolver90 = new AttributeResolver("TestAttributeSet.Attribute90");

		GraphContext context = CreateAbilityGraphContext(entity);

		resolver1.Resolve(context).AsInt().Should().Be(1);
		resolver90.Resolve(context).AsInt().Should().Be(90);
	}

	[Fact]
	[Trait("Resolver", "Tag")]
	public void Tag_resolver_returns_true_when_entity_has_tag()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var tag = Tag.RequestTag(_tagsManager, "enemy.undead.zombie");
		var resolver = new TagResolver(tag);

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "Tag")]
	public void Tag_resolver_returns_false_when_entity_does_not_have_tag()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var tag = Tag.RequestTag(_tagsManager, "enemy.beast.wolf");
		var resolver = new TagResolver(tag);

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "Tag")]
	public void Tag_resolver_returns_false_when_no_activation_context()
	{
		var tag = Tag.RequestTag(_tagsManager, "enemy.undead.zombie");
		var resolver = new TagResolver(tag);

		var context = new GraphContext();

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "Tag")]
	public void Tag_resolver_value_type_is_bool()
	{
		var tag = Tag.RequestTag(_tagsManager, "enemy.undead.zombie");
		var resolver = new TagResolver(tag);

		resolver.ValueType.Should().Be(typeof(bool));
	}

	[Fact]
	[Trait("Resolver", "Tag")]
	public void Tag_resolver_matches_parent_tag()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var parentTag = Tag.RequestTag(_tagsManager, "enemy.undead");
		var resolver = new TagResolver(parentTag);

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "Variant")]
	public void Variant_resolver_returns_stored_value()
	{
		var resolver = new VariantResolver(new Variant128(42.0), typeof(double));

		var context = new GraphContext();

		Variant128 result = resolver.Resolve(context);

		result.AsDouble().Should().Be(42.0);
	}

	[Fact]
	[Trait("Resolver", "Variant")]
	public void Variant_resolver_value_can_be_updated()
	{
		var resolver = new VariantResolver(new Variant128(10), typeof(int));

		resolver.Set(25);

		var context = new GraphContext();
		Variant128 result = resolver.Resolve(context);

		result.AsInt().Should().Be(25);
	}

	[Fact]
	[Trait("Resolver", "Variant")]
	public void Variant_resolver_reports_correct_value_type()
	{
		var intResolver = new VariantResolver(new Variant128(0), typeof(int));
		var boolResolver = new VariantResolver(new Variant128(false), typeof(bool));
		var doubleResolver = new VariantResolver(new Variant128(0.0), typeof(double));

		intResolver.ValueType.Should().Be(typeof(int));
		boolResolver.ValueType.Should().Be(typeof(bool));
		doubleResolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Variable")]
	public void Variable_resolver_reads_value_from_graph_variables()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("speed", 7.5);

		var context = new GraphContext();
		context.GraphVariables.InitializeFrom(graph.VariableDefinitions);

		var resolver = new VariableResolver("speed", typeof(double));

		Variant128 result = resolver.Resolve(context);

		result.AsDouble().Should().Be(7.5);
	}

	[Fact]
	[Trait("Resolver", "Variable")]
	public void Variable_resolver_returns_default_for_missing_variable()
	{
		var graph = new Graph();

		var context = new GraphContext();
		context.GraphVariables.InitializeFrom(graph.VariableDefinitions);

		var resolver = new VariableResolver("nonexistent", typeof(double));

		Variant128 result = resolver.Resolve(context);

		result.AsDouble().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Variable")]
	public void Variable_resolver_reflects_runtime_variable_changes()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("counter", 0);

		var context = new GraphContext();
		context.GraphVariables.InitializeFrom(graph.VariableDefinitions);

		var resolver = new VariableResolver("counter", typeof(int));

		resolver.Resolve(context).AsInt().Should().Be(0);

		context.GraphVariables.SetVar("counter", 42);

		resolver.Resolve(context).AsInt().Should().Be(42);
	}

	[Fact]
	[Trait("Resolver", "Variable")]
	public void Variable_resolver_value_type_is_double()
	{
		var resolver = new VariableResolver("anything", typeof(double));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Comparison")]
	public void Comparison_resolver_value_type_is_bool()
	{
		var resolver = new ComparisonResolver(
			new VariantResolver(new Variant128(0.0), typeof(double)),
			ComparisonOperation.Equal,
			new VariantResolver(new Variant128(0.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(bool));
	}

	[Fact]
	[Trait("Resolver", "Comparison")]
	public void Comparison_resolver_equal_returns_true_for_same_values()
	{
		var resolver = new ComparisonResolver(
			new VariantResolver(new Variant128(5.0), typeof(double)),
			ComparisonOperation.Equal,
			new VariantResolver(new Variant128(5.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "Comparison")]
	public void Comparison_resolver_equal_returns_false_for_different_values()
	{
		var resolver = new ComparisonResolver(
			new VariantResolver(new Variant128(5.0), typeof(double)),
			ComparisonOperation.Equal,
			new VariantResolver(new Variant128(10.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "Comparison")]
	public void Comparison_resolver_not_equal_returns_true_for_different_values()
	{
		var resolver = new ComparisonResolver(
			new VariantResolver(new Variant128(1.0), typeof(double)),
			ComparisonOperation.NotEqual,
			new VariantResolver(new Variant128(2.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "Comparison")]
	public void Comparison_resolver_less_than_returns_true_when_left_is_smaller()
	{
		var resolver = new ComparisonResolver(
			new VariantResolver(new Variant128(3.0), typeof(double)),
			ComparisonOperation.LessThan,
			new VariantResolver(new Variant128(10.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "Comparison")]
	public void Comparison_resolver_less_than_returns_false_at_boundary()
	{
		var resolver = new ComparisonResolver(
			new VariantResolver(new Variant128(10.0), typeof(double)),
			ComparisonOperation.LessThan,
			new VariantResolver(new Variant128(10.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "Comparison")]
	public void Comparison_resolver_greater_than_returns_true_when_left_is_larger()
	{
		var resolver = new ComparisonResolver(
			new VariantResolver(new Variant128(20.0), typeof(double)),
			ComparisonOperation.GreaterThan,
			new VariantResolver(new Variant128(10.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "Comparison")]
	public void Comparison_resolver_greater_than_or_equal_returns_true_at_boundary()
	{
		var resolver = new ComparisonResolver(
			new VariantResolver(new Variant128(10.0), typeof(double)),
			ComparisonOperation.GreaterThanOrEqual,
			new VariantResolver(new Variant128(10.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "Comparison")]
	public void Comparison_resolver_supports_nested_resolvers()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);

		var resolver = new ComparisonResolver(
			new AttributeResolver("TestAttributeSet.Attribute5"),
			ComparisonOperation.GreaterThan,
			new VariantResolver(new Variant128(3.0), typeof(double)));

		GraphContext context = CreateAbilityGraphContext(entity);

		resolver.Resolve(context).AsBool().Should().BeTrue("Attribute5 (5) > 3");
	}

	[Fact]
	[Trait("Resolver", "SharedVariable")]
	public void Shared_variable_resolver_reads_value_from_shared_variables()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		entity.SharedVariables.DefineVariable("abilityLock", true);

		var resolver = new SharedVariableResolver("abilityLock", typeof(bool));

		var context = new GraphContext { SharedVariables = entity.SharedVariables };

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "SharedVariable")]
	public void Shared_variable_resolver_returns_default_when_shared_variables_is_null()
	{
		var resolver = new SharedVariableResolver("abilityLock", typeof(double));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "SharedVariable")]
	public void Shared_variable_resolver_returns_default_for_missing_variable()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var resolver = new SharedVariableResolver("nonexistent", typeof(double));

		var context = new GraphContext { SharedVariables = entity.SharedVariables };

		resolver.Resolve(context).AsDouble().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "SharedVariable")]
	public void Shared_variable_resolver_value_type_is_double()
	{
		var resolver = new SharedVariableResolver("anything", typeof(double));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "SharedVariable")]
	public void Shared_variable_resolver_reflects_changes_across_graph_contexts()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		entity.SharedVariables.DefineVariable("sharedCounter", 0);

		var resolver = new SharedVariableResolver("sharedCounter", typeof(int));

		var context1 = new GraphContext { SharedVariables = entity.SharedVariables };
		var context2 = new GraphContext { SharedVariables = entity.SharedVariables };

		resolver.Resolve(context1).AsInt().Should().Be(0);

		entity.SharedVariables.SetVar("sharedCounter", 42);

		resolver.Resolve(context1).AsInt().Should().Be(42);
		resolver.Resolve(context2).AsInt().Should().Be(42);
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_returns_first_element_on_resolve()
	{
		var resolver = new ArrayVariableResolver(
			[new Variant128(10), new Variant128(20), new Variant128(30)],
			typeof(int));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(10);
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_returns_default_when_empty()
	{
		var resolver = new ArrayVariableResolver([], typeof(int));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_get_element_returns_correct_value()
	{
		var resolver = new ArrayVariableResolver(
			[new Variant128(10), new Variant128(20), new Variant128(30)],
			typeof(int));

		resolver.GetElement(0).AsInt().Should().Be(10);
		resolver.GetElement(1).AsInt().Should().Be(20);
		resolver.GetElement(2).AsInt().Should().Be(30);
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_set_element_updates_value()
	{
		var resolver = new ArrayVariableResolver(
			[new Variant128(10), new Variant128(20)],
			typeof(int));

		resolver.SetElement(1, new Variant128(99));

		resolver.GetElement(1).AsInt().Should().Be(99);
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_add_appends_element()
	{
		var resolver = new ArrayVariableResolver(
			[new Variant128(10)],
			typeof(int));

		resolver.Add(new Variant128(20));

		resolver.Length.Should().Be(2);
		resolver.GetElement(1).AsInt().Should().Be(20);
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_remove_at_removes_element()
	{
		var resolver = new ArrayVariableResolver(
			[new Variant128(10), new Variant128(20), new Variant128(30)],
			typeof(int));

		resolver.RemoveAt(1);

		resolver.Length.Should().Be(2);
		resolver.GetElement(0).AsInt().Should().Be(10);
		resolver.GetElement(1).AsInt().Should().Be(30);
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_clear_removes_all_elements()
	{
		var resolver = new ArrayVariableResolver(
			[new Variant128(10), new Variant128(20)],
			typeof(int));

		resolver.Clear();

		resolver.Length.Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_reports_correct_value_type()
	{
		var resolver = new ArrayVariableResolver([], typeof(double));

		resolver.ValueType.Should().Be(typeof(double));
	}

	private static GraphContext CreateAbilityGraphContext(TestEntity entity)
	{
		var graph = new Graph();
		var captureNode = new CaptureGraphContextNode();

		graph.AddNode(captureNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			captureNode.InputPorts[ActionNode.InputPort]));

		var behavior = new GraphAbilityBehavior(graph);

		AbilityData abilityData = CreateAbilityData("ResolverTest", () => behavior);
		AbilityHandle? handle = Grant(entity, abilityData);
		handle!.Activate(out _);

		return captureNode.CapturedGraphContext!;
	}

	private static AbilityHandle? Grant(TestEntity target, AbilityData data)
	{
		var grantConfig = new GrantAbilityConfig(
			data,
			new ScalableInt(1),
			AbilityDeactivationPolicy.CancelImmediately,
			AbilityDeactivationPolicy.CancelImmediately,
			false,
			false,
			LevelComparison.Higher);

		var effectData = new EffectData(
			"Grant",
			new DurationData(DurationType.Infinite),
			effectComponents: [new GrantAbilityEffectComponent([grantConfig])]);

		var grantEffect = new Effect(effectData, new EffectOwnership(null, null));
		_ = target.EffectsManager.ApplyEffect(grantEffect);
		target.Abilities.TryGetAbility(data, out AbilityHandle? handle);
		return handle;
	}

	private static AbilityData CreateAbilityData(string name, Func<IAbilityBehavior> behaviorFactory)
	{
		return new AbilityData(name, behaviorFactory: behaviorFactory);
	}
}
