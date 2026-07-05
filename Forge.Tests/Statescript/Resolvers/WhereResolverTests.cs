// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class WhereResolverTests
{
	[Fact]
	[Trait("Resolver", "Where")]
	public void Where_resolver_keeps_only_elements_matching_the_predicate()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(1), new Variant128(2)]);
		var source = new ArrayVariableResolver("numbers", typeof(int));

		var resolver = new WhereResolver(
			source,
			new ComparisonResolver(
				new ElementValueResolver(typeof(int)),
				ComparisonOperation.GreaterThan,
				new VariantResolver(new Variant128(1), typeof(int))));

		Variant128[] result = resolver.ResolveArray(context);

		result.Should().HaveCount(2);
		result[0].AsInt().Should().Be(3);
		result[1].AsInt().Should().Be(2);
	}

	[Fact]
	[Trait("Resolver", "Where")]
	public void Where_resolver_removes_matching_elements_when_combined_with_not()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(1), new Variant128(2)]);
		var source = new ArrayVariableResolver("numbers", typeof(int));

		var resolver = new WhereResolver(
			source,
			new NotResolver(
				new ComparisonResolver(
					new ElementValueResolver(typeof(int)),
					ComparisonOperation.GreaterThan,
					new VariantResolver(new Variant128(1), typeof(int)))));

		Variant128[] result = resolver.ResolveArray(context);

		result.Should().ContainSingle();
		result[0].AsInt().Should().Be(1);
	}

	[Fact]
	[Trait("Resolver", "Where")]
	public void Where_resolver_returns_empty_array_for_missing_variable()
	{
		var resolver = new WhereResolver(
			new ArrayVariableResolver("missing", typeof(int)),
			new ComparisonResolver(
				new ElementValueResolver(typeof(int)),
				ComparisonOperation.GreaterThan,
				new VariantResolver(new Variant128(1), typeof(int))));

		resolver.ResolveArray(new GraphContext()).Should().BeEmpty();
	}

	[Fact]
	[Trait("Resolver", "Where")]
	public void Where_resolver_rejects_non_boolean_predicates()
	{
		var source = new ArrayVariableResolver("numbers", typeof(int));

		Action act = () => _ = new WhereResolver(source, new VariantResolver(new Variant128(1), typeof(int)));

		act.Should().Throw<ArgumentException>();
	}
}
