// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class RandomInsideCircleResolverTests
{
	[Fact]
	[Trait("Resolver", "RandomInsideCircle")]
	public void RandomInsideCircle_resolver_value_type_is_vector2()
	{
		var resolver = new RandomInsideCircleResolver(new FixedRandom());

		resolver.ValueType.Should().Be(typeof(Vector2));
	}

	[Fact]
	[Trait("Resolver", "RandomInsideCircle")]
	public void RandomInsideCircle_resolver_returns_vector_inside_unit_circle()
	{
		var resolver = new RandomInsideCircleResolver(new FixedRandom(nextSingle: 0.25f));
		Vector2 result = resolver.Resolve(new GraphContext()).AsVector2();

		result.LengthSquared().Should().BeLessThanOrEqualTo(1.0f);
	}

	[Fact]
	[Trait("Resolver", "RandomInsideCircle")]
	public void RandomInsideCircle_resolver_returns_zero_when_random_radius_is_zero()
	{
		var resolver = new RandomInsideCircleResolver(new FixedRandom(nextSingle: 0.0f));

		resolver.Resolve(new GraphContext()).AsVector2().Should().Be(Vector2.Zero);
	}

	[Fact]
	[Trait("Resolver", "RandomInsideCircle")]
	public void RandomInsideCircle_resolver_returns_expected_point_for_fixed_random_value()
	{
		var resolver = new RandomInsideCircleResolver(new FixedRandom(nextSingle: 0.25f));
		Vector2 result = resolver.Resolve(new GraphContext()).AsVector2();

		result.X.Should().BeApproximately(0.0f, TestUtils.Tolerance);
		result.Y.Should().BeApproximately(0.5f, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "RandomInsideCircle")]
	public void RandomInsideCircle_resolver_can_reach_circle_boundary()
	{
		var random = new TrackingRandom(nextSingles: [0.999f], nextSinglesInclusive: [1.0f]);
		var resolver = new RandomInsideCircleResolver(random);
		Vector2 result = resolver.Resolve(new GraphContext()).AsVector2();

		result.Length().Should().Be(1.0f);
		random.NextSingleCalls.Should().Be(1);
		random.NextSingleInclusiveCalls.Should().Be(1);
	}

	[Fact]
	[Trait("Resolver", "RandomInsideCircle")]
	public void RandomInsideCircle_resolver_uses_exclusive_angle_and_inclusive_radius_sampling()
	{
		var random = new TrackingRandom(nextSingles: [0.25f], nextSinglesInclusive: [1.0f]);
		var resolver = new RandomInsideCircleResolver(random);
		Vector2 result = resolver.Resolve(new GraphContext()).AsVector2();

		result.X.Should().BeApproximately(0.0f, TestUtils.Tolerance);
		result.Y.Should().BeApproximately(1.0f, TestUtils.Tolerance);
		random.NextSingleCalls.Should().Be(1);
		random.NextSingleInclusiveCalls.Should().Be(1);
	}
}
