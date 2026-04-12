// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class VariableResolverTests
{
	[Fact]
	[Trait("Resolver", "Variable")]
	public void Variable_resolver_reads_value_from_graph_variables()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("speed", 7.5);

		var context = new GraphContext();
		context.GraphVariables.InitializeFrom(graph.VariableDefinitions);

		var resolver = new VariableResolver("speed", typeof(double));

		Variant128 result = resolver.Resolve(context);

		result.AsDouble().Should().Be(7.5);
	}

	[Fact]
	[Trait("Resolver", "Variable")]
	public void Variable_resolver_returns_default_for_missing_variable()
	{
		var graph = new Graph();

		var context = new GraphContext();
		context.GraphVariables.InitializeFrom(graph.VariableDefinitions);

		var resolver = new VariableResolver("nonexistent", typeof(double));

		Variant128 result = resolver.Resolve(context);

		result.AsDouble().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Variable")]
	public void Variable_resolver_reflects_runtime_variable_changes()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("counter", 0);

		var context = new GraphContext();
		context.GraphVariables.InitializeFrom(graph.VariableDefinitions);

		var resolver = new VariableResolver("counter", typeof(int));

		resolver.Resolve(context).AsInt().Should().Be(0);

		context.GraphVariables.SetVar("counter", 42);

		resolver.Resolve(context).AsInt().Should().Be(42);
	}

	[Fact]
	[Trait("Resolver", "Variable")]
	public void Variable_resolver_value_type_is_double()
	{
		var resolver = new VariableResolver("anything", typeof(double));

		resolver.ValueType.Should().Be(typeof(double));
	}
}
