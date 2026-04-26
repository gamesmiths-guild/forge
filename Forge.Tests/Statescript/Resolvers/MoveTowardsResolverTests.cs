// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class MoveTowardsResolverTests
{
	[Fact]
	[Trait("Resolver", "MoveTowards")]
	public void MoveTowards_resolver_float_value_type_is_float()
	{
		var resolver = new MoveTowardsResolver(
			new VariantResolver(new Variant128(0.0f), typeof(float)),
			new VariantResolver(new Variant128(10.0f), typeof(float)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "MoveTowards")]
	public void MoveTowards_resolver_float_moves_toward_target_without_overshoot()
	{
		var resolver = new MoveTowardsResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)),
			new VariantResolver(new Variant128(5.0f), typeof(float)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		resolver.Resolve(new GraphContext()).AsFloat().Should().Be(3.0f);
	}

	[Fact]
	[Trait("Resolver", "MoveTowards")]
	public void MoveTowards_resolver_float_returns_target_when_within_delta()
	{
		var resolver = new MoveTowardsResolver(
			new VariantResolver(new Variant128(4.0f), typeof(float)),
			new VariantResolver(new Variant128(5.0f), typeof(float)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		resolver.Resolve(new GraphContext()).AsFloat().Should().Be(5.0f);
	}

	[Fact]
	[Trait("Resolver", "MoveTowards")]
	public void MoveTowards_resolver_float_returns_current_when_max_delta_is_negative()
	{
		var resolver = new MoveTowardsResolver(
			new VariantResolver(new Variant128(4.0f), typeof(float)),
			new VariantResolver(new Variant128(10.0f), typeof(float)),
			new VariantResolver(new Variant128(-1.0f), typeof(float)));

		resolver.Resolve(new GraphContext()).AsFloat().Should().Be(4.0f);
	}

	[Fact]
	[Trait("Resolver", "MoveTowards")]
	public void MoveTowards_resolver_vector3_moves_toward_target()
	{
		var resolver = new MoveTowardsResolver(
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)),
			new VariantResolver(new Variant128(new Vector3(10, 0, 0)), typeof(Vector3)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		resolver.Resolve(new GraphContext()).AsVector3().Should().Be(new Vector3(2, 0, 0));
	}

	[Fact]
	[Trait("Resolver", "MoveTowards")]
	public void MoveTowards_resolver_vector2_returns_target_when_already_close()
	{
		var resolver = new MoveTowardsResolver(
			new VariantResolver(new Variant128(new Vector2(1.0f, 1.0f)), typeof(Vector2)),
			new VariantResolver(new Variant128(new Vector2(2.0f, 1.0f)), typeof(Vector2)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		resolver.Resolve(new GraphContext()).AsVector2().Should().Be(new Vector2(2.0f, 1.0f));
	}

	[Fact]
	[Trait("Resolver", "MoveTowards")]
	public void MoveTowards_resolver_vector4_returns_current_when_max_delta_is_zero()
	{
		var current = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
		var resolver = new MoveTowardsResolver(
			new VariantResolver(new Variant128(current), typeof(Vector4)),
			new VariantResolver(new Variant128(new Vector4(4.0f, 3.0f, 2.0f, 1.0f)), typeof(Vector4)),
			new VariantResolver(new Variant128(0.0f), typeof(float)));

		resolver.Resolve(new GraphContext()).AsVector4().Should().Be(current);
	}

	[Fact]
	[Trait("Resolver", "MoveTowards")]
	public void MoveTowards_resolver_throws_for_mismatched_current_and_target_types()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new MoveTowardsResolver(
			new VariantResolver(new Variant128(0.0f), typeof(float)),
			new VariantResolver(new Variant128(Vector2.Zero), typeof(Vector2)),
			new VariantResolver(new Variant128(1.0f), typeof(float)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "MoveTowards")]
	public void MoveTowards_resolver_throws_for_non_float_max_delta()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new MoveTowardsResolver(
			new VariantResolver(new Variant128(0.0f), typeof(float)),
			new VariantResolver(new Variant128(1.0f), typeof(float)),
			new VariantResolver(new Variant128(1), typeof(int)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}
}
