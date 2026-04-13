// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class TruncateResolverTests
{
	[Fact]
	[Trait("Resolver", "Truncate")]
	public void Truncate_resolver_float_value_type_is_float()
	{
		var resolver = new TruncateResolver(
			new VariantResolver(new Variant128(2.7f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Truncate")]
	public void Truncate_resolver_double_value_type_is_double()
	{
		var resolver = new TruncateResolver(
			new VariantResolver(new Variant128(2.7), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Truncate")]
	public void Truncate_resolver_decimal_value_type_is_decimal()
	{
		var resolver = new TruncateResolver(
			new VariantResolver(new Variant128(2.7m), typeof(decimal)));

		resolver.ValueType.Should().Be(typeof(decimal));
	}

	[Fact]
	[Trait("Resolver", "Truncate")]
	public void Truncate_resolver_positive_float()
	{
		var resolver = new TruncateResolver(
			new VariantResolver(new Variant128(2.7f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(2.0f);
	}

	[Fact]
	[Trait("Resolver", "Truncate")]
	public void Truncate_resolver_negative_float_rounds_toward_zero()
	{
		var resolver = new TruncateResolver(
			new VariantResolver(new Variant128(-2.7f), typeof(float)));

		var context = new GraphContext();

		// Truncate rounds toward zero: -2.7 → -2, not -3
		resolver.Resolve(context).AsFloat().Should().Be(-2.0f);
	}

	[Fact]
	[Trait("Resolver", "Truncate")]
	public void Truncate_resolver_positive_double()
	{
		var resolver = new TruncateResolver(
			new VariantResolver(new Variant128(3.99), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(3.0);
	}

	[Fact]
	[Trait("Resolver", "Truncate")]
	public void Truncate_resolver_negative_double_rounds_toward_zero()
	{
		var resolver = new TruncateResolver(
			new VariantResolver(new Variant128(-3.99), typeof(double)));

		var context = new GraphContext();

		// Truncate rounds toward zero: -3.99 → -3, not -4
		resolver.Resolve(context).AsDouble().Should().Be(-3.0);
	}

	[Fact]
	[Trait("Resolver", "Truncate")]
	public void Truncate_resolver_positive_decimal()
	{
		var resolver = new TruncateResolver(
			new VariantResolver(new Variant128(5.9m), typeof(decimal)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDecimal().Should().Be(5.0m);
	}

	[Fact]
	[Trait("Resolver", "Truncate")]
	public void Truncate_resolver_negative_decimal_rounds_toward_zero()
	{
		var resolver = new TruncateResolver(
			new VariantResolver(new Variant128(-5.9m), typeof(decimal)));

		var context = new GraphContext();

		// Truncate rounds toward zero: -5.9 → -5, not -6
		resolver.Resolve(context).AsDecimal().Should().Be(-5.0m);
	}

	[Fact]
	[Trait("Resolver", "Truncate")]
	public void Truncate_resolver_whole_number_returns_same()
	{
		var resolver = new TruncateResolver(
			new VariantResolver(new Variant128(5.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(5.0);
	}

	[Fact]
	[Trait("Resolver", "Truncate")]
	public void Truncate_resolver_small_positive_fraction()
	{
		var resolver = new TruncateResolver(
			new VariantResolver(new Variant128(0.9), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(0.0);
	}

	[Fact]
	[Trait("Resolver", "Truncate")]
	public void Truncate_resolver_small_negative_fraction()
	{
		var resolver = new TruncateResolver(
			new VariantResolver(new Variant128(-0.9), typeof(double)));

		var context = new GraphContext();

		// Truncate rounds toward zero: -0.9 → 0, not -1
		resolver.Resolve(context).AsDouble().Should().Be(0.0);
	}

	[Fact]
	[Trait("Resolver", "Truncate")]
	public void Truncate_resolver_supports_nesting()
	{
		// Truncate(2.7 + 1.1) = Truncate(3.8) = 3.0
		var sum = new AddResolver(
			new VariantResolver(new Variant128(2.7), typeof(double)),
			new VariantResolver(new Variant128(1.1), typeof(double)));

		var resolver = new TruncateResolver(sum);

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(3.0);
	}

	[Fact]
	[Trait("Resolver", "Truncate")]
	public void Truncate_resolver_throws_for_int_operand()
	{
#pragma warning disable CA1806
		Action act = () => new TruncateResolver(
			new VariantResolver(new Variant128(5), typeof(int)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Truncate")]
	public void Truncate_resolver_throws_for_vector_operand()
	{
#pragma warning disable CA1806
		Action act = () => new TruncateResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Truncate")]
	public void Truncate_resolver_throws_for_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new TruncateResolver(
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
