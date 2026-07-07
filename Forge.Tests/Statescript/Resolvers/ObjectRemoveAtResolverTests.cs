// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ObjectRemoveAtResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ObjectRemoveAt")]
	public void Object_remove_at_resolver_removes_the_element_at_the_resolved_index()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var entity2 = new TestEntity(_tagsManager, _cuesManager);
		var entity3 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1, entity2, entity3]);

		var resolver = new ObjectRemoveAtResolver<IForgeEntity>(
			new EntityArrayVariableResolver("targets"),
			new VariantResolver(new Variant128(0), typeof(int)));

		resolver.ResolveArray(context).Should().Equal(entity2, entity3);
	}

	[Fact]
	[Trait("Resolver", "ObjectRemoveAt")]
	public void Object_remove_at_resolver_keeps_the_array_unchanged_for_out_of_range_index()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1]);

		var resolver = new ObjectRemoveAtResolver<IForgeEntity>(
			new EntityArrayVariableResolver("targets"),
			new VariantResolver(new Variant128(3), typeof(int)));

		resolver.ResolveArray(context).Should().Equal(entity1);
	}
}
