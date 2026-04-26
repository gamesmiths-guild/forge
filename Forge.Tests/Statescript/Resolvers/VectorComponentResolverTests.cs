// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class VectorComponentResolverTests
{
	[Fact]
	[Trait("Resolver", "VectorComponent")]
	public void VectorComponent_resolver_value_type_is_float()
	{
		var resolver = new VectorComponentResolver(
			new VariantResolver(new Variant128(new Vector3(1, 2, 3)), typeof(Vector3)),
			VectorComponent.X);

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "VectorComponent")]
	public void VectorComponent_resolver_extracts_vector2_components()
	{
		var context = new GraphContext();

		var xResolver = new VectorComponentResolver(
			new VariantResolver(new Variant128(new Vector2(1, 2)), typeof(Vector2)),
			VectorComponent.X);
		var yResolver = new VectorComponentResolver(
			new VariantResolver(new Variant128(new Vector2(1, 2)), typeof(Vector2)),
			VectorComponent.Y);

		xResolver.Resolve(context).AsFloat().Should().Be(1.0f);
		yResolver.Resolve(context).AsFloat().Should().Be(2.0f);
	}

	[Fact]
	[Trait("Resolver", "VectorComponent")]
	public void VectorComponent_resolver_extracts_vector3_components()
	{
		var context = new GraphContext();

		var xResolver = new VectorComponentResolver(
			new VariantResolver(new Variant128(new Vector3(1, 2, 3)), typeof(Vector3)),
			VectorComponent.X);
		var yResolver = new VectorComponentResolver(
			new VariantResolver(new Variant128(new Vector3(1, 2, 3)), typeof(Vector3)),
			VectorComponent.Y);
		var zResolver = new VectorComponentResolver(
			new VariantResolver(new Variant128(new Vector3(1, 2, 3)), typeof(Vector3)),
			VectorComponent.Z);

		xResolver.Resolve(context).AsFloat().Should().Be(1.0f);
		yResolver.Resolve(context).AsFloat().Should().Be(2.0f);
		zResolver.Resolve(context).AsFloat().Should().Be(3.0f);
	}

	[Fact]
	[Trait("Resolver", "VectorComponent")]
	public void VectorComponent_resolver_extracts_vector4_components()
	{
		var context = new GraphContext();

		var xResolver = new VectorComponentResolver(
			new VariantResolver(new Variant128(new Vector4(1, 2, 3, 4)), typeof(Vector4)),
			VectorComponent.X);
		var yResolver = new VectorComponentResolver(
			new VariantResolver(new Variant128(new Vector4(1, 2, 3, 4)), typeof(Vector4)),
			VectorComponent.Y);
		var zResolver = new VectorComponentResolver(
			new VariantResolver(new Variant128(new Vector4(1, 2, 3, 4)), typeof(Vector4)),
			VectorComponent.Z);
		var wResolver = new VectorComponentResolver(
			new VariantResolver(new Variant128(new Vector4(1, 2, 3, 4)), typeof(Vector4)),
			VectorComponent.W);

		xResolver.Resolve(context).AsFloat().Should().Be(1.0f);
		yResolver.Resolve(context).AsFloat().Should().Be(2.0f);
		zResolver.Resolve(context).AsFloat().Should().Be(3.0f);
		wResolver.Resolve(context).AsFloat().Should().Be(4.0f);
	}

	[Fact]
	[Trait("Resolver", "VectorComponent")]
	public void VectorComponent_resolver_throws_for_invalid_vector2_component()
	{
#pragma warning disable CA1806
		Action act = () => new VectorComponentResolver(
			new VariantResolver(new Variant128(new Vector2(1, 2)), typeof(Vector2)),
			VectorComponent.Z);
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "VectorComponent")]
	public void VectorComponent_resolver_throws_for_invalid_vector3_component()
	{
#pragma warning disable CA1806
		Action act = () => new VectorComponentResolver(
			new VariantResolver(new Variant128(new Vector3(1, 2, 3)), typeof(Vector3)),
			VectorComponent.W);
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "VectorComponent")]
	public void VectorComponent_resolver_throws_for_unsupported_operand_type()
	{
#pragma warning disable CA1806
		Action act = () => new VectorComponentResolver(
			new VariantResolver(new Variant128(true), typeof(bool)),
			VectorComponent.X);
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
