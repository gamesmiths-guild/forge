// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.ResolverTestContextFactory;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class TagResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "Tag")]
	public void Tag_resolver_returns_true_when_entity_has_tag()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var tag = Tag.RequestTag(_tagsManager, "enemy.undead.zombie");
		var resolver = new TagResolver(tag);

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "Tag")]
	public void Tag_resolver_returns_false_when_entity_does_not_have_tag()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var tag = Tag.RequestTag(_tagsManager, "enemy.beast.wolf");
		var resolver = new TagResolver(tag);

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "Tag")]
	public void Tag_resolver_returns_false_when_no_activation_context()
	{
		var tag = Tag.RequestTag(_tagsManager, "enemy.undead.zombie");
		var resolver = new TagResolver(tag);

		var context = new GraphContext();

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "Tag")]
	public void Tag_resolver_value_type_is_bool()
	{
		var tag = Tag.RequestTag(_tagsManager, "enemy.undead.zombie");
		var resolver = new TagResolver(tag);

		resolver.ValueType.Should().Be(typeof(bool));
	}

	[Fact]
	[Trait("Resolver", "Tag")]
	public void Tag_resolver_matches_parent_tag()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var parentTag = Tag.RequestTag(_tagsManager, "enemy.undead");
		var resolver = new TagResolver(parentTag);

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeTrue();
	}
}
