// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class DotResolverTests
{
	[Fact]
	[Trait("Resolver", "Dot")]
	public void Dot_resolver_value_type_is_always_float()
	{
		var resolver = new DotResolver(
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Dot")]
	public void Dot_resolver_perpendicular_vectors_returns_zero()
	{
		var resolver = new DotResolver(
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(0.0f);
	}

	[Fact]
	[Trait("Resolver", "Dot")]
	public void Dot_resolver_parallel_vectors_returns_product_of_lengths()
	{
		var resolver = new DotResolver(
			new VariantResolver(new Variant128(new Vector3(3, 0, 0)), typeof(Vector3)),
			new VariantResolver(new Variant128(new Vector3(4, 0, 0)), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(12.0f);
	}

	[Fact]
	[Trait("Resolver", "Dot")]
	public void Dot_resolver_vector2()
	{
		var resolver = new DotResolver(
			new VariantResolver(new Variant128(new Vector2(1, 2)), typeof(Vector2)),
			new VariantResolver(new Variant128(new Vector2(3, 4)), typeof(Vector2)));

		var context = new GraphContext();

		// (1 * 3) + (2 * 4) = 11
		resolver.Resolve(context).AsFloat().Should().Be(11.0f);
	}

	[Fact]
	[Trait("Resolver", "Dot")]
	public void Dot_resolver_vector3()
	{
		var resolver = new DotResolver(
			new VariantResolver(new Variant128(new Vector3(1, 2, 3)), typeof(Vector3)),
			new VariantResolver(new Variant128(new Vector3(4, 5, 6)), typeof(Vector3)));

		var context = new GraphContext();

		// (1 * 4) + (2 * 5) + (3 * 6) = 32
		resolver.Resolve(context).AsFloat().Should().Be(32.0f);
	}

	[Fact]
	[Trait("Resolver", "Dot")]
	public void Dot_resolver_vector4()
	{
		var resolver = new DotResolver(
			new VariantResolver(new Variant128(new Vector4(1, 2, 3, 4)), typeof(Vector4)),
			new VariantResolver(new Variant128(new Vector4(5, 6, 7, 8)), typeof(Vector4)));

		var context = new GraphContext();

		// (1 * 5) + (2 * 6) + (3 * 7) + (4 * 8) = 70
		resolver.Resolve(context).AsFloat().Should().Be(70.0f);
	}

	[Fact]
	[Trait("Resolver", "Dot")]
	public void Dot_resolver_quaternion()
	{
		var left = new Quaternion(1.0f, 2.0f, 3.0f, 4.0f);
		var right = new Quaternion(5.0f, 6.0f, 7.0f, 8.0f);

		var resolver = new DotResolver(
			new VariantResolver(new Variant128(left), typeof(Quaternion)),
			new VariantResolver(new Variant128(right), typeof(Quaternion)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(Quaternion.Dot(left, right));
	}

	[Fact]
	[Trait("Resolver", "Dot")]
	public void Dot_resolver_mismatched_types_throws()
	{
		Func<DotResolver> act = () => new DotResolver(
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Dot")]
	public void Dot_resolver_non_vector_type_throws()
	{
		Func<DotResolver> act = () => new DotResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)),
			new VariantResolver(new Variant128(2.0f), typeof(float)));

		act.Should().Throw<ArgumentException>();
	}
}
