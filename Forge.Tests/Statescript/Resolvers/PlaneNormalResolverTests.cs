// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class PlaneNormalResolverTests
{
	[Fact]
	[Trait("Resolver", "PlaneNormal")]
	public void PlaneNormal_resolver_value_type_is_vector3()
	{
		var resolver = new PlaneNormalResolver(
			new VariantResolver(new Variant128(new Plane(Vector3.UnitX, 1.0f)), typeof(Plane)));

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	[Fact]
	[Trait("Resolver", "PlaneNormal")]
	public void PlaneNormal_resolver_extracts_normal()
	{
		var plane = new Plane(new Vector3(1, 2, 3), 4.0f);
		var resolver = new PlaneNormalResolver(
			new VariantResolver(new Variant128(plane), typeof(Plane)));

		resolver.Resolve(new GraphContext()).AsVector3().Should().Be(plane.Normal);
	}

	[Fact]
	[Trait("Resolver", "PlaneNormal")]
	public void PlaneNormal_resolver_preserves_non_unit_normal_components()
	{
		var plane = new Plane(new Vector3(-2.0f, 5.0f, 7.0f), -1.0f);
		var resolver = new PlaneNormalResolver(
			new VariantResolver(new Variant128(plane), typeof(Plane)));

		resolver.Resolve(new GraphContext()).AsVector3().Should().Be(new Vector3(-2.0f, 5.0f, 7.0f));
	}

	[Fact]
	[Trait("Resolver", "PlaneNormal")]
	public void PlaneNormal_resolver_throws_for_non_plane_operand()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new PlaneNormalResolver(
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}
}
