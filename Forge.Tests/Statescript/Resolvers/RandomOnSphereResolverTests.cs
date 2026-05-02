// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Core;
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

	[Fact]
	[Trait("Resolver", "RandomOnSphere")]
	public void RandomOnSphere_resolver_can_return_south_pole_when_random_is_near_one()
	{
		var resolver = new RandomOnSphereResolver(new SequenceRandom(0.99999994f, 0.0f));
		Vector3 result = resolver.Resolve(new GraphContext()).AsVector3();

		result.Length().Should().BeApproximately(1.0f, TestUtils.Tolerance);
		result.X.Should().BeApproximately(0.0f, 0.001f);
		result.Y.Should().BeApproximately(0.0f, 0.001f);
		result.Z.Should().BeLessThan(-0.999f);
	}

	[Fact]
	[Trait("Resolver", "RandomOnSphere")]
	public void RandomOnSphere_resolver_uses_inclusive_pole_sampling_and_exclusive_angle_sampling()
	{
		var random = new TrackingRandom(nextSingles: [0.25f], nextSinglesInclusive: [1.0f]);
		var resolver = new RandomOnSphereResolver(random);
		Vector3 result = resolver.Resolve(new GraphContext()).AsVector3();

		result.X.Should().BeApproximately(0.0f, TestUtils.Tolerance);
		result.Y.Should().BeApproximately(0.0f, TestUtils.Tolerance);
		result.Z.Should().BeApproximately(-1.0f, TestUtils.Tolerance);
		random.NextSingleCalls.Should().Be(1);
		random.NextSingleInclusiveCalls.Should().Be(1);
	}

	private sealed class SequenceRandom(params float[] values) : IRandom
	{
		private readonly Queue<float> _values = new([.. values]);

		public int NextInt()
		{
			throw new NotImplementedException();
		}

		public int NextInt(int maxValue)
		{
			throw new NotImplementedException();
		}

		public int NextInt(int minValue, int maxValue)
		{
			throw new NotImplementedException();
		}

		public int NextIntInclusive(int minValue, int maxValue)
		{
			throw new NotImplementedException();
		}

		public float NextSingle()
		{
			return _values.Dequeue();
		}

		public float NextSingleInclusive()
		{
			return _values.Dequeue();
		}

		public double NextDouble()
		{
			throw new NotImplementedException();
		}

		public double NextDoubleInclusive()
		{
			throw new NotImplementedException();
		}

		public long NextInt64()
		{
			throw new NotImplementedException();
		}

		public long NextInt64(long maxValue)
		{
			throw new NotImplementedException();
		}

		public long NextInt64(long minValue, long maxValue)
		{
			throw new NotImplementedException();
		}

		public long NextInt64Inclusive(long minValue, long maxValue)
		{
			throw new NotImplementedException();
		}

		public void NextBytes(byte[] buffer)
		{
			throw new NotImplementedException();
		}

		public void NextBytes(Span<byte> buffer)
		{
			throw new NotImplementedException();
		}
	}
}
