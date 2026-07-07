// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class TakeResolverTests
{
	[Fact]
	[Trait("Resolver", "Take")]
	public void Take_resolver_keeps_the_first_elements()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(1), new Variant128(2)]);
		var source = new ArrayVariableResolver("numbers", typeof(int));

		var resolver = new TakeResolver(source, new VariantResolver(new Variant128(2), typeof(int)));

		Variant128[] result = resolver.ResolveArray(context);

		result.Should().HaveCount(2);
		result[0].AsInt().Should().Be(3);
		result[1].AsInt().Should().Be(1);
	}

	[Fact]
	[Trait("Resolver", "Take")]
	public void Take_resolver_clamps_counts_larger_than_the_array()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable("numbers", [new Variant128(3), new Variant128(1)]);
		var source = new ArrayVariableResolver("numbers", typeof(int));

		var resolver = new TakeResolver(source, new VariantResolver(new Variant128(5), typeof(int)));

		resolver.ResolveArray(context).Should().HaveCount(2);
	}

	[Fact]
	[Trait("Resolver", "Take")]
	public void Take_resolver_returns_empty_array_for_negative_counts()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable("numbers", [new Variant128(3), new Variant128(1)]);
		var source = new ArrayVariableResolver("numbers", typeof(int));

		var resolver = new TakeResolver(source, new VariantResolver(new Variant128(-1), typeof(int)));

		resolver.ResolveArray(context).Should().BeEmpty();
	}
}
