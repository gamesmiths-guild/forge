// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ReverseResolverTests
{
	[Fact]
	[Trait("Resolver", "Reverse")]
	public void Reverse_resolver_reverses_the_element_order()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(1), new Variant128(2)]);
		var source = new ArrayVariableResolver("numbers", typeof(int));

		var resolver = new ReverseResolver(source);

		Variant128[] result = resolver.ResolveArray(context);

		result.Should().HaveCount(3);
		result[0].AsInt().Should().Be(2);
		result[1].AsInt().Should().Be(1);
		result[2].AsInt().Should().Be(3);
	}

	[Fact]
	[Trait("Resolver", "Reverse")]
	public void Reverse_resolver_returns_empty_array_for_missing_variable()
	{
		var resolver = new ReverseResolver(new ArrayVariableResolver("missing", typeof(int)));

		resolver.ResolveArray(new GraphContext()).Should().BeEmpty();
	}
}
