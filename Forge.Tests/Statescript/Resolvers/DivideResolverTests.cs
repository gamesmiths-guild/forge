// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class DivideResolverTests
{
	[Fact]
	[Trait("Resolver", "Divide")]
	public void Divide_resolver_int_over_int_value_type_is_int()
	{
		var resolver = new DivideResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(2), typeof(int)));

		resolver.ValueType.Should().Be(typeof(int));
	}

	[Fact]
	[Trait("Resolver", "Divide")]
	public void Divide_resolver_int_over_float_promotes_to_float()
	{
		var resolver = new DivideResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(3.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Divide")]
	public void Divide_resolver_vector3_over_vector3_value_type_is_vector3()
	{
		var resolver = new DivideResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	[Fact]
	[Trait("Resolver", "Divide")]
	public void Divide_resolver_divides_two_ints()
	{
		var resolver = new DivideResolver(
			new VariantResolver(new Variant128(20), typeof(int)),
			new VariantResolver(new Variant128(4), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(5);
	}

	[Fact]
	[Trait("Resolver", "Divide")]
	public void Divide_resolver_integer_division_truncates()
	{
		var resolver = new DivideResolver(
			new VariantResolver(new Variant128(7), typeof(int)),
			new VariantResolver(new Variant128(2), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(3);
	}

	[Fact]
	[Trait("Resolver", "Divide")]
	public void Divide_resolver_divides_two_doubles()
	{
		var resolver = new DivideResolver(
			new VariantResolver(new Variant128(10.0), typeof(double)),
			new VariantResolver(new Variant128(4.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(2.5);
	}

	[Fact]
	[Trait("Resolver", "Divide")]
	public void Divide_resolver_divides_two_floats()
	{
		var resolver = new DivideResolver(
			new VariantResolver(new Variant128(9.0f), typeof(float)),
			new VariantResolver(new Variant128(3.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(3.0f);
	}

	[Fact]
	[Trait("Resolver", "Divide")]
	public void Divide_resolver_divides_two_longs()
	{
		var resolver = new DivideResolver(
			new VariantResolver(new Variant128(1000L), typeof(long)),
			new VariantResolver(new Variant128(250L), typeof(long)));

		var context = new GraphContext();

		resolver.Resolve(context).AsLong().Should().Be(4L);
	}

	[Fact]
	[Trait("Resolver", "Divide")]
	public void Divide_resolver_divides_two_decimals()
	{
		var resolver = new DivideResolver(
			new VariantResolver(new Variant128(10.0m), typeof(decimal)),
			new VariantResolver(new Variant128(4.0m), typeof(decimal)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDecimal().Should().Be(2.5m);
	}

	[Fact]
	[Trait("Resolver", "Divide")]
	public void Divide_resolver_divides_int_and_double()
	{
		var resolver = new DivideResolver(
			new VariantResolver(new Variant128(7), typeof(int)),
			new VariantResolver(new Variant128(2.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(3.5);
	}

	[Fact]
	[Trait("Resolver", "Divide")]
	public void Divide_resolver_by_one_returns_same_value()
	{
		var resolver = new DivideResolver(
			new VariantResolver(new Variant128(42), typeof(int)),
			new VariantResolver(new Variant128(1), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(42);
	}

	[Fact]
	[Trait("Resolver", "Divide")]
	public void Divide_resolver_negative_dividend()
	{
		var resolver = new DivideResolver(
			new VariantResolver(new Variant128(-15), typeof(int)),
			new VariantResolver(new Variant128(3), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(-5);
	}

	[Fact]
	[Trait("Resolver", "Divide")]
	public void Divide_resolver_divides_two_vector2_componentwise()
	{
		var resolver = new DivideResolver(
			new VariantResolver(new Variant128(new Vector2(10, 20)), typeof(Vector2)),
			new VariantResolver(new Variant128(new Vector2(2, 5)), typeof(Vector2)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector2().Should().Be(new Vector2(5, 4));
	}

	[Fact]
	[Trait("Resolver", "Divide")]
	public void Divide_resolver_divides_two_vector3_componentwise()
	{
		var resolver = new DivideResolver(
			new VariantResolver(new Variant128(new Vector3(12, 20, 30)), typeof(Vector3)),
			new VariantResolver(new Variant128(new Vector3(3, 4, 5)), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(new Vector3(4, 5, 6));
	}

	[Fact]
	[Trait("Resolver", "Divide")]
	public void Divide_resolver_divides_two_vector4_componentwise()
	{
		var resolver = new DivideResolver(
			new VariantResolver(new Variant128(new Vector4(8, 12, 16, 20)), typeof(Vector4)),
			new VariantResolver(new Variant128(new Vector4(2, 3, 4, 5)), typeof(Vector4)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector4().Should().Be(new Vector4(4, 4, 4, 4));
	}

	[Fact]
	[Trait("Resolver", "Divide")]
	public void Divide_resolver_supports_nesting()
	{
		// (100 / 5) / 2 = 10
		var inner = new DivideResolver(
			new VariantResolver(new Variant128(100), typeof(int)),
			new VariantResolver(new Variant128(5), typeof(int)));

		var outer = new DivideResolver(
			inner,
			new VariantResolver(new Variant128(2), typeof(int)));

		var context = new GraphContext();

		outer.Resolve(context).AsInt().Should().Be(10);
	}

	[Fact]
	[Trait("Resolver", "Divide")]
	public void Divide_resolver_quaternion_over_quaternion_value_type_is_quaternion()
	{
		var resolver = new DivideResolver(
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)));

		resolver.ValueType.Should().Be(typeof(Quaternion));
	}

	[Fact]
	[Trait("Resolver", "Divide")]
	public void Divide_resolver_divides_two_quaternions()
	{
		var left = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2.0f);
		var right = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 4.0f);

		var resolver = new DivideResolver(
			new VariantResolver(new Variant128(left), typeof(Quaternion)),
			new VariantResolver(new Variant128(right), typeof(Quaternion)));

		var context = new GraphContext();
		var expected = Quaternion.Divide(left, right);
		Quaternion result = resolver.Resolve(context).AsQuaternion();

		result.X.Should().BeApproximately(expected.X, TestUtils.Tolerance);
		result.Y.Should().BeApproximately(expected.Y, TestUtils.Tolerance);
		result.Z.Should().BeApproximately(expected.Z, TestUtils.Tolerance);
		result.W.Should().BeApproximately(expected.W, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Divide")]
	public void Divide_resolver_throws_for_bool_operands()
	{
#pragma warning disable CA1806
		Action act = () => new DivideResolver(
			new VariantResolver(new Variant128(true), typeof(bool)),
			new VariantResolver(new Variant128(false), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Divide")]
	public void Divide_resolver_throws_for_mismatched_vector_types()
	{
#pragma warning disable CA1806
		Action act = () => new DivideResolver(
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
