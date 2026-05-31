// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ObjectVariableResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ObjectVariable")]
	public void Object_variable_resolver_reads_graph_object_variable()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var resolver = new ObjectVariableResolver<IForgeEntity>("selectedEntity");
		var context = new GraphContext();

		context.GraphVariables.DefineObjectVariable<IForgeEntity>("selectedEntity", entity);

		resolver.Resolve(context).Should().BeSameAs(entity);
	}

	[Fact]
	[Trait("Resolver", "ObjectVariable")]
	public void Object_variable_resolver_reads_shared_object_variable()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var sharedVariables = new Variables();
		var resolver = new ObjectVariableResolver<IForgeEntity>("selectedEntity", VariableScope.Shared);
		var context = new GraphContext { SharedVariables = sharedVariables };

		sharedVariables.DefineObjectVariable<IForgeEntity>("selectedEntity", entity);

		resolver.Resolve(context).Should().BeSameAs(entity);
	}

	[Fact]
	[Trait("Resolver", "ObjectVariable")]
	public void Object_variable_resolver_returns_null_for_missing_variable()
	{
		var resolver = new ObjectVariableResolver<IForgeEntity>("missing");

		resolver.Resolve(new GraphContext()).Should().BeNull();
	}
}
