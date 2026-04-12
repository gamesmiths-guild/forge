// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class SubtractResolverTests
{
	// --- Value Type ---

	[Fact]
	[Trait("Resolver", "Subtract")]
	public void Subtract_resolver_int_minus_int_value_type_is_int()
	{
		var resolver = new SubtractResolver(
			new VariantResolver(new Variant128(5), typeof(int)),
			new VariantResolver(new Variant128(3), typeof(int)));

		resolver.ValueType.Should().Be(typeof(int));
	}

	[Fact]
	[Trait("Resolver", "Subtract")]
	public void Subtract_resolver_int_minus_float_promotes_to_float()
	{
		var resolver = new SubtractResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(2.5f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Subtract")]
	public void Subtract_resolver_vector3_minus_vector3_value_type_is_vector3()
	{
		var resolver = new SubtractResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	// --- Arithmetic ---

	[Fact]
	[Trait("Resolver", "Subtract")]
	public void Subtract_resolver_subtracts_two_ints()
	{
		var resolver = new SubtractResolver(
			new VariantResolver(new Variant128(30), typeof(int)),
			new VariantResolver(new Variant128(10), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(20);
	}

	[Fact]
	[Trait("Resolver", "Subtract")]
	public void Subtract_resolver_subtracts_two_doubles()
	{
		var resolver = new SubtractResolver(
			new VariantResolver(new Variant128(10.5), typeof(double)),
			new VariantResolver(new Variant128(3.5), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(7.0);
	}

	[Fact]
	[Trait("Resolver", "Subtract")]
	public void Subtract_resolver_subtracts_two_floats()
	{
		var resolver = new SubtractResolver(
			new VariantResolver(new Variant128(5.0f), typeof(float)),
			new VariantResolver(new Variant128(2.5f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(2.5f);
	}

	[Fact]
	[Trait("Resolver", "Subtract")]
	public void Subtract_resolver_subtracts_two_longs()
	{
		var resolver = new SubtractResolver(
			new VariantResolver(new Variant128(300L), typeof(long)),
			new VariantResolver(new Variant128(100L), typeof(long)));

		var context = new GraphContext();

		resolver.Resolve(context).AsLong().Should().Be(200L);
	}

	[Fact]
	[Trait("Resolver", "Subtract")]
	public void Subtract_resolver_subtracts_two_decimals()
	{
		var resolver = new SubtractResolver(
			new VariantResolver(new Variant128(5.5m), typeof(decimal)),
			new VariantResolver(new Variant128(2.2m), typeof(decimal)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDecimal().Should().Be(3.3m);
	}

	[Fact]
	[Trait("Resolver", "Subtract")]
	public void Subtract_resolver_subtracts_int_and_float()
	{
		var resolver = new SubtractResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(2.5f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(7.5f);
	}

	[Fact]
	[Trait("Resolver", "Subtract")]
	public void Subtract_resolver_subtracts_byte_and_short()
	{
		var resolver = new SubtractResolver(
			new VariantResolver(new Variant128((byte)50), typeof(byte)),
			new VariantResolver(new Variant128((short)20), typeof(short)));

		var context = new GraphContext();

		// byte - short promotes to int
		resolver.Resolve(context).AsInt().Should().Be(30);
	}

	// --- Negative result ---

	[Fact]
	[Trait("Resolver", "Subtract")]
	public void Subtract_resolver_handles_negative_result()
	{
		var resolver = new SubtractResolver(
			new VariantResolver(new Variant128(3), typeof(int)),
			new VariantResolver(new Variant128(10), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(-7);
	}

	[Fact]
	[Trait("Resolver", "Subtract")]
	public void Subtract_resolver_subtracting_zero_returns_same_value()
	{
		var resolver = new SubtractResolver(
			new VariantResolver(new Variant128(42), typeof(int)),
			new VariantResolver(new Variant128(0), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(42);
	}

	// --- Vectors ---

	[Fact]
	[Trait("Resolver", "Subtract")]
	public void Subtract_resolver_subtracts_two_vector2()
	{
		var resolver = new SubtractResolver(
			new VariantResolver(new Variant128(new Vector2(5, 7)), typeof(Vector2)),
			new VariantResolver(new Variant128(new Vector2(2, 3)), typeof(Vector2)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector2().Should().Be(new Vector2(3, 4));
	}

	[Fact]
	[Trait("Resolver", "Subtract")]
	public void Subtract_resolver_subtracts_two_vector3()
	{
		var resolver = new SubtractResolver(
			new VariantResolver(new Variant128(new Vector3(10, 20, 30)), typeof(Vector3)),
			new VariantResolver(new Variant128(new Vector3(1, 2, 3)), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(new Vector3(9, 18, 27));
	}

	[Fact]
	[Trait("Resolver", "Subtract")]
	public void Subtract_resolver_subtracts_two_vector4()
	{
		var resolver = new SubtractResolver(
			new VariantResolver(new Variant128(new Vector4(10, 20, 30, 40)), typeof(Vector4)),
			new VariantResolver(new Variant128(new Vector4(1, 2, 3, 4)), typeof(Vector4)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector4().Should().Be(new Vector4(9, 18, 27, 36));
	}

	[Fact]
	[Trait("Resolver", "Subtract")]
	public void Subtract_resolver_subtracts_two_quaternions()
	{
		var left = new Quaternion(5, 6, 7, 8);
		var right = new Quaternion(1, 2, 3, 4);

		var resolver = new SubtractResolver(
			new VariantResolver(new Variant128(left), typeof(Quaternion)),
			new VariantResolver(new Variant128(right), typeof(Quaternion)));

		var context = new GraphContext();

		resolver.Resolve(context).AsQuaternion().Should().Be(left - right);
	}

	// --- Nesting ---

	[Fact]
	[Trait("Resolver", "Subtract")]
	public void Subtract_resolver_supports_nesting()
	{
		// (100 - 30) - 20 = 50
		var inner = new SubtractResolver(
			new VariantResolver(new Variant128(100), typeof(int)),
			new VariantResolver(new Variant128(30), typeof(int)));

		var outer = new SubtractResolver(
			inner,
			new VariantResolver(new Variant128(20), typeof(int)));

		var context = new GraphContext();

		outer.Resolve(context).AsInt().Should().Be(50);
	}

	// --- Invalid types ---

	[Fact]
	[Trait("Resolver", "Subtract")]
	public void Subtract_resolver_throws_for_bool_operands()
	{
#pragma warning disable CA1806
		Action act = () => new SubtractResolver(
			new VariantResolver(new Variant128(true), typeof(bool)),
			new VariantResolver(new Variant128(false), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Subtract")]
	public void Subtract_resolver_throws_for_mismatched_vector_types()
	{
#pragma warning disable CA1806
		Action act = () => new SubtractResolver(
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
