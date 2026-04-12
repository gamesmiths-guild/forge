// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class LerpResolverTests
{
	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_float_operands_value_type_is_float()
	{
		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(0.0f), typeof(float)),
			new VariantResolver(new Variant128(10.0f), typeof(float)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_double_operands_value_type_is_double()
	{
		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(0.0), typeof(double)),
			new VariantResolver(new Variant128(10.0), typeof(double)),
			new VariantResolver(new Variant128(0.5), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_mixed_float_and_double_promotes_to_double()
	{
		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(0.0f), typeof(float)),
			new VariantResolver(new Variant128(10.0), typeof(double)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_vector2_value_type_is_vector2()
	{
		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(Vector2.Zero), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(Vector2));
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_vector3_value_type_is_vector3()
	{
		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_vector4_value_type_is_vector4()
	{
		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(Vector4.Zero), typeof(Vector4)),
			new VariantResolver(new Variant128(Vector4.One), typeof(Vector4)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(Vector4));
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_quaternion_value_type_is_quaternion()
	{
		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(Quaternion));
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_at_zero_returns_start()
	{
		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(10.0f), typeof(float)),
			new VariantResolver(new Variant128(30.0f), typeof(float)),
			new VariantResolver(new Variant128(0.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(10.0f);
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_at_one_returns_end()
	{
		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(10.0f), typeof(float)),
			new VariantResolver(new Variant128(30.0f), typeof(float)),
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(30.0f);
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_at_half_returns_midpoint()
	{
		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(10.0f), typeof(float)),
			new VariantResolver(new Variant128(30.0f), typeof(float)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(20.0f);
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_at_quarter()
	{
		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(0.0), typeof(double)),
			new VariantResolver(new Variant128(100.0), typeof(double)),
			new VariantResolver(new Variant128(0.25), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(25.0);
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_double_interpolation()
	{
		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(10.0), typeof(double)),
			new VariantResolver(new Variant128(20.0), typeof(double)),
			new VariantResolver(new Variant128(0.3), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(13.0, 0.001);
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_negative_range()
	{
		// Lerp(-10, 10, 0.5) = -10 + (10 - (-10)) * 0.5 = -10 + 10 = 0
		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(-10.0f), typeof(float)),
			new VariantResolver(new Variant128(10.0f), typeof(float)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(0.0f);
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_beyond_one_extrapolates()
	{
		// Lerp(0, 10, 1.5) = 0 + (10 - 0) * 1.5 = 15
		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(0.0f), typeof(float)),
			new VariantResolver(new Variant128(10.0f), typeof(float)),
			new VariantResolver(new Variant128(1.5f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(15.0f);
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_vector2_at_half()
	{
		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(new Vector2(0, 0)), typeof(Vector2)),
			new VariantResolver(new Variant128(new Vector2(10, 20)), typeof(Vector2)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector2().Should().Be(new Vector2(5, 10));
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_vector2_at_zero_returns_start()
	{
		var a = new Vector2(1, 2);

		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(a), typeof(Vector2)),
			new VariantResolver(new Variant128(new Vector2(10, 20)), typeof(Vector2)),
			new VariantResolver(new Variant128(0.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector2().Should().Be(a);
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_vector2_at_one_returns_end()
	{
		var b = new Vector2(10, 20);

		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(new Vector2(1, 2)), typeof(Vector2)),
			new VariantResolver(new Variant128(b), typeof(Vector2)),
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector2().Should().Be(b);
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_vector3_at_half()
	{
		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(new Vector3(0, 0, 0)), typeof(Vector3)),
			new VariantResolver(new Variant128(new Vector3(10, 20, 30)), typeof(Vector3)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(new Vector3(5, 10, 15));
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_vector3_at_zero_returns_start()
	{
		var a = new Vector3(1, 2, 3);

		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(a), typeof(Vector3)),
			new VariantResolver(new Variant128(new Vector3(10, 20, 30)), typeof(Vector3)),
			new VariantResolver(new Variant128(0.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(a);
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_vector3_at_one_returns_end()
	{
		var b = new Vector3(10, 20, 30);

		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(new Vector3(1, 2, 3)), typeof(Vector3)),
			new VariantResolver(new Variant128(b), typeof(Vector3)),
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(b);
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_vector4_at_half()
	{
		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(new Vector4(0, 0, 0, 0)), typeof(Vector4)),
			new VariantResolver(new Variant128(new Vector4(10, 20, 30, 40)), typeof(Vector4)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector4().Should().Be(new Vector4(5, 10, 15, 20));
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_quaternion_at_half()
	{
		Quaternion a = Quaternion.Identity;
		var b = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI);

		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(a), typeof(Quaternion)),
			new VariantResolver(new Variant128(b), typeof(Quaternion)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));

		var context = new GraphContext();

		var expected = Quaternion.Lerp(a, b, 0.5f);
		Quaternion result = resolver.Resolve(context).AsQuaternion();

		result.X.Should().BeApproximately(expected.X, 0.001f);
		result.Y.Should().BeApproximately(expected.Y, 0.001f);
		result.Z.Should().BeApproximately(expected.Z, 0.001f);
		result.W.Should().BeApproximately(expected.W, 0.001f);
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_quaternion_at_zero_returns_start()
	{
		Quaternion a = Quaternion.Identity;
		var b = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);

		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(a), typeof(Quaternion)),
			new VariantResolver(new Variant128(b), typeof(Quaternion)),
			new VariantResolver(new Variant128(0.0f), typeof(float)));

		var context = new GraphContext();

		var expected = Quaternion.Lerp(a, b, 0.0f);
		Quaternion result = resolver.Resolve(context).AsQuaternion();

		result.X.Should().BeApproximately(expected.X, 0.001f);
		result.Y.Should().BeApproximately(expected.Y, 0.001f);
		result.Z.Should().BeApproximately(expected.Z, 0.001f);
		result.W.Should().BeApproximately(expected.W, 0.001f);
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_quaternion_at_one_returns_end()
	{
		Quaternion a = Quaternion.Identity;
		var b = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);

		var resolver = new LerpResolver(
			new VariantResolver(new Variant128(a), typeof(Quaternion)),
			new VariantResolver(new Variant128(b), typeof(Quaternion)),
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		var context = new GraphContext();

		var expected = Quaternion.Lerp(a, b, 1.0f);
		Quaternion result = resolver.Resolve(context).AsQuaternion();

		result.X.Should().BeApproximately(expected.X, 0.001f);
		result.Y.Should().BeApproximately(expected.Y, 0.001f);
		result.Z.Should().BeApproximately(expected.Z, 0.001f);
		result.W.Should().BeApproximately(expected.W, 0.001f);
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_supports_nesting()
	{
		// Lerp(Lerp(0, 10, 0.5), 20, 0.5) = Lerp(5, 20, 0.5) = 12.5
		var inner = new LerpResolver(
			new VariantResolver(new Variant128(0.0f), typeof(float)),
			new VariantResolver(new Variant128(10.0f), typeof(float)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));

		var outer = new LerpResolver(
			inner,
			new VariantResolver(new Variant128(20.0f), typeof(float)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));

		var context = new GraphContext();

		outer.Resolve(context).AsFloat().Should().Be(12.5f);
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_throws_for_int_operand()
	{
#pragma warning disable CA1806
		Action act = () => new LerpResolver(
			new VariantResolver(new Variant128(0), typeof(int)),
			new VariantResolver(new Variant128(10.0f), typeof(float)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_throws_for_decimal_operand()
	{
#pragma warning disable CA1806
		Action act = () => new LerpResolver(
			new VariantResolver(new Variant128(0.0m), typeof(decimal)),
			new VariantResolver(new Variant128(10.0m), typeof(decimal)),
			new VariantResolver(new Variant128(0.5m), typeof(decimal)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_throws_for_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new LerpResolver(
			new VariantResolver(new Variant128(true), typeof(bool)),
			new VariantResolver(new Variant128(false), typeof(bool)),
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_throws_for_mismatched_vector_types()
	{
#pragma warning disable CA1806
		Action act = () => new LerpResolver(
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_throws_for_vector_with_double_t()
	{
#pragma warning disable CA1806
		Action act = () => new LerpResolver(
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)),
			new VariantResolver(new Variant128(0.5), typeof(double)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Lerp")]
	public void Lerp_resolver_throws_for_vector_with_int_t()
	{
#pragma warning disable CA1806
		Action act = () => new LerpResolver(
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)),
			new VariantResolver(new Variant128(0), typeof(int)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
