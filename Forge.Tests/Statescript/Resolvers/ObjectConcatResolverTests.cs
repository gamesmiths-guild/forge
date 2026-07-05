// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ObjectConcatResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ObjectConcat")]
	public void Object_concat_resolver_appends_the_second_array_after_the_first()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var entity2 = new TestEntity(_tagsManager, _cuesManager);
		var entity3 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("first", [entity1, entity2]);
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("second", [entity3]);

		var resolver = new ObjectConcatResolver<IForgeEntity>(
			new EntityArrayVariableResolver("first"),
			new EntityArrayVariableResolver("second"));

		resolver.ResolveArray(context).Should().Equal(entity1, entity2, entity3);
	}

	[Fact]
	[Trait("Resolver", "ObjectConcat")]
	public void Object_concat_resolver_returns_the_other_side_when_one_is_empty()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("first", [entity1]);

		var resolver = new ObjectConcatResolver<IForgeEntity>(
			new EntityArrayVariableResolver("first"),
			new EntityArrayVariableResolver("missing"));

		resolver.ResolveArray(context).Should().Equal(entity1);
	}
}
