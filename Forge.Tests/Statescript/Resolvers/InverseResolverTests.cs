// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class InverseResolverTests
{
	[Fact]
	[Trait("Resolver", "Inverse")]
	public void Inverse_resolver_value_type_is_quaternion()
	{
		var resolver = new InverseResolver(
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)));

		resolver.ValueType.Should().Be(typeof(Quaternion));
	}

	[Fact]
	[Trait("Resolver", "Inverse")]
	public void Inverse_resolver_computes_inverse()
	{
		Quaternion quaternion = Quaternion.Identity;
		var resolver = new InverseResolver(
			new VariantResolver(new Variant128(quaternion), typeof(Quaternion)));

		var context = new GraphContext();
		var expected = Quaternion.Inverse(quaternion);
		Quaternion result = resolver.Resolve(context).AsQuaternion();

		result.X.Should().Be(expected.X);
		result.Y.Should().Be(expected.Y);
		result.Z.Should().Be(expected.Z);
		result.W.Should().Be(expected.W);
	}
}
