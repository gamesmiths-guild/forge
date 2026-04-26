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
		var left = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.5f);
		var right = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -0.25f);
		var resolver = new ConcatenateResolver(
			new VariantResolver(new Variant128(left), typeof(Quaternion)),
			new VariantResolver(new Variant128(right), typeof(Quaternion)));

		var expected = Quaternion.Concatenate(left, right);
		Quaternion result = resolver.Resolve(new GraphContext()).AsQuaternion();

		result.X.Should().Be(expected.X);
		result.Y.Should().Be(expected.Y);
		result.Z.Should().Be(expected.Z);
		result.W.Should().Be(expected.W);
	}

	[Fact]
	[Trait("Resolver", "Concatenate")]
	public void Concatenate_resolver_throws_for_non_quaternion_left_operand()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new ConcatenateResolver(
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)),
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Concatenate")]
	public void Concatenate_resolver_throws_for_non_quaternion_right_operand()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new ConcatenateResolver(
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}
}
