// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class FirstResolverTests
{
	[Fact]
	[Trait("Resolver", "First")]
	public void First_resolver_reads_the_first_element()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(1), new Variant128(2)]);
		var source = new ArrayVariableResolver("numbers", typeof(int));

		var resolver = new FirstResolver(source);

		resolver.ValueType.Should().Be(typeof(int));
		resolver.Resolve(context).AsInt().Should().Be(3);
	}

	[Fact]
	[Trait("Resolver", "First")]
	public void First_resolver_returns_default_for_empty_array()
	{
		var resolver = new FirstResolver(new ArrayVariableResolver("missing", typeof(int)));

		resolver.Resolve(new GraphContext()).AsInt().Should().Be(0);
	}
}
