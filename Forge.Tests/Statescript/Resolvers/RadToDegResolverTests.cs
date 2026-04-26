// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class RadToDegResolverTests
{
	[Fact]
	[Trait("Resolver", "RadToDeg")]
	public void RadToDeg_resolver_float_value_type_is_float()
	{
		var resolver = new RadToDegResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "RadToDeg")]
	public void RadToDeg_resolver_double_value_type_is_double()
	{
		var resolver = new RadToDegResolver(
			new VariantResolver(new Variant128(1.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "RadToDeg")]
	public void RadToDeg_resolver_int_promotes_to_double()
	{
		var resolver = new RadToDegResolver(
			new VariantResolver(new Variant128(1), typeof(int)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "RadToDeg")]
	public void RadToDeg_resolver_zero_returns_zero()
	{
		var resolver = new RadToDegResolver(
			new VariantResolver(new Variant128(0.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(0.0);
	}

	[Fact]
	[Trait("Resolver", "RadToDeg")]
	public void RadToDeg_resolver_pi_over_2_returns_90()
	{
		var resolver = new RadToDegResolver(
			new VariantResolver(new Variant128(Math.PI / 2.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(90.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "RadToDeg")]
	public void RadToDeg_resolver_pi_returns_180()
	{
		var resolver = new RadToDegResolver(
			new VariantResolver(new Variant128(Math.PI), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(180.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "RadToDeg")]
	public void RadToDeg_resolver_two_pi_returns_360()
	{
		var resolver = new RadToDegResolver(
			new VariantResolver(new Variant128(2.0 * Math.PI), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(360.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "RadToDeg")]
	public void RadToDeg_resolver_pi_over_4_returns_45()
	{
		var resolver = new RadToDegResolver(
			new VariantResolver(new Variant128(Math.PI / 4.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(45.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "RadToDeg")]
	public void RadToDeg_resolver_negative_radians()
	{
		var resolver = new RadToDegResolver(
			new VariantResolver(new Variant128(-Math.PI / 2.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(-90.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "RadToDeg")]
	public void RadToDeg_resolver_float_pi_over_2()
	{
		var resolver = new RadToDegResolver(
			new VariantResolver(new Variant128(MathF.PI / 2.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().BeApproximately(90.0f, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "RadToDeg")]
	public void RadToDeg_and_DegToRad_are_inverses()
	{
		// RadToDeg(DegToRad(45)) = 45
		var resolver = new RadToDegResolver(
			new DegToRadResolver(
				new VariantResolver(new Variant128(45.0), typeof(double))));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(45.0, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "RadToDeg")]
	public void RadToDeg_resolver_throws_for_decimal_operand()
	{
#pragma warning disable CA1806
		Action act = () => new RadToDegResolver(
			new VariantResolver(new Variant128(1.0m), typeof(decimal)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "RadToDeg")]
	public void RadToDeg_resolver_throws_for_vector_operand()
	{
#pragma warning disable CA1806
		Action act = () => new RadToDegResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "RadToDeg")]
	public void RadToDeg_resolver_throws_for_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new RadToDegResolver(
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
