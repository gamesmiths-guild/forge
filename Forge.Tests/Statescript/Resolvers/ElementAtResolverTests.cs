// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ElementAtResolverTests
{
	[Fact]
	[Trait("Resolver", "ElementAt")]
	public void Element_at_resolver_reads_the_element_at_the_resolved_index()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(1), new Variant128(2)]);
		var source = new ArrayVariableResolver("numbers", typeof(int));

		var resolver = new ElementAtResolver(source, new VariantResolver(new Variant128(1), typeof(int)));

		resolver.ValueType.Should().Be(typeof(int));
		resolver.Resolve(context).AsInt().Should().Be(1);
	}

	[Fact]
	[Trait("Resolver", "ElementAt")]
	public void Element_at_resolver_returns_default_for_out_of_range_index()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable("numbers", [new Variant128(3)]);
		var source = new ArrayVariableResolver("numbers", typeof(int));

		var resolver = new ElementAtResolver(source, new VariantResolver(new Variant128(5), typeof(int)));

		resolver.Resolve(context).AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "ElementAt")]
	public void Element_at_resolver_rejects_non_numeric_index_resolvers()
	{
		var source = new ArrayVariableResolver("numbers", typeof(int));

		Action act = () => _ = new ElementAtResolver(source, new VariantResolver(new Variant128(true), typeof(bool)));

		act.Should().Throw<ArgumentException>();
	}
}
