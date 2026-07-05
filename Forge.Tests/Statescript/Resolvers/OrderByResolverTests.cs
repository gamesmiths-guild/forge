// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class OrderByResolverTests
{
	[Fact]
	[Trait("Resolver", "OrderBy")]
	public void Order_by_resolver_sorts_elements_ascending_by_default()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(1), new Variant128(2)]);
		var source = new ArrayVariableResolver("numbers", typeof(int));

		var resolver = new OrderByResolver(source, new ElementValueResolver(typeof(int)));

		Variant128[] result = resolver.ResolveArray(context);

		result.Should().HaveCount(3);
		result[0].AsInt().Should().Be(1);
		result[1].AsInt().Should().Be(2);
		result[2].AsInt().Should().Be(3);
	}

	[Fact]
	[Trait("Resolver", "OrderBy")]
	public void Order_by_resolver_sorts_elements_descending_when_configured()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(1), new Variant128(2)]);
		var source = new ArrayVariableResolver("numbers", typeof(int));

		var resolver = new OrderByResolver(
			source,
			new ElementValueResolver(typeof(int)),
			SortDirection.Descending);

		Variant128[] result = resolver.ResolveArray(context);

		result.Should().HaveCount(3);
		result[0].AsInt().Should().Be(3);
		result[1].AsInt().Should().Be(2);
		result[2].AsInt().Should().Be(1);
	}

	[Fact]
	[Trait("Resolver", "OrderBy")]
	public void Order_by_resolver_returns_empty_array_for_missing_variable()
	{
		var resolver = new OrderByResolver(
			new ArrayVariableResolver("missing", typeof(int)),
			new ElementValueResolver(typeof(int)));

		resolver.ResolveArray(new GraphContext()).Should().BeEmpty();
	}

	[Fact]
	[Trait("Resolver", "OrderBy")]
	public void Order_by_resolver_rejects_non_numeric_key_selectors()
	{
		var source = new ArrayVariableResolver("numbers", typeof(int));

		Action act = () => _ = new OrderByResolver(
			source,
			new VariantResolver(new Variant128(true), typeof(bool)));

		act.Should().Throw<ArgumentException>();
	}
}
