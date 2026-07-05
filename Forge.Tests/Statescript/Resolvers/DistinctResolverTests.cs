// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class DistinctResolverTests
{
	[Fact]
	[Trait("Resolver", "Distinct")]
	public void Distinct_resolver_keeps_the_first_occurrence_of_each_value()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(1), new Variant128(3), new Variant128(2), new Variant128(1)]);
		var source = new ArrayVariableResolver("numbers", typeof(int));

		var resolver = new DistinctResolver(source);

		Variant128[] result = resolver.ResolveArray(context);

		result.Should().HaveCount(3);
		result[0].AsInt().Should().Be(3);
		result[1].AsInt().Should().Be(1);
		result[2].AsInt().Should().Be(2);
	}

	[Fact]
	[Trait("Resolver", "Distinct")]
	public void Distinct_resolver_returns_empty_array_for_missing_variable()
	{
		var resolver = new DistinctResolver(new ArrayVariableResolver("missing", typeof(int)));

		resolver.ResolveArray(new GraphContext()).Should().BeEmpty();
	}
}
