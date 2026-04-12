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
	public void Array_resolver_returns_first_element_on_resolve()
	{
		var resolver = new ArrayVariableResolver(
			[new Variant128(10), new Variant128(20), new Variant128(30)],
			typeof(int));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(10);
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_returns_default_when_empty()
	{
		var resolver = new ArrayVariableResolver([], typeof(int));

		var context = new GraphContext();

		resolver.Resolve(context).AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_get_element_returns_correct_value()
	{
		var resolver = new ArrayVariableResolver(
			[new Variant128(10), new Variant128(20), new Variant128(30)],
			typeof(int));

		resolver.GetElement(0).AsInt().Should().Be(10);
		resolver.GetElement(1).AsInt().Should().Be(20);
		resolver.GetElement(2).AsInt().Should().Be(30);
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_set_element_updates_value()
	{
		var resolver = new ArrayVariableResolver(
			[new Variant128(10), new Variant128(20)],
			typeof(int));

		resolver.SetElement(1, new Variant128(99));

		resolver.GetElement(1).AsInt().Should().Be(99);
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_add_appends_element()
	{
		var resolver = new ArrayVariableResolver(
			[new Variant128(10)],
			typeof(int));

		resolver.Add(new Variant128(20));

		resolver.Length.Should().Be(2);
		resolver.GetElement(1).AsInt().Should().Be(20);
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_remove_at_removes_element()
	{
		var resolver = new ArrayVariableResolver(
			[new Variant128(10), new Variant128(20), new Variant128(30)],
			typeof(int));

		resolver.RemoveAt(1);

		resolver.Length.Should().Be(2);
		resolver.GetElement(0).AsInt().Should().Be(10);
		resolver.GetElement(1).AsInt().Should().Be(30);
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_clear_removes_all_elements()
	{
		var resolver = new ArrayVariableResolver(
			[new Variant128(10), new Variant128(20)],
			typeof(int));

		resolver.Clear();

		resolver.Length.Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Array")]
	public void Array_resolver_reports_correct_value_type()
	{
		var resolver = new ArrayVariableResolver([], typeof(double));

		resolver.ValueType.Should().Be(typeof(double));
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
