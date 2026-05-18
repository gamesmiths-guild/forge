// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ArrayCompositionResolverTests
{
	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_resolves_nested_value_resolvers_in_order()
	{
		var resolver = new ArrayResolver(
			new PiResolver(typeof(float)),
			new EResolver(typeof(float)),
			new VariantResolver(new Variant128(1.5f), typeof(float)));

		Variant128[] result = resolver.ResolveArray(new GraphContext());

		result.Should().HaveCount(3);
		result[0].AsFloat().Should().Be(MathF.PI);
		result[1].AsFloat().Should().Be(MathF.E);
		result[2].AsFloat().Should().Be(1.5f);
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_infers_element_type_from_nested_resolvers()
	{
		var resolver = new ArrayResolver(new PiResolver(), new EResolver());

		resolver.ElementType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_allows_empty_arrays_with_explicit_type()
	{
		var resolver = new ArrayResolver(typeof(int));

		resolver.ElementType.Should().Be(typeof(int));
		resolver.ResolveArray(new GraphContext()).Should().BeEmpty();
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_throws_for_mismatched_nested_resolver_types()
	{
#pragma warning disable CA1806
		Action act = () => new ArrayResolver(
			typeof(float),
			new PiResolver(typeof(float)),
			new EResolver());
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Graph_context_resolves_array_property_from_nested_array_resolver()
	{
		var graph = new Graph();
		var readArray = new ReadArrayPropertyNode();

		graph.VariableDefinitions.DefineArrayProperty(
			"constants",
			new ArrayResolver(new PiResolver(), new EResolver()));
		readArray.BindInput(ReadArrayPropertyNode.InputArray, "constants");
		graph.AddNode(readArray);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			readArray.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);

		processor.StartGraph();

		readArray.LastReadArray.Should().NotBeNull();
		readArray.LastReadArray.Should().HaveCount(2);
		readArray.LastReadArray![0].AsDouble().Should().Be(Math.PI);
		readArray.LastReadArray[1].AsDouble().Should().Be(Math.E);
	}
}
