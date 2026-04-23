// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class DistanceResolverTests
{
	[Fact]
	[Trait("Resolver", "Distance")]
	public void Distance_resolver_value_type_is_float()
	{
		var resolver = new DistanceResolver(
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Distance")]
	public void Distance_resolver_vector2()
	{
		var resolver = new DistanceResolver(
			new VariantResolver(new Variant128(new Vector2(0, 0)), typeof(Vector2)),
			new VariantResolver(new Variant128(new Vector2(3, 4)), typeof(Vector2)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(5.0f);
	}

	[Fact]
	[Trait("Resolver", "Distance")]
	public void Distance_resolver_vector3()
	{
		var resolver = new DistanceResolver(
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(1.0f);
	}

	[Fact]
	[Trait("Resolver", "Distance")]
	public void Distance_resolver_vector4()
	{
		var resolver = new DistanceResolver(
			new VariantResolver(new Variant128(Vector4.Zero), typeof(Vector4)),
			new VariantResolver(new Variant128(Vector4.UnitX), typeof(Vector4)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(1.0f);
	}

	[Fact]
	[Trait("Resolver", "Distance")]
	public void Distance_resolver_same_point_returns_zero()
	{
		var resolver = new DistanceResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(0.0f);
	}

	[Fact]
	[Trait("Resolver", "Distance")]
	public void Distance_resolver_mismatched_types_throws()
	{
		Func<DistanceResolver> act = () => new DistanceResolver(
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Distance")]
	public void Distance_resolver_scalar_type_throws()
	{
		Func<DistanceResolver> act = () => new DistanceResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		act.Should().Throw<ArgumentException>();
	}
}
