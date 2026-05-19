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

public class EntityArrayResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "EntityArray")]
	public void Entity_array_resolver_reads_nested_entity_resolvers()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);
		var source = new TestEntity(_tagsManager, _cuesManager);
		var node = new ResolveReferenceArrayResolverNode<IForgeEntity>(new EntityArrayResolver(
			new OwnerEntityResolver(),
			new TargetEntityResolver(),
			new SourceEntityResolver()));

		ExecuteAbilityGraph(owner, node, target, source);

		node.LastResolvedArray.Should().NotBeNull();
		node.LastResolvedArray.Should().HaveCount(3);
		node.LastResolvedArray![0].Should().BeSameAs(owner);
		node.LastResolvedArray[1].Should().BeSameAs(target);
		node.LastResolvedArray[2].Should().BeSameAs(source);
	}

	[Fact]
	[Trait("Resolver", "EntityArray")]
	public void Entity_array_resolver_returns_null_entries_without_activation_context()
	{
		var resolver = new EntityArrayResolver(
			new OwnerEntityResolver(),
			new TargetEntityResolver(),
			new SourceEntityResolver());

		IForgeEntity?[] result = resolver.ResolveArray(new GraphContext());

		result.Should().BeEquivalentTo(new IForgeEntity?[] { null, null, null });
	}

	[Fact]
	[Trait("Resolver", "EntityArray")]
	public void Entity_array_resolver_throws_for_null_resolver_array()
	{
		IEntityResolver[]? resolvers = null;

#pragma warning disable CA1806
		Action act = () => new EntityArrayResolver(resolvers!);
#pragma warning restore CA1806

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	[Trait("Resolver", "EntityArray")]
	public void Entity_array_resolver_throws_for_null_nested_resolver()
	{
		IEntityResolver[] resolvers =
		[
			new OwnerEntityResolver(),
			null!,
		];

#pragma warning disable CA1806
		Action act = () => new EntityArrayResolver(resolvers);
#pragma warning restore CA1806

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Resolver", "EntityArray")]
	public void Graph_variable_definitions_validate_entity_array_resolver_output_type()
	{
		var graph = new Graph();

		graph.VariableDefinitions.DefineReferenceArrayProperty(
			"entities",
			new EntityArrayResolver(new OwnerEntityResolver()));

		graph.VariableDefinitions.ValidatePropertyType("entities", typeof(IForgeEntity[])).Should().BeTrue();
		graph.VariableDefinitions.ValidatePropertyType("entities", typeof(string[])).Should().BeFalse();
	}
}
