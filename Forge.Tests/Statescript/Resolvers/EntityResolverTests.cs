// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.ResolverTestContextFactory;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class EntityResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "Entity")]
	public void Owner_entity_resolver_reads_owner_entity()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		GraphContext context = CreateAbilityGraphContext(owner);

		new OwnerEntityResolver().Resolve(context).Should().BeSameAs(owner);
	}

	[Fact]
	[Trait("Resolver", "Entity")]
	public void Source_entity_resolver_reads_source_entity()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var source = new TestEntity(_tagsManager, _cuesManager);
		GraphContext context = CreateAbilityGraphContext(owner, target: null, source: source);

		new SourceEntityResolver().Resolve(context).Should().BeSameAs(source);
	}

	[Fact]
	[Trait("Resolver", "Entity")]
	public void Target_entity_resolver_reads_target_entity()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);
		var node = new ResolveReferenceResolverNode<IForgeEntity>(new TargetEntityResolver());

		ExecuteAbilityGraph(owner, node, target, source: null);

		node.LastResolvedValue.Should().BeSameAs(target);
	}

	[Fact]
	[Trait("Resolver", "Entity")]
	public void Entity_resolvers_return_null_without_activation_context()
	{
		var context = new GraphContext();

		new OwnerEntityResolver().Resolve(context).Should().BeNull();
		new SourceEntityResolver().Resolve(context).Should().BeNull();
		new TargetEntityResolver().Resolve(context).Should().BeNull();
	}
}
