// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ConcatenateResolverTests
{
	[Fact]
	[Trait("Resolver", "Concatenate")]
	public void Concatenate_resolver_value_type_is_quaternion()
	{
		var resolver = new ConcatenateResolver(
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)));

		resolver.ValueType.Should().Be(typeof(Quaternion));
	}

	[Fact]
	[Trait("Resolver", "Concatenate")]
	public void Concatenate_resolver_computes_concatenation()
	{
		Quaternion left = Quaternion.Identity;
		Quaternion right = Quaternion.Identity;
		var resolver = new ConcatenateResolver(
			new VariantResolver(new Variant128(left), typeof(Quaternion)),
			new VariantResolver(new Variant128(right), typeof(Quaternion)));

		var context = new GraphContext();
		var expected = Quaternion.Concatenate(left, right);
		Quaternion result = resolver.Resolve(context).AsQuaternion();

		result.X.Should().Be(expected.X);
		result.Y.Should().Be(expected.Y);
		result.Z.Should().Be(expected.Z);
		result.W.Should().Be(expected.W);
	}
}
