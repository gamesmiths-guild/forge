// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class DotNormalResolverTests
{
	[Fact]
	[Trait("Resolver", "DotNormal")]
	public void DotNormal_resolver_value_type_is_float()
	{
		var resolver = new DotNormalResolver(
			new VariantResolver(new Variant128(new Plane(1, 1, 1, 1)), typeof(Plane)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "DotNormal")]
	public void DotNormal_resolver_computes_dot_normal()
	{
		var plane = new Plane(1, 1, 1, 1);
		Vector3 normal = Vector3.One;
		var resolver = new DotNormalResolver(
			new VariantResolver(new Variant128(plane), typeof(Plane)),
			new VariantResolver(new Variant128(normal), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(Plane.DotNormal(plane, normal));
	}

	[Fact]
	[Trait("Resolver", "DotNormal")]
	public void DotNormal_resolver_ignores_plane_distance()
	{
		var plane = new Plane(Vector3.UnitZ, 10.0f);
		var normal = new Vector3(0.0f, 0.0f, -2.0f);
		var resolver = new DotNormalResolver(
			new VariantResolver(new Variant128(plane), typeof(Plane)),
			new VariantResolver(new Variant128(normal), typeof(Vector3)));

		resolver.Resolve(new GraphContext()).AsFloat().Should().Be(-2.0f);
	}

	[Fact]
	[Trait("Resolver", "DotNormal")]
	public void DotNormal_resolver_throws_for_non_plane_operand()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new DotNormalResolver(
			new VariantResolver(new Variant128(Vector4.One), typeof(Vector4)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "DotNormal")]
	public void DotNormal_resolver_throws_for_non_vector3_normal()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new DotNormalResolver(
			new VariantResolver(new Variant128(new Plane(0, 0, 1, 0)), typeof(Plane)),
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}
}
