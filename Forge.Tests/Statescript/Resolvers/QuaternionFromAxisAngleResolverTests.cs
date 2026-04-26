// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class QuaternionFromAxisAngleResolverTests
{
	[Fact]
	[Trait("Resolver", "QuaternionFromAxisAngle")]
	public void QuaternionFromAxisAngle_resolver_value_type_is_quaternion()
	{
		var resolver = new QuaternionFromAxisAngleResolver(
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)),
			new VariantResolver(new Variant128(0.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(Quaternion));
	}

	[Fact]
	[Trait("Resolver", "QuaternionFromAxisAngle")]
	public void QuaternionFromAxisAngle_resolver_creates_quaternion()
	{
		Vector3 axis = Vector3.UnitX;
		const float angle = (float)(Math.PI / 4f);
		var resolver = new QuaternionFromAxisAngleResolver(
			new VariantResolver(new Variant128(axis), typeof(Vector3)),
			new VariantResolver(new Variant128(angle), typeof(float)));

		var context = new GraphContext();
		var expected = Quaternion.CreateFromAxisAngle(axis, angle);
		Quaternion result = resolver.Resolve(context).AsQuaternion();

		result.X.Should().Be(expected.X);
		result.Y.Should().Be(expected.Y);
		result.Z.Should().Be(expected.Z);
		result.W.Should().Be(expected.W);
	}

	[Fact]
	[Trait("Resolver", "QuaternionFromAxisAngle")]
	public void QuaternionFromAxisAngle_resolver_supports_non_unit_axis()
	{
		var axis = new Vector3(2.0f, 0.0f, 0.0f);
		const float angle = 0.5f;
		var resolver = new QuaternionFromAxisAngleResolver(
			new VariantResolver(new Variant128(axis), typeof(Vector3)),
			new VariantResolver(new Variant128(angle), typeof(float)));

		var expected = Quaternion.CreateFromAxisAngle(axis, angle);
		Quaternion result = resolver.Resolve(new GraphContext()).AsQuaternion();

		result.X.Should().Be(expected.X);
		result.Y.Should().Be(expected.Y);
		result.Z.Should().Be(expected.Z);
		result.W.Should().Be(expected.W);
	}

	[Fact]
	[Trait("Resolver", "QuaternionFromAxisAngle")]
	public void QuaternionFromAxisAngle_resolver_throws_for_non_vector3_axis()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new QuaternionFromAxisAngleResolver(
			new VariantResolver(new Variant128(Vector2.Zero), typeof(Vector2)),
			new VariantResolver(new Variant128(0.0f), typeof(float)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "QuaternionFromAxisAngle")]
	public void QuaternionFromAxisAngle_resolver_throws_for_non_float_angle()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new QuaternionFromAxisAngleResolver(
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)),
			new VariantResolver(new Variant128(1), typeof(int)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}
}
