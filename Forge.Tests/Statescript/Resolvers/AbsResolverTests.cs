// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class AbsResolverTests
{
	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_int_value_type_is_int()
	{
		var resolver = new AbsResolver(
			new VariantResolver(new Variant128(-5), typeof(int)));

		resolver.ValueType.Should().Be(typeof(int));
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_double_value_type_is_double()
	{
		var resolver = new AbsResolver(
			new VariantResolver(new Variant128(-5.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_sbyte_promotes_to_int()
	{
		var resolver = new AbsResolver(
			new VariantResolver(new Variant128((sbyte)-10), typeof(sbyte)));

		resolver.ValueType.Should().Be(typeof(int));
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_short_promotes_to_int()
	{
		var resolver = new AbsResolver(
			new VariantResolver(new Variant128((short)-100), typeof(short)));

		resolver.ValueType.Should().Be(typeof(int));
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_negative_int_returns_positive()
	{
		var resolver = new AbsResolver(
			new VariantResolver(new Variant128(-42), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(42);
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_positive_int_returns_same()
	{
		var resolver = new AbsResolver(
			new VariantResolver(new Variant128(42), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(42);
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_negative_double_returns_positive()
	{
		var resolver = new AbsResolver(
			new VariantResolver(new Variant128(-3.14), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(3.14);
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_negative_float_returns_positive()
	{
		var resolver = new AbsResolver(
			new VariantResolver(new Variant128(-2.5f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(2.5f);
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_negative_long_returns_positive()
	{
		var resolver = new AbsResolver(
			new VariantResolver(new Variant128(-100L), typeof(long)));

		var context = new GraphContext();

		resolver.Resolve(context).AsLong().Should().Be(100L);
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_negative_decimal_returns_positive()
	{
		var resolver = new AbsResolver(
			new VariantResolver(new Variant128(-7.5m), typeof(decimal)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDecimal().Should().Be(7.5m);
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_negative_sbyte_returns_positive_int()
	{
		var resolver = new AbsResolver(
			new VariantResolver(new Variant128((sbyte)-10), typeof(sbyte)));

		var context = new GraphContext();

		// sbyte promotes to int
		resolver.Resolve(context).AsInt().Should().Be(10);
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_negative_short_returns_positive_int()
	{
		var resolver = new AbsResolver(
			new VariantResolver(new Variant128((short)-200), typeof(short)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(200);
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_zero_returns_zero()
	{
		var resolver = new AbsResolver(
			new VariantResolver(new Variant128(0), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_composes_with_subtract()
	{
		// Abs(3 - 10) = Abs(-7) = 7
		var subtract = new SubtractResolver(
			new VariantResolver(new Variant128(3), typeof(int)),
			new VariantResolver(new Variant128(10), typeof(int)));

		var abs = new AbsResolver(subtract);

		var context = new GraphContext();

		abs.Resolve(context).AsInt().Should().Be(7);
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_throws_for_unsigned_byte()
	{
#pragma warning disable CA1806
		Action act = () => new AbsResolver(
			new VariantResolver(new Variant128((byte)10), typeof(byte)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_throws_for_unsigned_ushort()
	{
#pragma warning disable CA1806
		Action act = () => new AbsResolver(
			new VariantResolver(new Variant128((ushort)10), typeof(ushort)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_throws_for_unsigned_uint()
	{
#pragma warning disable CA1806
		Action act = () => new AbsResolver(
			new VariantResolver(new Variant128(10u), typeof(uint)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_throws_for_unsigned_ulong()
	{
#pragma warning disable CA1806
		Action act = () => new AbsResolver(
			new VariantResolver(new Variant128(10UL), typeof(ulong)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_vector3_value_type_is_vector3()
	{
		var resolver = new AbsResolver(
			new VariantResolver(new Variant128(new Vector3(-1.0f, 2.0f, -3.0f)), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_vector2_returns_componentwise_absolute_value()
	{
		var resolver = new AbsResolver(
			new VariantResolver(new Variant128(new Vector2(-1.0f, 2.0f)), typeof(Vector2)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector2().Should().Be(new Vector2(1.0f, 2.0f));
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_vector3_returns_componentwise_absolute_value()
	{
		var resolver = new AbsResolver(
			new VariantResolver(new Variant128(new Vector3(-1.0f, 2.0f, -3.0f)), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(new Vector3(1.0f, 2.0f, 3.0f));
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_vector4_returns_componentwise_absolute_value()
	{
		var resolver = new AbsResolver(
			new VariantResolver(new Variant128(new Vector4(-1.0f, 2.0f, -3.0f, 4.0f)), typeof(Vector4)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector4().Should().Be(new Vector4(1.0f, 2.0f, 3.0f, 4.0f));
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_throws_for_quaternion_operand()
	{
#pragma warning disable CA1806
		Action act = () => new AbsResolver(
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Abs")]
	public void Abs_resolver_throws_for_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new AbsResolver(
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
