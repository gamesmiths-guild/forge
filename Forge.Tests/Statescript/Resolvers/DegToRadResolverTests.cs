// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class DegToRadResolverTests
{
	[Fact]
	[Trait("Resolver", "DegToRad")]
	public void DegToRad_resolver_float_value_type_is_float()
	{
		var resolver = new DegToRadResolver(
			new VariantResolver(new Variant128(90.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "DegToRad")]
	public void DegToRad_resolver_double_value_type_is_double()
	{
		var resolver = new DegToRadResolver(
			new VariantResolver(new Variant128(90.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "DegToRad")]
	public void DegToRad_resolver_int_promotes_to_double()
	{
		var resolver = new DegToRadResolver(
			new VariantResolver(new Variant128(90), typeof(int)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "DegToRad")]
	public void DegToRad_resolver_zero_returns_zero()
	{
		var resolver = new DegToRadResolver(
			new VariantResolver(new Variant128(0.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(0.0);
	}

	[Fact]
	[Trait("Resolver", "DegToRad")]
	public void DegToRad_resolver_90_degrees_returns_pi_over_2()
	{
		var resolver = new DegToRadResolver(
			new VariantResolver(new Variant128(90.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(Math.PI / 2.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "DegToRad")]
	public void DegToRad_resolver_180_degrees_returns_pi()
	{
		var resolver = new DegToRadResolver(
			new VariantResolver(new Variant128(180.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(Math.PI, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "DegToRad")]
	public void DegToRad_resolver_360_degrees_returns_two_pi()
	{
		var resolver = new DegToRadResolver(
			new VariantResolver(new Variant128(360.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(2.0 * Math.PI, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "DegToRad")]
	public void DegToRad_resolver_45_degrees()
	{
		var resolver = new DegToRadResolver(
			new VariantResolver(new Variant128(45.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(Math.PI / 4.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "DegToRad")]
	public void DegToRad_resolver_negative_degrees()
	{
		var resolver = new DegToRadResolver(
			new VariantResolver(new Variant128(-90.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(-Math.PI / 2.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "DegToRad")]
	public void DegToRad_resolver_float_90_degrees()
	{
		var resolver = new DegToRadResolver(
			new VariantResolver(new Variant128(90.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().BeApproximately(MathF.PI / 2.0f, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "DegToRad")]
	public void DegToRad_resolver_supports_nesting()
	{
		// DegToRad(90 + 90) = DegToRad(180) = π
		var inner = new AddResolver(
			new VariantResolver(new Variant128(90.0), typeof(double)),
			new VariantResolver(new Variant128(90.0), typeof(double)));

		var resolver = new DegToRadResolver(inner);

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(Math.PI, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "DegToRad")]
	public void DegToRad_resolver_throws_for_decimal_operand()
	{
#pragma warning disable CA1806
		Action act = () => new DegToRadResolver(
			new VariantResolver(new Variant128(90.0m), typeof(decimal)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "DegToRad")]
	public void DegToRad_resolver_throws_for_vector_operand()
	{
#pragma warning disable CA1806
		Action act = () => new DegToRadResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "DegToRad")]
	public void DegToRad_resolver_throws_for_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new DegToRadResolver(
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
