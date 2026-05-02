// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class PowResolverTests
{
	[Fact]
	[Trait("Resolver", "Pow")]
	public void Pow_resolver_float_operands_value_type_is_float()
	{
		var resolver = new PowResolver(
			new VariantResolver(new Variant128(2.0f), typeof(float)),
			new VariantResolver(new Variant128(3.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Pow")]
	public void Pow_resolver_double_operands_value_type_is_double()
	{
		var resolver = new PowResolver(
			new VariantResolver(new Variant128(2.0), typeof(double)),
			new VariantResolver(new Variant128(3.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Pow")]
	public void Pow_resolver_int_operands_promotes_to_double()
	{
		var resolver = new PowResolver(
			new VariantResolver(new Variant128(2), typeof(int)),
			new VariantResolver(new Variant128(3), typeof(int)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Pow")]
	public void Pow_resolver_mixed_float_and_double_promotes_to_double()
	{
		var resolver = new PowResolver(
			new VariantResolver(new Variant128(2.0f), typeof(float)),
			new VariantResolver(new Variant128(3.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Pow")]
	public void Pow_resolver_computes_float_power()
	{
		var resolver = new PowResolver(
			new VariantResolver(new Variant128(2.0f), typeof(float)),
			new VariantResolver(new Variant128(3.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(8.0f);
	}

	[Fact]
	[Trait("Resolver", "Pow")]
	public void Pow_resolver_computes_double_power()
	{
		var resolver = new PowResolver(
			new VariantResolver(new Variant128(2.0), typeof(double)),
			new VariantResolver(new Variant128(10.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(1024.0);
	}

	[Fact]
	[Trait("Resolver", "Pow")]
	public void Pow_resolver_computes_int_power_as_double()
	{
		var resolver = new PowResolver(
			new VariantResolver(new Variant128(3), typeof(int)),
			new VariantResolver(new Variant128(4), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(81.0);
	}

	[Fact]
	[Trait("Resolver", "Pow")]
	public void Pow_resolver_zero_exponent_returns_one()
	{
		var resolver = new PowResolver(
			new VariantResolver(new Variant128(5.0), typeof(double)),
			new VariantResolver(new Variant128(0.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(1.0);
	}

	[Fact]
	[Trait("Resolver", "Pow")]
	public void Pow_resolver_exponent_one_returns_base()
	{
		var resolver = new PowResolver(
			new VariantResolver(new Variant128(7.0), typeof(double)),
			new VariantResolver(new Variant128(1.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(7.0);
	}

	[Fact]
	[Trait("Resolver", "Pow")]
	public void Pow_resolver_fractional_exponent()
	{
		// 4^0.5 = 2
		var resolver = new PowResolver(
			new VariantResolver(new Variant128(4.0), typeof(double)),
			new VariantResolver(new Variant128(0.5), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(2.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Pow")]
	public void Pow_resolver_supports_nesting()
	{
		// Pow(Pow(2, 3), 2) = Pow(8, 2) = 64
		var inner = new PowResolver(
			new VariantResolver(new Variant128(2.0), typeof(double)),
			new VariantResolver(new Variant128(3.0), typeof(double)));

		var outer = new PowResolver(
			inner,
			new VariantResolver(new Variant128(2.0), typeof(double)));

		var context = new GraphContext();

		outer.Resolve(context).AsDouble().Should().Be(64.0);
	}

	[Fact]
	[Trait("Resolver", "Pow")]
	public void Pow_resolver_throws_for_decimal_operands()
	{
#pragma warning disable CA1806
		Action act = () => new PowResolver(
			new VariantResolver(new Variant128(2.0m), typeof(decimal)),
			new VariantResolver(new Variant128(3.0m), typeof(decimal)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Pow")]
	public void Pow_resolver_vector3_operands_value_type_is_vector3()
	{
		var resolver = new PowResolver(
			new VariantResolver(new Variant128(new Vector3(2.0f, 3.0f, 4.0f)), typeof(Vector3)),
			new VariantResolver(new Variant128(new Vector3(3.0f, 2.0f, 0.5f)), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	[Fact]
	[Trait("Resolver", "Pow")]
	public void Pow_resolver_vector2_computes_componentwise_power()
	{
		var resolver = new PowResolver(
			new VariantResolver(new Variant128(new Vector2(4.0f, 9.0f)), typeof(Vector2)),
			new VariantResolver(new Variant128(new Vector2(0.5f, 0.5f)), typeof(Vector2)));

		var context = new GraphContext();

		TestUtils.BeApproximately(resolver.Resolve(context).AsVector2(), new Vector2(2.0f, 3.0f));
	}

	[Fact]
	[Trait("Resolver", "Pow")]
	public void Pow_resolver_vector3_computes_componentwise_power()
	{
		var resolver = new PowResolver(
			new VariantResolver(new Variant128(new Vector3(2.0f, 3.0f, 4.0f)), typeof(Vector3)),
			new VariantResolver(new Variant128(new Vector3(3.0f, 2.0f, 0.5f)), typeof(Vector3)));

		var context = new GraphContext();

		TestUtils.BeApproximately(resolver.Resolve(context).AsVector3(), new Vector3(8.0f, 9.0f, 2.0f));
	}

	[Fact]
	[Trait("Resolver", "Pow")]
	public void Pow_resolver_vector4_computes_componentwise_power()
	{
		var resolver = new PowResolver(
			new VariantResolver(new Variant128(new Vector4(1.0f, 4.0f, 9.0f, 16.0f)), typeof(Vector4)),
			new VariantResolver(new Variant128(new Vector4(2.0f, 0.5f, 0.5f, 0.5f)), typeof(Vector4)));

		var context = new GraphContext();

		TestUtils.BeApproximately(resolver.Resolve(context).AsVector4(), new Vector4(1.0f, 2.0f, 3.0f, 4.0f));
	}

	[Fact]
	[Trait("Resolver", "Pow")]
	public void Pow_resolver_throws_for_mismatched_vector_operands()
	{
#pragma warning disable CA1806
		Action act = () => new PowResolver(
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Pow")]
	public void Pow_resolver_throws_for_bool_operands()
	{
#pragma warning disable CA1806
		Action act = () => new PowResolver(
			new VariantResolver(new Variant128(true), typeof(bool)),
			new VariantResolver(new Variant128(false), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
