// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class NotResolverTests
{
	[Fact]
	[Trait("Resolver", "Not")]
	public void Not_resolver_value_type_is_bool()
	{
		var resolver = new NotResolver(
			new VariantResolver(new Variant128(true), typeof(bool)));

		resolver.ValueType.Should().Be(typeof(bool));
	}

	[Fact]
	[Trait("Resolver", "Not")]
	public void Not_resolver_returns_true_when_operand_is_false()
	{
		var resolver = new NotResolver(
			new VariantResolver(new Variant128(false), typeof(bool)));

		resolver.Resolve(new GraphContext()).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "Not")]
	public void Not_resolver_returns_false_when_operand_is_true()
	{
		var resolver = new NotResolver(
			new VariantResolver(new Variant128(true), typeof(bool)));

		resolver.Resolve(new GraphContext()).AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "Not")]
	public void Not_resolver_supports_nested_boolean_resolvers()
	{
		var resolver = new NotResolver(
			new ComparisonResolver(
				new VariantResolver(new Variant128(3), typeof(int)),
				ComparisonOperation.GreaterThan,
				new VariantResolver(new Variant128(10), typeof(int))));

		resolver.Resolve(new GraphContext()).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "Not")]
	public void Not_resolver_double_negation_returns_original_value()
	{
		var resolver = new NotResolver(
			new NotResolver(
				new VariantResolver(new Variant128(true), typeof(bool))));

		resolver.Resolve(new GraphContext()).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "Not")]
	public void Not_resolver_throws_for_non_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new NotResolver(
			new VariantResolver(new Variant128(1), typeof(int)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
