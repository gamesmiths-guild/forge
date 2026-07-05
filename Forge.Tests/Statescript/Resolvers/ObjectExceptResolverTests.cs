// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ObjectExceptResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ObjectExcept")]
	public void Object_except_resolver_removes_references_found_in_the_other_array()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var entity2 = new TestEntity(_tagsManager, _cuesManager);
		var entity3 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1, entity2, entity3]);
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("excluded", [entity2]);

		var resolver = new ObjectExceptResolver<IForgeEntity>(
			new EntityArrayVariableResolver("targets"),
			new EntityArrayVariableResolver("excluded"));

		resolver.ResolveArray(context).Should().Equal(entity1, entity3);
	}

	[Fact]
	[Trait("Resolver", "ObjectExcept")]
	public void Object_except_resolver_keeps_the_array_unchanged_when_the_other_is_empty()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1]);

		var resolver = new ObjectExceptResolver<IForgeEntity>(
			new EntityArrayVariableResolver("targets"),
			new EntityArrayVariableResolver("missing"));

		resolver.ResolveArray(context).Should().Equal(entity1);
	}
}
