// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ACosHResolverTests
{
	[Fact]
	[Trait("Resolver", "ACosH")]
	public void ACosH_resolver_float_value_type_is_float()
	{
		var resolver = new ACosHResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "ACosH")]
	public void ACosH_resolver_double_value_type_is_double()
	{
		var resolver = new ACosHResolver(
			new VariantResolver(new Variant128(1.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "ACosH")]
	public void ACosH_resolver_int_promotes_to_double()
	{
		var resolver = new ACosHResolver(
			new VariantResolver(new Variant128(1), typeof(int)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "ACosH")]
	public void ACosH_resolver_one_returns_zero()
	{
		// ACosH(1) = 0
		var resolver = new ACosHResolver(
			new VariantResolver(new Variant128(1.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(0.0, 0.0001);
	}

	[Fact]
	[Trait("Resolver", "ACosH")]
	public void ACosH_resolver_two()
	{
		// ACosH(2) ≈ 1.3170
		var resolver = new ACosHResolver(
			new VariantResolver(new Variant128(2.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(1.3170, 0.001);
	}

	[Fact]
	[Trait("Resolver", "ACosH")]
	public void ACosH_resolver_float_computation()
	{
		var resolver = new ACosHResolver(
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().BeApproximately(1.317f, 0.01f);
	}

	[Fact]
	[Trait("Resolver", "ACosH")]
	public void ACosH_resolver_throws_for_decimal_operand()
	{
#pragma warning disable CA1806
		Action act = () => new ACosHResolver(
			new VariantResolver(new Variant128(2.0m), typeof(decimal)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "ACosH")]
	public void ACosH_resolver_throws_for_vector_operand()
	{
#pragma warning disable CA1806
		Action act = () => new ACosHResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "ACosH")]
	public void ACosH_resolver_throws_for_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new ACosHResolver(
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
