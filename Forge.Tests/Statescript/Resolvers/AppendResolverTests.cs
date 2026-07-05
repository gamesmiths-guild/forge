// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class AppendResolverTests
{
	[Fact]
	[Trait("Resolver", "Append")]
	public void Append_resolver_adds_elements_to_the_end()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable("numbers", [new Variant128(3)]);
		var source = new ArrayVariableResolver("numbers", typeof(int));

		var resolver = new AppendResolver(
			source,
			new VariantResolver(new Variant128(1), typeof(int)),
			new VariantResolver(new Variant128(2), typeof(int)));

		Variant128[] result = resolver.ResolveArray(context);

		result.Should().HaveCount(3);
		result[0].AsInt().Should().Be(3);
		result[1].AsInt().Should().Be(1);
		result[2].AsInt().Should().Be(2);
	}

	[Fact]
	[Trait("Resolver", "Append")]
	public void Append_resolver_rejects_element_resolvers_of_a_different_type()
	{
		var source = new ArrayVariableResolver("numbers", typeof(int));

		Action act = () => _ = new AppendResolver(source, new VariantResolver(new Variant128(1f), typeof(float)));

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "Append")]
	public void Append_resolver_rejects_null_element_resolvers()
	{
		var source = new ArrayVariableResolver("numbers", typeof(int));

		Action act = () => _ = new AppendResolver(source, (IPropertyResolver)null!);

		act.Should().Throw<ArgumentException>();
	}
}
