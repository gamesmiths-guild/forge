// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class LengthSquaredResolverTests
{
	[Fact]
	[Trait("Resolver", "LengthSquared")]
	public void LengthSquared_resolver_value_type_is_float()
	{
		var resolver = new LengthSquaredResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "LengthSquared")]
	public void LengthSquared_resolver_vector2()
	{
		var resolver = new LengthSquaredResolver(
			new VariantResolver(new Variant128(new Vector2(3, 4)), typeof(Vector2)));

		var context = new GraphContext();

		// 9 + 16 = 25
		resolver.Resolve(context).AsFloat().Should().Be(25.0f);
	}

	[Fact]
	[Trait("Resolver", "LengthSquared")]
	public void LengthSquared_resolver_vector3()
	{
		var resolver = new LengthSquaredResolver(
			new VariantResolver(new Variant128(new Vector3(1, 2, 3)), typeof(Vector3)));

		var context = new GraphContext();

		// 1 + 4 + 9 = 14
		resolver.Resolve(context).AsFloat().Should().Be(14.0f);
	}

	[Fact]
	[Trait("Resolver", "LengthSquared")]
	public void LengthSquared_resolver_vector4()
	{
		var resolver = new LengthSquaredResolver(
			new VariantResolver(new Variant128(new Vector4(1, 1, 1, 1)), typeof(Vector4)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(4.0f);
	}

	[Fact]
	[Trait("Resolver", "LengthSquared")]
	public void LengthSquared_resolver_scalar_type_throws()
	{
		Func<LengthSquaredResolver> act = () => new LengthSquaredResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		act.Should().Throw<ArgumentException>();
	}
}
