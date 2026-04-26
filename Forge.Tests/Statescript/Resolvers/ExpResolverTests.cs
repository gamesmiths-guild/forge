// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ExpResolverTests
{
	[Fact]
	[Trait("Resolver", "Exp")]
	public void Exp_resolver_float_value_type_is_float()
	{
		var resolver = new ExpResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Exp")]
	public void Exp_resolver_double_value_type_is_double()
	{
		var resolver = new ExpResolver(
			new VariantResolver(new Variant128(1.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Exp")]
	public void Exp_resolver_int_promotes_to_double()
	{
		var resolver = new ExpResolver(
			new VariantResolver(new Variant128(1), typeof(int)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Exp")]
	public void Exp_resolver_zero_returns_one()
	{
		// e^0 = 1
		var resolver = new ExpResolver(
			new VariantResolver(new Variant128(0.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(1.0);
	}

	[Fact]
	[Trait("Resolver", "Exp")]
	public void Exp_resolver_one_returns_e()
	{
		// e^1 = e ≈ 2.71828
		var resolver = new ExpResolver(
			new VariantResolver(new Variant128(1.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(Math.E, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Exp")]
	public void Exp_resolver_two_returns_e_squared()
	{
		// e^2 ≈ 7.38905
		var resolver = new ExpResolver(
			new VariantResolver(new Variant128(2.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(7.38905, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Exp")]
	public void Exp_resolver_negative_returns_reciprocal()
	{
		// e^-1 = 1/e ≈ 0.3679
		var resolver = new ExpResolver(
			new VariantResolver(new Variant128(-1.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(1.0 / Math.E, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Exp")]
	public void Exp_resolver_float_computation()
	{
		var resolver = new ExpResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().BeApproximately(MathF.E, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Exp")]
	public void Exp_resolver_supports_nesting()
	{
		// Exp(Exp(0)) = Exp(1) = e
		var inner = new ExpResolver(
			new VariantResolver(new Variant128(0.0), typeof(double)));

		var outer = new ExpResolver(inner);

		var context = new GraphContext();

		outer.Resolve(context).AsDouble().Should().BeApproximately(Math.E, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Exp")]
	public void Exp_resolver_throws_for_decimal_operand()
	{
#pragma warning disable CA1806
		Action act = () => new ExpResolver(
			new VariantResolver(new Variant128(1.0m), typeof(decimal)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Exp")]
	public void Exp_resolver_throws_for_vector_operand()
	{
#pragma warning disable CA1806
		Action act = () => new ExpResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Exp")]
	public void Exp_resolver_throws_for_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new ExpResolver(
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
