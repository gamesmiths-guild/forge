// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ATanResolverTests
{
	[Fact]
	[Trait("Resolver", "ATan")]
	public void ATan_resolver_float_value_type_is_float()
	{
		var resolver = new ATanResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "ATan")]
	public void ATan_resolver_double_value_type_is_double()
	{
		var resolver = new ATanResolver(
			new VariantResolver(new Variant128(1.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "ATan")]
	public void ATan_resolver_int_promotes_to_double()
	{
		var resolver = new ATanResolver(
			new VariantResolver(new Variant128(1), typeof(int)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "ATan")]
	public void ATan_resolver_zero_returns_zero()
	{
		var resolver = new ATanResolver(
			new VariantResolver(new Variant128(0.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(0.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "ATan")]
	public void ATan_resolver_one_returns_pi_over_4()
	{
		// ATan(1) = π/4
		var resolver = new ATanResolver(
			new VariantResolver(new Variant128(1.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(Math.PI / 4.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "ATan")]
	public void ATan_resolver_negative_one_returns_negative_pi_over_4()
	{
		var resolver = new ATanResolver(
			new VariantResolver(new Variant128(-1.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(-Math.PI / 4.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "ATan")]
	public void ATan_resolver_float_precision()
	{
		var resolver = new ATanResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().BeApproximately(MathF.PI / 4.0f, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "ATan")]
	public void ATan_resolver_supports_nesting()
	{
		// ATan(ATan(1.0)) = ATan(π/4) ≈ ATan(0.7854) ≈ 0.66577
		var inner = new ATanResolver(
			new VariantResolver(new Variant128(1.0), typeof(double)));

		var outer = new ATanResolver(inner);

		var context = new GraphContext();

		outer.Resolve(context).AsDouble().Should().BeApproximately(0.66577, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "ATan")]
	public void ATan_resolver_throws_for_decimal_operand()
	{
#pragma warning disable CA1806
		Action act = () => new ATanResolver(
			new VariantResolver(new Variant128(1.0m), typeof(decimal)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "ATan")]
	public void ATan_resolver_throws_for_vector_operand()
	{
#pragma warning disable CA1806
		Action act = () => new ATanResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "ATan")]
	public void ATan_resolver_throws_for_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new ATanResolver(
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
