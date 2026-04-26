// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ReflectResolverTests
{
	[Fact]
	[Trait("Resolver", "Reflect")]
	public void Reflect_resolver_vector2_value_type_is_vector2()
	{
		var resolver = new ReflectResolver(
			new VariantResolver(new Variant128(Vector2.UnitX), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector2.UnitY), typeof(Vector2)));

		resolver.ValueType.Should().Be(typeof(Vector2));
	}

	[Fact]
	[Trait("Resolver", "Reflect")]
	public void Reflect_resolver_vector3_value_type_is_vector3()
	{
		var resolver = new ReflectResolver(
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	[Fact]
	[Trait("Resolver", "Reflect")]
	public void Reflect_resolver_vector2_horizontal_bounce()
	{
		// Incident going right-down, normal pointing up => reflects to right-up
		var incident = new Vector2(1, -1);
		Vector2 normal = Vector2.UnitY;

		var resolver = new ReflectResolver(
			new VariantResolver(new Variant128(incident), typeof(Vector2)),
			new VariantResolver(new Variant128(normal), typeof(Vector2)));

		var context = new GraphContext();
		Vector2 result = resolver.Resolve(context).AsVector2();

		result.Should().Be(Vector2.Reflect(incident, normal));
	}

	[Fact]
	[Trait("Resolver", "Reflect")]
	public void Reflect_resolver_vector3()
	{
		var incident = new Vector3(1, -1, 0);
		Vector3 normal = Vector3.UnitY;

		var resolver = new ReflectResolver(
			new VariantResolver(new Variant128(incident), typeof(Vector3)),
			new VariantResolver(new Variant128(normal), typeof(Vector3)));

		var context = new GraphContext();
		Vector3 result = resolver.Resolve(context).AsVector3();

		result.Should().Be(Vector3.Reflect(incident, normal));
	}

	[Fact]
	[Trait("Resolver", "Reflect")]
	public void Reflect_resolver_mismatched_types_throws()
	{
		Func<ReflectResolver> act = () => new ReflectResolver(
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Reflect")]
	public void Reflect_resolver_vector4_throws()
	{
		Func<ReflectResolver> act = () => new ReflectResolver(
			new VariantResolver(new Variant128(Vector4.One), typeof(Vector4)),
			new VariantResolver(new Variant128(Vector4.One), typeof(Vector4)));

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Reflect")]
	public void Reflect_resolver_scalar_type_throws()
	{
		Func<ReflectResolver> act = () => new ReflectResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		act.Should().Throw<ArgumentException>();
	}
}
