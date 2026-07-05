// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class EntityElementAtResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "EntityElementAt")]
	public void Entity_element_at_resolver_composes_with_attribute_resolver()
	{
		var entity1 = new VitalTestEntity(_tagsManager, _cuesManager);
		var entity2 = new VitalTestEntity(_tagsManager, _cuesManager);
		entity1.VitalAttributeSet.UpdateBaseHealth(40);
		entity2.VitalAttributeSet.UpdateBaseHealth(70);

		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1, entity2]);

		var resolver = new AttributeResolver(
			"VitalAttributeSet.CurrentHealth",
			new EntityElementAtResolver(
				new EntityArrayVariableResolver("targets"),
				new VariantResolver(new Variant128(1), typeof(int))));

		resolver.Resolve(context).AsInt().Should().Be(70);
	}
}
