// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class RandomResolverTests
{
	[Fact]
	[Trait("Resolver", "Random")]
	public void Random_resolver_int_operands_value_type_is_int()
	{
		var resolver = new RandomResolver(
			new FixedRandom(),
			new VariantResolver(new Variant128(0), typeof(int)),
			new VariantResolver(new Variant128(10), typeof(int)));

		resolver.ValueType.Should().Be(typeof(int));
	}

	[Fact]
	[Trait("Resolver", "Random")]
	public void Random_resolver_float_operands_value_type_is_float()
	{
		var resolver = new RandomResolver(
			new FixedRandom(),
			new VariantResolver(new Variant128(0.0f), typeof(float)),
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Random")]
	public void Random_resolver_double_operands_value_type_is_double()
	{
		var resolver = new RandomResolver(
			new FixedRandom(),
			new VariantResolver(new Variant128(0.0), typeof(double)),
			new VariantResolver(new Variant128(1.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Random")]
	public void Random_resolver_mixed_int_float_promotes_to_float()
	{
		var resolver = new RandomResolver(
			new FixedRandom(),
			new VariantResolver(new Variant128(0), typeof(int)),
			new VariantResolver(new Variant128(10.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Random")]
	public void Random_resolver_int_range_uses_next_int()
	{
		// FixedRandom.NextInt64(0, 11) returns 7 when max is inclusive.
		var resolver = new RandomResolver(
			new FixedRandom(nextInt: 7),
			new VariantResolver(new Variant128(0), typeof(int)),
			new VariantResolver(new Variant128(10), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(7);
	}

	[Fact]
	[Trait("Resolver", "Random")]
	public void Random_resolver_int_range_can_use_exclusive_max()
	{
		var resolver = new RandomResolver(
			new FixedRandom(nextInt: 9),
			new VariantResolver(new Variant128(0), typeof(int)),
			new VariantResolver(new Variant128(10), typeof(int)),
			maxInclusive: false);

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(9);
	}

	[Fact]
	[Trait("Resolver", "Random")]
	public void Random_resolver_int_range_includes_max_by_default()
	{
		var resolver = new RandomResolver(
			new FixedRandom(nextInt: 10),
			new VariantResolver(new Variant128(0), typeof(int)),
			new VariantResolver(new Variant128(10), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(10);
	}

	[Fact]
	[Trait("Resolver", "Random")]
	public void Random_resolver_float_range_with_zero_random()
	{
		// NextSingle returns 0.0 → min + 0 * (max - min) = min = 5.0f
		var resolver = new RandomResolver(
			new FixedRandom(nextSingle: 0.0f),
			new VariantResolver(new Variant128(5.0f), typeof(float)),
			new VariantResolver(new Variant128(15.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(5.0f);
	}

	[Fact]
	[Trait("Resolver", "Random")]
	public void Random_resolver_float_range_with_half_random()
	{
		// NextSingle returns 0.5 → 5 + 0.5 * (15 - 5) = 5 + 5 = 10
		var resolver = new RandomResolver(
			new FixedRandom(nextSingle: 0.5f),
			new VariantResolver(new Variant128(5.0f), typeof(float)),
			new VariantResolver(new Variant128(15.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(10.0f);
	}

	[Fact]
	[Trait("Resolver", "Random")]
	public void Random_resolver_float_range_includes_max_by_default_when_random_is_one()
	{
		var resolver = new RandomResolver(
			new FixedRandom(nextSingle: 1.0f),
			new VariantResolver(new Variant128(5.0f), typeof(float)),
			new VariantResolver(new Variant128(15.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(15.0f);
	}

	[Fact]
	[Trait("Resolver", "Random")]
	public void Random_resolver_float_range_excludes_max_when_configured()
	{
		var resolver = new RandomResolver(
			new FixedRandom(nextSingle: 1.0f),
			new VariantResolver(new Variant128(5.0f), typeof(float)),
			new VariantResolver(new Variant128(15.0f), typeof(float)),
			maxInclusive: false);

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().BeGreaterThanOrEqualTo(5.0f);
		resolver.Resolve(context).AsFloat().Should().BeLessThan(15.0f);
	}

	[Fact]
	[Trait("Resolver", "Random")]
	public void Random_resolver_double_range_with_zero_random()
	{
		// NextDouble returns 0.0 → min
		var resolver = new RandomResolver(
			new FixedRandom(nextDouble: 0.0),
			new VariantResolver(new Variant128(10.0), typeof(double)),
			new VariantResolver(new Variant128(20.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(10.0);
	}

	[Fact]
	[Trait("Resolver", "Random")]
	public void Random_resolver_double_range_with_full_random()
	{
		// NextDouble returns 0.999 → stays within range and approaches max
		var resolver = new RandomResolver(
			new FixedRandom(nextDouble: 0.999),
			new VariantResolver(new Variant128(0.0), typeof(double)),
			new VariantResolver(new Variant128(100.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(99.9, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Random")]
	public void Random_resolver_double_range_includes_max_by_default_when_random_is_one()
	{
		var resolver = new RandomResolver(
			new FixedRandom(nextDouble: 1.0),
			new VariantResolver(new Variant128(10.0), typeof(double)),
			new VariantResolver(new Variant128(20.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(20.0);
	}

	[Fact]
	[Trait("Resolver", "Random")]
	public void Random_resolver_double_range_excludes_max_when_configured()
	{
		var resolver = new RandomResolver(
			new FixedRandom(nextDouble: 1.0),
			new VariantResolver(new Variant128(10.0), typeof(double)),
			new VariantResolver(new Variant128(20.0), typeof(double)),
			maxInclusive: false);

		var context = new GraphContext();
		var result = resolver.Resolve(context).AsDouble();

		result.Should().BeGreaterThanOrEqualTo(10.0);
		result.Should().BeLessThan(20.0);
	}

	[Fact]
	[Trait("Resolver", "Random")]
	public void Random_resolver_supports_nesting_in_bounds()
	{
		// Random(0, 5 + 5) with fixed value → Random(0, 10)
		var resolver = new RandomResolver(
			new FixedRandom(nextInt: 3),
			new VariantResolver(new Variant128(0), typeof(int)),
			new AddResolver(
				new VariantResolver(new Variant128(5), typeof(int)),
				new VariantResolver(new Variant128(5), typeof(int))));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(3);
	}

	[Fact]
	[Trait("Resolver", "Random")]
	public void Random_resolver_throws_for_decimal_operands()
	{
#pragma warning disable CA1806
		Action act = () => new RandomResolver(
			new FixedRandom(),
			new VariantResolver(new Variant128(0.0m), typeof(decimal)),
			new VariantResolver(new Variant128(1.0m), typeof(decimal)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Random")]
	public void Random_resolver_throws_for_vector_operands()
	{
#pragma warning disable CA1806
		Action act = () => new RandomResolver(
			new FixedRandom(),
			new VariantResolver(new Variant128(Vector3.Zero), typeof(Vector3)),
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Random")]
	public void Random_resolver_throws_for_long_operands()
	{
#pragma warning disable CA1806
		Action act = () => new RandomResolver(
			new FixedRandom(),
			new VariantResolver(new Variant128(0L), typeof(long)),
			new VariantResolver(new Variant128(10L), typeof(long)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Random")]
	public void Random_resolver_throws_for_bool_operands()
	{
#pragma warning disable CA1806
		Action act = () => new RandomResolver(
			new FixedRandom(),
			new VariantResolver(new Variant128(true), typeof(bool)),
			new VariantResolver(new Variant128(false), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
