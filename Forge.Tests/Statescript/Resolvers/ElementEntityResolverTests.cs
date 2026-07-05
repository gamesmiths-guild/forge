// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ElementEntityResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ElementEntity")]
	public void Element_entity_resolver_returns_null_outside_array_iteration()
	{
		var resolver = new ElementEntityResolver();

		resolver.Resolve(new GraphContext()).Should().BeNull();
	}

	[Fact]
	[Trait("Resolver", "ElementEntity")]
	public void Element_entity_resolver_composes_with_attribute_resolver_per_element()
	{
		var entity1 = new VitalTestEntity(_tagsManager, _cuesManager);
		var entity2 = new VitalTestEntity(_tagsManager, _cuesManager);
		entity1.VitalAttributeSet.UpdateBaseHealth(40);
		entity2.VitalAttributeSet.UpdateBaseHealth(70);

		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable("targets", [entity1, entity2]);
		var source = new ObjectArrayVariableResolver<VitalTestEntity>("targets");

		var resolver = new SelectResolver(
			source,
			new AttributeResolver("VitalAttributeSet.CurrentHealth", new ElementEntityResolver()));

		Variant128[] result = resolver.ResolveArray(context);

		result.Should().HaveCount(2);
		result[0].AsInt().Should().Be(40);
		result[1].AsInt().Should().Be(70);
	}
}
