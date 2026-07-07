// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ObjectIndexOfResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ObjectIndexOf")]
	public void Object_index_of_resolver_returns_the_index_of_the_reference()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var entity2 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1, entity2]);
		context.GraphVariables.DefineObjectVariable<IForgeEntity>("candidate", entity2);

		var resolver = new ObjectIndexOfResolver(
			new EntityArrayVariableResolver("targets"),
			new EntityVariableResolver("candidate"));

		resolver.Resolve(context).AsInt().Should().Be(1);
	}

	[Fact]
	[Trait("Resolver", "ObjectIndexOf")]
	public void Object_index_of_resolver_returns_minus_one_when_the_reference_is_absent()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1]);
		context.GraphVariables.DefineObjectVariable<IForgeEntity>(
			"candidate",
			new TestEntity(_tagsManager, _cuesManager));

		var resolver = new ObjectIndexOfResolver(
			new EntityArrayVariableResolver("targets"),
			new EntityVariableResolver("candidate"));

		resolver.Resolve(context).AsInt().Should().Be(-1);
	}
}
