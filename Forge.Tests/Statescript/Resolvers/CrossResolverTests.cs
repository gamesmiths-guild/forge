// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class CrossResolverTests
{
	[Fact]
	[Trait("Resolver", "Cross")]
	public void Cross_resolver_value_type_is_vector3()
	{
		var resolver = new CrossResolver(
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(Vector3));
	}

	[Fact]
	[Trait("Resolver", "Cross")]
	public void Cross_resolver_unit_x_cross_unit_y_returns_unit_z()
	{
		var resolver = new CrossResolver(
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.UnitY), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(Vector3.UnitZ);
	}

	[Fact]
	[Trait("Resolver", "Cross")]
	public void Cross_resolver_parallel_vectors_returns_zero()
	{
		var resolver = new CrossResolver(
			new VariantResolver(new Variant128(new Vector3(1, 0, 0)), typeof(Vector3)),
			new VariantResolver(new Variant128(new Vector3(2, 0, 0)), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsVector3().Should().Be(Vector3.Zero);
	}

	[Fact]
	[Trait("Resolver", "Cross")]
	public void Cross_resolver_general_case()
	{
		var resolver = new CrossResolver(
			new VariantResolver(new Variant128(new Vector3(1, 2, 3)), typeof(Vector3)),
			new VariantResolver(new Variant128(new Vector3(4, 5, 6)), typeof(Vector3)));

		var context = new GraphContext();
		Vector3 result = resolver.Resolve(context).AsVector3();

		// Cross product: ((2 * 6) - (3 * 5), (3 * 4) - (1 * 6), (1 * 5) - (2 * 4)) = (-3, 6, -3)
		result.Should().Be(new Vector3(-3, 6, -3));
	}

	[Fact]
	[Trait("Resolver", "Cross")]
	public void Cross_resolver_non_vector3_type_throws()
	{
		Func<CrossResolver> act = () => new CrossResolver(
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)),
			new VariantResolver(new Variant128(Vector2.One), typeof(Vector2)));

		act.Should().Throw<ArgumentException>();
	}
}
