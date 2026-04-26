// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class RandomOnSphereResolverTests
{
	[Fact]
	[Trait("Resolver", "RandomOnSphere")]
	public void RandomOnSphere_resolver_value_type_is_vector3()
	{
		var resolver = new RandomOnSphereResolver(new FixedRandom());

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	[Fact]
	[Trait("Resolver", "RandomOnSphere")]
	public void RandomOnSphere_resolver_returns_unit_vector()
	{
		var resolver = new RandomOnSphereResolver(new FixedRandom(nextSingle: 0.25f));
		Vector3 result = resolver.Resolve(new GraphContext()).AsVector3();

		result.Length().Should().BeApproximately(1.0f, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "RandomOnSphere")]
	public void RandomOnSphere_resolver_returns_north_pole_when_random_is_zero()
	{
		var resolver = new RandomOnSphereResolver(new FixedRandom(nextSingle: 0.0f));

		resolver.Resolve(new GraphContext()).AsVector3().Should().Be(Vector3.UnitZ);
	}

	[Fact]
	[Trait("Resolver", "RandomOnSphere")]
	public void RandomOnSphere_resolver_returns_expected_point_for_fixed_random_value()
	{
		var resolver = new RandomOnSphereResolver(new FixedRandom(nextSingle: 0.25f));
		Vector3 result = resolver.Resolve(new GraphContext()).AsVector3();

		result.X.Should().BeApproximately(0.0f, TestUtils.Tolerance);
		result.Y.Should().BeApproximately(0.8660254f, TestUtils.Tolerance);
		result.Z.Should().BeApproximately(0.5f, TestUtils.Tolerance);
	}
}
