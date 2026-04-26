// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class LookAtResolverTests
{
	[Fact]
	[Trait("Resolver", "LookAt")]
	public void LookAt_resolver_value_type_is_quaternion()
	{
		var resolver = new LookAtResolver(
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitZ), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(Quaternion));
	}

	[Fact]
	[Trait("Resolver", "LookAt")]
	public void LookAt_resolver_returns_identity_when_already_facing_target()
	{
		var resolver = new LookAtResolver(
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitZ), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)));

		Quaternion result = resolver.Resolve(new GraphContext()).AsQuaternion();

		result.X.Should().BeApproximately(Quaternion.Identity.X, TestUtils.Tolerance);
		result.Y.Should().BeApproximately(Quaternion.Identity.Y, TestUtils.Tolerance);
		result.Z.Should().BeApproximately(Quaternion.Identity.Z, TestUtils.Tolerance);
		result.W.Should().BeApproximately(Quaternion.Identity.W, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "LookAt")]
	public void LookAt_resolver_rotates_forward_toward_target_direction()
	{
		var resolver = new LookAtResolver(
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)));

		Quaternion result = resolver.Resolve(new GraphContext()).AsQuaternion();
		var forward = Vector3.Transform(Vector3.UnitZ, result);
		var up = Vector3.Transform(Vector3.UnitY, result);

		forward.X.Should().BeApproximately(Vector3.UnitX.X, TestUtils.Tolerance);
		forward.Y.Should().BeApproximately(Vector3.UnitX.Y, TestUtils.Tolerance);
		forward.Z.Should().BeApproximately(Vector3.UnitX.Z, TestUtils.Tolerance);
		up.X.Should().BeApproximately(Vector3.UnitY.X, TestUtils.Tolerance);
		up.Y.Should().BeApproximately(Vector3.UnitY.Y, TestUtils.Tolerance);
		up.Z.Should().BeApproximately(Vector3.UnitY.Z, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "LookAt")]
	public void LookAt_resolver_returns_identity_when_from_and_to_are_same()
	{
		var origin = new Vector3(2.0f, 3.0f, 4.0f);
		var resolver = new LookAtResolver(
			new VariantResolver(new Variant128(origin), typeof(Vector3)),
			new VariantResolver(new Variant128(origin), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)));

		Quaternion result = resolver.Resolve(new GraphContext()).AsQuaternion();

		result.X.Should().BeApproximately(Quaternion.Identity.X, TestUtils.Tolerance);
		result.Y.Should().BeApproximately(Quaternion.Identity.Y, TestUtils.Tolerance);
		result.Z.Should().BeApproximately(Quaternion.Identity.Z, TestUtils.Tolerance);
		result.W.Should().BeApproximately(Quaternion.Identity.W, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "LookAt")]
	public void LookAt_resolver_uses_fallback_axis_when_up_is_parallel_to_forward()
	{
		var resolver = new LookAtResolver(
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)));

		Quaternion result = resolver.Resolve(new GraphContext()).AsQuaternion();
		var forward = Vector3.Transform(Vector3.UnitZ, result);

		forward.X.Should().BeApproximately(Vector3.UnitY.X, TestUtils.Tolerance);
		forward.Y.Should().BeApproximately(Vector3.UnitY.Y, TestUtils.Tolerance);
		forward.Z.Should().BeApproximately(Vector3.UnitY.Z, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "LookAt")]
	public void LookAt_resolver_throws_for_non_vector3_inputs()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new LookAtResolver(
			new VariantResolver(new Variant128(Vector2.Zero), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector3.UnitZ), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}
}
