// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class MultiplyResolverTests
{
	[Fact]
	[Trait("Resolver", "Multiply")]
	public void Multiply_resolver_int_times_int_value_type_is_int()
	{
		var resolver = new MultiplyResolver(
			new VariantResolver(new Variant128(3), typeof(int)),
			new VariantResolver(new Variant128(4), typeof(int)));

		resolver.ValueType.Should().Be(typeof(int));
	}

	[Fact]
	[Trait("Resolver", "Multiply")]
	public void Multiply_resolver_int_times_float_promotes_to_float()
	{
		var resolver = new MultiplyResolver(
			new VariantResolver(new Variant128(3), typeof(int)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Multiply")]
	public void Multiply_resolver_vector3_times_vector3_value_type_is_vector3()
	{
		var resolver = new MultiplyResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	[Fact]
	[Trait("Resolver", "Multiply")]
	public void Multiply_resolver_multiplies_two_ints()
	{
		var resolver = new MultiplyResolver(
			new VariantResolver(new Variant128(6), typeof(int)),
			new VariantResolver(new Variant128(7), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(42);
	}

	[Fact]
	[Trait("Resolver", "Multiply")]
	public void Multiply_resolver_multiplies_two_doubles()
	{
		var resolver = new MultiplyResolver(
			new VariantResolver(new Variant128(2.5), typeof(double)),
			new VariantResolver(new Variant128(4.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(10.0);
	}

	[Fact]
	[Trait("Resolver", "Multiply")]
	public void Multiply_resolver_multiplies_two_floats()
	{
		var resolver = new MultiplyResolver(
			new VariantResolver(new Variant128(3.0f), typeof(float)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(6.0f);
	}

	[Fact]
	[Trait("Resolver", "Multiply")]
	public void Multiply_resolver_multiplies_two_longs()
	{
		var resolver = new MultiplyResolver(
			new VariantResolver(new Variant128(100L), typeof(long)),
			new VariantResolver(new Variant128(200L), typeof(long)));

		var context = new GraphContext();

		resolver.Resolve(context).AsLong().Should().Be(20000L);
	}

	[Fact]
	[Trait("Resolver", "Multiply")]
	public void Multiply_resolver_multiplies_two_decimals()
	{
		var resolver = new MultiplyResolver(
			new VariantResolver(new Variant128(2.5m), typeof(decimal)),
			new VariantResolver(new Variant128(4.0m), typeof(decimal)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDecimal().Should().Be(10.0m);
	}

	[Fact]
	[Trait("Resolver", "Multiply")]
	public void Multiply_resolver_multiplies_int_and_double()
	{
		var resolver = new MultiplyResolver(
			new VariantResolver(new Variant128(3), typeof(int)),
			new VariantResolver(new Variant128(2.5), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(7.5);
	}

	[Fact]
	[Trait("Resolver", "Multiply")]
	public void Multiply_resolver_by_zero_returns_zero()
	{
		var resolver = new MultiplyResolver(
			new VariantResolver(new Variant128(42), typeof(int)),
			new VariantResolver(new Variant128(0), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Multiply")]
	public void Multiply_resolver_by_one_returns_same_value()
	{
		var resolver = new MultiplyResolver(
			new VariantResolver(new Variant128(42), typeof(int)),
			new VariantResolver(new Variant128(1), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(42);
	}

	[Fact]
	[Trait("Resolver", "Multiply")]
	public void Multiply_resolver_negative_times_positive()
	{
		var resolver = new MultiplyResolver(
			new VariantResolver(new Variant128(-5), typeof(int)),
			new VariantResolver(new Variant128(3), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(-15);
	}

	[Fact]
	[Trait("Resolver", "Multiply")]
	public void Multiply_resolver_multiplies_two_vector2_componentwise()
	{
		var resolver = new MultiplyResolver(
			new VariantResolver(new Variant128(new Vector2(2, 3)), typeof(Vector2)),
			new VariantResolver(new Variant128(new Vector2(4, 5)), typeof(Vector2)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector2().Should().Be(new Vector2(8, 15));
	}

	[Fact]
	[Trait("Resolver", "Multiply")]
	public void Multiply_resolver_multiplies_two_vector3_componentwise()
	{
		var resolver = new MultiplyResolver(
			new VariantResolver(new Variant128(new Vector3(1, 2, 3)), typeof(Vector3)),
			new VariantResolver(new Variant128(new Vector3(4, 5, 6)), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(new Vector3(4, 10, 18));
	}

	[Fact]
	[Trait("Resolver", "Multiply")]
	public void Multiply_resolver_multiplies_two_vector4_componentwise()
	{
		var resolver = new MultiplyResolver(
			new VariantResolver(new Variant128(new Vector4(1, 2, 3, 4)), typeof(Vector4)),
			new VariantResolver(new Variant128(new Vector4(2, 2, 2, 2)), typeof(Vector4)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector4().Should().Be(new Vector4(2, 4, 6, 8));
	}

	[Fact]
	[Trait("Resolver", "Multiply")]
	public void Multiply_resolver_multiplies_two_quaternions()
	{
		var left = new Quaternion(1, 0, 0, 1);
		var right = new Quaternion(0, 1, 0, 1);

		var resolver = new MultiplyResolver(
			new VariantResolver(new Variant128(left), typeof(Quaternion)),
			new VariantResolver(new Variant128(right), typeof(Quaternion)));

		var context = new GraphContext();

		resolver.Resolve(context).AsQuaternion().Should().Be(left * right);
	}

	[Fact]
	[Trait("Resolver", "Multiply")]
	public void Multiply_resolver_supports_nesting()
	{
		// (2 * 3) * 4 = 24
		var inner = new MultiplyResolver(
			new VariantResolver(new Variant128(2), typeof(int)),
			new VariantResolver(new Variant128(3), typeof(int)));

		var outer = new MultiplyResolver(
			inner,
			new VariantResolver(new Variant128(4), typeof(int)));

		var context = new GraphContext();

		outer.Resolve(context).AsInt().Should().Be(24);
	}

	[Fact]
	[Trait("Resolver", "Multiply")]
	public void Multiply_resolver_throws_for_bool_operands()
	{
#pragma warning disable CA1806
		Action act = () => new MultiplyResolver(
			new VariantResolver(new Variant128(true), typeof(bool)),
			new VariantResolver(new Variant128(false), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Multiply")]
	public void Multiply_resolver_throws_for_mismatched_vector_types()
	{
#pragma warning disable CA1806
		Action act = () => new MultiplyResolver(
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
