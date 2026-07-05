// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ElementResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "Element")]
	public void Element_resolver_returns_null_outside_array_iteration()
	{
		var resolver = new ElementResolver<IForgeEntity>();

		resolver.Resolve(new GraphContext()).Should().BeNull();
	}

	[Fact]
	[Trait("Resolver", "Element")]
	public void Element_resolver_reads_each_iterated_element()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var entity2 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1, null!, entity2]);
		var source = new EntityArrayVariableResolver("targets");

		var resolver = new ObjectWhereResolver<IForgeEntity>(
			source,
			new IsValidResolver(new ElementResolver<IForgeEntity>()));

		IForgeEntity[] result = resolver.ResolveArray(context);

		result.Should().Equal(entity1, entity2);
	}
}
