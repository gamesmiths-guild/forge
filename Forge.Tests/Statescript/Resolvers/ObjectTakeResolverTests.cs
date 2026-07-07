// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ObjectTakeResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ObjectTake")]
	public void Object_take_resolver_keeps_the_first_elements()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var entity2 = new TestEntity(_tagsManager, _cuesManager);
		var entity3 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1, entity2, entity3]);

		var resolver = new ObjectTakeResolver<IForgeEntity>(
			new EntityArrayVariableResolver("targets"),
			new VariantResolver(new Variant128(2), typeof(int)));

		resolver.ResolveArray(context).Should().Equal(entity1, entity2);
	}

	[Fact]
	[Trait("Resolver", "ObjectTake")]
	public void Object_take_resolver_clamps_counts_larger_than_the_array()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1]);

		var resolver = new ObjectTakeResolver<IForgeEntity>(
			new EntityArrayVariableResolver("targets"),
			new VariantResolver(new Variant128(5), typeof(int)));

		resolver.ResolveArray(context).Should().Equal(entity1);
	}
}
