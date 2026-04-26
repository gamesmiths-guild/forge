// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class CbrtResolverTests
{
	[Fact]
	[Trait("Resolver", "Cbrt")]
	public void Cbrt_resolver_float_value_type_is_float()
	{
		var resolver = new CbrtResolver(
			new VariantResolver(new Variant128(8.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Cbrt")]
	public void Cbrt_resolver_double_value_type_is_double()
	{
		var resolver = new CbrtResolver(
			new VariantResolver(new Variant128(8.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Cbrt")]
	public void Cbrt_resolver_int_promotes_to_double()
	{
		var resolver = new CbrtResolver(
			new VariantResolver(new Variant128(8), typeof(int)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Cbrt")]
	public void Cbrt_resolver_computes_float_cbrt()
	{
		var resolver = new CbrtResolver(
			new VariantResolver(new Variant128(27.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().BeApproximately(3.0f, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Cbrt")]
	public void Cbrt_resolver_computes_double_cbrt()
	{
		var resolver = new CbrtResolver(
			new VariantResolver(new Variant128(125.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(5.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Cbrt")]
	public void Cbrt_resolver_computes_int_cbrt_as_double()
	{
		var resolver = new CbrtResolver(
			new VariantResolver(new Variant128(64), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(4.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Cbrt")]
	public void Cbrt_resolver_negative_value()
	{
		// Cbrt(-8) = -2
		var resolver = new CbrtResolver(
			new VariantResolver(new Variant128(-8.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(-2.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Cbrt")]
	public void Cbrt_resolver_zero_returns_zero()
	{
		var resolver = new CbrtResolver(
			new VariantResolver(new Variant128(0.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(0.0);
	}

	[Fact]
	[Trait("Resolver", "Cbrt")]
	public void Cbrt_resolver_one_returns_one()
	{
		var resolver = new CbrtResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().BeApproximately(1.0f, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Cbrt")]
	public void Cbrt_resolver_supports_nesting()
	{
		// Cbrt(Cbrt(729)) = Cbrt(9) ≈ 2.0801 — wait: Cbrt(729)=9, Cbrt(9)≈2.08008
		var inner = new CbrtResolver(
			new VariantResolver(new Variant128(729.0), typeof(double)));

		var outer = new CbrtResolver(inner);

		var context = new GraphContext();

		outer.Resolve(context).AsDouble().Should().BeApproximately(2.08008, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Cbrt")]
	public void Cbrt_resolver_throws_for_decimal_operand()
	{
#pragma warning disable CA1806
		Action act = () => new CbrtResolver(
			new VariantResolver(new Variant128(8.0m), typeof(decimal)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Cbrt")]
	public void Cbrt_resolver_throws_for_vector_operand()
	{
#pragma warning disable CA1806
		Action act = () => new CbrtResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Cbrt")]
	public void Cbrt_resolver_throws_for_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new CbrtResolver(
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
