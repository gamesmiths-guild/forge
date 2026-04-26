// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class TanResolverTests
{
	[Fact]
	[Trait("Resolver", "Tan")]
	public void Tan_resolver_float_value_type_is_float()
	{
		var resolver = new TanResolver(
			new VariantResolver(new Variant128(0.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Tan")]
	public void Tan_resolver_double_value_type_is_double()
	{
		var resolver = new TanResolver(
			new VariantResolver(new Variant128(0.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Tan")]
	public void Tan_resolver_int_promotes_to_double()
	{
		var resolver = new TanResolver(
			new VariantResolver(new Variant128(0), typeof(int)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Tan")]
	public void Tan_resolver_zero_returns_zero()
	{
		var resolver = new TanResolver(
			new VariantResolver(new Variant128(0.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().BeApproximately(0.0f, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Tan")]
	public void Tan_resolver_pi_over_4_returns_one()
	{
		// Tan(π/4) = 1.0
		var resolver = new TanResolver(
			new VariantResolver(new Variant128(Math.PI / 4.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(1.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Tan")]
	public void Tan_resolver_pi_returns_zero()
	{
		var resolver = new TanResolver(
			new VariantResolver(new Variant128(Math.PI), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(0.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Tan")]
	public void Tan_resolver_negative_pi_over_4()
	{
		// Tan(-π/4) = -1.0
		var resolver = new TanResolver(
			new VariantResolver(new Variant128(-Math.PI / 4.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(-1.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Tan")]
	public void Tan_resolver_supports_nesting()
	{
		// Tan(Tan(0.5)) — just verify no crash and reasonable value
		var inner = new TanResolver(
			new VariantResolver(new Variant128(0.5), typeof(double)));

		var outer = new TanResolver(inner);

		var context = new GraphContext();

		// Tan(0.5) ≈ 0.5463, Tan(0.5463) ≈ 0.60802
		outer.Resolve(context).AsDouble().Should().BeApproximately(0.60802, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Tan")]
	public void Tan_resolver_throws_for_decimal_operand()
	{
#pragma warning disable CA1806
		Action act = () => new TanResolver(
			new VariantResolver(new Variant128(0.0m), typeof(decimal)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Tan")]
	public void Tan_resolver_throws_for_vector_operand()
	{
#pragma warning disable CA1806
		Action act = () => new TanResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Tan")]
	public void Tan_resolver_throws_for_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new TanResolver(
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
