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

	[Theory]
	[Trait("Resolver", "RandomInsideSphere")]
	[InlineData(1.0f, 1.0f)]
	[InlineData(0.5f, 1.0f)]
	public void RandomInsideSphere_resolver_can_reach_sphere_boundary(float random1, float random2)
	{
		var random = new TrackingRandom(nextSingles: [0.999f], nextSinglesInclusive: [random1, random2]);
		var resolver = new RandomInsideSphereResolver(random);
		Vector3 result = resolver.Resolve(new GraphContext()).AsVector3();

		result.Length().Should().Be(1.0f);
		random.NextSingleCalls.Should().Be(1);
		random.NextSingleInclusiveCalls.Should().Be(2);
	}

	[Fact]
	[Trait("Resolver", "RandomInsideSphere")]
	public void RandomInsideSphere_resolver_uses_exclusive_angle_and_inclusive_surface_radius_sampling()
	{
		var random = new TrackingRandom(nextSingles: [0.0f, 0.25f], nextSinglesInclusive: [1.0f, 1.0f]);
		var resolver = new RandomInsideSphereResolver(random);
		Vector3 result = resolver.Resolve(new GraphContext()).AsVector3();

		result.X.Should().BeApproximately(0.0f, TestUtils.Tolerance);
		result.Y.Should().BeApproximately(0.0f, TestUtils.Tolerance);
		result.Z.Should().BeApproximately(-1.0f, TestUtils.Tolerance);
		random.NextSingleCalls.Should().Be(1);
		random.NextSingleInclusiveCalls.Should().Be(2);
	}
}
