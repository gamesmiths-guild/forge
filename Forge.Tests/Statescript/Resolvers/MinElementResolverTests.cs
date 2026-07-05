// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class MinElementResolverTests
{
	[Fact]
	[Trait("Resolver", "MinElement")]
	public void Min_element_resolver_returns_the_smallest_element()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(1), new Variant128(2)]);

		var resolver = new MinElementResolver(new ArrayVariableResolver("numbers", typeof(int)));

		resolver.ValueType.Should().Be(typeof(int));
		resolver.Resolve(context).AsInt().Should().Be(1);
	}

	[Fact]
	[Trait("Resolver", "MinElement")]
	public void Min_element_resolver_returns_default_for_empty_arrays()
	{
		var resolver = new MinElementResolver(new ArrayVariableResolver("missing", typeof(int)));

		resolver.Resolve(new GraphContext()).AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "MinElement")]
	public void Min_element_resolver_rejects_non_numeric_element_types()
	{
		Action act = () => _ = new MinElementResolver(new ArrayVariableResolver("flags", typeof(bool)));

		act.Should().Throw<ArgumentException>();
	}
}
