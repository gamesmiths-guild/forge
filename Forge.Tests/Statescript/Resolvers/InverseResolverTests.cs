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
		var quaternion = Quaternion.CreateFromYawPitchRoll(0.5f, 0.25f, -0.75f);
		var resolver = new InverseResolver(
			new VariantResolver(new Variant128(quaternion), typeof(Quaternion)));

		var expected = Quaternion.Inverse(quaternion);
		Quaternion result = resolver.Resolve(new GraphContext()).AsQuaternion();

		result.X.Should().Be(expected.X);
		result.Y.Should().Be(expected.Y);
		result.Z.Should().Be(expected.Z);
		result.W.Should().Be(expected.W);
	}

	[Fact]
	[Trait("Resolver", "Inverse")]
	public void Inverse_resolver_multiplied_by_original_returns_identity()
	{
		var quaternion = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.75f);
		var resolver = new InverseResolver(
			new VariantResolver(new Variant128(quaternion), typeof(Quaternion)));

		Quaternion inverse = resolver.Resolve(new GraphContext()).AsQuaternion();
		var combined = Quaternion.Normalize(Quaternion.Concatenate(quaternion, inverse));

		combined.X.Should().Be(0.0f);
		combined.Y.Should().Be(0.0f);
		combined.Z.Should().Be(0.0f);
		combined.W.Should().Be(1.0f);
	}

	[Fact]
	[Trait("Resolver", "Inverse")]
	public void Inverse_resolver_throws_for_non_quaternion_operand()
	{
#pragma warning disable CA1806 // Do not ignore method results
		Action act = () => new InverseResolver(
			new VariantResolver(new Variant128(Vector3.UnitX), typeof(Vector3)));
#pragma warning restore CA1806 // Do not ignore method results

		act.Should().Throw<ArgumentException>();
	}
}
