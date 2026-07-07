// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ElementIndexResolverTests
{
	[Fact]
	[Trait("Resolver", "ElementIndex")]
	public void Element_index_resolver_returns_default_outside_array_iteration()
	{
		var resolver = new ElementIndexResolver();

		resolver.Resolve(new GraphContext()).AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "ElementIndex")]
	public void Element_index_resolver_reads_each_iterated_element_index()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(9), new Variant128(9), new Variant128(9)]);
		var source = new ArrayVariableResolver("numbers", typeof(int));

		var resolver = new SelectResolver(source, new ElementIndexResolver());

		Variant128[] result = resolver.ResolveArray(context);

		result.Should().HaveCount(3);
		result[0].AsInt().Should().Be(0);
		result[1].AsInt().Should().Be(1);
		result[2].AsInt().Should().Be(2);
	}
}
