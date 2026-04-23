// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class SqrtResolverTests
{
	[Fact]
	[Trait("Resolver", "Sqrt")]
	public void Sqrt_resolver_float_value_type_is_float()
	{
		var resolver = new SqrtResolver(
			new VariantResolver(new Variant128(4.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Sqrt")]
	public void Sqrt_resolver_double_value_type_is_double()
	{
		var resolver = new SqrtResolver(
			new VariantResolver(new Variant128(4.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Sqrt")]
	public void Sqrt_resolver_int_promotes_to_double()
	{
		var resolver = new SqrtResolver(
			new VariantResolver(new Variant128(4), typeof(int)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Sqrt")]
	public void Sqrt_resolver_computes_float_sqrt()
	{
		var resolver = new SqrtResolver(
			new VariantResolver(new Variant128(9.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(3.0f);
	}

	[Fact]
	[Trait("Resolver", "Sqrt")]
	public void Sqrt_resolver_computes_double_sqrt()
	{
		var resolver = new SqrtResolver(
			new VariantResolver(new Variant128(25.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(5.0);
	}

	[Fact]
	[Trait("Resolver", "Sqrt")]
	public void Sqrt_resolver_computes_int_sqrt_as_double()
	{
		var resolver = new SqrtResolver(
			new VariantResolver(new Variant128(16), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(4.0);
	}

	[Fact]
	[Trait("Resolver", "Sqrt")]
	public void Sqrt_resolver_irrational_result()
	{
		var resolver = new SqrtResolver(
			new VariantResolver(new Variant128(2.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(1.41421356, 0.0001);
	}

	[Fact]
	[Trait("Resolver", "Sqrt")]
	public void Sqrt_resolver_zero_returns_zero()
	{
		var resolver = new SqrtResolver(
			new VariantResolver(new Variant128(0.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(0.0);
	}

	[Fact]
	[Trait("Resolver", "Sqrt")]
	public void Sqrt_resolver_one_returns_one()
	{
		var resolver = new SqrtResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(1.0f);
	}

	[Fact]
	[Trait("Resolver", "Sqrt")]
	public void Sqrt_resolver_supports_nesting()
	{
		// Sqrt(Sqrt(256)) = Sqrt(16) = 4
		var inner = new SqrtResolver(
			new VariantResolver(new Variant128(256.0), typeof(double)));

		var outer = new SqrtResolver(inner);

		var context = new GraphContext();

		outer.Resolve(context).AsDouble().Should().Be(4.0);
	}

	[Fact]
	[Trait("Resolver", "Sqrt")]
	public void Sqrt_resolver_throws_for_decimal_operand()
	{
#pragma warning disable CA1806
		Action act = () => new SqrtResolver(
			new VariantResolver(new Variant128(4.0m), typeof(decimal)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Sqrt")]
	public void Sqrt_resolver_vector3_value_type_is_vector3()
	{
		var resolver = new SqrtResolver(
			new VariantResolver(new Variant128(new Vector3(1.0f, 4.0f, 9.0f)), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	[Fact]
	[Trait("Resolver", "Sqrt")]
	public void Sqrt_resolver_vector2_computes_componentwise_square_root()
	{
		var resolver = new SqrtResolver(
			new VariantResolver(new Variant128(new Vector2(1.0f, 4.0f)), typeof(Vector2)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector2().Should().Be(new Vector2(1.0f, 2.0f));
	}

	[Fact]
	[Trait("Resolver", "Sqrt")]
	public void Sqrt_resolver_vector3_computes_componentwise_square_root()
	{
		var resolver = new SqrtResolver(
			new VariantResolver(new Variant128(new Vector3(1.0f, 4.0f, 9.0f)), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(new Vector3(1.0f, 2.0f, 3.0f));
	}

	[Fact]
	[Trait("Resolver", "Sqrt")]
	public void Sqrt_resolver_vector4_computes_componentwise_square_root()
	{
		var resolver = new SqrtResolver(
			new VariantResolver(new Variant128(new Vector4(1.0f, 4.0f, 9.0f, 16.0f)), typeof(Vector4)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector4().Should().Be(new Vector4(1.0f, 2.0f, 3.0f, 4.0f));
	}

	[Fact]
	[Trait("Resolver", "Sqrt")]
	public void Sqrt_resolver_throws_for_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new SqrtResolver(
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
