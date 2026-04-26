// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class SinResolverTests
{
	[Fact]
	[Trait("Resolver", "Sin")]
	public void Sin_resolver_float_value_type_is_float()
	{
		var resolver = new SinResolver(
			new VariantResolver(new Variant128(0.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Sin")]
	public void Sin_resolver_double_value_type_is_double()
	{
		var resolver = new SinResolver(
			new VariantResolver(new Variant128(0.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Sin")]
	public void Sin_resolver_int_promotes_to_double()
	{
		var resolver = new SinResolver(
			new VariantResolver(new Variant128(0), typeof(int)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Sin")]
	public void Sin_resolver_zero_returns_zero()
	{
		var resolver = new SinResolver(
			new VariantResolver(new Variant128(0.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().BeApproximately(0.0f, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Sin")]
	public void Sin_resolver_pi_over_2_returns_one()
	{
		var resolver = new SinResolver(
			new VariantResolver(new Variant128(Math.PI / 2.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(1.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Sin")]
	public void Sin_resolver_pi_returns_zero()
	{
		var resolver = new SinResolver(
			new VariantResolver(new Variant128(Math.PI), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(0.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Sin")]
	public void Sin_resolver_float_pi_over_6()
	{
		// Sin(π/6) = 0.5
		var resolver = new SinResolver(
			new VariantResolver(new Variant128(MathF.PI / 6.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().BeApproximately(0.5f, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Sin")]
	public void Sin_resolver_supports_nesting()
	{
		// Sin(Sin(π/2)) = Sin(1.0) ≈ 0.84147
		var inner = new SinResolver(
			new VariantResolver(new Variant128(Math.PI / 2.0), typeof(double)));

		var outer = new SinResolver(inner);

		var context = new GraphContext();

		outer.Resolve(context).AsDouble().Should().BeApproximately(0.84147, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Sin")]
	public void Sin_resolver_throws_for_decimal_operand()
	{
#pragma warning disable CA1806
		Action act = () => new SinResolver(
			new VariantResolver(new Variant128(0.0m), typeof(decimal)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Sin")]
	public void Sin_resolver_throws_for_vector_operand()
	{
#pragma warning disable CA1806
		Action act = () => new SinResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Sin")]
	public void Sin_resolver_throws_for_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new SinResolver(
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
