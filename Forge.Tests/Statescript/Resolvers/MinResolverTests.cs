// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class MinResolverTests
{
	[Fact]
	[Trait("Resolver", "Min")]
	public void Min_resolver_int_and_int_value_type_is_int()
	{
		var resolver = new MinResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(5), typeof(int)));

		resolver.ValueType.Should().Be(typeof(int));
	}

	[Fact]
	[Trait("Resolver", "Min")]
	public void Min_resolver_int_and_double_promotes_to_double()
	{
		var resolver = new MinResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(5.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Min")]
	public void Min_resolver_returns_smaller_int()
	{
		var resolver = new MinResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(3), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(3);
	}

	[Fact]
	[Trait("Resolver", "Min")]
	public void Min_resolver_returns_smaller_when_left_is_smaller()
	{
		var resolver = new MinResolver(
			new VariantResolver(new Variant128(2), typeof(int)),
			new VariantResolver(new Variant128(8), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(2);
	}

	[Fact]
	[Trait("Resolver", "Min")]
	public void Min_resolver_equal_values_returns_that_value()
	{
		var resolver = new MinResolver(
			new VariantResolver(new Variant128(7), typeof(int)),
			new VariantResolver(new Variant128(7), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(7);
	}

	[Fact]
	[Trait("Resolver", "Min")]
	public void Min_resolver_negative_values()
	{
		var resolver = new MinResolver(
			new VariantResolver(new Variant128(-3), typeof(int)),
			new VariantResolver(new Variant128(-10), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(-10);
	}

	[Fact]
	[Trait("Resolver", "Min")]
	public void Min_resolver_computes_double_min()
	{
		var resolver = new MinResolver(
			new VariantResolver(new Variant128(3.14), typeof(double)),
			new VariantResolver(new Variant128(2.71), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(2.71);
	}

	[Fact]
	[Trait("Resolver", "Min")]
	public void Min_resolver_computes_float_min()
	{
		var resolver = new MinResolver(
			new VariantResolver(new Variant128(5.0f), typeof(float)),
			new VariantResolver(new Variant128(3.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(3.0f);
	}

	[Fact]
	[Trait("Resolver", "Min")]
	public void Min_resolver_computes_long_min()
	{
		var resolver = new MinResolver(
			new VariantResolver(new Variant128(100L), typeof(long)),
			new VariantResolver(new Variant128(50L), typeof(long)));

		var context = new GraphContext();

		resolver.Resolve(context).AsLong().Should().Be(50L);
	}

	[Fact]
	[Trait("Resolver", "Min")]
	public void Min_resolver_computes_decimal_min()
	{
		var resolver = new MinResolver(
			new VariantResolver(new Variant128(10.5m), typeof(decimal)),
			new VariantResolver(new Variant128(3.2m), typeof(decimal)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDecimal().Should().Be(3.2m);
	}

	[Fact]
	[Trait("Resolver", "Min")]
	public void Min_resolver_mixed_int_and_double()
	{
		var resolver = new MinResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(5.5), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(5.5);
	}

	[Fact]
	[Trait("Resolver", "Min")]
	public void Min_resolver_supports_nesting()
	{
		// Min(Min(10, 5), 3) = Min(5, 3) = 3
		var inner = new MinResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(5), typeof(int)));

		var outer = new MinResolver(
			inner,
			new VariantResolver(new Variant128(3), typeof(int)));

		var context = new GraphContext();

		outer.Resolve(context).AsInt().Should().Be(3);
	}

	[Fact]
	[Trait("Resolver", "Min")]
	public void Min_resolver_vector3_value_type_is_vector3()
	{
		var resolver = new MinResolver(
			new VariantResolver(new Variant128(new Vector3(1.0f, 5.0f, 3.0f)), typeof(Vector3)),
			new VariantResolver(new Variant128(new Vector3(2.0f, 4.0f, 6.0f)), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	[Fact]
	[Trait("Resolver", "Min")]
	public void Min_resolver_vector2_returns_componentwise_minimum()
	{
		var resolver = new MinResolver(
			new VariantResolver(new Variant128(new Vector2(1.0f, 5.0f)), typeof(Vector2)),
			new VariantResolver(new Variant128(new Vector2(2.0f, 4.0f)), typeof(Vector2)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector2().Should().Be(new Vector2(1.0f, 4.0f));
	}

	[Fact]
	[Trait("Resolver", "Min")]
	public void Min_resolver_vector3_returns_componentwise_minimum()
	{
		var resolver = new MinResolver(
			new VariantResolver(new Variant128(new Vector3(1.0f, 5.0f, 3.0f)), typeof(Vector3)),
			new VariantResolver(new Variant128(new Vector3(2.0f, 4.0f, 6.0f)), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(new Vector3(1.0f, 4.0f, 3.0f));
	}

	[Fact]
	[Trait("Resolver", "Min")]
	public void Min_resolver_vector4_returns_componentwise_minimum()
	{
		var resolver = new MinResolver(
			new VariantResolver(new Variant128(new Vector4(1.0f, 5.0f, 3.0f, 8.0f)), typeof(Vector4)),
			new VariantResolver(new Variant128(new Vector4(2.0f, 4.0f, 6.0f, 7.0f)), typeof(Vector4)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector4().Should().Be(new Vector4(1.0f, 4.0f, 3.0f, 7.0f));
	}

	[Fact]
	[Trait("Resolver", "Min")]
	public void Min_resolver_throws_for_quaternion_operands()
	{
#pragma warning disable CA1806
		Action act = () => new MinResolver(
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Min")]
	public void Min_resolver_throws_for_bool_operands()
	{
#pragma warning disable CA1806
		Action act = () => new MinResolver(
			new VariantResolver(new Variant128(true), typeof(bool)),
			new VariantResolver(new Variant128(false), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
