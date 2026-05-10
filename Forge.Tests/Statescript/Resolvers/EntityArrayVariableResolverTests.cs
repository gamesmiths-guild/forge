// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class EntityArrayVariableResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "EntityArrayVariable")]
	public void Entity_array_variable_resolver_reads_graph_reference_array()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var entity2 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		var resolver = new EntityArrayVariableResolver("targets");

		context.GraphVariables.DefineReferenceArrayVariable<IForgeEntity>("targets", [entity1, entity2]);

		IForgeEntity?[] result = resolver.ResolveArray(context);

		result.Should().Equal(entity1, entity2);
	}

	[Fact]
	[Trait("Resolver", "EntityArrayVariable")]
	public void Entity_array_variable_resolver_reads_shared_reference_array()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var entity2 = new TestEntity(_tagsManager, _cuesManager);
		var sharedVariables = new Variables();
		var context = new GraphContext { SharedVariables = sharedVariables };
		var resolver = new EntityArrayVariableResolver("targets", VariableScope.Shared);

		sharedVariables.DefineReferenceArrayVariable<IForgeEntity>("targets", [entity1, entity2]);

		IForgeEntity?[] result = resolver.ResolveArray(context);

		result.Should().Equal(entity1, entity2);
	}

	[Fact]
	[Trait("Resolver", "EntityArrayVariable")]
	public void Entity_array_variable_resolver_returns_empty_array_for_missing_variable()
	{
		var resolver = new EntityArrayVariableResolver("missing");

		resolver.ResolveArray(new GraphContext()).Should().BeEmpty();
	}
}
