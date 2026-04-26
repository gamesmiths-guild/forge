// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class AndResolverTests
{
	[Fact]
	[Trait("Resolver", "And")]
	public void And_resolver_value_type_is_bool()
	{
		var resolver = new AndResolver(
			new VariantResolver(new Variant128(true), typeof(bool)),
			new VariantResolver(new Variant128(false), typeof(bool)));

		resolver.ValueType.Should().Be(typeof(bool));
	}

	[Fact]
	[Trait("Resolver", "And")]
	public void And_resolver_returns_true_when_both_operands_are_true()
	{
		var resolver = new AndResolver(
			new VariantResolver(new Variant128(true), typeof(bool)),
			new VariantResolver(new Variant128(true), typeof(bool)));

		resolver.Resolve(new GraphContext()).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "And")]
	public void And_resolver_returns_false_when_left_operand_is_false()
	{
		var resolver = new AndResolver(
			new VariantResolver(new Variant128(false), typeof(bool)),
			new VariantResolver(new Variant128(true), typeof(bool)));

		resolver.Resolve(new GraphContext()).AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "And")]
	public void And_resolver_returns_false_when_right_operand_is_false()
	{
		var resolver = new AndResolver(
			new VariantResolver(new Variant128(true), typeof(bool)),
			new VariantResolver(new Variant128(false), typeof(bool)));

		resolver.Resolve(new GraphContext()).AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "And")]
	public void And_resolver_supports_nested_boolean_resolvers()
	{
		var resolver = new AndResolver(
			new ComparisonResolver(
				new VariantResolver(new Variant128(10), typeof(int)),
				ComparisonOperation.GreaterThan,
				new VariantResolver(new Variant128(5), typeof(int))),
			new NotResolver(
				new ComparisonResolver(
					new VariantResolver(new Variant128(3), typeof(int)),
					ComparisonOperation.Equal,
					new VariantResolver(new Variant128(4), typeof(int)))));

		resolver.Resolve(new GraphContext()).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "And")]
	public void And_resolver_resolves_both_operands()
	{
		var left = new CountingBoolResolver(false);
		var right = new CountingBoolResolver(true);
		var resolver = new AndResolver(left, right);

		resolver.Resolve(new GraphContext()).AsBool().Should().BeFalse();
		left.ResolveCount.Should().Be(1);
		right.ResolveCount.Should().Be(1);
	}

	[Fact]
	[Trait("Resolver", "And")]
	public void And_resolver_throws_for_non_bool_left_operand()
	{
#pragma warning disable CA1806
		Action act = () => new AndResolver(
			new VariantResolver(new Variant128(1), typeof(int)),
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "And")]
	public void And_resolver_throws_for_non_bool_right_operand()
	{
#pragma warning disable CA1806
		Action act = () => new AndResolver(
			new VariantResolver(new Variant128(true), typeof(bool)),
			new VariantResolver(new Variant128(1), typeof(int)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	private sealed class CountingBoolResolver(bool value) : IPropertyResolver
	{
		public int ResolveCount { get; private set; }

		public Type ValueType => typeof(bool);

		public Variant128 Resolve(GraphContext graphContext)
		{
			ResolveCount++;
			return new Variant128(value);
		}
	}
}
