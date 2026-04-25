// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class PlaneFromVerticesResolverTests
{
	[Fact]
	[Trait("Resolver", "PlaneFromVertices")]
	public void PlaneFromVertices_resolver_value_type_is_plane()
	{
		var resolver = new PlaneFromVerticesResolver(
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(Plane));
	}

	[Fact]
	[Trait("Resolver", "PlaneFromVertices")]
	public void PlaneFromVertices_resolver_creates_plane()
	{
		Vector3 point1 = Vector3.Zero;
		Vector3 point2 = Vector3.UnitX;
		Vector3 point3 = Vector3.UnitY;
		var resolver = new PlaneFromVerticesResolver(
			new VariantResolver(new Variant128(point1), typeof(Vector3)),
			new VariantResolver(new Variant128(point2), typeof(Vector3)),
			new VariantResolver(new Variant128(point3), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsPlane().Should().Be(Plane.CreateFromVertices(point1, point2, point3));
	}

	[Fact]
	[Trait("Resolver", "PlaneFromVertices")]
	public void PlaneFromVertices_resolver_supports_offset_vertices()
	{
		var point1 = new Vector3(1.0f, 2.0f, 3.0f);
		var point2 = new Vector3(2.0f, 2.0f, 3.0f);
		var point3 = new Vector3(1.0f, 3.0f, 3.0f);
		var resolver = new PlaneFromVerticesResolver(
			new VariantResolver(new Variant128(point1), typeof(Vector3)),
			new VariantResolver(new Variant128(point2), typeof(Vector3)),
			new VariantResolver(new Variant128(point3), typeof(Vector3)));

		Plane result = resolver.Resolve(new GraphContext()).AsPlane();

		Plane.DotCoordinate(result, point1).Should().Be(0.0f);
		Plane.DotCoordinate(result, point2).Should().Be(0.0f);
		Plane.DotCoordinate(result, point3).Should().Be(0.0f);
	}

	[Fact]
	[Trait("Resolver", "PlaneFromVertices")]
	public void PlaneFromVertices_resolver_throws_for_non_vector3_operand()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new PlaneFromVerticesResolver(
			new VariantResolver(new Variant128(Vector2.Zero), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}
}
