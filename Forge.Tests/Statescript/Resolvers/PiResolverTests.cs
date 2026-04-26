// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class PiResolverTests
{
	[Fact]
	[Trait("Resolver", "Pi")]
	public void Pi_resolver_default_value_type_is_double()
	{
		var resolver = new PiResolver();

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Pi")]
	public void Pi_resolver_explicit_double_value_type_is_double()
	{
		var resolver = new PiResolver(typeof(double));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Pi")]
	public void Pi_resolver_explicit_float_value_type_is_float()
	{
		var resolver = new PiResolver(typeof(float));

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Pi")]
	public void Pi_resolver_double_returns_math_pi()
	{
		var resolver = new PiResolver();

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(Math.PI);
	}

	[Fact]
	[Trait("Resolver", "Pi")]
	public void Pi_resolver_float_returns_mathf_pi()
	{
		var resolver = new PiResolver(typeof(float));

		var context = new GraphContext();

		resolver.Resolve(context).AsFloat().Should().Be(MathF.PI);
	}

	[Fact]
	[Trait("Resolver", "Pi")]
	public void Pi_resolver_value_is_approximately_3_14159()
	{
		var resolver = new PiResolver();

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(3.14159, TestUtils.Tolerance);
	}

	[Fact]
	[Trait("Resolver", "Pi")]
	public void Pi_resolver_throws_for_int_type()
	{
#pragma warning disable CA1806
		Action act = () => new PiResolver(typeof(int));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Pi")]
	public void Pi_resolver_throws_for_decimal_type()
	{
#pragma warning disable CA1806
		Action act = () => new PiResolver(typeof(decimal));
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Pi")]
	public void Pi_resolver_usable_in_composition()
	{
		// Sin(Pi) ≈ 0
		var resolver = new SinResolver(new PiResolver());

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().BeApproximately(0.0, TestUtils.Tolerance);
	}
}
