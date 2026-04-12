// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class SignResolverTests
{
	[Fact]
	[Trait("Resolver", "Sign")]
	public void Sign_resolver_value_type_is_always_int()
	{
		var floatResolver = new SignResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		var doubleResolver = new SignResolver(
			new VariantResolver(new Variant128(1.0), typeof(double)));

		var intResolver = new SignResolver(
			new VariantResolver(new Variant128(1), typeof(int)));

		floatResolver.ValueType.Should().Be(typeof(int));
		doubleResolver.ValueType.Should().Be(typeof(int));
		intResolver.ValueType.Should().Be(typeof(int));
	}

	[Fact]
	[Trait("Resolver", "Sign")]
	public void Sign_resolver_positive_float_returns_one()
	{
		var resolver = new SignResolver(
			new VariantResolver(new Variant128(5.5f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(1);
	}

	[Fact]
	[Trait("Resolver", "Sign")]
	public void Sign_resolver_negative_float_returns_negative_one()
	{
		var resolver = new SignResolver(
			new VariantResolver(new Variant128(-3.7f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(-1);
	}

	[Fact]
	[Trait("Resolver", "Sign")]
	public void Sign_resolver_zero_float_returns_zero()
	{
		var resolver = new SignResolver(
			new VariantResolver(new Variant128(0.0f), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Sign")]
	public void Sign_resolver_positive_double_returns_one()
	{
		var resolver = new SignResolver(
			new VariantResolver(new Variant128(42.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(1);
	}

	[Fact]
	[Trait("Resolver", "Sign")]
	public void Sign_resolver_negative_double_returns_negative_one()
	{
		var resolver = new SignResolver(
			new VariantResolver(new Variant128(-0.001), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(-1);
	}

	[Fact]
	[Trait("Resolver", "Sign")]
	public void Sign_resolver_positive_int_returns_one()
	{
		var resolver = new SignResolver(
			new VariantResolver(new Variant128(100), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(1);
	}

	[Fact]
	[Trait("Resolver", "Sign")]
	public void Sign_resolver_negative_int_returns_negative_one()
	{
		var resolver = new SignResolver(
			new VariantResolver(new Variant128(-50), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(-1);
	}

	[Fact]
	[Trait("Resolver", "Sign")]
	public void Sign_resolver_zero_int_returns_zero()
	{
		var resolver = new SignResolver(
			new VariantResolver(new Variant128(0), typeof(int)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Sign")]
	public void Sign_resolver_positive_decimal_returns_one()
	{
		var resolver = new SignResolver(
			new VariantResolver(new Variant128(9.99m), typeof(decimal)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(1);
	}

	[Fact]
	[Trait("Resolver", "Sign")]
	public void Sign_resolver_negative_long_returns_negative_one()
	{
		var resolver = new SignResolver(
			new VariantResolver(new Variant128(-999L), typeof(long)));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(-1);
	}

	[Fact]
	[Trait("Resolver", "Sign")]
	public void Sign_resolver_supports_nesting()
	{
		// Sign(Sign(-5.0)) = Sign(-1) — but Sign returns int, and Sign(int 1) = 1
		// Actually: Sign(-5.0f) = -1 (int), then Sign(-1 int) = -1
		var inner = new SignResolver(
			new VariantResolver(new Variant128(-5.0f), typeof(float)));

		var outer = new SignResolver(inner);

		var context = new GraphContext();

		outer.Resolve(context).AsInt().Should().Be(-1);
	}

	[Fact]
	[Trait("Resolver", "Sign")]
	public void Sign_resolver_throws_for_unsigned_type()
	{
#pragma warning disable CA1806
		Action act = () => new SignResolver(
			new VariantResolver(new Variant128(5U), typeof(uint)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Sign")]
	public void Sign_resolver_throws_for_vector_operand()
	{
#pragma warning disable CA1806
		Action act = () => new SignResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Sign")]
	public void Sign_resolver_throws_for_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new SignResolver(
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
