// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ExceptResolverTests
{
	[Fact]
	[Trait("Resolver", "Except")]
	public void Except_resolver_removes_elements_found_in_the_other_array()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(1), new Variant128(2), new Variant128(1)]);
		context.GraphVariables.DefineArrayVariable("excluded", [new Variant128(1)]);

		var resolver = new ExceptResolver(
			new ArrayVariableResolver("numbers", typeof(int)),
			new ArrayVariableResolver("excluded", typeof(int)));

		Variant128[] result = resolver.ResolveArray(context);

		result.Should().HaveCount(2);
		result[0].AsInt().Should().Be(3);
		result[1].AsInt().Should().Be(2);
	}

	[Fact]
	[Trait("Resolver", "Except")]
	public void Except_resolver_keeps_the_array_unchanged_when_the_other_is_empty()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable("numbers", [new Variant128(3), new Variant128(1)]);

		var resolver = new ExceptResolver(
			new ArrayVariableResolver("numbers", typeof(int)),
			new ArrayVariableResolver("missing", typeof(int)));

		resolver.ResolveArray(context).Should().HaveCount(2);
	}
}
