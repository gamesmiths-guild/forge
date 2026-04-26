// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class DotCoordinateResolverTests
{
	[Fact]
	[Trait("Resolver", "DotCoordinate")]
	public void DotCoordinate_resolver_value_type_is_float()
	{
		var resolver = new DotCoordinateResolver(
			new VariantResolver(new Variant128(new Plane(1, 1, 1, 1)), typeof(Plane)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "DotCoordinate")]
	public void DotCoordinate_resolver_computes_dot_coordinate()
	{
		var plane = new Plane(1, 1, 1, 1);
		Vector3 coordinate = Vector3.One;
		var resolver = new DotCoordinateResolver(
			new VariantResolver(new Variant128(plane), typeof(Plane)),
			new VariantResolver(new Variant128(coordinate), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(Plane.DotCoordinate(plane, coordinate));
	}

	[Fact]
	[Trait("Resolver", "DotCoordinate")]
	public void DotCoordinate_resolver_supports_point_on_plane()
	{
		var plane = new Plane(Vector3.UnitY, -3.0f);
		var coordinate = new Vector3(1.0f, 3.0f, 2.0f);
		var resolver = new DotCoordinateResolver(
			new VariantResolver(new Variant128(plane), typeof(Plane)),
			new VariantResolver(new Variant128(coordinate), typeof(Vector3)));

		resolver.Resolve(new GraphContext()).AsFloat().Should().Be(0.0f);
	}

	[Fact]
	[Trait("Resolver", "DotCoordinate")]
	public void DotCoordinate_resolver_throws_for_non_plane_operand()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new DotCoordinateResolver(
			new VariantResolver(new Variant128(Vector4.One), typeof(Vector4)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "DotCoordinate")]
	public void DotCoordinate_resolver_throws_for_non_vector3_coordinate()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new DotCoordinateResolver(
			new VariantResolver(new Variant128(new Plane(0, 1, 0, 0)), typeof(Plane)),
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}
}
