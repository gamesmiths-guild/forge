// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class RemoveAtResolverTests
{
	[Fact]
	[Trait("Resolver", "RemoveAt")]
	public void Remove_at_resolver_removes_the_element_at_the_resolved_index()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(1), new Variant128(2)]);
		var source = new ArrayVariableResolver("numbers", typeof(int));

		var resolver = new RemoveAtResolver(source, new VariantResolver(new Variant128(1), typeof(int)));

		Variant128[] result = resolver.ResolveArray(context);

		result.Should().HaveCount(2);
		result[0].AsInt().Should().Be(3);
		result[1].AsInt().Should().Be(2);
	}

	[Fact]
	[Trait("Resolver", "RemoveAt")]
	public void Remove_at_resolver_keeps_the_array_unchanged_for_out_of_range_index()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable("numbers", [new Variant128(3), new Variant128(1)]);
		var source = new ArrayVariableResolver("numbers", typeof(int));

		var resolver = new RemoveAtResolver(source, new VariantResolver(new Variant128(5), typeof(int)));

		resolver.ResolveArray(context).Should().HaveCount(2);
	}
}
