// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class NormalizeResolverTests
{
	[Fact]
	[Trait("Resolver", "Normalize")]
	public void Normalize_resolver_vector3_value_type_is_vector3()
	{
		var resolver = new NormalizeResolver(
			new VariantResolver(new Variant128(new Vector3(3, 0, 0)), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	[Fact]
	[Trait("Resolver", "Normalize")]
	public void Normalize_resolver_vector3_returns_unit_vector()
	{
		var resolver = new NormalizeResolver(
			new VariantResolver(new Variant128(new Vector3(3, 0, 0)), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(Vector3.UnitX);
	}

	[Fact]
	[Trait("Resolver", "Normalize")]
	public void Normalize_resolver_vector2()
	{
		var resolver = new NormalizeResolver(
			new VariantResolver(new Variant128(new Vector2(0, 5)), typeof(Vector2)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector2().Should().Be(Vector2.UnitY);
	}

	[Fact]
	[Trait("Resolver", "Normalize")]
	public void Normalize_resolver_vector4()
	{
		var resolver = new NormalizeResolver(
			new VariantResolver(new Variant128(new Vector4(0, 0, 0, 7)), typeof(Vector4)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector4().Should().Be(Vector4.UnitW);
	}

	[Fact]
	[Trait("Resolver", "Normalize")]
	public void Normalize_resolver_quaternion_value_type_is_quaternion()
	{
		var resolver = new NormalizeResolver(
			new VariantResolver(new Variant128(new Quaternion(1, 2, 3, 4)), typeof(Quaternion)));

		resolver.ValueType.Should().Be(typeof(Quaternion));
	}

	[Fact]
	[Trait("Resolver", "Normalize")]
	public void Normalize_resolver_quaternion_returns_unit_quaternion()
	{
		var q = new Quaternion(1, 2, 3, 4);
		var expected = Quaternion.Normalize(q);

		var resolver = new NormalizeResolver(
			new VariantResolver(new Variant128(q), typeof(Quaternion)));

		var context = new GraphContext();

		resolver.Resolve(context).AsQuaternion().Should().Be(expected);
	}

	[Fact]
	[Trait("Resolver", "Normalize")]
	public void Normalize_resolver_plane_value_type_is_plane()
	{
		var resolver = new NormalizeResolver(
			new VariantResolver(new Variant128(new Plane(new Vector3(0, 2, 0), 4.0f)), typeof(Plane)));

		resolver.ValueType.Should().Be(typeof(Plane));
	}

	[Fact]
	[Trait("Resolver", "Normalize")]
	public void Normalize_resolver_plane_returns_normalized_plane()
	{
		var plane = new Plane(new Vector3(0, 2, 0), 4.0f);
		var expected = Plane.Normalize(plane);

		var resolver = new NormalizeResolver(
			new VariantResolver(new Variant128(plane), typeof(Plane)));

		var context = new GraphContext();

		resolver.Resolve(context).AsPlane().Should().Be(expected);
	}

	[Fact]
	[Trait("Resolver", "Normalize")]
	public void Normalize_resolver_scalar_type_throws()
	{
		Func<NormalizeResolver> act = () => new NormalizeResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		act.Should().Throw<ArgumentException>();
	}
}
