// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class CosResolverTests
{
	[Fact]
	[Trait("Resolver", "Cos")]
	public void Cos_resolver_float_value_type_is_float()
	{
		var resolver = new CosResolver(
			new VariantResolver(new Variant128(0.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Cos")]
	public void Cos_resolver_double_value_type_is_double()
	{
		var resolver = new CosResolver(
			new VariantResolver(new Variant128(0.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Cos")]
	public void Cos_resolver_int_promotes_to_double()
	{
		var resolver = new CosResolver(
			new VariantResolver(new Variant128(0), typeof(int)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Cos")]
	public void Cos_resolver_zero_returns_one()
	{
		var resolver = new CosResolver(
			new VariantResolver(new Variant128(0.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().BeApproximately(1.0f, 0.001f);
	}

	[Fact]
	[Trait("Resolver", "Cos")]
	public void Cos_resolver_pi_over_2_returns_zero()
	{
		var resolver = new CosResolver(
			new VariantResolver(new Variant128(Math.PI / 2.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(0.0, 0.0001);
	}

	[Fact]
	[Trait("Resolver", "Cos")]
	public void Cos_resolver_pi_returns_negative_one()
	{
		var resolver = new CosResolver(
			new VariantResolver(new Variant128(Math.PI), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(-1.0, 0.0001);
	}

	[Fact]
	[Trait("Resolver", "Cos")]
	public void Cos_resolver_float_pi_over_3()
	{
		// Cos(π/3) = 0.5
		var resolver = new CosResolver(
			new VariantResolver(new Variant128(MathF.PI / 3.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().BeApproximately(0.5f, 0.001f);
	}

	[Fact]
	[Trait("Resolver", "Cos")]
	public void Cos_resolver_supports_nesting()
	{
		// Cos(Cos(0)) = Cos(1.0) ≈ 0.5403
		var inner = new CosResolver(
			new VariantResolver(new Variant128(0.0), typeof(double)));

		var outer = new CosResolver(inner);

		var context = new GraphContext();

		outer.Resolve(context).AsDouble().Should().BeApproximately(0.5403, 0.001);
	}

	[Fact]
	[Trait("Resolver", "Cos")]
	public void Cos_resolver_throws_for_decimal_operand()
	{
#pragma warning disable CA1806
		Action act = () => new CosResolver(
			new VariantResolver(new Variant128(0.0m), typeof(decimal)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Cos")]
	public void Cos_resolver_throws_for_vector_operand()
	{
#pragma warning disable CA1806
		Action act = () => new CosResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Cos")]
	public void Cos_resolver_throws_for_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new CosResolver(
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
