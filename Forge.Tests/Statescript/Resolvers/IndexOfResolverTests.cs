// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class IndexOfResolverTests
{
	[Fact]
	[Trait("Resolver", "IndexOf")]
	public void Index_of_resolver_returns_the_index_of_the_first_occurrence()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(1), new Variant128(1)]);

		var resolver = new IndexOfResolver(
			new ArrayVariableResolver("numbers", typeof(int)),
			new VariantResolver(new Variant128(1), typeof(int)));

		resolver.Resolve(context).AsInt().Should().Be(1);
	}

	[Fact]
	[Trait("Resolver", "IndexOf")]
	public void Index_of_resolver_returns_minus_one_when_the_value_is_absent()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable("numbers", [new Variant128(3)]);

		var resolver = new IndexOfResolver(
			new ArrayVariableResolver("numbers", typeof(int)),
			new VariantResolver(new Variant128(9), typeof(int)));

		resolver.Resolve(context).AsInt().Should().Be(-1);
	}
}
