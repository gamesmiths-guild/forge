// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class LogResolverTests
{
	[Fact]
	[Trait("Resolver", "Log")]
	public void Log_resolver_float_value_type_is_float()
	{
		var resolver = new LogResolver(
			new VariantResolver(new Variant128(1.0f), typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Log")]
	public void Log_resolver_double_value_type_is_double()
	{
		var resolver = new LogResolver(
			new VariantResolver(new Variant128(1.0), typeof(double)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Log")]
	public void Log_resolver_int_promotes_to_double()
	{
		var resolver = new LogResolver(
			new VariantResolver(new Variant128(1), typeof(int)));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Log")]
	public void Log_resolver_one_returns_zero()
	{
		// Log(1) = 0
		var resolver = new LogResolver(
			new VariantResolver(new Variant128(1.0), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(0.0);
	}

	[Fact]
	[Trait("Resolver", "Log")]
	public void Log_resolver_e_returns_one()
	{
		// Log(e) = 1
		var resolver = new LogResolver(
			new VariantResolver(new Variant128(Math.E), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(1.0, 0.0001);
	}

	[Fact]
	[Trait("Resolver", "Log")]
	public void Log_resolver_e_squared()
	{
		// Log(e^2) = 2
		var resolver = new LogResolver(
			new VariantResolver(new Variant128(Math.E * Math.E), typeof(double)));

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(2.0, 0.0001);
	}

	[Fact]
	[Trait("Resolver", "Log")]
	public void Log_resolver_float_computation()
	{
		// Log(e) ≈ 1
		var resolver = new LogResolver(
			new VariantResolver(new Variant128(MathF.E), typeof(float)));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().BeApproximately(1.0f, 0.001f);
	}

	[Fact]
	[Trait("Resolver", "Log")]
	public void Log_resolver_supports_nesting()
	{
		// Log(Log(e^e)) = Log(e) = 1 — Wait: Log(e^e) = e, Log(e) = 1
		var inner = new LogResolver(
			new VariantResolver(new Variant128(Math.Pow(Math.E, Math.E)), typeof(double)));

		var outer = new LogResolver(inner);

		var context = new GraphContext();

		outer.Resolve(context).AsDouble().Should().BeApproximately(1.0, 0.0001);
	}

	[Fact]
	[Trait("Resolver", "Log")]
	public void Log_resolver_throws_for_decimal_operand()
	{
#pragma warning disable CA1806
		Action act = () => new LogResolver(
			new VariantResolver(new Variant128(1.0m), typeof(decimal)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Log")]
	public void Log_resolver_throws_for_vector_operand()
	{
#pragma warning disable CA1806
		Action act = () => new LogResolver(
			new VariantResolver(new Variant128(Vector3.One), typeof(Vector3)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Log")]
	public void Log_resolver_throws_for_bool_operand()
	{
#pragma warning disable CA1806
		Action act = () => new LogResolver(
			new VariantResolver(new Variant128(true), typeof(bool)));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}
}
