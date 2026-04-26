// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class CosHResolverTests
{
	[Fact]
	[Trait("Resolver", "CosH")]
	public void CosH_resolver_float_value_type_is_float()
	{
		var resolver = new CosHResolver(
			new VariantResolver(new Variant128(0.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "CosH")]
	public void CosH_resolver_double_value_type_is_double()
	{
		var resolver = new CosHResolver(
			new VariantResolver(new Variant128(0.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "CosH")]
	public void CosH_resolver_int_promotes_to_double()
	{
		var resolver = new CosHResolver(
			new VariantResolver(new Variant128(0), typeof(int)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "CosH")]
	public void CosH_resolver_zero_returns_one()
	{
		// CosH(0) = 1
		var resolver = new CosHResolver(
			new VariantResolver(new Variant128(0.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(1.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "CosH")]
	public void CosH_resolver_one()
	{
		// CosH(1) ≈ 1.5431
		var resolver = new CosHResolver(
			new VariantResolver(new Variant128(1.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(1.54308, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "CosH")]
	public void CosH_resolver_is_even_function()
	{
		// CosH(-1) = CosH(1) ≈ 1.5431
		var resolver = new CosHResolver(
			new VariantResolver(new Variant128(-1.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(1.54308, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "CosH")]
	public void CosH_resolver_float_computation()
	{
		var resolver = new CosHResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().BeApproximately(1.54308f, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "CosH")]
	public void CosH_resolver_throws_for_decimal_operand()
	{
#pragma warning disable CA1806
		Action act = () => new CosHResolver(
			new VariantResolver(new Variant128(0.0m), typeof(decimal)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "CosH")]
	public void CosH_resolver_throws_for_vector_operand()
	{
#pragma warning disable CA1806
		Action act = () => new CosHResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "CosH")]
	public void CosH_resolver_throws_for_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new CosHResolver(
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
