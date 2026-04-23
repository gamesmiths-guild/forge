// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ClampResolverTests
{
	[Fact]
	[Trait("Resolver", "Clamp")]
	public void Clamp_resolver_all_int_value_type_is_int()
	{
		var resolver = new ClampResolver(
			new VariantResolver(new Variant128(5), typeof(int)),
			new VariantResolver(new Variant128(0), typeof(int)),
			new VariantResolver(new Variant128(10), typeof(int)));

		resolver.ValueType.Should().Be(typeof(int));
	}

	[Fact]
	[Trait("Resolver", "Clamp")]
	public void Clamp_resolver_mixed_int_and_double_promotes_to_double()
	{
		var resolver = new ClampResolver(
			new VariantResolver(new Variant128(5), typeof(int)),
			new VariantResolver(new Variant128(0.0), typeof(double)),
			new VariantResolver(new Variant128(10), typeof(int)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Clamp")]
	public void Clamp_resolver_value_within_range_returns_value()
	{
		var resolver = new ClampResolver(
			new VariantResolver(new Variant128(5), typeof(int)),
			new VariantResolver(new Variant128(0), typeof(int)),
			new VariantResolver(new Variant128(10), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(5);
	}

	[Fact]
	[Trait("Resolver", "Clamp")]
	public void Clamp_resolver_value_below_min_returns_min()
	{
		var resolver = new ClampResolver(
			new VariantResolver(new Variant128(-5), typeof(int)),
			new VariantResolver(new Variant128(0), typeof(int)),
			new VariantResolver(new Variant128(10), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Clamp")]
	public void Clamp_resolver_value_above_max_returns_max()
	{
		var resolver = new ClampResolver(
			new VariantResolver(new Variant128(15), typeof(int)),
			new VariantResolver(new Variant128(0), typeof(int)),
			new VariantResolver(new Variant128(10), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(10);
	}

	[Fact]
	[Trait("Resolver", "Clamp")]
	public void Clamp_resolver_value_at_min_boundary()
	{
		var resolver = new ClampResolver(
			new VariantResolver(new Variant128(0), typeof(int)),
			new VariantResolver(new Variant128(0), typeof(int)),
			new VariantResolver(new Variant128(10), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Clamp")]
	public void Clamp_resolver_value_at_max_boundary()
	{
		var resolver = new ClampResolver(
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(0), typeof(int)),
			new VariantResolver(new Variant128(10), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(10);
	}

	[Fact]
	[Trait("Resolver", "Clamp")]
	public void Clamp_resolver_computes_double_clamp()
	{
		var resolver = new ClampResolver(
			new VariantResolver(new Variant128(1.5), typeof(double)),
			new VariantResolver(new Variant128(0.0), typeof(double)),
			new VariantResolver(new Variant128(1.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(1.0);
	}

	[Fact]
	[Trait("Resolver", "Clamp")]
	public void Clamp_resolver_computes_float_clamp()
	{
		var resolver = new ClampResolver(
			new VariantResolver(new Variant128(-1.0f), typeof(float)),
			new VariantResolver(new Variant128(0.0f), typeof(float)),
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(0.0f);
	}

	[Fact]
	[Trait("Resolver", "Clamp")]
	public void Clamp_resolver_computes_long_clamp()
	{
		var resolver = new ClampResolver(
			new VariantResolver(new Variant128(200L), typeof(long)),
			new VariantResolver(new Variant128(0L), typeof(long)),
			new VariantResolver(new Variant128(100L), typeof(long)));

		var context = new GraphContext();

		resolver.Resolve(context).AsLong().Should().Be(100L);
	}

	[Fact]
	[Trait("Resolver", "Clamp")]
	public void Clamp_resolver_computes_decimal_clamp()
	{
		var resolver = new ClampResolver(
			new VariantResolver(new Variant128(5.5m), typeof(decimal)),
			new VariantResolver(new Variant128(1.0m), typeof(decimal)),
			new VariantResolver(new Variant128(10.0m), typeof(decimal)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDecimal().Should().Be(5.5m);
	}

	[Fact]
	[Trait("Resolver", "Clamp")]
	public void Clamp_resolver_supports_nesting()
	{
		// Clamp(Clamp(50, 0, 100), 10, 80) = Clamp(50, 10, 80) = 50
		var inner = new ClampResolver(
			new VariantResolver(new Variant128(50), typeof(int)),
			new VariantResolver(new Variant128(0), typeof(int)),
			new VariantResolver(new Variant128(100), typeof(int)));

		var outer = new ClampResolver(
			inner,
			new VariantResolver(new Variant128(10), typeof(int)),
			new VariantResolver(new Variant128(80), typeof(int)));

		var context = new GraphContext();

		outer.Resolve(context).AsInt().Should().Be(50);
	}

	[Fact]
	[Trait("Resolver", "Clamp")]
	public void Clamp_resolver_vector3_value_type_is_vector3()
	{
		var resolver = new ClampResolver(
			new VariantResolver(new Variant128(new Vector3(2.0f, -1.0f, 5.0f)), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)),
			new VariantResolver(new Variant128(new Vector3(4.0f, 4.0f, 4.0f)), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	[Fact]
	[Trait("Resolver", "Clamp")]
	public void Clamp_resolver_vector2_clamps_componentwise()
	{
		var resolver = new ClampResolver(
			new VariantResolver(new Variant128(new Vector2(-2.0f, 6.0f)), typeof(Vector2)),
			new VariantResolver(new Variant128(new Vector2(0.0f, 1.0f)), typeof(Vector2)),
			new VariantResolver(new Variant128(new Vector2(5.0f, 4.0f)), typeof(Vector2)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector2().Should().Be(new Vector2(0.0f, 4.0f));
	}

	[Fact]
	[Trait("Resolver", "Clamp")]
	public void Clamp_resolver_vector3_clamps_componentwise()
	{
		var resolver = new ClampResolver(
			new VariantResolver(new Variant128(new Vector3(-2.0f, 3.0f, 8.0f)), typeof(Vector3)),
			new VariantResolver(new Variant128(new Vector3(0.0f, 1.0f, 2.0f)), typeof(Vector3)),
			new VariantResolver(new Variant128(new Vector3(5.0f, 4.0f, 6.0f)), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(new Vector3(0.0f, 3.0f, 6.0f));
	}

	[Fact]
	[Trait("Resolver", "Clamp")]
	public void Clamp_resolver_vector4_clamps_componentwise()
	{
		var resolver = new ClampResolver(
			new VariantResolver(new Variant128(new Vector4(-2.0f, 3.0f, 8.0f, 0.5f)), typeof(Vector4)),
			new VariantResolver(new Variant128(new Vector4(0.0f, 1.0f, 2.0f, 1.0f)), typeof(Vector4)),
			new VariantResolver(new Variant128(new Vector4(5.0f, 4.0f, 6.0f, 2.0f)), typeof(Vector4)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector4().Should().Be(new Vector4(0.0f, 3.0f, 6.0f, 1.0f));
	}

	[Fact]
	[Trait("Resolver", "Clamp")]
	public void Clamp_resolver_throws_for_mismatched_vector_operands()
	{
#pragma warning disable CA1806
		Action act = () => new ClampResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector2.Zero), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Clamp")]
	public void Clamp_resolver_throws_for_quaternion_operands()
	{
#pragma warning disable CA1806
		Action act = () => new ClampResolver(
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Clamp")]
	public void Clamp_resolver_throws_for_bool_operands()
	{
#pragma warning disable CA1806
		Action act = () => new ClampResolver(
			new VariantResolver(new Variant128(true), typeof(bool)),
			new VariantResolver(new Variant128(false), typeof(bool)),
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
