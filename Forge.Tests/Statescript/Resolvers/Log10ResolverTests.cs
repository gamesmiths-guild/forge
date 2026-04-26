// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class Log10ResolverTests
{
	[Fact]
	[Trait("Resolver", "Log10")]
	public void Log10_resolver_float_value_type_is_float()
	{
		var resolver = new Log10Resolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Log10")]
	public void Log10_resolver_double_value_type_is_double()
	{
		var resolver = new Log10Resolver(
			new VariantResolver(new Variant128(1.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Log10")]
	public void Log10_resolver_int_promotes_to_double()
	{
		var resolver = new Log10Resolver(
			new VariantResolver(new Variant128(1), typeof(int)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Log10")]
	public void Log10_resolver_one_returns_zero()
	{
		// Log10(1) = 0
		var resolver = new Log10Resolver(
			new VariantResolver(new Variant128(1.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(0.0);
	}

	[Fact]
	[Trait("Resolver", "Log10")]
	public void Log10_resolver_ten_returns_one()
	{
		// Log10(10) = 1
		var resolver = new Log10Resolver(
			new VariantResolver(new Variant128(10.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(1.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Log10")]
	public void Log10_resolver_hundred_returns_two()
	{
		// Log10(100) = 2
		var resolver = new Log10Resolver(
			new VariantResolver(new Variant128(100.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(2.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Log10")]
	public void Log10_resolver_thousand_returns_three()
	{
		// Log10(1000) = 3
		var resolver = new Log10Resolver(
			new VariantResolver(new Variant128(1000.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(3.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Log10")]
	public void Log10_resolver_float_computation()
	{
		var resolver = new Log10Resolver(
			new VariantResolver(new Variant128(100.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().BeApproximately(2.0f, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Log10")]
	public void Log10_resolver_throws_for_decimal_operand()
	{
#pragma warning disable CA1806
		Action act = () => new Log10Resolver(
			new VariantResolver(new Variant128(1.0m), typeof(decimal)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Log10")]
	public void Log10_resolver_throws_for_vector_operand()
	{
#pragma warning disable CA1806
		Action act = () => new Log10Resolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Log10")]
	public void Log10_resolver_throws_for_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new Log10Resolver(
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
