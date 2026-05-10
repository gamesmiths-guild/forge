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
	public void Variable_resolver_reads_value_from_shared_variables()
	{
		var sharedVariables = new Variables();
		sharedVariables.DefineVariable("abilityLock", true);

		var resolver = new VariableResolver("abilityLock", typeof(bool), VariableScope.Shared);
		var context = new GraphContext { SharedVariables = sharedVariables };

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "Variable")]
	public void Variable_resolver_returns_default_when_shared_variables_is_null()
	{
		var resolver = new VariableResolver("abilityLock", typeof(double), VariableScope.Shared);

		var context = new GraphContext();

		resolver.Resolve(context).AsDouble().Should().Be(0);
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
	public void Variable_resolver_reflects_changes_across_shared_graph_contexts()
	{
		var sharedVariables = new Variables();
		sharedVariables.DefineVariable("sharedCounter", 0);

		var resolver = new VariableResolver("sharedCounter", typeof(int), VariableScope.Shared);

		var context1 = new GraphContext { SharedVariables = sharedVariables };
		var context2 = new GraphContext { SharedVariables = sharedVariables };

		resolver.Resolve(context1).AsInt().Should().Be(0);

		sharedVariables.SetVar("sharedCounter", 42);

		resolver.Resolve(context1).AsInt().Should().Be(42);
		resolver.Resolve(context2).AsInt().Should().Be(42);
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
