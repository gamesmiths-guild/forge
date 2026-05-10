// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ArrayResolverTests
{
	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_reads_graph_array_variable()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineArrayVariable("targets", 10, 20, 30);
		var context = new GraphContext();
		context.GraphVariables.InitializeFrom(graph.VariableDefinitions);
		var resolver = new ArrayVariableResolver("targets", typeof(int));

		Variant128[] result = resolver.ResolveArray(context);

		result.Should().HaveCount(3);
		result[0].AsInt().Should().Be(10);
		result[1].AsInt().Should().Be(20);
		result[2].AsInt().Should().Be(30);
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_reads_shared_array_variable()
	{
		var sharedVariables = new Variables();
		sharedVariables.DefineArrayVariable(
			"targets",
			[new Variant128(10), new Variant128(20), new Variant128(30)]);
		var resolver = new ArrayVariableResolver("targets", typeof(int), VariableScope.Shared);
		var context = new GraphContext { SharedVariables = sharedVariables };

		Variant128[] result = resolver.ResolveArray(context);

		result.Should().HaveCount(3);
		result[0].AsInt().Should().Be(10);
		result[1].AsInt().Should().Be(20);
		result[2].AsInt().Should().Be(30);
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_returns_empty_array_when_shared_variables_is_null()
	{
		var resolver = new ArrayVariableResolver("targets", typeof(int), VariableScope.Shared);
		var context = new GraphContext();

		resolver.ResolveArray(context).Should().BeEmpty();
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_returns_empty_array_for_missing_variable()
	{
		var graph = new Graph();
		var context = new GraphContext();
		context.GraphVariables.InitializeFrom(graph.VariableDefinitions);
		var resolver = new ArrayVariableResolver("missing", typeof(int));

		resolver.ResolveArray(context).Should().BeEmpty();
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_reflects_runtime_array_changes()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineArrayVariable("targets", 10, 20);
		var context = new GraphContext();
		context.GraphVariables.InitializeFrom(graph.VariableDefinitions);
		var resolver = new ArrayVariableResolver("targets", typeof(int));

		context.GraphVariables.SetArrayElement("targets", 1, 99);

		Variant128[] result = resolver.ResolveArray(context);

		result.Should().HaveCount(2);
		result[1].AsInt().Should().Be(99);
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_reports_correct_element_type()
	{
		var resolver = new ArrayVariableResolver("anything", typeof(double));

		resolver.ElementType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "ArrayProperty")]
	public void Array_property_resolver_returns_resolved_array()
	{
		Variant128[] expected = [new Variant128(10), new Variant128(20), new Variant128(30)];
		var resolver = new TestArrayPropertyResolver(typeof(int), [expected]);

		var context = new GraphContext();

		Variant128[] result = resolver.ResolveArray(context);

		result.Should().BeEquivalentTo(expected);
	}

	[Fact]
	[Trait("Resolver", "ArrayProperty")]
	public void Array_property_resolver_reports_correct_element_type()
	{
		var intResolver = new TestArrayPropertyResolver(typeof(int), [[]]);
		var doubleResolver = new TestArrayPropertyResolver(typeof(double), [[]]);

		intResolver.ElementType.Should().Be(typeof(int));
		doubleResolver.ElementType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "ArrayProperty")]
	public void Array_property_resolver_returns_empty_array()
	{
		var resolver = new TestArrayPropertyResolver(typeof(int), [[]]);

		var context = new GraphContext();

		Variant128[] result = resolver.ResolveArray(context);

		result.Should().BeEmpty();
	}
}
