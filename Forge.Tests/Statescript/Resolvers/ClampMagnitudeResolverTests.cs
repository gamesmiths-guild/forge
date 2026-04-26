// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ClampMagnitudeResolverTests
{
	[Fact]
	[Trait("Resolver", "ClampMagnitude")]
	public void ClampMagnitude_resolver_vector2_value_type_is_vector2()
	{
		var resolver = new ClampMagnitudeResolver(
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(Vector2));
	}

	[Fact]
	[Trait("Resolver", "ClampMagnitude")]
	public void ClampMagnitude_resolver_vector3_value_type_is_vector3()
	{
		var resolver = new ClampMagnitudeResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	[Fact]
	[Trait("Resolver", "ClampMagnitude")]
	public void ClampMagnitude_resolver_vector4_value_type_is_vector4()
	{
		var resolver = new ClampMagnitudeResolver(
			new VariantResolver(new Variant128(Vector4.One), typeof(Vector4)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(Vector4));
	}

	[Fact]
	[Trait("Resolver", "ClampMagnitude")]
	public void ClampMagnitude_resolver_vector3_clamps_length_without_changing_direction()
	{
		var resolver = new ClampMagnitudeResolver(
			new VariantResolver(new Variant128(new Vector3(3, 4, 0)), typeof(Vector3)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		Vector3 result = resolver.Resolve(new GraphContext()).AsVector3();

		result.X.Should().BeApproximately(1.2f, TestUtils.Tolerance);
		result.Y.Should().BeApproximately(1.6f, TestUtils.Tolerance);
		result.Z.Should().BeApproximately(0.0f, TestUtils.Tolerance);
		result.Length().Should().BeApproximately(2.0f, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "ClampMagnitude")]
	public void ClampMagnitude_resolver_vector2_returns_original_when_already_within_limit()
	{
		var value = new Vector2(1.0f, 2.0f);
		var resolver = new ClampMagnitudeResolver(
			new VariantResolver(new Variant128(value), typeof(Vector2)),
			new VariantResolver(new Variant128(3.0f), typeof(float)));

		resolver.Resolve(new GraphContext()).AsVector2().Should().Be(value);
	}

	[Fact]
	[Trait("Resolver", "ClampMagnitude")]
	public void ClampMagnitude_resolver_vector4_returns_zero_when_max_length_is_zero()
	{
		var resolver = new ClampMagnitudeResolver(
			new VariantResolver(new Variant128(new Vector4(1.0f, 2.0f, 3.0f, 4.0f)), typeof(Vector4)),
			new VariantResolver(new Variant128(0.0f), typeof(float)));

		resolver.Resolve(new GraphContext()).AsVector4().Should().Be(Vector4.Zero);
	}

	[Fact]
	[Trait("Resolver", "ClampMagnitude")]
	public void ClampMagnitude_resolver_returns_zero_when_max_length_is_negative()
	{
		var resolver = new ClampMagnitudeResolver(
			new VariantResolver(new Variant128(new Vector3(3, 4, 5)), typeof(Vector3)),
			new VariantResolver(new Variant128(-1.0f), typeof(float)));

		resolver.Resolve(new GraphContext()).AsVector3().Should().Be(Vector3.Zero);
	}

	[Fact]
	[Trait("Resolver", "ClampMagnitude")]
	public void ClampMagnitude_resolver_throws_for_non_vector_value_type()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new ClampMagnitudeResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "ClampMagnitude")]
	public void ClampMagnitude_resolver_throws_for_non_float_max_length()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new ClampMagnitudeResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)),
			new VariantResolver(new Variant128(2), typeof(int)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}
}
