// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class DotNormalResolverTests
{
	[Fact]
	[Trait("Resolver", "DotNormal")]
	public void DotNormal_resolver_value_type_is_float()
	{
		var resolver = new DotNormalResolver(
			new VariantResolver(new Variant128(new Plane(1, 1, 1, 1)), typeof(Plane)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "DotNormal")]
	public void DotNormal_resolver_computes_dot_normal()
	{
		var plane = new Plane(1, 1, 1, 1);
		Vector3 normal = Vector3.One;
		var resolver = new DotNormalResolver(
			new VariantResolver(new Variant128(plane), typeof(Plane)),
			new VariantResolver(new Variant128(normal), typeof(Vector3)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(Plane.DotNormal(plane, normal));
	}
}
