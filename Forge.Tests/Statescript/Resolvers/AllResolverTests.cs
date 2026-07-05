// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class AllResolverTests
{
	[Fact]
	[Trait("Resolver", "All")]
	public void All_resolver_returns_true_when_every_element_matches_the_predicate()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(2)]);

		var resolver = new AllResolver(
			new ArrayVariableResolver("numbers", typeof(int)),
			new ComparisonResolver(
				new ElementValueResolver(typeof(int)),
				ComparisonOperation.GreaterThan,
				new VariantResolver(new Variant128(1), typeof(int))));

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "All")]
	public void All_resolver_returns_false_when_any_element_fails_the_predicate()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(1)]);

		var resolver = new AllResolver(
			new ArrayVariableResolver("numbers", typeof(int)),
			new ComparisonResolver(
				new ElementValueResolver(typeof(int)),
				ComparisonOperation.GreaterThan,
				new VariantResolver(new Variant128(1), typeof(int))));

		resolver.Resolve(context).AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "All")]
	public void All_resolver_returns_true_for_empty_arrays()
	{
		var resolver = new AllResolver(
			new ArrayVariableResolver("missing", typeof(int)),
			new ComparisonResolver(
				new ElementValueResolver(typeof(int)),
				ComparisonOperation.GreaterThan,
				new VariantResolver(new Variant128(1), typeof(int))));

		resolver.Resolve(new GraphContext()).AsBool().Should().BeTrue();
	}
}
