// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class NegateResolverTests
{
	[Fact]
	[Trait("Resolver", "Negate")]
	public void Negate_resolver_int_value_type_is_int()
	{
		var resolver = new NegateResolver(
			new VariantResolver(new Variant128(5), typeof(int)));

		resolver.ValueType.Should().Be(typeof(int));
	}

	[Fact]
	[Trait("Resolver", "Negate")]
	public void Negate_resolver_double_value_type_is_double()
	{
		var resolver = new NegateResolver(
			new VariantResolver(new Variant128(5.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Negate")]
	public void Negate_resolver_byte_promotes_to_int()
	{
		var resolver = new NegateResolver(
			new VariantResolver(new Variant128((byte)10), typeof(byte)));

		resolver.ValueType.Should().Be(typeof(int));
	}

	[Fact]
	[Trait("Resolver", "Negate")]
	public void Negate_resolver_vector3_value_type_is_vector3()
	{
		var resolver = new NegateResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	[Fact]
	[Trait("Resolver", "Negate")]
	public void Negate_resolver_negates_positive_int()
	{
		var resolver = new NegateResolver(
			new VariantResolver(new Variant128(42), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(-42);
	}

	[Fact]
	[Trait("Resolver", "Negate")]
	public void Negate_resolver_negates_negative_int()
	{
		var resolver = new NegateResolver(
			new VariantResolver(new Variant128(-42), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(42);
	}

	[Fact]
	[Trait("Resolver", "Negate")]
	public void Negate_resolver_negates_double()
	{
		var resolver = new NegateResolver(
			new VariantResolver(new Variant128(3.14), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(-3.14);
	}

	[Fact]
	[Trait("Resolver", "Negate")]
	public void Negate_resolver_negates_float()
	{
		var resolver = new NegateResolver(
			new VariantResolver(new Variant128(2.5f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(-2.5f);
	}

	[Fact]
	[Trait("Resolver", "Negate")]
	public void Negate_resolver_negates_long()
	{
		var resolver = new NegateResolver(
			new VariantResolver(new Variant128(100L), typeof(long)));

		var context = new GraphContext();

		resolver.Resolve(context).AsLong().Should().Be(-100L);
	}

	[Fact]
	[Trait("Resolver", "Negate")]
	public void Negate_resolver_negates_decimal()
	{
		var resolver = new NegateResolver(
			new VariantResolver(new Variant128(7.5m), typeof(decimal)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDecimal().Should().Be(-7.5m);
	}

	[Fact]
	[Trait("Resolver", "Negate")]
	public void Negate_resolver_negates_byte_as_int()
	{
		var resolver = new NegateResolver(
			new VariantResolver(new Variant128((byte)10), typeof(byte)));

		var context = new GraphContext();

		// byte promotes to int, then negated
		resolver.Resolve(context).AsInt().Should().Be(-10);
	}

	[Fact]
	[Trait("Resolver", "Negate")]
	public void Negate_resolver_zero_returns_zero()
	{
		var resolver = new NegateResolver(
			new VariantResolver(new Variant128(0), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Negate")]
	public void Negate_resolver_negates_vector2()
	{
		var resolver = new NegateResolver(
			new VariantResolver(new Variant128(new Vector2(1, -2)), typeof(Vector2)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector2().Should().Be(new Vector2(-1, 2));
	}

	[Fact]
	[Trait("Resolver", "Negate")]
	public void Negate_resolver_negates_vector3()
	{
		var resolver = new NegateResolver(
			new VariantResolver(new Variant128(new Vector3(1, -2, 3)), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(new Vector3(-1, 2, -3));
	}

	[Fact]
	[Trait("Resolver", "Negate")]
	public void Negate_resolver_negates_vector4()
	{
		var resolver = new NegateResolver(
			new VariantResolver(new Variant128(new Vector4(1, -2, 3, -4)), typeof(Vector4)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector4().Should().Be(new Vector4(-1, 2, -3, 4));
	}

	[Fact]
	[Trait("Resolver", "Negate")]
	public void Negate_resolver_negates_quaternion()
	{
		var q = new Quaternion(1, -2, 3, -4);

		var resolver = new NegateResolver(
			new VariantResolver(new Variant128(q), typeof(Quaternion)));

		var context = new GraphContext();

		resolver.Resolve(context).AsQuaternion().Should().Be(-q);
	}

	[Fact]
	[Trait("Resolver", "Negate")]
	public void Negate_resolver_double_negate_returns_original()
	{
		var inner = new NegateResolver(
			new VariantResolver(new Variant128(42), typeof(int)));

		var outer = new NegateResolver(inner);

		var context = new GraphContext();

		outer.Resolve(context).AsInt().Should().Be(42);
	}

	[Fact]
	[Trait("Resolver", "Negate")]
	public void Negate_resolver_throws_for_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new NegateResolver(
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
