// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ContainsResolverTests
{
	[Fact]
	[Trait("Resolver", "Contains")]
	public void Contains_resolver_returns_true_when_the_value_is_present()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(1), new Variant128(2)]);

		var resolver = new ContainsResolver(
			new ArrayVariableResolver("numbers", typeof(int)),
			new VariantResolver(new Variant128(1), typeof(int)));

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "Contains")]
	public void Contains_resolver_returns_false_when_the_value_is_absent()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable("numbers", [new Variant128(3)]);

		var resolver = new ContainsResolver(
			new ArrayVariableResolver("numbers", typeof(int)),
			new VariantResolver(new Variant128(9), typeof(int)));

		resolver.Resolve(context).AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "Contains")]
	public void Contains_resolver_rejects_value_resolvers_of_a_different_type()
	{
		var source = new ArrayVariableResolver("numbers", typeof(int));

		Action act = () => _ = new ContainsResolver(source, new VariantResolver(new Variant128(1f), typeof(float)));

		act.Should().Throw<ArgumentException>();
	}
}
