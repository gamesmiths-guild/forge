// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ObjectReverseResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ObjectReverse")]
	public void Object_reverse_resolver_reverses_the_element_order()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var entity2 = new TestEntity(_tagsManager, _cuesManager);
		var entity3 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1, entity2, entity3]);

		var resolver = new ObjectReverseResolver<IForgeEntity>(new EntityArrayVariableResolver("targets"));

		resolver.ResolveArray(context).Should().Equal(entity3, entity2, entity1);
	}

	[Fact]
	[Trait("Resolver", "ObjectReverse")]
	public void Object_reverse_resolver_returns_empty_array_for_missing_variable()
	{
		var resolver = new ObjectReverseResolver<IForgeEntity>(new EntityArrayVariableResolver("missing"));

		resolver.ResolveArray(new GraphContext()).Should().BeEmpty();
	}
}
