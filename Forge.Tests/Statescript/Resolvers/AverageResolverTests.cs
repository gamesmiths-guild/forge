// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class AverageResolverTests
{
	[Fact]
	[Trait("Resolver", "Average")]
	public void Average_resolver_averages_int_elements_as_double()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(1), new Variant128(2)]);

		var resolver = new AverageResolver(new ArrayVariableResolver("numbers", typeof(int)));

		resolver.ValueType.Should().Be(typeof(double));
		resolver.Resolve(context).AsDouble().Should().Be(2d);
	}

	[Fact]
	[Trait("Resolver", "Average")]
	public void Average_resolver_averages_float_elements_as_float()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(1f), new Variant128(2f)]);

		var resolver = new AverageResolver(new ArrayVariableResolver("numbers", typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
		resolver.Resolve(context).AsFloat().Should().Be(1.5f);
	}

	[Fact]
	[Trait("Resolver", "Average")]
	public void Average_resolver_returns_zero_for_empty_arrays()
	{
		var resolver = new AverageResolver(new ArrayVariableResolver("missing", typeof(int)));

		resolver.Resolve(new GraphContext()).AsDouble().Should().Be(0d);
	}
}
