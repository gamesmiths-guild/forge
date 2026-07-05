// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class MaxElementResolverTests
{
	[Fact]
	[Trait("Resolver", "MaxElement")]
	public void Max_element_resolver_returns_the_largest_element()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(1), new Variant128(2)]);

		var resolver = new MaxElementResolver(new ArrayVariableResolver("numbers", typeof(int)));

		resolver.ValueType.Should().Be(typeof(int));
		resolver.Resolve(context).AsInt().Should().Be(3);
	}

	[Fact]
	[Trait("Resolver", "MaxElement")]
	public void Max_element_resolver_returns_default_for_empty_arrays()
	{
		var resolver = new MaxElementResolver(new ArrayVariableResolver("missing", typeof(int)));

		resolver.Resolve(new GraphContext()).AsInt().Should().Be(0);
	}
}
