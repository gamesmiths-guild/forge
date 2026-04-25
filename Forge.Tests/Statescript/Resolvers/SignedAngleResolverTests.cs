// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class SignedAngleResolverTests
{
	[Fact]
	[Trait("Resolver", "SignedAngle")]
	public void SignedAngle_resolver_vector2_computes_positive_signed_angle()
	{
		var resolver = new SignedAngleResolver(
			new VariantResolver(new Variant128(Vector2.UnitX), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector2.UnitY), typeof(Vector2)));

		resolver.Resolve(new GraphContext()).AsFloat().Should().Be(MathF.PI / 2.0f);
	}

	[Fact]
	[Trait("Resolver", "SignedAngle")]
	public void SignedAngle_resolver_vector2_computes_negative_signed_angle()
	{
		var resolver = new SignedAngleResolver(
			new VariantResolver(new Variant128(Vector2.UnitY), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector2.UnitX), typeof(Vector2)));

		resolver.Resolve(new GraphContext()).AsFloat().Should().Be(-MathF.PI / 2.0f);
	}

	[Fact]
	[Trait("Resolver", "SignedAngle")]
	public void SignedAngle_resolver_vector3_uses_axis()
	{
		var resolver = new SignedAngleResolver(
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitZ), typeof(Vector3)));

		resolver.Resolve(new GraphContext()).AsFloat().Should().Be(MathF.PI / 2.0f);
	}

	[Fact]
	[Trait("Resolver", "SignedAngle")]
	public void SignedAngle_resolver_vector3_flips_sign_with_opposite_axis()
	{
		var resolver = new SignedAngleResolver(
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)),
			new VariantResolver(new Variant128(-Vector3.UnitZ), typeof(Vector3)));

		resolver.Resolve(new GraphContext()).AsFloat().Should().Be(-MathF.PI / 2.0f);
	}

	[Fact]
	[Trait("Resolver", "SignedAngle")]
	public void SignedAngle_resolver_throws_for_axis_with_vector2()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new SignedAngleResolver(
			new VariantResolver(new Variant128(Vector2.UnitX), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector2.UnitY), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector3.UnitZ), typeof(Vector3)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "SignedAngle")]
	public void SignedAngle_resolver_throws_for_vector3_without_axis()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new SignedAngleResolver(
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "SignedAngle")]
	public void SignedAngle_resolver_throws_for_mismatched_types()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new SignedAngleResolver(
			new VariantResolver(new Variant128(Vector2.UnitX), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}
}
