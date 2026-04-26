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
}
