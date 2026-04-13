// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ACosResolverTests
{
	[Fact]
	[Trait("Resolver", "ACos")]
	public void ACos_resolver_float_value_type_is_float()
	{
		var resolver = new ACosResolver(
			new VariantResolver(new Variant128(0.5f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "ACos")]
	public void ACos_resolver_double_value_type_is_double()
	{
		var resolver = new ACosResolver(
			new VariantResolver(new Variant128(0.5), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "ACos")]
	public void ACos_resolver_int_promotes_to_double()
	{
		var resolver = new ACosResolver(
			new VariantResolver(new Variant128(1), typeof(int)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "ACos")]
	public void ACos_resolver_one_returns_zero()
	{
		// ACos(1) = 0
		var resolver = new ACosResolver(
			new VariantResolver(new Variant128(1.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(0.0, 0.0001);
	}

	[Fact]
	[Trait("Resolver", "ACos")]
	public void ACos_resolver_zero_returns_pi_over_2()
	{
		// ACos(0) = π/2
		var resolver = new ACosResolver(
			new VariantResolver(new Variant128(0.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(Math.PI / 2.0, 0.0001);
	}

	[Fact]
	[Trait("Resolver", "ACos")]
	public void ACos_resolver_negative_one_returns_pi()
	{
		// ACos(-1) = π
		var resolver = new ACosResolver(
			new VariantResolver(new Variant128(-1.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(Math.PI, 0.0001);
	}

	[Fact]
	[Trait("Resolver", "ACos")]
	public void ACos_resolver_half_returns_pi_over_3()
	{
		// ACos(0.5) = π/3
		var resolver = new ACosResolver(
			new VariantResolver(new Variant128(0.5), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(Math.PI / 3.0, 0.0001);
	}

	[Fact]
	[Trait("Resolver", "ACos")]
	public void ACos_resolver_float_computation()
	{
		var resolver = new ACosResolver(
			new VariantResolver(new Variant128(0.5f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().BeApproximately(MathF.PI / 3.0f, 0.001f);
	}

	[Fact]
	[Trait("Resolver", "ACos")]
	public void ACos_resolver_throws_for_decimal_operand()
	{
#pragma warning disable CA1806
		Action act = () => new ACosResolver(
			new VariantResolver(new Variant128(0.5m), typeof(decimal)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "ACos")]
	public void ACos_resolver_throws_for_vector_operand()
	{
#pragma warning disable CA1806
		Action act = () => new ACosResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "ACos")]
	public void ACos_resolver_throws_for_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new ACosResolver(
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
