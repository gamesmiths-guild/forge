// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class QuaternionFromPitchYawRollResolverTests
{
  private const float Tolerance = 0.0001f;

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
    public void QuaternionFromPitchYawRoll_resolver_creates_quaternion_with_default_order()
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

       result.X.Should().BeApproximately(expected.X, Tolerance);
		result.Y.Should().BeApproximately(expected.Y, Tolerance);
		result.Z.Should().BeApproximately(expected.Z, Tolerance);
		result.W.Should().BeApproximately(expected.W, Tolerance);
	}

	[Fact]
	[Trait("Resolver", "QuaternionFromPitchYawRoll")]
	public void QuaternionFromPitchYawRoll_resolver_supports_explicit_euler_order()
	{
      var angles = new Vector3(0.25f, 0.5f, -0.75f);
		var resolver = new QuaternionFromPitchYawRollResolver(
          new VariantResolver(new Variant128(angles.X), typeof(float)),
			new VariantResolver(new Variant128(angles.Y), typeof(float)),
			new VariantResolver(new Variant128(angles.Z), typeof(float)),
			new VariantResolver(new Variant128((int)EulerOrder.RollYawPitch), typeof(int)));

       Quaternion result = resolver.Resolve(new GraphContext()).AsQuaternion();
		var extracted = new EulerAnglesFromQuaternionResolver(
			new VariantResolver(new Variant128(result), typeof(Quaternion)),
			new VariantResolver(new Variant128((int)EulerOrder.RollYawPitch), typeof(int)))
			.Resolve(new GraphContext())
			.AsVector3();

       extracted.X.Should().BeApproximately(angles.X, Tolerance);
		extracted.Y.Should().BeApproximately(angles.Y, Tolerance);
		extracted.Z.Should().BeApproximately(angles.Z, Tolerance);
	}

	[Fact]
	[Trait("Resolver", "QuaternionFromPitchYawRoll")]
	public void QuaternionFromPitchYawRoll_resolver_throws_for_non_float_angle()
	{
		Action act = () => new QuaternionFromPitchYawRollResolver(
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)),
			new VariantResolver(new Variant128(0.0f), typeof(float)),
			new VariantResolver(new Variant128(0.0f), typeof(float)));

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "QuaternionFromPitchYawRoll")]
	public void QuaternionFromPitchYawRoll_resolver_throws_for_non_int_order()
	{
		Action act = () => new QuaternionFromPitchYawRollResolver(
			new VariantResolver(new Variant128(0.0f), typeof(float)),
			new VariantResolver(new Variant128(0.0f), typeof(float)),
			new VariantResolver(new Variant128(0.0f), typeof(float)),
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "QuaternionFromPitchYawRoll")]
	public void QuaternionFromPitchYawRoll_resolver_throws_for_unsupported_euler_order()
	{
		var resolver = new QuaternionFromPitchYawRollResolver(
			new VariantResolver(new Variant128(0.0f), typeof(float)),
			new VariantResolver(new Variant128(0.0f), typeof(float)),
			new VariantResolver(new Variant128(0.0f), typeof(float)),
			new VariantResolver(new Variant128(999), typeof(int)));

		Action act = () => resolver.Resolve(new GraphContext());

		act.Should().Throw<ArgumentOutOfRangeException>();
	}
}
