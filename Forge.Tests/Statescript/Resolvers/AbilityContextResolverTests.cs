// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.ResolverTestContextFactory;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class AbilityContextResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "AbilityContext")]
	public void Ability_level_resolver_reads_current_ability_level()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var resolver = new AbilityLevelResolver();

		GraphContext context = CreateAbilityGraphContext(owner, level: 6);

		resolver.Resolve(context).AsInt().Should().Be(6);
	}

	[Fact]
	[Trait("Resolver", "AbilityContext")]
	public void Ability_ownership_resolver_reads_current_owner_and_source()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var source = new TestEntity(_tagsManager, _cuesManager);
		var resolver = new AbilityOwnershipResolver();

		GraphContext context = CreateAbilityGraphContext(owner, target: null, source: source);

		EffectOwnership ownership = resolver.Resolve(context);
		ownership.Owner.Should().BeSameAs(owner);
		ownership.Source.Should().BeSameAs(source);
	}

	[Fact]
	[Trait("Resolver", "AbilityContext")]
	public void Ownership_resolver_composes_effect_ownership_from_nested_entity_resolvers()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var source = new TestEntity(_tagsManager, _cuesManager);
		var resolver = new OwnershipResolver(new AbilityOwnerResolver(), new AbilitySourceResolver());

		GraphContext context = CreateAbilityGraphContext(owner, target: null, source: source);

		EffectOwnership ownership = resolver.Resolve(context);
		ownership.Owner.Should().BeSameAs(owner);
		ownership.Source.Should().BeSameAs(source);
	}

	[Fact]
	[Trait("Resolver", "AbilityContext")]
	public void Ability_context_resolvers_return_defaults_without_activation_context()
	{
		var context = new GraphContext();

		new AbilityLevelResolver().Resolve(context).AsInt().Should().Be(0);
		new AbilityOwnershipResolver().Resolve(context).Should().Be(new EffectOwnership(null, null));
		new OwnershipResolver().Resolve(context).Should().Be(new EffectOwnership(null, null));
	}
}
