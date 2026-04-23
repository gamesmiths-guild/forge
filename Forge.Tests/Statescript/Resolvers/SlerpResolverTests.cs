// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class SlerpResolverTests
{
	[Fact]
	[Trait("Resolver", "Slerp")]
	public void Slerp_resolver_value_type_is_quaternion()
	{
		var resolver = new SlerpResolver(
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(Quaternion));
	}

	[Fact]
	[Trait("Resolver", "Slerp")]
	public void Slerp_resolver_at_zero_returns_start()
	{
		Quaternion a = Quaternion.Identity;
		var b = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);

		var resolver = new SlerpResolver(
			new VariantResolver(new Variant128(a), typeof(Quaternion)),
			new VariantResolver(new Variant128(b), typeof(Quaternion)),
			new VariantResolver(new Variant128(0.0f), typeof(float)));

		var context = new GraphContext();

		var expected = Quaternion.Slerp(a, b, 0.0f);
		Quaternion result = resolver.Resolve(context).AsQuaternion();

		result.X.Should().BeApproximately(expected.X, 0.001f);
		result.Y.Should().BeApproximately(expected.Y, 0.001f);
		result.Z.Should().BeApproximately(expected.Z, 0.001f);
		result.W.Should().BeApproximately(expected.W, 0.001f);
	}

	[Fact]
	[Trait("Resolver", "Slerp")]
	public void Slerp_resolver_at_one_returns_end()
	{
		Quaternion a = Quaternion.Identity;
		var b = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);

		var resolver = new SlerpResolver(
			new VariantResolver(new Variant128(a), typeof(Quaternion)),
			new VariantResolver(new Variant128(b), typeof(Quaternion)),
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		var context = new GraphContext();

		var expected = Quaternion.Slerp(a, b, 1.0f);
		Quaternion result = resolver.Resolve(context).AsQuaternion();

		result.X.Should().BeApproximately(expected.X, 0.001f);
		result.Y.Should().BeApproximately(expected.Y, 0.001f);
		result.Z.Should().BeApproximately(expected.Z, 0.001f);
		result.W.Should().BeApproximately(expected.W, 0.001f);
	}

	[Fact]
	[Trait("Resolver", "Slerp")]
	public void Slerp_resolver_at_half()
	{
		Quaternion a = Quaternion.Identity;
		var b = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);
		var expected = Quaternion.Slerp(a, b, 0.5f);

		var resolver = new SlerpResolver(
			new VariantResolver(new Variant128(a), typeof(Quaternion)),
			new VariantResolver(new Variant128(b), typeof(Quaternion)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsQuaternion().Should().Be(expected);
	}

	[Fact]
	[Trait("Resolver", "Slerp")]
	public void Slerp_resolver_non_quaternion_a_throws()
	{
		Func<SlerpResolver> act = () => new SlerpResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)),
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(0.5f), typeof(float)));

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Slerp")]
	public void Slerp_resolver_non_float_t_throws()
	{
		Func<SlerpResolver> act = () => new SlerpResolver(
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(Quaternion.Identity), typeof(Quaternion)),
			new VariantResolver(new Variant128(0.5), typeof(double)));

		act.Should().Throw<ArgumentException>();
	}
}
