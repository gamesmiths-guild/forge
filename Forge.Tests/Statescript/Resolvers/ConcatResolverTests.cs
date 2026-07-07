// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ConcatResolverTests
{
	[Fact]
	[Trait("Resolver", "Concat")]
	public void Concat_resolver_appends_the_second_array_after_the_first()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable("first", [new Variant128(3), new Variant128(1)]);
		context.GraphVariables.DefineArrayVariable("second", [new Variant128(2)]);

		var resolver = new ConcatResolver(
			new ArrayVariableResolver("first", typeof(int)),
			new ArrayVariableResolver("second", typeof(int)));

		Variant128[] result = resolver.ResolveArray(context);

		result.Should().HaveCount(3);
		result[0].AsInt().Should().Be(3);
		result[1].AsInt().Should().Be(1);
		result[2].AsInt().Should().Be(2);
	}

	[Fact]
	[Trait("Resolver", "Concat")]
	public void Concat_resolver_rejects_mismatched_element_types()
	{
		Action act = () => _ = new ConcatResolver(
			new ArrayVariableResolver("first", typeof(int)),
			new ArrayVariableResolver("second", typeof(float)));

		act.Should().Throw<ArgumentException>();
	}
}
