// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class TransformResolverTests
{
	[Fact]
	[Trait("Resolver", "Transform")]
	public void Transform_resolver_vector2_value_type_is_vector2()
	{
		var resolver = new TransformResolver(
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)),
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)));

		resolver.ValueType.Should().Be(typeof(Vector2));
	}

	[Fact]
	[Trait("Resolver", "Transform")]
	public void Transform_resolver_vector3_value_type_is_vector3()
	{
		var resolver = new TransformResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)),
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)));

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	[Fact]
	[Trait("Resolver", "Transform")]
	public void Transform_resolver_vector4_value_type_is_vector4()
	{
		var resolver = new TransformResolver(
			new VariantResolver(new Variant128(Vector4.One), typeof(Vector4)),
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)));

		resolver.ValueType.Should().Be(typeof(Vector4));
	}

	[Fact]
	[Trait("Resolver", "Transform")]
	public void Transform_resolver_plane_value_type_is_plane()
	{
		var resolver = new TransformResolver(
			new VariantResolver(new Variant128(new Plane(1, 1, 1, 1)), typeof(Plane)),
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)));

		resolver.ValueType.Should().Be(typeof(Plane));
	}

	[Fact]
	[Trait("Resolver", "Transform")]
	public void Transform_resolver_vector2()
	{
		Vector2 vector = Vector2.One;
		Quaternion rotation = Quaternion.Identity;
		var resolver = new TransformResolver(
			new VariantResolver(new Variant128(vector), typeof(Vector2)),
			new VariantResolver(new Variant128(rotation), typeof(Quaternion)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector2().Should().Be(Vector2.Transform(vector, rotation));
	}

	[Fact]
	[Trait("Resolver", "Transform")]
	public void Transform_resolver_vector3()
	{
		Vector3 vector = Vector3.One;
		Quaternion rotation = Quaternion.Identity;
		var resolver = new TransformResolver(
			new VariantResolver(new Variant128(vector), typeof(Vector3)),
			new VariantResolver(new Variant128(rotation), typeof(Quaternion)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(Vector3.Transform(vector, rotation));
	}

	[Fact]
	[Trait("Resolver", "Transform")]
	public void Transform_resolver_vector4()
	{
		Vector4 vector = Vector4.One;
		Quaternion rotation = Quaternion.Identity;
		var resolver = new TransformResolver(
			new VariantResolver(new Variant128(vector), typeof(Vector4)),
			new VariantResolver(new Variant128(rotation), typeof(Quaternion)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector4().Should().Be(Vector4.Transform(vector, rotation));
	}

	[Fact]
	[Trait("Resolver", "Transform")]
	public void Transform_resolver_plane()
	{
		var plane = new Plane(1, 1, 1, 1);
		Quaternion rotation = Quaternion.Identity;
		var resolver = new TransformResolver(
			new VariantResolver(new Variant128(plane), typeof(Plane)),
			new VariantResolver(new Variant128(rotation), typeof(Quaternion)));

		var context = new GraphContext();

		resolver.Resolve(context).AsPlane().Should().Be(Plane.Transform(plane, rotation));
	}

	[Fact]
	[Trait("Resolver", "Transform")]
	public void Transform_resolver_non_quaternion_rotation_throws()
	{
		Func<TransformResolver> act = () => new TransformResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)),
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		act.Should().Throw<ArgumentException>();
	}
}
