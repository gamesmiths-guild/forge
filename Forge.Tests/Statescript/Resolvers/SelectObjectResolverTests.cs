// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class SelectObjectResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "SelectObject")]
	public void Select_object_resolver_projects_each_object_element()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var entity2 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1, entity2]);

		var resolver = new SelectObjectResolver<IForgeEntity>(
			new EntityArrayVariableResolver("targets"),
			new ElementEntityResolver());

		resolver.ElementType.Should().Be(typeof(IForgeEntity));
		resolver.ResolveArray(context).Should().Equal(entity1, entity2);
	}

	[Fact]
	[Trait("Resolver", "SelectObject")]
	public void Select_object_resolver_returns_empty_array_for_missing_variable()
	{
		var resolver = new SelectObjectResolver<IForgeEntity>(
			new EntityArrayVariableResolver("missing"),
			new ElementEntityResolver());

		resolver.ResolveArray(new GraphContext()).Should().BeEmpty();
	}
}
