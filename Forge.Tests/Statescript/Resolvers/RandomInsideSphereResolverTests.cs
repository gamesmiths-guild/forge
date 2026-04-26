// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class RandomInsideSphereResolverTests
{
	[Fact]
	[Trait("Resolver", "RandomInsideSphere")]
	public void RandomInsideSphere_resolver_value_type_is_vector3()
	{
		var resolver = new RandomInsideSphereResolver(new FixedRandom());

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	[Fact]
	[Trait("Resolver", "RandomInsideSphere")]
	public void RandomInsideSphere_resolver_returns_vector_inside_unit_sphere()
	{
		var resolver = new RandomInsideSphereResolver(new FixedRandom(nextSingle: 0.25f));
		Vector3 result = resolver.Resolve(new GraphContext()).AsVector3();

		result.LengthSquared().Should().BeLessThanOrEqualTo(1.0f);
	}

	[Fact]
	[Trait("Resolver", "RandomInsideSphere")]
	public void RandomInsideSphere_resolver_returns_zero_when_random_radius_is_zero()
	{
		var resolver = new RandomInsideSphereResolver(new FixedRandom(nextSingle: 0.0f));

		resolver.Resolve(new GraphContext()).AsVector3().Should().Be(Vector3.Zero);
	}

	[Fact]
	[Trait("Resolver", "RandomInsideSphere")]
	public void RandomInsideSphere_resolver_returns_expected_point_for_fixed_random_value()
	{
		var resolver = new RandomInsideSphereResolver(new FixedRandom(nextSingle: 0.25f));
		Vector3 result = resolver.Resolve(new GraphContext()).AsVector3();

		result.X.Should().BeApproximately(0.0f, TestUtils.Tolerance);
		result.Y.Should().BeApproximately(0.5455618f, TestUtils.Tolerance);
		result.Z.Should().BeApproximately(0.31498027f, TestUtils.Tolerance);
	}
}
