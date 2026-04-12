// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class VariantResolverTests
{
	[Fact]
	[Trait("Resolver", "Variant")]
	public void Variant_resolver_returns_stored_value()
	{
		var resolver = new VariantResolver(new Variant128(42.0), typeof(double));

		var context = new GraphContext();

		Variant128 result = resolver.Resolve(context);

		result.AsDouble().Should().Be(42.0);
	}

	[Fact]
	[Trait("Resolver", "Variant")]
	public void Variant_resolver_value_can_be_updated()
	{
		var resolver = new VariantResolver(new Variant128(10), typeof(int));

		resolver.Set(25);

		var context = new GraphContext();
		Variant128 result = resolver.Resolve(context);

		result.AsInt().Should().Be(25);
	}

	[Fact]
	[Trait("Resolver", "Variant")]
	public void Variant_resolver_reports_correct_value_type()
	{
		var intResolver = new VariantResolver(new Variant128(0), typeof(int));
		var boolResolver = new VariantResolver(new Variant128(false), typeof(bool));
		var doubleResolver = new VariantResolver(new Variant128(0.0), typeof(double));

		intResolver.ValueType.Should().Be(typeof(int));
		boolResolver.ValueType.Should().Be(typeof(bool));
		doubleResolver.ValueType.Should().Be(typeof(double));
	}
}
