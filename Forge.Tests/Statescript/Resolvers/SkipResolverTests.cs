// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class SkipResolverTests
{
	[Fact]
	[Trait("Resolver", "Skip")]
	public void Skip_resolver_drops_the_first_elements()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(1), new Variant128(2)]);
		var source = new ArrayVariableResolver("numbers", typeof(int));

		var resolver = new SkipResolver(source, new VariantResolver(new Variant128(1), typeof(int)));

		Variant128[] result = resolver.ResolveArray(context);

		result.Should().HaveCount(2);
		result[0].AsInt().Should().Be(1);
		result[1].AsInt().Should().Be(2);
	}

	[Fact]
	[Trait("Resolver", "Skip")]
	public void Skip_resolver_returns_empty_array_when_skipping_more_than_the_length()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable("numbers", [new Variant128(3), new Variant128(1)]);
		var source = new ArrayVariableResolver("numbers", typeof(int));

		var resolver = new SkipResolver(source, new VariantResolver(new Variant128(5), typeof(int)));

		resolver.ResolveArray(context).Should().BeEmpty();
	}

	[Fact]
	[Trait("Resolver", "Skip")]
	public void Skip_resolver_keeps_all_elements_for_negative_counts()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable("numbers", [new Variant128(3), new Variant128(1)]);
		var source = new ArrayVariableResolver("numbers", typeof(int));

		var resolver = new SkipResolver(source, new VariantResolver(new Variant128(-1), typeof(int)));

		resolver.ResolveArray(context).Should().HaveCount(2);
	}
}
