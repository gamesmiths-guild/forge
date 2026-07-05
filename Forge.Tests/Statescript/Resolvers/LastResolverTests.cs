// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class LastResolverTests
{
	[Fact]
	[Trait("Resolver", "Last")]
	public void Last_resolver_reads_the_last_element()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(1), new Variant128(2)]);
		var source = new ArrayVariableResolver("numbers", typeof(int));

		var resolver = new LastResolver(source);

		resolver.ValueType.Should().Be(typeof(int));
		resolver.Resolve(context).AsInt().Should().Be(2);
	}

	[Fact]
	[Trait("Resolver", "Last")]
	public void Last_resolver_returns_default_for_empty_array()
	{
		var resolver = new LastResolver(new ArrayVariableResolver("missing", typeof(int)));

		resolver.Resolve(new GraphContext()).AsInt().Should().Be(0);
	}
}
