// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class Log2ResolverTests
{
	[Fact]
	[Trait("Resolver", "Log2")]
	public void Log2_resolver_float_value_type_is_float()
	{
		var resolver = new Log2Resolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Log2")]
	public void Log2_resolver_double_value_type_is_double()
	{
		var resolver = new Log2Resolver(
			new VariantResolver(new Variant128(1.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Log2")]
	public void Log2_resolver_int_promotes_to_double()
	{
		var resolver = new Log2Resolver(
			new VariantResolver(new Variant128(1), typeof(int)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Log2")]
	public void Log2_resolver_one_returns_zero()
	{
		// Log2(1) = 0
		var resolver = new Log2Resolver(
			new VariantResolver(new Variant128(1.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(0.0);
	}

	[Fact]
	[Trait("Resolver", "Log2")]
	public void Log2_resolver_two_returns_one()
	{
		// Log2(2) = 1
		var resolver = new Log2Resolver(
			new VariantResolver(new Variant128(2.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(1.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Log2")]
	public void Log2_resolver_eight_returns_three()
	{
		// Log2(8) = 3
		var resolver = new Log2Resolver(
			new VariantResolver(new Variant128(8.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(3.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Log2")]
	public void Log2_resolver_1024_returns_10()
	{
		// Log2(1024) = 10
		var resolver = new Log2Resolver(
			new VariantResolver(new Variant128(1024.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(10.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Log2")]
	public void Log2_resolver_float_computation()
	{
		var resolver = new Log2Resolver(
			new VariantResolver(new Variant128(4.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().BeApproximately(2.0f, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Log2")]
	public void Log2_resolver_throws_for_decimal_operand()
	{
#pragma warning disable CA1806
		Action act = () => new Log2Resolver(
			new VariantResolver(new Variant128(1.0m), typeof(decimal)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Log2")]
	public void Log2_resolver_throws_for_vector_operand()
	{
#pragma warning disable CA1806
		Action act = () => new Log2Resolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Log2")]
	public void Log2_resolver_throws_for_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new Log2Resolver(
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
