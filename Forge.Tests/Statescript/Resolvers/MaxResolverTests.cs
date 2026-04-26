// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class MaxResolverTests
{
	[Fact]
	[Trait("Resolver", "Max")]
	public void Max_resolver_int_and_int_value_type_is_int()
	{
		var resolver = new MaxResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(5), typeof(int)));

		resolver.ValueType.Should().Be(typeof(int));
	}

	[Fact]
	[Trait("Resolver", "Max")]
	public void Max_resolver_int_and_double_promotes_to_double()
	{
		var resolver = new MaxResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(5.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Max")]
	public void Max_resolver_returns_larger_int()
	{
		var resolver = new MaxResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(3), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(10);
	}

	[Fact]
	[Trait("Resolver", "Max")]
	public void Max_resolver_returns_larger_when_right_is_larger()
	{
		var resolver = new MaxResolver(
			new VariantResolver(new Variant128(2), typeof(int)),
			new VariantResolver(new Variant128(8), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(8);
	}

	[Fact]
	[Trait("Resolver", "Max")]
	public void Max_resolver_equal_values_returns_that_value()
	{
		var resolver = new MaxResolver(
			new VariantResolver(new Variant128(7), typeof(int)),
			new VariantResolver(new Variant128(7), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(7);
	}

	[Fact]
	[Trait("Resolver", "Max")]
	public void Max_resolver_negative_values()
	{
		var resolver = new MaxResolver(
			new VariantResolver(new Variant128(-3), typeof(int)),
			new VariantResolver(new Variant128(-10), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(-3);
	}

	[Fact]
	[Trait("Resolver", "Max")]
	public void Max_resolver_computes_double_max()
	{
		var resolver = new MaxResolver(
			new VariantResolver(new Variant128(3.14), typeof(double)),
			new VariantResolver(new Variant128(2.71), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(3.14);
	}

	[Fact]
	[Trait("Resolver", "Max")]
	public void Max_resolver_computes_float_max()
	{
		var resolver = new MaxResolver(
			new VariantResolver(new Variant128(5.0f), typeof(float)),
			new VariantResolver(new Variant128(3.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(5.0f);
	}

	[Fact]
	[Trait("Resolver", "Max")]
	public void Max_resolver_computes_long_max()
	{
		var resolver = new MaxResolver(
			new VariantResolver(new Variant128(100L), typeof(long)),
			new VariantResolver(new Variant128(50L), typeof(long)));

		var context = new GraphContext();

		resolver.Resolve(context).AsLong().Should().Be(100L);
	}

	[Fact]
	[Trait("Resolver", "Max")]
	public void Max_resolver_computes_decimal_max()
	{
		var resolver = new MaxResolver(
			new VariantResolver(new Variant128(10.5m), typeof(decimal)),
			new VariantResolver(new Variant128(3.2m), typeof(decimal)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDecimal().Should().Be(10.5m);
	}

	[Fact]
	[Trait("Resolver", "Max")]
	public void Max_resolver_mixed_int_and_double()
	{
		var resolver = new MaxResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(5.5), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(10.0);
	}

	[Fact]
	[Trait("Resolver", "Max")]
	public void Max_resolver_supports_nesting()
	{
		// Max(Max(1, 5), 3) = Max(5, 3) = 5
		var inner = new MaxResolver(
			new VariantResolver(new Variant128(1), typeof(int)),
			new VariantResolver(new Variant128(5), typeof(int)));

		var outer = new MaxResolver(
			inner,
			new VariantResolver(new Variant128(3), typeof(int)));

		var context = new GraphContext();

		outer.Resolve(context).AsInt().Should().Be(5);
	}

	[Fact]
	[Trait("Resolver", "Max")]
	public void Max_resolver_vector3_value_type_is_vector3()
	{
		var resolver = new MaxResolver(
			new VariantResolver(new Variant128(new Vector3(1.0f, 5.0f, 3.0f)), typeof(Vector3)),
			new VariantResolver(new Variant128(new Vector3(2.0f, 4.0f, 6.0f)), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	[Fact]
	[Trait("Resolver", "Max")]
	public void Max_resolver_vector2_returns_componentwise_maximum()
	{
		var resolver = new MaxResolver(
			new VariantResolver(new Variant128(new Vector2(1.0f, 5.0f)), typeof(Vector2)),
			new VariantResolver(new Variant128(new Vector2(2.0f, 4.0f)), typeof(Vector2)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector2().Should().Be(new Vector2(2.0f, 5.0f));
	}

	[Fact]
	[Trait("Resolver", "Max")]
	public void Max_resolver_vector3_returns_componentwise_maximum()
	{
		var resolver = new MaxResolver(
			new VariantResolver(new Variant128(new Vector3(1.0f, 5.0f, 3.0f)), typeof(Vector3)),
			new VariantResolver(new Variant128(new Vector3(2.0f, 4.0f, 6.0f)), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(new Vector3(2.0f, 5.0f, 6.0f));
	}

	[Fact]
	[Trait("Resolver", "Max")]
	public void Max_resolver_vector4_returns_componentwise_maximum()
	{
		var resolver = new MaxResolver(
			new VariantResolver(new Variant128(new Vector4(1.0f, 5.0f, 3.0f, 8.0f)), typeof(Vector4)),
			new VariantResolver(new Variant128(new Vector4(2.0f, 4.0f, 6.0f, 7.0f)), typeof(Vector4)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector4().Should().Be(new Vector4(2.0f, 5.0f, 6.0f, 8.0f));
	}

	[Fact]
	[Trait("Resolver", "Max")]
	public void Max_resolver_throws_for_quaternion_operands()
	{
#pragma warning disable CA1806
		Action act = () => new MaxResolver(
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Max")]
	public void Max_resolver_throws_for_bool_operands()
	{
#pragma warning disable CA1806
		Action act = () => new MaxResolver(
			new VariantResolver(new Variant128(true), typeof(bool)),
			new VariantResolver(new Variant128(false), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
