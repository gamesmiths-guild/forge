// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ObjectFirstResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ObjectFirst")]
	public void Object_first_resolver_reads_the_first_element()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var entity2 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1, entity2]);
		var source = new EntityArrayVariableResolver("targets");

		var resolver = new ObjectFirstResolver<IForgeEntity>(source);

		resolver.Resolve(context).Should().BeSameAs(entity1);
	}

	[Fact]
	[Trait("Resolver", "ObjectFirst")]
	public void Object_first_resolver_returns_null_for_empty_array()
	{
		var resolver = new ObjectFirstResolver<IForgeEntity>(new EntityArrayVariableResolver("missing"));

		resolver.Resolve(new GraphContext()).Should().BeNull();
	}
}
