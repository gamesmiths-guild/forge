// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class EResolverTests
{
	[Fact]
	[Trait("Resolver", "E")]
	public void E_resolver_default_value_type_is_double()
	{
		var resolver = new EResolver();

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "E")]
	public void E_resolver_explicit_double_value_type_is_double()
	{
		var resolver = new EResolver(typeof(double));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "E")]
	public void E_resolver_explicit_float_value_type_is_float()
	{
		var resolver = new EResolver(typeof(float));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "E")]
	public void E_resolver_double_returns_math_e()
	{
		var resolver = new EResolver();

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(Math.E);
	}

	[Fact]
	[Trait("Resolver", "E")]
	public void E_resolver_float_returns_mathf_e()
	{
		var resolver = new EResolver(typeof(float));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(MathF.E);
	}

	[Fact]
	[Trait("Resolver", "E")]
	public void E_resolver_value_is_approximately_2_71828()
	{
		var resolver = new EResolver();

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(2.71828, 0.001);
	}

	[Fact]
	[Trait("Resolver", "E")]
	public void E_resolver_throws_for_int_type()
	{
#pragma warning disable CA1806
		Action act = () => new EResolver(typeof(int));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "E")]
	public void E_resolver_throws_for_decimal_type()
	{
#pragma warning disable CA1806
		Action act = () => new EResolver(typeof(decimal));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "E")]
	public void E_resolver_usable_in_composition()
	{
		// Log(e) = 1
		var resolver = new LogResolver(new EResolver());

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(1.0, 0.0001);
	}
}
