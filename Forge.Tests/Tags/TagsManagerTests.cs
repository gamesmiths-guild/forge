// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Tags;

public class TagsManagerTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	public static readonly string[][] SharedTagSets = [
		["tag"],
		["tag", "simple", "simple.tag", "other.tag"],
		["color.red", "color.green", "color.blue", "color.dark.red", "color.dark.green", "color.dark.blue"],
		[
			"tag", "simple", "simple.tag", "other.tag",
			"enemy.undead.zombie", "enemy.undead.skeleton", "enemy.undead.ghoul", "enemy.undead.vampire",
			"enemy.beast.wolf", "enemy.beast.wolf.gray", "enemy.beast.boar",
			"enemy.humanoid.goblin", "enemy.humanoid.orc",
			"item.consumable.potion.health", "item.consumable.potion.mana", "item.consumable.potion.stamina",
			"item.consumable.food.apple", "item.consumable.food.bread",
			"item.equipment.weapon.sword", "item.equipment.weapon.dagger", "item.equipment.weapon.axe",
			"color.red", "color.green", "color.blue", "color.dark.red", "color.dark.green", "color.dark.blue"
		],
		[]
	];

	private readonly TagsManager _tagsManagerWithAllTestTags = fixture.TagsManager;

	public static TheoryData<string[]> TagsData => [.. SharedTagSets];

	public static TheoryData<string[], int> TagsWithNodeCountData =>
		new()
		{
			{ SharedTagSets[0], 1 },
			{ SharedTagSets[1], 5 },
			{ SharedTagSets[2], 8 },
			{ SharedTagSets[3], 40 },
			{ SharedTagSets[4], 0 },
		};

	[Theory]
	[Trait("Initialization", "Valid tags")]
	[MemberData(nameof(TagsWithNodeCountData))]
	public void Tags_manager_initializes_with_correct_values(string[] managerTags, int nodeCount)
	{
		var tagsManager = new TagsManager(managerTags);

		tagsManager.NodesCount.Should().Be(nodeCount);
		tagsManager.InvalidTagNetIndex.Should().Be((ushort)(nodeCount + 1));
	}

	[Theory]
	[Trait("Destroy", "Correctly")]
	[MemberData(nameof(TagsData))]
	public void Destroyed_tags_manager_is_empty(string[] managerTags)
	{
		var tagsManager = new TagsManager(managerTags);

		tagsManager.DestroyTagTree();

		tagsManager.NodesCount.Should().Be(0);
		tagsManager.InvalidTagNetIndex.Should().Be(1);
	}

	[Theory]
	[Trait("RequestAllTags", "Explicit tags only")]
	[MemberData(nameof(TagsData))]
	public void Request_all_tags_with_explicit_tags_only_are_valid(string[] managerTags)
	{
		var tagsManager = new TagsManager(managerTags);

		TagContainer allTagsContainer = tagsManager.RequestAllTags(true);

		TagContainer validationContainer = ConstructContainer(tagsManager, managerTags);

		allTagsContainer.Should().BeEquivalentTo(validationContainer);
	}

	[Theory]
	[Trait("RequestAllTags", "All tags")]
	[MemberData(nameof(TagsData))]
	public void Request_all_tags_with_all_tags_are_valid(string[] managerTags)
	{
		var tagsManager = new TagsManager(managerTags);

		TagContainer allTagsContainer = tagsManager.RequestAllTags(false);

		TagContainer validationContainer = ConstructContainer(tagsManager, managerTags, false);

		allTagsContainer.Should().BeEquivalentTo(validationContainer);
	}

	[Theory]
	[Trait("GetNumberOfTagNodes", "Valid tags")]
	[InlineData("tag", 1)]
	[InlineData("simple.tag", 2)]
	[InlineData("simple", 1)]
	[InlineData("color.dark.green", 3)]
	[InlineData("color.dark", 2)]
	[InlineData("item.consumable.food.bread", 4)]
	public void Get_number_of_tag_nodes_gives_the_correct_value_of_tag_nodes(string tagKey, int tagNodesCount)
	{
		TagsManager tagsManager = _tagsManagerWithAllTestTags;

		var tag = Tag.RequestTag(tagsManager, tagKey);

		tagsManager.GetNumberOfTagNodes(tag).Should().Be(tagNodesCount);
	}

	[Fact]
	[Trait("GetNumberOfTagNodes", "Invalid tag")]
	public void Get_number_of_tag_nodes_for_invalid_tag_is_zero()
	{
		TagsManager tagsManager = _tagsManagerWithAllTestTags;

		tagsManager.GetNumberOfTagNodes(Tag.Empty).Should().Be(0);
	}

	[Theory]
	[Trait("SplitTagKey", "Valid tags")]
	[InlineData("tag", new string[] { "tag" })]
	[InlineData("simple.tag", new string[] { "simple", "tag" })]
	[InlineData("simple", new string[] { "simple" })]
	[InlineData("color.dark.green", new string[] { "color", "dark", "green" })]
	[InlineData("color.dark", new string[] { "color", "dark" })]
	[InlineData("item.consumable.food.bread", new string[] { "item", "consumable", "food", "bread" })]
	public void Split_tag_key_splits_key_correctly(string tagKey, string[] tagNodesKeys)
	{
		TagsManager tagsManager = _tagsManagerWithAllTestTags;

		var tag = Tag.RequestTag(tagsManager, tagKey);

		tagsManager.SplitTagKey(tag).Should().BeEquivalentTo(tagNodesKeys);
	}

	[Fact]
	[Trait("SplitTagKey", "Invalid tag")]
	public void Split_tag_key_for_invalid_tag_is_empty()
	{
		TagsManager tagsManager = _tagsManagerWithAllTestTags;

		tagsManager.SplitTagKey(Tag.Empty).Should().BeEmpty();
	}

	[Theory]
	[Trait("RequestTagContainer", "Valid tags")]
	[InlineData((object)new string[] { "tag" })]
	[InlineData((object)new string[] { "tag", "simple.tag", "other.tag" })]
	[InlineData((object)new string[] { "simple" })]
	[InlineData((object)new string[] { "color.red", "color.green", "color.dark.blue" })]
	[InlineData((object)new string[] { "enemy.humanoid.orc", "item.consumable.food.bread" })]
	public void Request_tag_container_gives_a_valid_container(string[] tagKeys)
	{
		TagsManager tagsManager = _tagsManagerWithAllTestTags;

		TagContainer requestedContainer = tagsManager.RequestTagContainer(tagKeys);

		TagContainer validationContainer = ConstructContainer(tagsManager, tagKeys);

		requestedContainer.Should().BeEquivalentTo(validationContainer);
	}

	[Theory]
	[Trait("RequestTagContainer", "Invalid tag")]
	[InlineData((object)new string[] { "invalid" })]
	[InlineData((object)new string[] { "invalid.tag", "simple.tag", "other.tag" })]
	[InlineData((object)new string[] { "tag", "", "other.tag" })]
	public void Request_tag_container_for_invalid_tags_throw_exception(string[] tagKeys)
	{
		TagsManager tagsManager = _tagsManagerWithAllTestTags;

		Action act = () =>
		{
			tagsManager.RequestTagContainer(tagKeys);
		};

		act.Should().Throw<TagNotRegisteredException>().WithMessage($"{nameof(Tag)} for StringKey*");
	}

	[Theory]
	[Trait("RequestTagContainer", "Invalid tag")]
	[InlineData(new string[] { "invalid" }, new string[] { })]
	[InlineData(new string[] { "invalid.tag", "simple.tag", "other.tag" }, new string[] { "simple.tag", "other.tag" })]
	[InlineData(new string[] { "tag", "", "other.tag" }, new string[] { "tag", "other.tag" })]
	public void Request_tag_container_for_invalid_tags_without_errors_skips_invalid_tags(
		string[] tagKeys,
		string[] finalKeys)
	{
		TagsManager tagsManager = _tagsManagerWithAllTestTags;

		TagContainer requestedContainer = tagsManager.RequestTagContainer(tagKeys, false);

		TagContainer validationContainer = ConstructContainer(tagsManager, finalKeys);

		requestedContainer.Should().BeEquivalentTo(validationContainer);
	}

	[Theory]
	[Trait("RequestTagParents", "Valid tags")]
	[InlineData("simple.tag", new string[] { "simple", "simple.tag" })]
	[InlineData("color.dark.green", new string[] { "color", "color.dark", "color.dark.green" })]
	public void Requested_tag_parents_are_correct(string tagKey, string[] parentTags)
	{
		TagsManager tagsManager = _tagsManagerWithAllTestTags;

		var tag = Tag.RequestTag(tagsManager, tagKey);

		TagContainer parentTagsContainer = tagsManager.RequestTagParents(tag);

		TagContainer validationContainer = ConstructContainer(tagsManager, parentTags);

		parentTagsContainer.Should().BeEquivalentTo(validationContainer);
	}

	[Fact]
	[Trait("RequestTagParents", "Invalid tag")]
	public void Requested_tag_parents_for_invalid_tag_is_an_empty_container()
	{
		TagsManager tagsManager = _tagsManagerWithAllTestTags;

		TagContainer parentTagsContainer = tagsManager.RequestTagParents(Tag.Empty);

		parentTagsContainer.Should().BeEmpty();
	}

	[Theory]
	[Trait("RequestTagChildren", "Valid tags")]
	[InlineData("simple", new string[] { "simple.tag" })]
	[InlineData("tag", new string[] { })]
	[InlineData("color.dark", new string[] { "color.dark.red", "color.dark.green", "color.dark.blue" })]
	public void Requested_tag_children_are_correct(string tagKey, string[] parentTags)
	{
		TagsManager tagsManager = _tagsManagerWithAllTestTags;

		var tag = Tag.RequestTag(tagsManager, tagKey);

		TagContainer childTagsContainer = tagsManager.RequestTagChildren(tag);

		TagContainer validationContainer = ConstructContainer(tagsManager, parentTags);

		childTagsContainer.Should().BeEquivalentTo(validationContainer);
	}

	[Fact]
	[Trait("RequestTagChildren", "Invalid tag")]
	public void Requested_tag_children_for_invalid_tag_is_an_empty_container()
	{
		TagsManager tagsManager = _tagsManagerWithAllTestTags;

		TagContainer parentTagsContainer = tagsManager.RequestTagChildren(Tag.Empty);

		parentTagsContainer.Should().BeEmpty();
	}

	[Theory]
	[Trait("ToString", "Valid tags")]
	[InlineData(
		new string[]
		{
			"tag",
			"simple",
			"simple.tag",
			"other.tag",
		},
#pragma warning disable SA1118 // Parameter should not span multiple lines
		"other\r\n"
		+ "other.tag\r\n"
		+ "simple\r\n"
		+ "simple.tag\r\n"
		+ "tag\r\n")]
	[InlineData(
		new string[]
		{
			"color.red",
			"color.green",
			"color.blue",
			"color.dark.red",
			"color.dark.green",
			"color.dark.blue",
		},
		"color\r\n"
		+ "color.blue\r\n"
		+ "color.dark\r\n"
		+ "color.dark.blue\r\n"
		+ "color.dark.green\r\n"
		+ "color.dark.red\r\n"
		+ "color.green\r\n"
		+ "color.red\r\n")]
	[InlineData(new string[] { }, "")]
#pragma warning restore SA1118 // Parameter should not span multiple lines
	public void To_string(string[] managerTags, string output)
	{
		var tagsManager = new TagsManager(managerTags);

		tagsManager.ToString().Should().Be(output);
	}

	private static TagContainer ConstructContainer(
		TagsManager tagsManager,
		string[] tagKeys,
		bool explicitTagsOnly = true)
	{
		var tagSet = new HashSet<Tag>();

		foreach (var tagKey in tagKeys)
		{
			var tag = Tag.RequestTag(tagsManager, tagKey);

			if (explicitTagsOnly)
			{
				tagSet.Add(tag);
			}
			else
			{
				TagContainer? parentTags = tag.GetTagParents();

				if (parentTags is not null)
				{
					foreach (Tag auxTag in parentTags)
					{
						tagSet.Add(auxTag);
					}
				}
			}
		}

		return new TagContainer(tagsManager, tagSet);
	}
}
