// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ObjectContainsResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ObjectContains")]
	public void Object_contains_resolver_returns_true_when_the_reference_is_present()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var entity2 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1, entity2]);
		context.GraphVariables.DefineObjectVariable<IForgeEntity>("candidate", entity2);

		var resolver = new ObjectContainsResolver(
			new EntityArrayVariableResolver("targets"),
			new EntityVariableResolver("candidate"));

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "ObjectContains")]
	public void Object_contains_resolver_returns_false_when_the_reference_is_absent()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1]);
		context.GraphVariables.DefineObjectVariable<IForgeEntity>(
			"candidate",
			new TestEntity(_tagsManager, _cuesManager));

		var resolver = new ObjectContainsResolver(
			new EntityArrayVariableResolver("targets"),
			new EntityVariableResolver("candidate"));

		resolver.Resolve(context).AsBool().Should().BeFalse();
	}
}
