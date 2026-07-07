// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class SumResolverTests
{
	[Fact]
	[Trait("Resolver", "Sum")]
	public void Sum_resolver_adds_all_int_elements()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(1), new Variant128(2)]);

		var resolver = new SumResolver(new ArrayVariableResolver("numbers", typeof(int)));

		resolver.ValueType.Should().Be(typeof(int));
		resolver.Resolve(context).AsInt().Should().Be(6);
	}

	[Fact]
	[Trait("Resolver", "Sum")]
	public void Sum_resolver_adds_all_float_elements()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(1.5f), new Variant128(2.25f)]);

		var resolver = new SumResolver(new ArrayVariableResolver("numbers", typeof(float)));

		resolver.ValueType.Should().Be(typeof(float));
		resolver.Resolve(context).AsFloat().Should().Be(3.75f);
	}

	[Fact]
	[Trait("Resolver", "Sum")]
	public void Sum_resolver_returns_zero_for_empty_arrays()
	{
		var resolver = new SumResolver(new ArrayVariableResolver("missing", typeof(int)));

		resolver.Resolve(new GraphContext()).AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Sum")]
	public void Sum_resolver_rejects_non_numeric_element_types()
	{
		Action act = () => _ = new SumResolver(new ArrayVariableResolver("flags", typeof(bool)));

		act.Should().Throw<ArgumentException>();
	}
}
