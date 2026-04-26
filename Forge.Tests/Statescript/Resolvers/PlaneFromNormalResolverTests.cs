// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class PlaneFromNormalResolverTests
{
	[Fact]
	[Trait("Resolver", "PlaneFromNormal")]
	public void PlaneFromNormal_resolver_value_type_is_plane()
	{
		var resolver = new PlaneFromNormalResolver(
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(Plane));
	}

	[Fact]
	[Trait("Resolver", "PlaneFromNormal")]
	public void PlaneFromNormal_resolver_creates_plane()
	{
		Vector3 normal = Vector3.UnitY;
		const float distance = 2.0f;
		var resolver = new PlaneFromNormalResolver(
			new VariantResolver(new Variant128(normal), typeof(Vector3)),
			new VariantResolver(new Variant128(distance), typeof(float)));

		resolver.Resolve(new GraphContext()).AsPlane().Should().Be(new Plane(normal, distance));
	}

	[Fact]
	[Trait("Resolver", "PlaneFromNormal")]
	public void PlaneFromNormal_resolver_preserves_non_unit_normal_and_negative_distance()
	{
		var normal = new Vector3(2.0f, -3.0f, 4.0f);
		const float distance = -5.0f;
		var resolver = new PlaneFromNormalResolver(
			new VariantResolver(new Variant128(normal), typeof(Vector3)),
			new VariantResolver(new Variant128(distance), typeof(float)));

		Plane result = resolver.Resolve(new GraphContext()).AsPlane();

		result.Normal.Should().Be(normal);
		result.D.Should().Be(distance);
	}

	[Fact]
	[Trait("Resolver", "PlaneFromNormal")]
	public void PlaneFromNormal_resolver_throws_for_non_vector3_normal()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new PlaneFromNormalResolver(
			new VariantResolver(new Variant128(Vector2.Zero), typeof(Vector2)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "PlaneFromNormal")]
	public void PlaneFromNormal_resolver_throws_for_non_float_distance()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new PlaneFromNormalResolver(
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)),
			new VariantResolver(new Variant128(2), typeof(int)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}
}
