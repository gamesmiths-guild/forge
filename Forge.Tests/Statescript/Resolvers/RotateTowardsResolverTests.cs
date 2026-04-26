// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class RotateTowardsResolverTests
{
	[Fact]
	[Trait("Resolver", "RotateTowards")]
	public void RotateTowards_resolver_value_type_is_quaternion()
	{
		var resolver = new RotateTowardsResolver(
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(Quaternion));
	}

	[Fact]
	[Trait("Resolver", "RotateTowards")]
	public void RotateTowards_resolver_rotates_toward_target_by_max_delta()
	{
		Quaternion current = Quaternion.Identity;
		var target = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2.0f);
		var resolver = new RotateTowardsResolver(
			new VariantResolver(new Variant128(current), typeof(Quaternion)),
			new VariantResolver(new Variant128(target), typeof(Quaternion)),
			new VariantResolver(new Variant128(MathF.PI / 4.0f), typeof(float)));

		Quaternion result = resolver.Resolve(new GraphContext()).AsQuaternion();
		var expected = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 4.0f);

		result.X.Should().BeApproximately(expected.X, TestUtils.Tolerance);
		result.Y.Should().BeApproximately(expected.Y, TestUtils.Tolerance);
		result.Z.Should().BeApproximately(expected.Z, TestUtils.Tolerance);
		result.W.Should().BeApproximately(expected.W, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "RotateTowards")]
	public void RotateTowards_resolver_returns_target_when_within_delta()
	{
		Quaternion current = Quaternion.Identity;
		var target = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.5f);
		var resolver = new RotateTowardsResolver(
			new VariantResolver(new Variant128(current), typeof(Quaternion)),
			new VariantResolver(new Variant128(target), typeof(Quaternion)),
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		Quaternion result = resolver.Resolve(new GraphContext()).AsQuaternion();

		result.X.Should().BeApproximately(target.X, TestUtils.Tolerance);
		result.Y.Should().BeApproximately(target.Y, TestUtils.Tolerance);
		result.Z.Should().BeApproximately(target.Z, TestUtils.Tolerance);
		result.W.Should().BeApproximately(target.W, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "RotateTowards")]
	public void RotateTowards_resolver_returns_current_when_max_delta_is_negative()
	{
		var current = Quaternion.CreateFromAxisAngle(Vector3.UnitX, 0.25f);
		var target = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 1.0f);
		var resolver = new RotateTowardsResolver(
			new VariantResolver(new Variant128(current), typeof(Quaternion)),
			new VariantResolver(new Variant128(target), typeof(Quaternion)),
			new VariantResolver(new Variant128(-0.1f), typeof(float)));

		Quaternion result = resolver.Resolve(new GraphContext()).AsQuaternion();

		result.X.Should().BeApproximately(current.X, TestUtils.Tolerance);
		result.Y.Should().BeApproximately(current.Y, TestUtils.Tolerance);
		result.Z.Should().BeApproximately(current.Z, TestUtils.Tolerance);
		result.W.Should().BeApproximately(current.W, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "RotateTowards")]
	public void RotateTowards_resolver_throws_for_non_quaternion_inputs()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new RotateTowardsResolver(
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)),
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "RotateTowards")]
	public void RotateTowards_resolver_throws_for_non_float_max_delta()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new RotateTowardsResolver(
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(1), typeof(int)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}
}
