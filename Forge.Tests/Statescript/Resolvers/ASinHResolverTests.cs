// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ASinHResolverTests
{
	[Fact]
	[Trait("Resolver", "ASinH")]
	public void ASinH_resolver_float_value_type_is_float()
	{
		var resolver = new ASinHResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "ASinH")]
	public void ASinH_resolver_double_value_type_is_double()
	{
		var resolver = new ASinHResolver(
			new VariantResolver(new Variant128(1.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "ASinH")]
	public void ASinH_resolver_int_promotes_to_double()
	{
		var resolver = new ASinHResolver(
			new VariantResolver(new Variant128(0), typeof(int)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "ASinH")]
	public void ASinH_resolver_zero_returns_zero()
	{
		// ASinH(0) = 0
		var resolver = new ASinHResolver(
			new VariantResolver(new Variant128(0.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(0.0, 0.0001);
	}

	[Fact]
	[Trait("Resolver", "ASinH")]
	public void ASinH_resolver_one()
	{
		// ASinH(1) ≈ 0.8814
		var resolver = new ASinHResolver(
			new VariantResolver(new Variant128(1.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(0.8814, 0.001);
	}

	[Fact]
	[Trait("Resolver", "ASinH")]
	public void ASinH_resolver_negative_value()
	{
		// ASinH is an odd function: ASinH(-1) ≈ -0.8814
		var resolver = new ASinHResolver(
			new VariantResolver(new Variant128(-1.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(-0.8814, 0.001);
	}

	[Fact]
	[Trait("Resolver", "ASinH")]
	public void ASinH_resolver_float_computation()
	{
		var resolver = new ASinHResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().BeApproximately(0.8814f, 0.01f);
	}

	[Fact]
	[Trait("Resolver", "ASinH")]
	public void ASinH_resolver_throws_for_decimal_operand()
	{
#pragma warning disable CA1806
		Action act = () => new ASinHResolver(
			new VariantResolver(new Variant128(1.0m), typeof(decimal)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "ASinH")]
	public void ASinH_resolver_throws_for_vector_operand()
	{
#pragma warning disable CA1806
		Action act = () => new ASinHResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "ASinH")]
	public void ASinH_resolver_throws_for_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new ASinHResolver(
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
