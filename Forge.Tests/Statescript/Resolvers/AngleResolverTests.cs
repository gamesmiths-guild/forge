// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class AngleResolverTests
{
	[Fact]
	[Trait("Resolver", "Angle")]
	public void Angle_resolver_value_type_is_float()
	{
		var resolver = new AngleResolver(
			new VariantResolver(new Variant128(Vector2.UnitX), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector2.UnitY), typeof(Vector2)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Angle")]
	public void Angle_resolver_vector2_right_angle()
	{
		var resolver = new AngleResolver(
			new VariantResolver(new Variant128(Vector2.UnitX), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector2.UnitY), typeof(Vector2)));

		resolver.Resolve(new GraphContext()).AsFloat().Should().Be(MathF.PI / 2.0f);
	}

	[Fact]
	[Trait("Resolver", "Angle")]
	public void Angle_resolver_vector3_opposite_vectors_returns_pi()
	{
		var resolver = new AngleResolver(
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)),
			new VariantResolver(new Variant128(-Vector3.UnitX), typeof(Vector3)));

		resolver.Resolve(new GraphContext()).AsFloat().Should().Be(MathF.PI);
	}

	[Fact]
	[Trait("Resolver", "Angle")]
	public void Angle_resolver_quaternion_matches_rotation_difference()
	{
		Quaternion from = Quaternion.Identity;
		var to = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 4.0f);
		var resolver = new AngleResolver(
			new VariantResolver(new Variant128(from), typeof(Quaternion)),
			new VariantResolver(new Variant128(to), typeof(Quaternion)));

		resolver.Resolve(new GraphContext()).AsFloat().Should().BeApproximately(MathF.PI / 4.0f, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Angle")]
	public void Angle_resolver_zero_length_vector_returns_zero()
	{
		var resolver = new AngleResolver(
			new VariantResolver(new Variant128(Vector2.Zero), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector2.UnitY), typeof(Vector2)));

		resolver.Resolve(new GraphContext()).AsFloat().Should().Be(0.0f);
	}

	[Fact]
	[Trait("Resolver", "Angle")]
	public void Angle_resolver_throws_for_mismatched_types()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new AngleResolver(
			new VariantResolver(new Variant128(Vector2.UnitX), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Angle")]
	public void Angle_resolver_throws_for_unsupported_type()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new AngleResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}
}
