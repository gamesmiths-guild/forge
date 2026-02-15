// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
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

		var context = new TestGraphContext { Owner = entity };

		Variant128 result = resolver.Resolve(context);

		result.AsInt().Should().Be(5);
	}

	[Fact]
	[Trait("Resolver", "Attribute")]
	public void Attribute_resolver_returns_default_for_missing_attribute()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var resolver = new AttributeResolver("TestAttributeSet.NonExistent");

		var context = new TestGraphContext { Owner = entity };

		Variant128 result = resolver.Resolve(context);

		result.AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Attribute")]
	public void Attribute_resolver_returns_default_when_owner_is_null()
	{
		var resolver = new AttributeResolver("TestAttributeSet.Attribute5");

		var context = new TestGraphContext { Owner = null };

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

		var context = new TestGraphContext { Owner = entity };

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

		var context = new TestGraphContext { Owner = entity };

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

		var context = new TestGraphContext { Owner = entity };

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "Tag")]
	public void Tag_resolver_returns_false_when_owner_is_null()
	{
		var tag = Tag.RequestTag(_tagsManager, "enemy.undead.zombie");
		var resolver = new TagResolver(tag);

		var context = new TestGraphContext { Owner = null };

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

		var context = new TestGraphContext { Owner = entity };

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "Variant")]
	public void Variant_resolver_returns_stored_value()
	{
		var resolver = new VariantResolver(new Variant128(42.0), typeof(double));

		var context = new TestGraphContext();

		Variant128 result = resolver.Resolve(context);

		result.AsDouble().Should().Be(42.0);
	}

	[Fact]
	[Trait("Resolver", "Variant")]
	public void Variant_resolver_value_can_be_updated()
	{
		var resolver = new VariantResolver(new Variant128(10), typeof(int));

		resolver.Set(25);

		var context = new TestGraphContext();
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

		var context = new TestGraphContext();
		context.GraphVariables.InitializeFrom(graph.VariableDefinitions);

		var resolver = new VariableResolver("speed");

		Variant128 result = resolver.Resolve(context);

		result.AsDouble().Should().Be(7.5);
	}

	[Fact]
	[Trait("Resolver", "Variable")]
	public void Variable_resolver_returns_default_for_missing_variable()
	{
		var graph = new Graph();

		var context = new TestGraphContext();
		context.GraphVariables.InitializeFrom(graph.VariableDefinitions);

		var resolver = new VariableResolver("nonexistent");

		Variant128 result = resolver.Resolve(context);

		result.AsDouble().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Variable")]
	public void Variable_resolver_reflects_runtime_variable_changes()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("counter", 0);

		var context = new TestGraphContext();
		context.GraphVariables.InitializeFrom(graph.VariableDefinitions);

		var resolver = new VariableResolver("counter");

		resolver.Resolve(context).AsInt().Should().Be(0);

		context.GraphVariables.SetVar("counter", 42);

		resolver.Resolve(context).AsInt().Should().Be(42);
	}

	[Fact]
	[Trait("Resolver", "Variable")]
	public void Variable_resolver_value_type_is_double()
	{
		var resolver = new VariableResolver("anything");

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

		var context = new TestGraphContext();

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

		var context = new TestGraphContext();

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

		var context = new TestGraphContext();

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

		var context = new TestGraphContext();

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

		var context = new TestGraphContext();

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

		var context = new TestGraphContext();

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

		var context = new TestGraphContext();

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

		var context = new TestGraphContext { Owner = entity };

		resolver.Resolve(context).AsBool().Should().BeTrue("Attribute5 (5) > 3");
	}
}
