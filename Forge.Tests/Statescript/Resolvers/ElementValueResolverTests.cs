// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ElementValueResolverTests
{
	[Fact]
	[Trait("Resolver", "ElementValue")]
	public void Element_value_resolver_returns_default_outside_array_iteration()
	{
		var resolver = new ElementValueResolver(typeof(int));

		resolver.Resolve(new GraphContext()).AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "ElementValue")]
	public void Element_value_resolver_reports_configured_value_type()
	{
		new ElementValueResolver(typeof(float)).ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "ElementValue")]
	public void Element_value_resolver_reads_each_iterated_element()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(1), new Variant128(2)]);
		var source = new ArrayVariableResolver("numbers", typeof(int));

		var resolver = new SelectResolver(source, new ElementValueResolver(typeof(int)));

		Variant128[] result = resolver.ResolveArray(context);

		result.Should().HaveCount(3);
		result[0].AsInt().Should().Be(3);
		result[1].AsInt().Should().Be(1);
		result[2].AsInt().Should().Be(2);
	}
}
