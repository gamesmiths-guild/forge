// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class QuaternionFromEulerAnglesResolverTests
{
	[Fact]
	[Trait("Resolver", "QuaternionFromEulerAngles")]
	public void QuaternionFromEulerAngles_resolver_value_type_is_quaternion()
	{
		var resolver = new QuaternionFromEulerAnglesResolver(
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(Quaternion));
	}

	[Fact]
	[Trait("Resolver", "QuaternionFromEulerAngles")]
	public void QuaternionFromEulerAngles_resolver_creates_quaternion_with_default_order()
	{
		var eulerAngles = new Vector3(0.3f, 0.5f, -0.2f);
		var resolver = new QuaternionFromEulerAnglesResolver(
			new VariantResolver(new Variant128(eulerAngles), typeof(Vector3)));

		Quaternion expected = Quaternion.CreateFromAxisAngle(Vector3.UnitX, eulerAngles.X)
			* Quaternion.CreateFromAxisAngle(Vector3.UnitY, eulerAngles.Y)
			* Quaternion.CreateFromAxisAngle(Vector3.UnitZ, eulerAngles.Z);
		Quaternion result = resolver.Resolve(new GraphContext()).AsQuaternion();

		result.X.Should().Be(expected.X);
		result.Y.Should().Be(expected.Y);
		result.Z.Should().Be(expected.Z);
		result.W.Should().Be(expected.W);
	}

	[Fact]
	[Trait("Resolver", "QuaternionFromEulerAngles")]
	public void QuaternionFromEulerAngles_resolver_supports_explicit_euler_order()
	{
		var eulerAngles = new Vector3(-0.4f, 0.25f, 0.8f);
		var resolver = new QuaternionFromEulerAnglesResolver(
			new VariantResolver(new Variant128(eulerAngles), typeof(Vector3)),
			new VariantResolver(new Variant128((int)EulerOrder.XZY), typeof(int)));

		Quaternion result = resolver.Resolve(new GraphContext()).AsQuaternion();
		Vector3 roundTripAngles = new EulerAnglesFromQuaternionResolver(
			new VariantResolver(new Variant128(result), typeof(Quaternion)),
			new VariantResolver(new Variant128((int)EulerOrder.XZY), typeof(int)))
				.Resolve(new GraphContext())
				.AsVector3();

		roundTripAngles.X.Should().BeApproximately(eulerAngles.X, TestUtils.Tolerance);
		roundTripAngles.Y.Should().BeApproximately(eulerAngles.Y, TestUtils.Tolerance);
		roundTripAngles.Z.Should().BeApproximately(eulerAngles.Z, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "QuaternionFromEulerAngles")]
	public void QuaternionFromEulerAngles_resolver_throws_for_non_vector3_angles()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new QuaternionFromEulerAnglesResolver(
			new VariantResolver(new Variant128(0.0f), typeof(float)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "QuaternionFromEulerAngles")]
	public void QuaternionFromEulerAngles_resolver_throws_for_non_int_order()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new QuaternionFromEulerAnglesResolver(
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)),
			new VariantResolver(new Variant128(1.0f), typeof(float)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "QuaternionFromEulerAngles")]
	public void QuaternionFromEulerAngles_resolver_throws_for_unsupported_euler_order()
	{
		var resolver = new QuaternionFromEulerAnglesResolver(
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)),
			new VariantResolver(new Variant128(-1), typeof(int)));

		Action act = () => resolver.Resolve(new GraphContext());

		act.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("order");
	}
}
