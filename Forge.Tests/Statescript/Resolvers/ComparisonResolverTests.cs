// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.ResolverTestContextFactory;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ComparisonResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

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
	[Trait("Resolver", "Comparison")]
	public void Comparison_resolver_supports_different_variant_types()
	{
		var resolver = new ComparisonResolver(
			new VariantResolver(new Variant128(20), typeof(int)),
			ComparisonOperation.GreaterThan,
			new VariantResolver(new Variant128(10.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}
}
