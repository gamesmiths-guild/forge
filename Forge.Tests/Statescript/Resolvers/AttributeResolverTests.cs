// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.ResolverTestContextFactory;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class AttributeResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "Attribute")]
	public void Attribute_resolver_returns_current_value_of_existing_attribute()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var resolver = new AttributeResolver("TestAttributeSet.Attribute5");

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsInt().Should().Be(5);
	}

	[Fact]
	[Trait("Resolver", "Attribute")]
	public void Attribute_resolver_returns_default_for_missing_attribute()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var resolver = new AttributeResolver("TestAttributeSet.NonExistent");

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Attribute")]
	public void Attribute_resolver_returns_default_when_no_activation_context()
	{
		var resolver = new AttributeResolver("TestAttributeSet.Attribute5");

		var context = new GraphContext();

		Variant128 result = resolver.Resolve(context);

		result.AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Attribute")]
	public void Attribute_resolver_value_type_is_int()
	{
		var resolver = new AttributeResolver("TestAttributeSet.Attribute5");

		resolver.ValueType.Should().Be(typeof(int));
	}

	[Fact]
	[Trait("Resolver", "Attribute")]
	public void Attribute_resolver_reads_different_attributes()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var resolver1 = new AttributeResolver("TestAttributeSet.Attribute1");
		var resolver90 = new AttributeResolver("TestAttributeSet.Attribute90");

		GraphContext context = CreateAbilityGraphContext(entity);

		resolver1.Resolve(context).AsInt().Should().Be(1);
		resolver90.Resolve(context).AsInt().Should().Be(90);
	}
}
