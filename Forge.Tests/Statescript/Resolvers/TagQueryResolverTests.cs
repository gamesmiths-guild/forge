// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.ResolverTestContextFactory;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class TagQueryResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "TagQuery")]
	public void Tag_query_resolver_returns_true_when_entity_matches_single_tag_query()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var tag = Tag.RequestTag(_tagsManager, "enemy.undead.zombie");
		var resolver = new TagQueryResolver(tag);

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "TagQuery")]
	public void Tag_query_resolver_returns_false_when_entity_does_not_match_query()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var query = TagQuery.MakeQueryMatchTag(Tag.RequestTag(_tagsManager, "enemy.beast.wolf"));
		var resolver = new TagQueryResolver(query);

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "TagQuery")]
	public void Tag_query_resolver_returns_false_when_no_activation_context()
	{
		var query = TagQuery.MakeQueryMatchTag(Tag.RequestTag(_tagsManager, "enemy.undead.zombie"));
		var resolver = new TagQueryResolver(query);

		var context = new GraphContext();

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "TagQuery")]
	public void Tag_query_resolver_value_type_is_bool()
	{
		var query = TagQuery.MakeQueryMatchTag(Tag.RequestTag(_tagsManager, "enemy.undead.zombie"));
		var resolver = new TagQueryResolver(query);

		resolver.ValueType.Should().Be(typeof(bool));
	}

	[Fact]
	[Trait("Resolver", "TagQuery")]
	public void Tag_query_resolver_matches_parent_tag_through_query()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var query = TagQuery.MakeQueryMatchTag(Tag.RequestTag(_tagsManager, "enemy.undead"));
		var resolver = new TagQueryResolver(query);

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "TagQuery")]
	public void Tag_query_resolver_can_target_base_tags_only()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		ApplyModifierTags(entity, "item.equipment.weapon.axe");
		var resolver = new TagQueryResolver(
			Tag.RequestTag(_tagsManager, "enemy.undead"),
			TagQuerySource.BaseTags);

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "TagQuery")]
	public void Tag_query_resolver_does_not_match_modifier_only_tag_when_targeting_base_tags()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		ApplyModifierTags(entity, "item.equipment.weapon.axe");
		var resolver = new TagQueryResolver(
			Tag.RequestTag(_tagsManager, "item.equipment.weapon.axe"),
			TagQuerySource.BaseTags);

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "TagQuery")]
	public void Tag_query_resolver_can_target_modifier_tags_only()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		ApplyModifierTags(entity, "item.equipment.weapon.axe");
		var resolver = new TagQueryResolver(
			Tag.RequestTag(_tagsManager, "item.equipment.weapon.axe"),
			TagQuerySource.ModifierTags);

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "TagQuery")]
	public void Tag_query_resolver_does_not_match_base_only_tag_when_targeting_modifier_tags()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		ApplyModifierTags(entity, "item.equipment.weapon.axe");
		var resolver = new TagQueryResolver(
			Tag.RequestTag(_tagsManager, "enemy.undead"),
			TagQuerySource.ModifierTags);

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "TagQuery")]
	public void Tag_query_resolver_matches_modifier_only_tag_with_default_combined_source()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		ApplyModifierTags(entity, "item.equipment.weapon.axe");
		var resolver = new TagQueryResolver(Tag.RequestTag(_tagsManager, "item.equipment.weapon.axe"));

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "TagQuery")]
	public void Tag_query_resolver_can_read_selected_entity_from_variable_without_activation_context()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		var resolver = new TagQueryResolver(
			Tag.RequestTag(_tagsManager, "item.equipment.weapon.axe"),
			new EntityVariableResolver("selectedEntity"),
			TagQuerySource.ModifierTags);

		ApplyModifierTags(entity, "item.equipment.weapon.axe");
		context.GraphVariables.DefineReferenceVariable<IForgeEntity>("selectedEntity", entity);

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "TagQuery")]
	public void Tag_query_resolver_can_read_target_entity()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);
		var node = new ResolvePropertyNode(
			new TagQueryResolver(
				Tag.RequestTag(_tagsManager, "item.equipment.weapon.axe"),
				new TargetEntityResolver(),
				TagQuerySource.ModifierTags));

		ApplyModifierTags(target, "item.equipment.weapon.axe");

		ExecuteAbilityGraph(owner, node, target, source: null);

		node.LastResolvedValue.AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "TagQuery")]
	public void Tag_query_resolver_supports_nested_query_expressions()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		TagQueryExpression queryExpression = new TagQueryExpression(_tagsManager)
			.AllExpressionsMatch()
			.AddExpression(
				new TagQueryExpression(_tagsManager)
					.AnyTagsMatch()
					.AddTag("enemy.beast")
					.AddTag("enemy.undead"))
			.AddExpression(
				new TagQueryExpression(_tagsManager)
					.NoTagsMatch()
					.AddTag("enemy.beast.wolf"));
		var resolver = new TagQueryResolver(queryExpression);

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "TagQuery")]
	public void Tag_query_resolver_returns_false_when_nested_query_expression_fails()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		TagQueryExpression queryExpression = new TagQueryExpression(_tagsManager)
			.AllExpressionsMatch()
			.AddExpression(
				new TagQueryExpression(_tagsManager)
					.AnyTagsMatch()
					.AddTag("enemy.undead"))
			.AddExpression(
				new TagQueryExpression(_tagsManager)
					.NoTagsMatch()
					.AddTag("enemy.undead"));
		var resolver = new TagQueryResolver(queryExpression);

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeFalse();
	}

	private void ApplyModifierTags(TestEntity entity, params string[] tagKeys)
	{
		var effectData = new EffectData(
			"TagQueryResolverTestModifierTags",
			new DurationData(DurationType.Infinite),
			effectComponents: [new ModifierTagsEffectComponent(_tagsManager.RequestTagContainer(tagKeys))]);

		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		_ = entity.EffectsManager.ApplyEffect(effect);
	}
}
