// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class RoundResolverTests
{
	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_float_value_type_is_float()
	{
		var resolver = new RoundResolver(
			new VariantResolver(new Variant128(2.5f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_double_value_type_is_double()
	{
		var resolver = new RoundResolver(
			new VariantResolver(new Variant128(2.5), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_decimal_value_type_is_decimal()
	{
		var resolver = new RoundResolver(
			new VariantResolver(new Variant128(2.5m), typeof(decimal)));

		resolver.ValueType.Should().Be(typeof(decimal));
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_rounds_up_float()
	{
		var resolver = new RoundResolver(
			new VariantResolver(new Variant128(2.7f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(3.0f);
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_rounds_down_float()
	{
		var resolver = new RoundResolver(
			new VariantResolver(new Variant128(2.3f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(2.0f);
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_bankers_rounding_even_float()
	{
		// 2.5 rounds to 2 (banker's rounding: round to even)
		var resolver = new RoundResolver(
			new VariantResolver(new Variant128(2.5f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(2.0f);
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_bankers_rounding_odd_float()
	{
		// 3.5 rounds to 4 (banker's rounding: round to even)
		var resolver = new RoundResolver(
			new VariantResolver(new Variant128(3.5f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(4.0f);
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_positive_double()
	{
		var resolver = new RoundResolver(
			new VariantResolver(new Variant128(3.7), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(4.0);
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_negative_double()
	{
		var resolver = new RoundResolver(
			new VariantResolver(new Variant128(-2.3), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(-2.0);
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_positive_decimal()
	{
		var resolver = new RoundResolver(
			new VariantResolver(new Variant128(5.6m), typeof(decimal)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDecimal().Should().Be(6.0m);
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_negative_decimal()
	{
		var resolver = new RoundResolver(
			new VariantResolver(new Variant128(-5.4m), typeof(decimal)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDecimal().Should().Be(-5.0m);
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_whole_number_returns_same()
	{
		var resolver = new RoundResolver(
			new VariantResolver(new Variant128(5.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(5.0);
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_supports_nesting()
	{
		// Round(2.3 + 0.3) = Round(2.6) = 3.0
		var sum = new AddResolver(
			new VariantResolver(new Variant128(2.3), typeof(double)),
			new VariantResolver(new Variant128(0.3), typeof(double)));

		var resolver = new RoundResolver(sum);

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(3.0);
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_throws_for_int_operand()
	{
#pragma warning disable CA1806
		Action act = () => new RoundResolver(
			new VariantResolver(new Variant128(5), typeof(int)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_throws_for_vector_operand()
	{
#pragma warning disable CA1806
		Action act = () => new RoundResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_throws_for_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new RoundResolver(
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_with_digits_rounds_to_specified_decimal_places()
	{
		// Round(3.14159, 2) = 3.14
		var resolver = new RoundResolver(
			new VariantResolver(new Variant128(3.14159), typeof(double)),
			digits: 2);

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(3.14);
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_with_digits_one_place()
	{
		// Round(2.75, 1) = 2.8 (banker's rounding: 2.75 → 2.8 because 8 is even)
		var resolver = new RoundResolver(
			new VariantResolver(new Variant128(2.76), typeof(double)),
			digits: 1);

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(2.8);
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_with_away_from_zero_mode()
	{
		// Round(2.5, AwayFromZero) = 3 (not 2 like banker's rounding)
		var resolver = new RoundResolver(
			new VariantResolver(new Variant128(2.5), typeof(double)),
			mode: MidpointRounding.AwayFromZero);

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(3.0);
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_with_away_from_zero_negative()
	{
		// Round(-2.5, AwayFromZero) = -3
		var resolver = new RoundResolver(
			new VariantResolver(new Variant128(-2.5), typeof(double)),
			mode: MidpointRounding.AwayFromZero);

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(-3.0);
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_with_digits_and_mode()
	{
		// Round(2.345, 2, AwayFromZero) = 2.35
		var resolver = new RoundResolver(
			new VariantResolver(new Variant128(2.345), typeof(double)),
			digits: 2,
			mode: MidpointRounding.AwayFromZero);

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(2.35);
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_float_with_digits()
	{
		// Round(3.14159f, 2) = 3.14f
		var resolver = new RoundResolver(
			new VariantResolver(new Variant128(3.14159f), typeof(float)),
			digits: 2);

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().BeApproximately(3.14f, 0.001f);
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_decimal_with_digits()
	{
		// Round(3.14159m, 3) = 3.142m
		var resolver = new RoundResolver(
			new VariantResolver(new Variant128(3.14159m), typeof(decimal)),
			digits: 3);

		var context = new GraphContext();

		resolver.Resolve(context).AsDecimal().Should().Be(3.142m);
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_default_parameters_preserve_original_behavior()
	{
		// Default: digits=0, mode=ToEven → same as original behavior
		// Round(2.5) = 2.0 (banker's rounding)
		var resolver = new RoundResolver(
			new VariantResolver(new Variant128(2.5), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(2.0);
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_throws_for_negative_digits()
	{
#pragma warning disable CA1806
		Action act = () => new RoundResolver(
			new VariantResolver(new Variant128(3.14), typeof(double)),
			digits: -1);
#pragma warning restore CA1806

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	[Trait("Resolver", "Round")]
	public void Round_resolver_throws_for_digits_above_15()
	{
#pragma warning disable CA1806
		Action act = () => new RoundResolver(
			new VariantResolver(new Variant128(3.14), typeof(double)),
			digits: 16);
#pragma warning restore CA1806

		act.Should().Throw<ArgumentOutOfRangeException>();
	}
}
