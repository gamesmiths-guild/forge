// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Tags;

public class TagQueryTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = fixture.TagsManager;

	[Theory]
	[Trait("Mathes", "True")]
	[InlineData((object)new string[] { "color.red" })]
	[InlineData((object)new string[] { "color.blue" })]
	[InlineData((object)new string[] { "color.red", "item.consumable.potion.health" })]
	[InlineData((object)new string[] { "color.blue", "item.consumable.potion.mana" })]
	public void Container_matches_built_query(string[] tagKeys)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(tagKeys);

		var query = new TagQuery();
		query.Build(new TagQueryExpression(_tagsManager)
			.AllExpressionsMatch()
				.AddExpression(new TagQueryExpression(_tagsManager)
					.AnyTagsMatch()
						.AddTag("color.red")
						.AddTag("color.blue"))
				.AddExpression(new TagQueryExpression(_tagsManager)
					.NoExpressionsMatch()
						.AddExpression(new TagQueryExpression(_tagsManager)
							.AllTagsMatch()
								.AddTag("color.red")
								.AddTag("color.blue"))
						.AddExpression(new TagQueryExpression(_tagsManager)
							.AnyTagsMatch()
								.AddTag("color.green"))));

		query.Matches(tagContainer).Should().BeTrue();
	}

	[Theory]
	[Trait("Mathes", "False")]
	[InlineData((object)new string[] { "color.red", "color.blue" })]
	[InlineData((object)new string[] { "color.red", "color.green" })]
	[InlineData((object)new string[] { "color.red", "color.blue", "item.consumable.potion.stamina" })]
	[InlineData((object)new string[] { "color.green", "item.consumable.potion.stamina" })]
	[InlineData((object)new string[] { "enemy.beast.boar", "item.equipment.weapon.sword" })]
	public void Container_does_not_match_built_query(string[] tagKeys)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(tagKeys);

		var query = new TagQuery();
		query.Build(new TagQueryExpression(_tagsManager)
			.AllExpressionsMatch()
				.AddExpression(new TagQueryExpression(_tagsManager)
					.AnyTagsMatch()
						.AddTag("color.red")
						.AddTag("color.blue"))
				.AddExpression(new TagQueryExpression(_tagsManager)
					.NoExpressionsMatch()
						.AddExpression(new TagQueryExpression(_tagsManager)
							.AllTagsMatch()
								.AddTag("color.red")
								.AddTag("color.blue"))
						.AddExpression(new TagQueryExpression(_tagsManager)
							.AnyTagsMatch()
								.AddTag("color.green"))));

		query.Matches(tagContainer).Should().BeFalse();
	}

	[Theory]
	[Trait("MakeQueryMatchAnyTags", "True")]
	[InlineData(
		new string[] { "color.red", "color.blue" },
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		new string[] { "color.red", "color.green" },
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		new string[] { "color.red", "color.blue", "item.consumable.potion.stamina" },
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		new string[] { "color", "item.consumable.potion.stamina" },
		new string[] { "color.green", "item.equipment.weapon.sword" })]
	[InlineData(
		new string[] { "enemy", "item" },
		new string[] { "color.red", "enemy.beast.boar" })]
	public void Make_query_match_any_tags_matches(string[] tagKeys, string[] validationTagKeys)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(tagKeys);

		var query = TagQuery.MakeQueryMatchAnyTags(tagContainer);

		TagContainer validationContainer = _tagsManager.RequestTagContainer(validationTagKeys);

		query.Matches(validationContainer).Should().BeTrue();
	}

	[Theory]
	[Trait("MakeQueryMatchAnyTags", "False")]
	[InlineData(
		new string[] { "color.red", "color.blue" },
		new string[] { "color.green", "color.dark.blue" })]
	[InlineData(
		new string[] { "color.green" },
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		new string[] { "color.red", "color.blue", "item.consumable.potion.stamina" },
		new string[] { "enemy.humanoid", "item.equipment" })]
	[InlineData(
		new string[] { "color.green", "item.consumable.potion.stamina" },
		new string[] { "color.blue", "item.consumable.potion.mana" })]
	[InlineData(
		new string[] { "enemy.beast.boar", "item.equipment.weapon.sword" },
		new string[] { "color.red", "enemy.beast.wolf" })]
	public void Make_query_match_any_tags_does_not_match(string[] tagKeys, string[] validationTagKeys)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(tagKeys);

		var query = TagQuery.MakeQueryMatchAnyTags(tagContainer);

		TagContainer validationContainer = _tagsManager.RequestTagContainer(validationTagKeys);

		query.Matches(validationContainer).Should().BeFalse();
	}

	[Theory]
	[Trait("MakeQueryMatchAnyTagsExact", "True")]
	[InlineData(
		new string[] { "color.red", "color.blue" },
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		new string[] { "color.red", "color.green" },
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		new string[] { "color.red", "color.blue", "item.consumable.potion.stamina" },
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		new string[] { "color.green", "item.consumable.potion.stamina" },
		new string[] { "item.equipment.weapon.sword", "item.consumable.potion.stamina" })]
	[InlineData(
		new string[] { "enemy.beast.boar", "item.equipment.weapon.sword" },
		new string[] { "color.red", "enemy.beast.boar" })]
	public void Make_query_match_any_tags_exact_matches(string[] tagKeys, string[] validationTagKeys)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(tagKeys);

		var query = TagQuery.MakeQueryMatchAnyTagsExact(tagContainer);

		TagContainer validationContainer = _tagsManager.RequestTagContainer(validationTagKeys);

		query.Matches(validationContainer).Should().BeTrue();
	}

	[Theory]
	[Trait("MakeQueryMatchAnyTagsExact", "False")]
	[InlineData(
		new string[] { "color.red", "color.blue" },
		new string[] { "color", "color.dark.blue" })]
	[InlineData(
		new string[] { "color.green" },
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		new string[] { "color.red", "color.blue", "item.consumable.potion.stamina" },
		new string[] { "enemy.humanoid", "item.equipment" })]
	[InlineData(
		new string[] { "color.green", "item.consumable.potion.stamina" },
		new string[] { "color.blue", "item.consumable.potion" })]
	[InlineData(
		new string[] { "enemy.beast.boar", "item.equipment.weapon.sword" },
		new string[] { "color.red", "enemy.beast.wolf" })]
	public void Make_query_match_any_tags_exact_does_not_match(string[] tagKeys, string[] validationTagKeys)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(tagKeys);

		var query = TagQuery.MakeQueryMatchAnyTagsExact(tagContainer);

		TagContainer validationContainer = _tagsManager.RequestTagContainer(validationTagKeys);

		query.Matches(validationContainer).Should().BeFalse();
	}

	[Theory]
	[Trait("MakeQueryMatchAllTags", "True")]
	[InlineData(
		new string[] { "color.red", "color.blue" },
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		new string[] { "color.red", "color.green" },
		new string[] { "color.red", "color.green", "color.blue" })]
	[InlineData(
		new string[] { "color", "item" },
		new string[] { "color.green", "item.equipment.weapon.sword" })]
	[InlineData(
		new string[] { "enemy", "item", "color" },
		new string[] { "color.red", "enemy.beast.boar", "item.consumable.food.apple" })]
	public void Make_query_match_all_tags_matches(string[] tagKeys, string[] validationTagKeys)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(tagKeys);

		var query = TagQuery.MakeQueryMatchAllTags(tagContainer);

		TagContainer validationContainer = _tagsManager.RequestTagContainer(validationTagKeys);

		query.Matches(validationContainer).Should().BeTrue();
	}

	[Theory]
	[Trait("MakeQueryMatchAllTags", "False")]
	[InlineData(
		new string[] { "color.red", "color.blue" },
		new string[] { "color.green", "color.dark.blue" })]
	[InlineData(
		new string[] { "color.green" },
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		new string[] { "color.red", "color.blue", "item.consumable.potion.stamina" },
		new string[] { "enemy.humanoid", "item.equipment" })]
	[InlineData(
		new string[] { "color.green", "item.consumable.potion.stamina" },
		new string[] { "color.blue", "item.consumable.potion.mana" })]
	[InlineData(
		new string[] { "enemy.beast.boar", "item.equipment.weapon.sword" },
		new string[] { "color.red", "enemy.beast.wolf" })]
	public void Make_query_match_all_tags_does_not_match(string[] tagKeys, string[] validationTagKeys)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(tagKeys);

		var query = TagQuery.MakeQueryMatchAllTags(tagContainer);

		TagContainer validationContainer = _tagsManager.RequestTagContainer(validationTagKeys);

		query.Matches(validationContainer).Should().BeFalse();
	}

	[Theory]
	[Trait("MakeQueryMatchAllTagsExact", "True")]
	[InlineData(
		new string[] { "color.red", "color.blue" },
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		new string[] { "color.red", "color.green" },
		new string[] { "color.red", "color.green", "color.blue" })]
	[InlineData(
		new string[] { "color", "item" },
		new string[] { "color.green", "item.equipment.weapon.sword", "color", "item" })]
	public void Make_query_match_all_tags_exact_matches(string[] tagKeys, string[] validationTagKeys)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(tagKeys);

		var query = TagQuery.MakeQueryMatchAllTagsExact(tagContainer);

		TagContainer validationContainer = _tagsManager.RequestTagContainer(validationTagKeys);

		query.Matches(validationContainer).Should().BeTrue();
	}

	[Theory]
	[Trait("MakeQueryMatchAllTagsExact", "False")]
	[InlineData(
		new string[] { "color.red", "color.blue" },
		new string[] { "color.green", "color.dark.blue" })]
	[InlineData(
		new string[] { "color.green" },
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		new string[] { "color.red", "color.blue", "item.consumable.potion.stamina" },
		new string[] { "enemy.humanoid", "item.equipment" })]
	[InlineData(
		new string[] { "color.green", "item.consumable.potion.stamina" },
		new string[] { "color.blue", "item.consumable.potion.mana" })]
	[InlineData(
		new string[] { "enemy", "item", "color" },
		new string[] { "color.red", "enemy.beast.boar", "item.consumable.food.apple" })]
	public void Make_query_match_all_tags_exact_does_not_match(string[] tagKeys, string[] validationTagKeys)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(tagKeys);

		var query = TagQuery.MakeQueryMatchAllTagsExact(tagContainer);

		TagContainer validationContainer = _tagsManager.RequestTagContainer(validationTagKeys);

		query.Matches(validationContainer).Should().BeFalse();
	}

	[Theory]
	[Trait("MakeQueryMatchNoTags", "True")]
	[InlineData(
		new string[] { "color.red", "color.blue" },
		new string[] { "color", "color.dark.blue" })]
	[InlineData(
		new string[] { "color.red", "color.green" },
		new string[] { "color.blue" })]
	[InlineData(
		new string[] { "color.green", "item.equipment.weapon.sword" },
		new string[] { "color", "item" })]
	[InlineData(
		new string[] { "enemy.humanoid", "item.equipment", "color.dark" },
		new string[] { "color.red", "enemy.beast.boar", "item.consumable.food.apple" })]
	public void Make_query_match_no_tags_matches(string[] tagKeys, string[] validationTagKeys)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(tagKeys);

		var query = TagQuery.MakeQueryMatchNoTags(tagContainer);

		TagContainer validationContainer = _tagsManager.RequestTagContainer(validationTagKeys);

		query.Matches(validationContainer).Should().BeTrue();
	}

	[Theory]
	[Trait("MakeQueryMatchNoTags", "False")]
	[InlineData(
		new string[] { "color.red", "color.blue" },
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		new string[] { "color.red", "color.green" },
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		new string[] { "color.red", "color.blue", "item.consumable.potion.stamina" },
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		new string[] { "color", "item.consumable.potion.stamina" },
		new string[] { "color.green", "item.equipment.weapon.sword" })]
	[InlineData(
		new string[] { "enemy", "item" },
		new string[] { "color.red", "enemy.beast.boar" })]
	public void Make_query_match_no_tags_does_not_match(string[] tagKeys, string[] validationTagKeys)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(tagKeys);

		var query = TagQuery.MakeQueryMatchNoTags(tagContainer);

		TagContainer validationContainer = _tagsManager.RequestTagContainer(validationTagKeys);

		query.Matches(validationContainer).Should().BeFalse();
	}

	[Theory]
	[Trait("MakeQueryMatchNoTagsExact", "True")]
	[InlineData(
		new string[] { "color.red", "color.blue" },
		new string[] { "color", "color.dark.blue" })]
	[InlineData(
		new string[] { "color.red", "color.green" },
		new string[] { "color.blue" })]
	[InlineData(
		new string[] { "color", "item" },
		new string[] { "color.green", "item.equipment.weapon.sword" })]
	[InlineData(
		new string[] { "enemy", "item", "color" },
		new string[] { "color.red", "enemy.beast.boar", "item.consumable.food.apple" })]
	public void Make_query_match_no_tags_exact_matches(string[] tagKeys, string[] validationTagKeys)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(tagKeys);

		var query = TagQuery.MakeQueryMatchNoTagsExact(tagContainer);

		TagContainer validationContainer = _tagsManager.RequestTagContainer(validationTagKeys);

		query.Matches(validationContainer).Should().BeTrue();
	}

	[Theory]
	[Trait("MakeQueryMatchNoTagsExact", "False")]
	[InlineData(
		new string[] { "color.red", "color.blue" },
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		new string[] { "color.red", "color.green" },
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		new string[] { "color.red", "color.blue", "item.consumable.potion.stamina" },
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		new string[] { "color.green", "item.consumable.potion.stamina" },
		new string[] { "item.equipment.weapon.sword", "item.consumable.potion.stamina" })]
	[InlineData(
		new string[] { "enemy.beast.boar", "item.equipment.weapon.sword" },
		new string[] { "color.red", "enemy.beast.boar" })]
	public void Make_query_match_no_tags_exact_does_not_match(string[] tagKeys, string[] validationTagKeys)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(tagKeys);

		var query = TagQuery.MakeQueryMatchNoTagsExact(tagContainer);

		TagContainer validationContainer = _tagsManager.RequestTagContainer(validationTagKeys);

		query.Matches(validationContainer).Should().BeFalse();
	}

	[Theory]
	[Trait("MakeQueryMatchTag", "True")]
	[InlineData(
		"color.red",
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		"color",
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		"item",
		new string[] { "color.red", "color.blue", "item.consumable.potion.stamina" })]
	[InlineData(
		"enemy.beast",
		new string[] { "color.red", "enemy.beast.boar" })]
	public void Make_query_match_tag_matches(string tagKey, string[] validationTagKeys)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);

		var query = TagQuery.MakeQueryMatchTag(tag);

		TagContainer validationContainer = _tagsManager.RequestTagContainer(validationTagKeys);

		query.Matches(validationContainer).Should().BeTrue();
	}

	[Theory]
	[Trait("MakeQueryMatchTag", "False")]
	[InlineData(
		"color.red",
		new string[] { "color.green", "color.dark.blue" })]
	[InlineData(
		"color.green",
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		"color",
		new string[] { "enemy.humanoid", "item.equipment" })]
	[InlineData(
		"item.consumable.potion.stamina",
		new string[] { "color.blue", "item.consumable.potion.mana" })]
	[InlineData(
		"enemy.beast.boar",
		new string[] { "color.red", "enemy.beast.wolf" })]
	public void Make_query_match_tag_does_not_match(string tagKey, string[] validationTagKeys)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);

		var query = TagQuery.MakeQueryMatchTag(tag);

		TagContainer validationContainer = _tagsManager.RequestTagContainer(validationTagKeys);

		query.Matches(validationContainer).Should().BeFalse();
	}

	[Theory]
	[Trait("MakeQueryMatchTagExact", "True")]
	[InlineData(
		"color.red",
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		"color.blue",
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		"item.consumable.potion.stamina",
		new string[] { "color.red", "color.blue", "item.consumable.potion.stamina" })]
	[InlineData(
		"enemy.beast.boar",
		new string[] { "color.red", "enemy.beast.boar", "item.consumable.potion.stamina" })]
	public void Make_query_match_tag_exact_matches(string tagKey, string[] validationTagKeys)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);

		var query = TagQuery.MakeQueryMatchTagExact(tag);

		TagContainer validationContainer = _tagsManager.RequestTagContainer(validationTagKeys);

		query.Matches(validationContainer).Should().BeTrue();
	}

	[Theory]
	[Trait("MakeQueryMatchTagExact", "False")]
	[InlineData(
		"color.red",
		new string[] { "color.green", "color.dark.blue" })]
	[InlineData(
		"color.green",
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		"color",
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		"item",
		new string[] { "color.red", "color.blue", "item.consumable.potion.stamina" })]
	[InlineData(
		"enemy.beast",
		new string[] { "color.red", "enemy.beast.boar" })]
	public void Make_query_match_tag_exact_does_not_match(string tagKey, string[] validationTagKeys)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);

		var query = TagQuery.MakeQueryMatchTagExact(tag);

		TagContainer validationContainer = _tagsManager.RequestTagContainer(validationTagKeys);

		query.Matches(validationContainer).Should().BeFalse();
	}

	[Theory]
	[Trait("ReplaceTagsFast", "Valid")]
	[InlineData((object)new string[]
		{
			"color.red",
			"color.blue",
			"color.green",
		})]
	[InlineData((object)new string[]
		{
			"color.blue",
			"color.green",
			"color.red",
		})]
	[InlineData((object)new string[]
		{
			"item.consumable.potion.health",
			"item.consumable.potion.mana",
			"item.consumable.potion.stamina",
		})]
	public void Replace_tags_fast_replaces_tags_correctly(string[] tagKeys)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(tagKeys);

		var query = new TagQuery();
		query.Build(new TagQueryExpression(_tagsManager)
			.AllExpressionsMatch()
				.AddExpression(new TagQueryExpression(_tagsManager)
					.AnyTagsMatch()
						.AddTag("color.red")
						.AddTag("color.blue"))
				.AddExpression(new TagQueryExpression(_tagsManager)
					.NoExpressionsMatch()
						.AddExpression(new TagQueryExpression(_tagsManager)
							.AllTagsMatch()
								.AddTag("color.red")
								.AddTag("color.blue"))
						.AddExpression(new TagQueryExpression(_tagsManager)
							.AnyTagsMatch()
								.AddTag("color.green"))));

		query.ReplaceTagsFast(tagContainer);

		TagContainer validationContainer1 = _tagsManager.RequestTagContainer([tagKeys[0]]);
		TagContainer validationContainer2 = _tagsManager.RequestTagContainer([tagKeys[1]]);
		TagContainer validationContainer3 = _tagsManager.RequestTagContainer([tagKeys[2]]);
		TagContainer validationContainer4 = _tagsManager.RequestTagContainer([tagKeys[0], tagKeys[1]]);

		query.Matches(validationContainer1).Should().BeTrue();
		query.Matches(validationContainer2).Should().BeTrue();
		query.Matches(validationContainer3).Should().BeFalse();
		query.Matches(validationContainer4).Should().BeFalse();
	}

	[Theory]
	[Trait("ReplaceTagsFast", "Valid")]
	[InlineData("color.red")]
	[InlineData("item.consumable.potion.health")]
	public void Replace_tag_fast_replaces_tags_correctly(string tagKey)
	{
		var query = TagQuery.MakeQueryMatchTag(Tag.RequestTag(_tagsManager, "tag"));

		var tag = Tag.RequestTag(_tagsManager, tagKey);

		query.ReplaceTagFast(tag);

		TagContainer validationContainer = _tagsManager.RequestTagContainer([tagKey]);

		query.Matches(validationContainer).Should().BeTrue();
	}
}
