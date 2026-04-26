// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class RejectResolverTests
{
	[Fact]
	[Trait("Resolver", "Reject")]
	public void Reject_resolver_vector2_value_type_is_vector2()
	{
		var resolver = new RejectResolver(
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector2.UnitX), typeof(Vector2)));

		resolver.ValueType.Should().Be(typeof(Vector2));
	}

	[Fact]
	[Trait("Resolver", "Reject")]
	public void Reject_resolver_vector3_rejects_projected_component()
	{
		var resolver = new RejectResolver(
			new VariantResolver(new Variant128(new Vector3(2, 2, 0)), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)));

		resolver.Resolve(new GraphContext()).AsVector3().Should().Be(new Vector3(0, 2, 0));
	}

	[Fact]
	[Trait("Resolver", "Reject")]
	public void Reject_resolver_returns_original_vector_when_value_is_perpendicular()
	{
		var value = Vector3.UnitY;
		var resolver = new RejectResolver(
			new VariantResolver(new Variant128(value), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)));

		resolver.Resolve(new GraphContext()).AsVector3().Should().Be(value);
	}

	[Fact]
	[Trait("Resolver", "Reject")]
	public void Reject_resolver_returns_zero_when_value_is_zero()
	{
		var resolver = new RejectResolver(
			new VariantResolver(new Variant128(Vector2.Zero), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector2.UnitX), typeof(Vector2)));

		resolver.Resolve(new GraphContext()).AsVector2().Should().Be(Vector2.Zero);
	}

	[Fact]
	[Trait("Resolver", "Reject")]
	public void Reject_resolver_returns_original_vector_when_onto_is_zero()
	{
		var value = new Vector2(3.0f, 4.0f);
		var resolver = new RejectResolver(
			new VariantResolver(new Variant128(value), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector2.Zero), typeof(Vector2)));

		resolver.Resolve(new GraphContext()).AsVector2().Should().Be(value);
	}

	[Fact]
	[Trait("Resolver", "Reject")]
	public void Reject_resolver_supports_vector4_rejection()
	{
		var resolver = new RejectResolver(
			new VariantResolver(new Variant128(new Vector4(2.0f, 4.0f, 6.0f, 8.0f)), typeof(Vector4)),
			new VariantResolver(new Variant128(new Vector4(1.0f, 0.0f, 0.0f, 0.0f)), typeof(Vector4)));

		resolver.Resolve(new GraphContext()).AsVector4().Should().Be(new Vector4(0.0f, 4.0f, 6.0f, 8.0f));
	}

	[Fact]
	[Trait("Resolver", "Reject")]
	public void Reject_resolver_throws_for_mismatched_types()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new RejectResolver(
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Reject")]
	public void Reject_resolver_throws_for_non_vector_types()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new RejectResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}
}
