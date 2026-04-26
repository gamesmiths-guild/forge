// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ATan2ResolverTests
{
	[Fact]
	[Trait("Resolver", "ATan2")]
	public void ATan2_resolver_float_operands_value_type_is_float()
	{
		var resolver = new ATan2Resolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)),
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "ATan2")]
	public void ATan2_resolver_double_operands_value_type_is_double()
	{
		var resolver = new ATan2Resolver(
			new VariantResolver(new Variant128(1.0), typeof(double)),
			new VariantResolver(new Variant128(1.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "ATan2")]
	public void ATan2_resolver_int_operands_promotes_to_double()
	{
		var resolver = new ATan2Resolver(
			new VariantResolver(new Variant128(1), typeof(int)),
			new VariantResolver(new Variant128(1), typeof(int)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "ATan2")]
	public void ATan2_resolver_one_one_returns_pi_over_4()
	{
		// ATan2(1, 1) = π/4
		var resolver = new ATan2Resolver(
			new VariantResolver(new Variant128(1.0), typeof(double)),
			new VariantResolver(new Variant128(1.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(Math.PI / 4.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "ATan2")]
	public void ATan2_resolver_zero_positive_returns_zero()
	{
		// ATan2(0, 1) = 0
		var resolver = new ATan2Resolver(
			new VariantResolver(new Variant128(0.0), typeof(double)),
			new VariantResolver(new Variant128(1.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(0.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "ATan2")]
	public void ATan2_resolver_positive_zero_returns_pi_over_2()
	{
		// ATan2(1, 0) = π/2
		var resolver = new ATan2Resolver(
			new VariantResolver(new Variant128(1.0), typeof(double)),
			new VariantResolver(new Variant128(0.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(Math.PI / 2.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "ATan2")]
	public void ATan2_resolver_negative_y_negative_x()
	{
		// ATan2(-1, -1) = -3π/4
		var resolver = new ATan2Resolver(
			new VariantResolver(new Variant128(-1.0), typeof(double)),
			new VariantResolver(new Variant128(-1.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(-3.0 * Math.PI / 4.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "ATan2")]
	public void ATan2_resolver_float_computation()
	{
		var resolver = new ATan2Resolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)),
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().BeApproximately(MathF.PI / 4.0f, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "ATan2")]
	public void ATan2_resolver_supports_nesting()
	{
		// ATan2(ATan2(1, 1), 1) = ATan2(π/4, 1) ≈ ATan2(0.7854, 1) ≈ 0.66577
		var inner = new ATan2Resolver(
			new VariantResolver(new Variant128(1.0), typeof(double)),
			new VariantResolver(new Variant128(1.0), typeof(double)));

		var outer = new ATan2Resolver(
			inner,
			new VariantResolver(new Variant128(1.0), typeof(double)));

		var context = new GraphContext();

		outer.Resolve(context).AsDouble().Should().BeApproximately(0.66577, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "ATan2")]
	public void ATan2_resolver_throws_for_decimal_operands()
	{
#pragma warning disable CA1806
		Action act = () => new ATan2Resolver(
			new VariantResolver(new Variant128(1.0m), typeof(decimal)),
			new VariantResolver(new Variant128(1.0m), typeof(decimal)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "ATan2")]
	public void ATan2_resolver_throws_for_vector_operands()
	{
#pragma warning disable CA1806
		Action act = () => new ATan2Resolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "ATan2")]
	public void ATan2_resolver_throws_for_bool_operands()
	{
#pragma warning disable CA1806
		Action act = () => new ATan2Resolver(
			new VariantResolver(new Variant128(true), typeof(bool)),
			new VariantResolver(new Variant128(false), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
