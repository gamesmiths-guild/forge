// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class PlaneDistanceResolverTests
{
	[Fact]
	[Trait("Resolver", "PlaneDistance")]
	public void PlaneDistance_resolver_value_type_is_float()
	{
		var resolver = new PlaneDistanceResolver(
			new VariantResolver(new Variant128(new Plane(Vector3.UnitY, 2.0f)), typeof(Plane)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "PlaneDistance")]
	public void PlaneDistance_resolver_extracts_distance()
	{
		var plane = new Plane(new Vector3(1, 2, 3), 4.0f);
		var resolver = new PlaneDistanceResolver(
			new VariantResolver(new Variant128(plane), typeof(Plane)));

		resolver.Resolve(new GraphContext()).AsFloat().Should().Be(plane.D);
	}

	[Fact]
	[Trait("Resolver", "PlaneDistance")]
	public void PlaneDistance_resolver_extracts_negative_distance()
	{
		var plane = new Plane(Vector3.UnitZ, -3.5f);
		var resolver = new PlaneDistanceResolver(
			new VariantResolver(new Variant128(plane), typeof(Plane)));

		resolver.Resolve(new GraphContext()).AsFloat().Should().Be(-3.5f);
	}

	[Fact]
	[Trait("Resolver", "PlaneDistance")]
	public void PlaneDistance_resolver_throws_for_non_plane_operand()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new PlaneDistanceResolver(
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}
}
