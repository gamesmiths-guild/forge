// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ObjectAppendResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ObjectAppend")]
	public void Object_append_resolver_adds_elements_to_the_end()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var entity2 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1]);
		context.GraphVariables.DefineObjectVariable<IForgeEntity>("extra", entity2);

		var resolver = new ObjectAppendResolver<IForgeEntity>(
			new EntityArrayVariableResolver("targets"),
			new EntityVariableResolver("extra"));

		resolver.ResolveArray(context).Should().Equal(entity1, entity2);
	}

	[Fact]
	[Trait("Resolver", "ObjectAppend")]
	public void Object_append_resolver_appends_to_an_empty_source()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable<IForgeEntity>("extra", entity1);

		var resolver = new ObjectAppendResolver<IForgeEntity>(
			new EntityArrayVariableResolver("missing"),
			new EntityVariableResolver("extra"));

		resolver.ResolveArray(context).Should().Equal(entity1);
	}
}
