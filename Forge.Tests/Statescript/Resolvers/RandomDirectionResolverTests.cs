// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class RandomDirectionResolverTests
{
	[Fact]
	[Trait("Resolver", "RandomDirection")]
	public void RandomDirection_resolver_value_type_is_vector2()
	{
		var resolver = new RandomDirectionResolver(new FixedRandom());

		resolver.ValueType.Should().Be(typeof(Vector2));
	}

	[Fact]
	[Trait("Resolver", "RandomDirection")]
	public void RandomDirection_resolver_returns_unit_vector()
	{
		var resolver = new RandomDirectionResolver(new FixedRandom(nextSingle: 0.25f));
		Vector2 result = resolver.Resolve(new GraphContext()).AsVector2();

		result.Length().Should().BeApproximately(1.0f, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "RandomDirection")]
	public void RandomDirection_resolver_returns_unit_x_when_random_is_zero()
	{
		var resolver = new RandomDirectionResolver(new FixedRandom(nextSingle: 0.0f));
		Vector2 result = resolver.Resolve(new GraphContext()).AsVector2();

		result.X.Should().BeApproximately(1.0f, TestUtils.Tolerance);
		result.Y.Should().BeApproximately(0.0f, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "RandomDirection")]
	public void RandomDirection_resolver_returns_expected_direction_for_fixed_random_value()
	{
		var resolver = new RandomDirectionResolver(new FixedRandom(nextSingle: 0.25f));
		Vector2 result = resolver.Resolve(new GraphContext()).AsVector2();

		result.X.Should().BeApproximately(0.0f, TestUtils.Tolerance);
		result.Y.Should().BeApproximately(1.0f, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "RandomDirection")]
	public void RandomDirection_resolver_can_return_positive_x_boundary_when_random_is_near_one()
	{
		var resolver = new RandomDirectionResolver(new FixedRandom(nextSingle: 0.99999994f));
		Vector2 result = resolver.Resolve(new GraphContext()).AsVector2();

		result.X.Should().BeApproximately(1.0f, TestUtils.Tolerance);
		result.Y.Should().BeApproximately(0.0f, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "RandomDirection")]
	public void RandomDirection_resolver_uses_exclusive_angle_sampling()
	{
		var random = new TrackingRandom(nextSingles: [0.25f], nextSinglesInclusive: [1.0f]);
		var resolver = new RandomDirectionResolver(random);
		Vector2 result = resolver.Resolve(new GraphContext()).AsVector2();

		result.X.Should().BeApproximately(0.0f, TestUtils.Tolerance);
		result.Y.Should().BeApproximately(1.0f, TestUtils.Tolerance);
		random.NextSingleCalls.Should().Be(1);
		random.NextSingleInclusiveCalls.Should().Be(0);
	}
}
