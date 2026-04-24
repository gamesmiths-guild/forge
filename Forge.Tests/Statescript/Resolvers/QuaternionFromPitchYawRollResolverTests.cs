// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class QuaternionFromPitchYawRollResolverTests
{
	[Fact]
	[Trait("Resolver", "QuaternionFromPitchYawRoll")]
	public void QuaternionFromPitchYawRoll_resolver_value_type_is_quaternion()
	{
		var resolver = new QuaternionFromPitchYawRollResolver(
			new VariantResolver(new Variant128(0.0f), typeof(float)),
			new VariantResolver(new Variant128(0.0f), typeof(float)),
			new VariantResolver(new Variant128(0.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(Quaternion));
	}

	[Fact]
	[Trait("Resolver", "QuaternionFromPitchYawRoll")]
	public void QuaternionFromPitchYawRoll_resolver_creates_quaternion()
	{
		const float pitch = (float)Math.PI;
		const float yaw = (float)(Math.PI / 2f);
		const float roll = (float)(Math.PI / 6f);
		var resolver = new QuaternionFromPitchYawRollResolver(
			new VariantResolver(new Variant128(pitch), typeof(float)),
			new VariantResolver(new Variant128(yaw), typeof(float)),
			new VariantResolver(new Variant128(roll), typeof(float)));

		var context = new GraphContext();
		var expected = Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);
		Quaternion result = resolver.Resolve(context).AsQuaternion();

		result.X.Should().Be(expected.X);
		result.Y.Should().Be(expected.Y);
		result.Z.Should().Be(expected.Z);
		result.W.Should().Be(expected.W);
	}
}
