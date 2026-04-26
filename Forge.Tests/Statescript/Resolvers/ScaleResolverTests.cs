// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ScaleResolverTests
{
	[Fact]
	[Trait("Resolver", "Scale")]
	public void Scale_resolver_vector2_value_type_is_vector2()
	{
		var resolver = new ScaleResolver(
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(Vector2));
	}

	[Fact]
	[Trait("Resolver", "Scale")]
	public void Scale_resolver_vector3_value_type_is_vector3()
	{
		var resolver = new ScaleResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	[Fact]
	[Trait("Resolver", "Scale")]
	public void Scale_resolver_vector2_by_scalar()
	{
		var resolver = new ScaleResolver(
			new VariantResolver(new Variant128(new Vector2(1, 2)), typeof(Vector2)),
			new VariantResolver(new Variant128(3.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector2().Should().Be(new Vector2(3, 6));
	}

	[Fact]
	[Trait("Resolver", "Scale")]
	public void Scale_resolver_vector3_by_scalar()
	{
		var resolver = new ScaleResolver(
			new VariantResolver(new Variant128(new Vector3(1, 2, 3)), typeof(Vector3)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(new Vector3(2, 4, 6));
	}

	[Fact]
	[Trait("Resolver", "Scale")]
	public void Scale_resolver_vector4_by_scalar()
	{
		var resolver = new ScaleResolver(
			new VariantResolver(new Variant128(new Vector4(1, 2, 3, 4)), typeof(Vector4)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector4().Should().Be(new Vector4(0.5f, 1.0f, 1.5f, 2.0f));
	}

	[Fact]
	[Trait("Resolver", "Scale")]
	public void Scale_resolver_by_zero_returns_zero_vector()
	{
		var resolver = new ScaleResolver(
			new VariantResolver(new Variant128(new Vector3(1, 2, 3)), typeof(Vector3)),
			new VariantResolver(new Variant128(0.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(Vector3.Zero);
	}

	[Fact]
	[Trait("Resolver", "Scale")]
	public void Scale_resolver_by_negative_scalar()
	{
		var resolver = new ScaleResolver(
			new VariantResolver(new Variant128(new Vector3(1, 2, 3)), typeof(Vector3)),
			new VariantResolver(new Variant128(-1.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(new Vector3(-1, -2, -3));
	}

	[Fact]
	[Trait("Resolver", "Scale")]
	public void Scale_resolver_non_vector_type_throws()
	{
		Func<ScaleResolver> act = () => new ScaleResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Scale")]
	public void Scale_resolver_non_float_scalar_throws()
	{
		Func<ScaleResolver> act = () => new ScaleResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)),
			new VariantResolver(new Variant128(2.0), typeof(double)));

		act.Should().Throw<ArgumentException>();
	}
}
