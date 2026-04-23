// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class LengthResolverTests
{
	[Fact]
	[Trait("Resolver", "Length")]
	public void Length_resolver_value_type_is_float()
	{
		var resolver = new LengthResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Length")]
	public void Length_resolver_vector3_unit_x()
	{
		var resolver = new LengthResolver(
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(1.0f);
	}

	[Fact]
	[Trait("Resolver", "Length")]
	public void Length_resolver_vector2()
	{
		var resolver = new LengthResolver(
			new VariantResolver(new Variant128(new Vector2(3, 4)), typeof(Vector2)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(5.0f);
	}

	[Fact]
	[Trait("Resolver", "Length")]
	public void Length_resolver_vector4()
	{
		var resolver = new LengthResolver(
			new VariantResolver(new Variant128(new Vector4(1, 0, 0, 0)), typeof(Vector4)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(1.0f);
	}

	[Fact]
	[Trait("Resolver", "Length")]
	public void Length_resolver_quaternion_value_type_is_float()
	{
		var resolver = new LengthResolver(
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Length")]
	public void Length_resolver_quaternion()
	{
		var quaternion = new Quaternion(1.0f, 2.0f, 3.0f, 4.0f);

		var resolver = new LengthResolver(
			new VariantResolver(new Variant128(quaternion), typeof(Quaternion)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(quaternion.Length());
	}

	[Fact]
	[Trait("Resolver", "Length")]
	public void Length_resolver_scalar_type_throws()
	{
		Func<LengthResolver> act = () => new LengthResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		act.Should().Throw<ArgumentException>();
	}
}
