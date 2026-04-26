// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class EulerAnglesFromQuaternionResolverTests
{
	[Fact]
	[Trait("Resolver", "EulerAnglesFromQuaternion")]
	public void EulerAnglesFromQuaternion_resolver_returns_vector3()
	{
		var resolver = new EulerAnglesFromQuaternionResolver(
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)));

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	[Fact]
	[Trait("Resolver", "EulerAnglesFromQuaternion")]
	public void EulerAnglesFromQuaternion_resolver_extracts_default_order_angles()
	{
		var expected = new Vector3(0.35f, -0.2f, 0.6f);
		Quaternion quaternion = new QuaternionFromEulerAnglesResolver(
			new VariantResolver(new Variant128(expected), typeof(Vector3)))
				.Resolve(new GraphContext())
				.AsQuaternion();
		var resolver = new EulerAnglesFromQuaternionResolver(
			new VariantResolver(new Variant128(quaternion), typeof(Quaternion)));

		Vector3 result = resolver.Resolve(new GraphContext()).AsVector3();

		result.X.Should().BeApproximately(expected.X, TestUtils.Tolerance);
		result.Y.Should().BeApproximately(expected.Y, TestUtils.Tolerance);
		result.Z.Should().BeApproximately(expected.Z, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "EulerAnglesFromQuaternion")]
	public void EulerAnglesFromQuaternion_resolver_extracts_angles_for_explicit_order()
	{
		var expected = new Vector3(0.25f, 0.4f, -0.5f);
		Quaternion quaternion = new QuaternionFromEulerAnglesResolver(
			new VariantResolver(new Variant128(expected), typeof(Vector3)),
			new VariantResolver(new Variant128((int)EulerOrder.ZYX), typeof(int)))
				.Resolve(new GraphContext())
				.AsQuaternion();
		var resolver = new EulerAnglesFromQuaternionResolver(
			new VariantResolver(new Variant128(quaternion), typeof(Quaternion)),
			new VariantResolver(new Variant128((int)EulerOrder.ZYX), typeof(int)));

		Vector3 result = resolver.Resolve(new GraphContext()).AsVector3();

		result.X.Should().BeApproximately(expected.X, TestUtils.Tolerance);
		result.Y.Should().BeApproximately(expected.Y, TestUtils.Tolerance);
		result.Z.Should().BeApproximately(expected.Z, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "EulerAnglesFromQuaternion")]
	public void EulerAnglesFromQuaternion_resolver_round_trips_quaternion()
	{
		Quaternion original = new QuaternionFromEulerAnglesResolver(
			new VariantResolver(new Variant128(new Vector3(-0.3f, 0.45f, 0.2f)), typeof(Vector3)),
			new VariantResolver(new Variant128((int)EulerOrder.XZY), typeof(int)))
				.Resolve(new GraphContext())
				.AsQuaternion();
		var resolver = new EulerAnglesFromQuaternionResolver(
			new VariantResolver(new Variant128(original), typeof(Quaternion)),
			new VariantResolver(new Variant128((int)EulerOrder.XZY), typeof(int)));

		Vector3 result = resolver.Resolve(new GraphContext()).AsVector3();
		Quaternion roundTrip = new QuaternionFromEulerAnglesResolver(
			new VariantResolver(new Variant128(result), typeof(Vector3)),
			new VariantResolver(new Variant128((int)EulerOrder.XZY), typeof(int)))
				.Resolve(new GraphContext())
				.AsQuaternion();

		Quaternion.Dot(original, roundTrip).Should().BeApproximately(1.0f, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "EulerAnglesFromQuaternion")]
	public void EulerAnglesFromQuaternion_resolver_throws_for_non_quaternion_operand()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new EulerAnglesFromQuaternionResolver(
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "EulerAnglesFromQuaternion")]
	public void EulerAnglesFromQuaternion_resolver_throws_for_non_int_order()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new EulerAnglesFromQuaternionResolver(
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(1.0f), typeof(float)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "EulerAnglesFromQuaternion")]
	public void EulerAnglesFromQuaternion_resolver_throws_for_unsupported_euler_order()
	{
		var resolver = new EulerAnglesFromQuaternionResolver(
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(999), typeof(int)));

		Action act = () => resolver.Resolve(new GraphContext());

		act.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("order");
	}
}
