// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class DistanceSquaredResolverTests
{
	[Fact]
	[Trait("Resolver", "DistanceSquared")]
	public void DistanceSquared_resolver_value_type_is_float()
	{
		var resolver = new DistanceSquaredResolver(
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "DistanceSquared")]
	public void DistanceSquared_resolver_vector2()
	{
		var resolver = new DistanceSquaredResolver(
			new VariantResolver(new Variant128(new Vector2(0, 0)), typeof(Vector2)),
			new VariantResolver(new Variant128(new Vector2(3, 4)), typeof(Vector2)));

		var context = new GraphContext();

		// 9 + 16 = 25
		resolver.Resolve(context).AsFloat().Should().Be(25.0f);
	}

	[Fact]
	[Trait("Resolver", "DistanceSquared")]
	public void DistanceSquared_resolver_vector3()
	{
		var resolver = new DistanceSquaredResolver(
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)),
			new VariantResolver(new Variant128(new Vector3(1, 2, 2)), typeof(Vector3)));

		var context = new GraphContext();

		// 1 + 4 + 4 = 9
		resolver.Resolve(context).AsFloat().Should().Be(9.0f);
	}

	[Fact]
	[Trait("Resolver", "DistanceSquared")]
	public void DistanceSquared_resolver_vector4()
	{
		var resolver = new DistanceSquaredResolver(
			new VariantResolver(new Variant128(Vector4.Zero), typeof(Vector4)),
			new VariantResolver(new Variant128(new Vector4(1, 1, 1, 1)), typeof(Vector4)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(4.0f);
	}

	[Fact]
	[Trait("Resolver", "DistanceSquared")]
	public void DistanceSquared_resolver_scalar_type_throws()
	{
		Func<DistanceSquaredResolver> act = () => new DistanceSquaredResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		act.Should().Throw<ArgumentException>();
	}
}
