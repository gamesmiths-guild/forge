// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class QuaternionFromYawPitchRollResolverTests
{
	[Fact]
	[Trait("Resolver", "QuaternionFromYawPitchRoll")]
	public void QuaternionFromYawPitchRoll_resolver_value_type_is_quaternion()
	{
		var resolver = new QuaternionFromYawPitchRollResolver(
			new VariantResolver(new Variant128(0.0f), typeof(float)),
			new VariantResolver(new Variant128(0.0f), typeof(float)),
			new VariantResolver(new Variant128(0.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(Quaternion));
	}

	[Fact]
	[Trait("Resolver", "QuaternionFromYawPitchRoll")]
	public void QuaternionFromYawPitchRoll_resolver_creates_quaternion_using_system_numerics_order()
	{
		const float yaw = (float)(Math.PI / 2f);
		const float pitch = (float)Math.PI;
		const float roll = (float)(Math.PI / 6f);
		var resolver = new QuaternionFromYawPitchRollResolver(
			new VariantResolver(new Variant128(yaw), typeof(float)),
			new VariantResolver(new Variant128(pitch), typeof(float)),
			new VariantResolver(new Variant128(roll), typeof(float)));

		var expected = Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);
		Quaternion result = resolver.Resolve(new GraphContext()).AsQuaternion();

		result.X.Should().Be(expected.X);
		result.Y.Should().Be(expected.Y);
		result.Z.Should().Be(expected.Z);
		result.W.Should().Be(expected.W);
	}

	[Fact]
	[Trait("Resolver", "QuaternionFromYawPitchRoll")]
	public void QuaternionFromYawPitchRoll_resolver_matches_euler_yxz_order()
	{
		var angles = new Vector3(0.25f, 0.5f, -0.75f);
		var resolver = new QuaternionFromYawPitchRollResolver(
			new VariantResolver(new Variant128(angles.Y), typeof(float)),
			new VariantResolver(new Variant128(angles.X), typeof(float)),
			new VariantResolver(new Variant128(angles.Z), typeof(float)));

		Quaternion result = resolver.Resolve(new GraphContext()).AsQuaternion();
		Quaternion expected = new QuaternionFromEulerAnglesResolver(
			new VariantResolver(new Variant128(angles), typeof(Vector3)),
			new VariantResolver(new Variant128((int)EulerOrder.YXZ), typeof(int)))
			.Resolve(new GraphContext())
			.AsQuaternion();

		result.X.Should().Be(expected.X);
		result.Y.Should().Be(expected.Y);
		result.Z.Should().Be(expected.Z);
		result.W.Should().Be(expected.W);
	}

	[Fact]
	[Trait("Resolver", "QuaternionFromYawPitchRoll")]
	public void QuaternionFromYawPitchRoll_resolver_round_trips_with_euler_angles_from_quaternion_yxz()
	{
		var expected = new Vector3(-0.2f, 0.35f, 0.6f);
		Quaternion quaternion = new QuaternionFromYawPitchRollResolver(
			new VariantResolver(new Variant128(expected.Y), typeof(float)),
			new VariantResolver(new Variant128(expected.X), typeof(float)),
			new VariantResolver(new Variant128(expected.Z), typeof(float)))
			.Resolve(new GraphContext())
			.AsQuaternion();

		Vector3 extracted = new EulerAnglesFromQuaternionResolver(
			new VariantResolver(new Variant128(quaternion), typeof(Quaternion)),
			new VariantResolver(new Variant128((int)EulerOrder.YXZ), typeof(int)))
			.Resolve(new GraphContext())
			.AsVector3();

		extracted.X.Should().Be(expected.X);
		extracted.Y.Should().Be(expected.Y);
		extracted.Z.Should().Be(expected.Z);
	}

	[Fact]
	[Trait("Resolver", "QuaternionFromYawPitchRoll")]
	public void QuaternionFromYawPitchRoll_resolver_throws_for_non_float_angle()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new QuaternionFromYawPitchRollResolver(
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)),
			new VariantResolver(new Variant128(0.0f), typeof(float)),
			new VariantResolver(new Variant128(0.0f), typeof(float)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}
}
