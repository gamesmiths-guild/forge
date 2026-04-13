// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class CopySignResolverTests
{
	[Fact]
	[Trait("Resolver", "CopySign")]
	public void CopySign_resolver_float_operands_value_type_is_float()
	{
		var resolver = new CopySignResolver(
			new VariantResolver(new Variant128(5.0f), typeof(float)),
			new VariantResolver(new Variant128(-1.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "CopySign")]
	public void CopySign_resolver_double_operands_value_type_is_double()
	{
		var resolver = new CopySignResolver(
			new VariantResolver(new Variant128(5.0), typeof(double)),
			new VariantResolver(new Variant128(-1.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "CopySign")]
	public void CopySign_resolver_int_operands_promotes_to_double()
	{
		var resolver = new CopySignResolver(
			new VariantResolver(new Variant128(5), typeof(int)),
			new VariantResolver(new Variant128(-1), typeof(int)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "CopySign")]
	public void CopySign_resolver_positive_magnitude_negative_sign()
	{
		// CopySign(5.0, -3.0) = -5.0
		var resolver = new CopySignResolver(
			new VariantResolver(new Variant128(5.0), typeof(double)),
			new VariantResolver(new Variant128(-3.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(-5.0);
	}

	[Fact]
	[Trait("Resolver", "CopySign")]
	public void CopySign_resolver_negative_magnitude_positive_sign()
	{
		// CopySign(-5.0, 3.0) = 5.0
		var resolver = new CopySignResolver(
			new VariantResolver(new Variant128(-5.0), typeof(double)),
			new VariantResolver(new Variant128(3.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(5.0);
	}

	[Fact]
	[Trait("Resolver", "CopySign")]
	public void CopySign_resolver_both_positive()
	{
		// CopySign(5.0, 3.0) = 5.0
		var resolver = new CopySignResolver(
			new VariantResolver(new Variant128(5.0), typeof(double)),
			new VariantResolver(new Variant128(3.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(5.0);
	}

	[Fact]
	[Trait("Resolver", "CopySign")]
	public void CopySign_resolver_both_negative()
	{
		// CopySign(-5.0, -3.0) = -5.0
		var resolver = new CopySignResolver(
			new VariantResolver(new Variant128(-5.0), typeof(double)),
			new VariantResolver(new Variant128(-3.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(-5.0);
	}

	[Fact]
	[Trait("Resolver", "CopySign")]
	public void CopySign_resolver_float_computation()
	{
		// CopySign(10.0f, -1.0f) = -10.0f
		var resolver = new CopySignResolver(
			new VariantResolver(new Variant128(10.0f), typeof(float)),
			new VariantResolver(new Variant128(-1.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(-10.0f);
	}

	[Fact]
	[Trait("Resolver", "CopySign")]
	public void CopySign_resolver_zero_magnitude()
	{
		// CopySign(0.0, -1.0) = -0.0
		var resolver = new CopySignResolver(
			new VariantResolver(new Variant128(0.0), typeof(double)),
			new VariantResolver(new Variant128(-1.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(-0.0);
	}

	[Fact]
	[Trait("Resolver", "CopySign")]
	public void CopySign_resolver_supports_nesting()
	{
		// CopySign(CopySign(5, -1), 1) = CopySign(-5, 1) = 5
		var inner = new CopySignResolver(
			new VariantResolver(new Variant128(5.0), typeof(double)),
			new VariantResolver(new Variant128(-1.0), typeof(double)));

		var outer = new CopySignResolver(
			inner,
			new VariantResolver(new Variant128(1.0), typeof(double)));

		var context = new GraphContext();

		outer.Resolve(context).AsDouble().Should().Be(5.0);
	}

	[Fact]
	[Trait("Resolver", "CopySign")]
	public void CopySign_resolver_throws_for_decimal_operands()
	{
#pragma warning disable CA1806
		Action act = () => new CopySignResolver(
			new VariantResolver(new Variant128(5.0m), typeof(decimal)),
			new VariantResolver(new Variant128(-1.0m), typeof(decimal)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "CopySign")]
	public void CopySign_resolver_throws_for_vector_operands()
	{
#pragma warning disable CA1806
		Action act = () => new CopySignResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "CopySign")]
	public void CopySign_resolver_throws_for_bool_operands()
	{
#pragma warning disable CA1806
		Action act = () => new CopySignResolver(
			new VariantResolver(new Variant128(true), typeof(bool)),
			new VariantResolver(new Variant128(false), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
