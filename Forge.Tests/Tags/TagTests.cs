// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Tags;

public class TagTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = fixture.TagsManager;

	[Theory]
	[Trait("Request", "Registered tags")]
	[InlineData("simple")]
	[InlineData("Simple.Tag")]
	[InlineData("enemy.undead.skeleton")]
	[InlineData("enemy.beast.wolf")]
	[InlineData("item.equipment.weapon.dagger")]
	[InlineData("COLOR.RED")]
	public void Requested_registered_tags_are_valid(string tagKey)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);

		tag.IsValid.Should().BeTrue();
		tag.TagKey.Should().Be(tagKey);
	}

	[Fact]
	[Trait("Request", "Unregistered tags")]
	public void Requested_unregistered_tags_throws_exception()
	{
		Action act = () =>
		{
			Tag.RequestTag(_tagsManager, "foo.bar");
		};

		act.Should().Throw<TagNotRegisteredException>().WithMessage($"{nameof(Tag)} for StringKey*");
	}

	[Fact]
	[Trait("Request", "Unregistered tag")]
	public void Requested_unregistered_tags_without_error_are_invalid()
	{
		var tag = Tag.RequestTag(_tagsManager, "foo.bar", false);

		tag.IsValid.Should().BeFalse();
	}

	[Fact]
	[Trait("Request", "Empty tag")]
	public void Requested_tag_for_Empty_StringKey_throws_exception()
	{
		Action act = () =>
		{
			Tag.RequestTag(_tagsManager, StringKey.Empty);
		};

		act.Should().Throw<TagNotRegisteredException>().WithMessage($"{nameof(Tag)} for StringKey*");
	}

	[Theory]
	[Trait("Request", "Parent tags")]
	[InlineData("other")]
	[InlineData("enemy")]
	[InlineData("enemy.undead")]
	[InlineData("item.consumable.potion")]
	[InlineData("item.consumable.food")]
	[InlineData("item.equipment")]
	[InlineData("item")]
	[InlineData("color")]
	public void Requested_non_explicitly_registered_parent_tags_are_valid(string tagKey)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);

		tag.IsValid.Should().BeTrue();
	}

	[Theory]
	[Trait("Serialization", "Serialize")]
	[InlineData("other", 35u)]
	[InlineData("other.tag", 36u)]
	[InlineData("enemy.beast.wolf", 11u)]
	[InlineData("enemy.beast.wolf.gray", 12u)]
	[InlineData("item.equipment.weapon.sword", 34u)]
	[InlineData("color", 0u)]
	public void Registered_tag_serializes_successfully_with_valid_net_index(string tagKey, ushort netIndex)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);

		Tag.NetSerialize(_tagsManager, tag, out var tagNetIndex).Should().BeTrue();

		tagNetIndex.Should().Be(netIndex);
	}

	[Fact]
	[Trait("Serialization", "Serialize")]
	public void Unregistered_tag_serializes_successfully_with_InvalidTagNetIndex()
	{
		var tag = Tag.RequestTag(_tagsManager, "foo.bar", false);

		Tag.NetSerialize(_tagsManager, tag, out var tagNetIndex).Should().BeTrue();

		tagNetIndex.Should().Be(_tagsManager.InvalidTagNetIndex);
	}

	[Fact]
	[Trait("Serialization", "Serialize")]
	public void Empty_StringKey_serializes_successfully_with_InvalidTagNetIndex()
	{
		Tag.NetSerialize(_tagsManager, Tag.Empty, out var tagNetIndex).Should().BeTrue();

		tagNetIndex.Should().Be(_tagsManager.InvalidTagNetIndex);
	}

	[Theory]
	[Trait("Serialization", "Deserialize")]
	[InlineData(new byte[] { 35, 0 }, "other")]
	[InlineData(new byte[] { 36, 0 }, "other.tag")]
	[InlineData(new byte[] { 11, 0 }, "enemy.beast.wolf")]
	[InlineData(new byte[] { 12, 0 }, "enemy.beast.wolf.gray")]
	[InlineData(new byte[] { 34, 0 }, "item.equipment.weapon.sword")]
	[InlineData(new byte[] { 0, 0 }, "color")]
	public void Tag_deserializes_successfully_with_propper_net_index(byte[] stream, string tagKey)
	{
		Tag.NetDeserialize(_tagsManager, stream, out Tag tag).Should().BeTrue();

		var tagToCheck = Tag.RequestTag(_tagsManager, tagKey);

		tag.Should().Be(tagToCheck);
	}

	[Fact]
	[Trait("Serialization", "Deserialize")]
	public void InvalidTagNetIndex_deserializes_successfully_as_Empty_Tag()
	{
		Tag.NetDeserialize(
			_tagsManager,
			[(byte)_tagsManager.InvalidTagNetIndex, 0],
			out Tag tag).Should().BeTrue();

		tag.Should().Be(Tag.Empty);
	}

	[Fact]
	[Trait("Serialization", "Deserialize")]
	public void Higher_than_InvalidTagNetIndex_value_deserialization_throws_exception()
	{
		Action act = () =>
		{
			Tag.NetDeserialize(
				_tagsManager,
				[(byte)(_tagsManager.InvalidTagNetIndex + 1), 0],
				out Tag _);
		};

		act.Should().Throw<InvalidTagNetIndexException>().WithMessage("Received invalid tag net index*");
	}

	[Theory]
	[Trait("IsActive", "Correct")]
	[InlineData("Entity.Attributes.Strengh")]
	[InlineData("item.consumable.potion.stamina")]
	[InlineData("color.black")]
	[InlineData("enemy.undead.vampire")]
	public void Correctly_formatted_string_is_a_valid_tag_keys(string tagKey)
	{
		Tag.IsValidKey(tagKey, out _, out _).Should().BeTrue();
	}

	[Theory]
	[Trait("IsActive", "Incorrect")]
	[InlineData(" Entity,Attr ibutes,Strength  ", "Entity_Attr_ibutes_Strength")]
	[InlineData("item.consumable.potion.stamina ", "item.consumable.potion.stamina")]
	[InlineData("color.dark.green.", "color.dark.green")]
	[InlineData(".enemy.humanoid.goblin", "enemy.humanoid.goblin")]
	[InlineData("item equipment weapon axe", "item_equipment_weapon_axe")]
	public void Returns_a_corrected_key_when_passing_an_invalid_one(string tagKey, string fixedKey)
	{
		Tag.IsValidKey(tagKey, out _, out var outFixedString).Should().BeFalse();
		outFixedString.Should().Be(fixedKey);
	}

	[Theory]
	[Trait("SingleTagContainer", "Valid tags")]
	[InlineData("enemy.undead.ghoul")]
	public void SingleTag_container_for_registered_tags_is_valid(string tagKey)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);

		TagContainer? tagContainer = tag.GetSingleTagContainer();

		var tagContainerCheck = new TagContainer(tag);

		tagContainer.Should().BeEquivalentTo(tagContainerCheck);
	}

	[Fact]
	[Trait("SingleTagContainer", "Invalid tags")]
	public void SingleTag_container_for_unregistered_tags_is_null()
	{
		var tag = Tag.RequestTag(_tagsManager, "foo.bar", false);

		TagContainer? tagContainer = tag.GetSingleTagContainer();

		tagContainer.Should().BeNull();
	}

	[Fact]
	[Trait("SingleTagContainer", "Invalid tags")]
	public void SingleTag_containers_for_Empty_tag_is_null()
	{
		Tag tag = Tag.Empty;

		TagContainer? tagContainer = tag.GetSingleTagContainer();

		tagContainer.Should().BeNull();
	}

	[Theory]
	[Trait("RequestDirectParent", "Valid tags")]
	[InlineData("simple.tag", "simple")]
	[InlineData("enemy.undead", "enemy")]
	[InlineData("enemy.undead.skeleton", "enemy.undead")]
	[InlineData("item.consumable.potion.health", "item.consumable.potion")]
	[InlineData("color.dark.red", "color.dark")]
	public void Returns_the_correct_requested_direct_parents(string tagKey, string parentKey)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);

		Tag directParent = tag.RequestDirectParent();

		var directParentCheck = Tag.RequestTag(_tagsManager, parentKey);

		directParent.Should().Be(directParentCheck);
	}

	[Theory]
	[Trait("RequestDirectParent", "Valid tags")]
	[InlineData("simple")]
	[InlineData("other")]
	[InlineData("enemy")]
	[InlineData("item")]
	[InlineData("color")]
	public void Direct_parent_of_a_root_tag_is_an_Empty_tag(string tagKey)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);

		Tag directParent = tag.RequestDirectParent();

		directParent.Should().Be(Tag.Empty);
	}

	[Theory]
	[Trait("GetTagParents", "Valid tags")]
	[InlineData("simple")]
	[InlineData("color.red")]
	[InlineData("enemy.beast.boar")]
	[InlineData("item.equipment.weapon")]
	[InlineData("item.consumable.potion.health")]
	public void Returns_the_correct_container_with_all_parent_tags(string tagKey)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);

		TagContainer? parentsContainer = tag.GetTagParents();

		var parents = new HashSet<Tag>([tag]);

		foreach (var parentTagKey in ExtractTagParents(tagKey))
		{
			parents.Add(Tag.RequestTag(_tagsManager, parentTagKey));
		}

		var parentsContainerCheck = new TagContainer(_tagsManager, parents);

		parentsContainer.Should().BeEquivalentTo(parentsContainerCheck);
	}

	[Theory]
	[Trait("GetTagParents", "Valid tags")]
	[InlineData("simple")]
	[InlineData("other")]
	[InlineData("enemy")]
	[InlineData("item")]
	[InlineData("color")]
	public void Parents_container_of_a_root_tag_is_a_SingleTag_container(string tagKey)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);

		TagContainer? parentsContainer = tag.GetTagParents();

		parentsContainer.Should().BeEquivalentTo(tag.GetSingleTagContainer());
	}

	[Theory]
	[Trait("ParseParentTags", "Valid tags")]
	[InlineData("simple")]
	[InlineData("color.green")]
	[InlineData("enemy.beast.wolf")]
	[InlineData("item.equipment")]
	[InlineData("item.consumable.potion")]
	[InlineData("item.consumable.potion.mana")]
	public void ParseParents_parses_parents_correctly(string tagKey)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);

		HashSet<Tag> directParent = tag.ParseParentTags();

		List<string> parentTagKeys = ExtractTagParents(tagKey);

		foreach (var parentTagKey in parentTagKeys)
		{
			directParent.Should().Contain(Tag.RequestTag(_tagsManager, parentTagKey));
		}

		directParent.Should().HaveCount(parentTagKeys.Count);
	}

	[Theory]
	[Trait("ParseParentTags", "Root tag")]
	[InlineData("simple")]
	[InlineData("other")]
	[InlineData("enemy")]
	[InlineData("item")]
	[InlineData("color")]
	public void Parse_parents_result_of_a_root_tag_is_empty(string tagKey)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);

		HashSet<Tag> directParent = tag.ParseParentTags();

		directParent.Should().BeEmpty();
	}

	[Theory]
	[Trait("MatchesTag", "Matches")]
	[InlineData("simple", "simple")]
	[InlineData("simple.tag", "simple.tag")]
	[InlineData("simple.tag", "simple")]
	[InlineData("enemy.beast.wolf", "enemy.beast")]
	[InlineData("enemy.beast.wolf.gray", "enemy")]
	[InlineData("color.dark.blue", "color")]
	[InlineData("color.dark.blue", "color.dark")]
	public void Valid_tags_match(string tagKey1, string tagKey2)
	{
		var tag1 = Tag.RequestTag(_tagsManager, tagKey1);
		var tag2 = Tag.RequestTag(_tagsManager, tagKey2);

		tag1.MatchesTag(tag2).Should().BeTrue();
	}

	[Theory]
	[Trait("MatchesTag", "Dont match")]
	[InlineData("simple", "other")]
	[InlineData("simple", "simple.tag")]
	[InlineData("simple.tag", "tag")]
	[InlineData("enemy.beast.wolf", "enemy.undead")]
	[InlineData("color.blue", "color.dark.blue")]
	[InlineData("color.dark.blue", "color.blue")]
	[InlineData("tag", "simple.tag")]
	public void Valid_tags_do_not_match(string tagKey1, string tagKey2)
	{
		var tag1 = Tag.RequestTag(_tagsManager, tagKey1);
		var tag2 = Tag.RequestTag(_tagsManager, tagKey2);

		tag1.MatchesTag(tag2).Should().BeFalse();
	}

	[Theory]
	[Trait("MatchesTag", "Invalid")]
	[InlineData("foo.bar", "foo.bar")]
	[InlineData("foo.bar", "foo")]
	[InlineData("foo", "foo.bar")]
	public void Invalid_tags_never_match(string tagKey1, string tagKey2)
	{
		var tag1 = Tag.RequestTag(_tagsManager, tagKey1, false);
		var tag2 = Tag.RequestTag(_tagsManager, tagKey2, false);

		tag1.MatchesTag(tag2).Should().BeFalse();
	}

	[Theory]
	[Trait("MatchesTag", "Empty tag")]
	[InlineData("simple")]
	[InlineData("color.green")]
	[InlineData("enemy.beast.wolf.gray")]
	[InlineData("item")]
	[InlineData("item.consumable")]
	[InlineData("foo.bar")]
	public void No_tags_match_Empty_tag(string tagKey)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey, false);

		tag.MatchesTag(Tag.Empty).Should().BeFalse();
		Tag.Empty.MatchesTag(tag).Should().BeFalse();
	}

	[Fact]
	[Trait("MatchesTag", "Empty tag")]
	public void Empty_tag_does_not_match_itself()
	{
		Tag.Empty.MatchesTag(Tag.Empty).Should().BeFalse();
	}

	[Theory]
	[Trait("MatchesTagExact", "Matches")]
	[InlineData("simple", "simple")]
	[InlineData("simple.tag", "simple.tag")]
	[InlineData("enemy.beast.wolf", "enemy.beast.wolf")]
	[InlineData("enemy.beast.wolf.gray", "enemy.beast.wolf.gray")]
	[InlineData("color.blue", "color.blue")]
	[InlineData("color.dark.blue", "color.dark.blue")]
	[InlineData("color.dark", "color.dark")]
	public void Valid_tags_match_exactly(string tagKey1, string tagKey2)
	{
		var tag1 = Tag.RequestTag(_tagsManager, tagKey1);
		var tag2 = Tag.RequestTag(_tagsManager, tagKey2);

		tag1.MatchesTagExact(tag2).Should().BeTrue();
	}

	[Theory]
	[Trait("MatchesTagExact", "Dont match")]
	[InlineData("simple", "other")]
	[InlineData("simple", "simple.tag")]
	[InlineData("simple.tag", "tag")]
	[InlineData("enemy.beast.wolf", "enemy.undead")]
	[InlineData("color.blue", "color.dark.blue")]
	[InlineData("color.dark.blue", "color.blue")]
	[InlineData("tag", "simple.tag")]
	public void Valid_tags_do_not_match_exactly(string tagKey1, string tagKey2)
	{
		var tag1 = Tag.RequestTag(_tagsManager, tagKey1);
		var tag2 = Tag.RequestTag(_tagsManager, tagKey2);

		tag1.MatchesTagExact(tag2).Should().BeFalse();
	}

	[Theory]
	[Trait("MatchesTagExact", "Invalid")]
	[InlineData("foo.bar", "foo.bar")]
	[InlineData("foo.bar", "foo")]
	[InlineData("foo", "foo.bar")]
	public void Invalid_tags_never_match_exactly(string tagKey1, string tagKey2)
	{
		var tag1 = Tag.RequestTag(_tagsManager, tagKey1, false);
		var tag2 = Tag.RequestTag(_tagsManager, tagKey2, false);

		tag1.MatchesTagExact(tag2).Should().BeFalse();
	}

	[Theory]
	[Trait("MatchesTagExact", "Empty tag")]
	[InlineData("simple")]
	[InlineData("color.green")]
	[InlineData("enemy.beast.wolf.gray")]
	[InlineData("item")]
	[InlineData("item.consumable")]
	[InlineData("foo.bar")]
	public void No_tags_match_exactly_Empty_tag(string tagKey)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey, false);

		tag.MatchesTagExact(Tag.Empty).Should().BeFalse();
		Tag.Empty.MatchesTagExact(tag).Should().BeFalse();
	}

	[Fact]
	[Trait("MatchesTagExact", "Empty tag")]
	public void Empty_tag_does_not_match_exactly_itself()
	{
		Tag.Empty.MatchesTagExact(Tag.Empty).Should().BeFalse();
	}

	[Theory]
	[Trait("MatchesAny", "Valid tags")]
	[InlineData(
		"simple.tag",
		new string[] { "simple.tag" })]
	[InlineData(
		"simple.tag",
		new string[] { "simple.tag", "other" })]
	[InlineData(
		"color.green",
		new string[] { "simple.tag", "other", "color.green" })]
	[InlineData(
		"item.consumable.potion.health",
		new string[] { "item.consumable.potion" })]
	[InlineData(
		"item.consumable.potion.health",
		new string[] { "item", "color.green" })]
	public void Valid_tags_MatchAny_container(string tagKey, string[] containerKeys)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);

		TagContainer tagContainer = BuildContainerFromStrings(containerKeys);

		tag.MatchesAny(tagContainer).Should().BeTrue();
	}

	[Theory]
	[Trait("MatchesAny", "Valid tags")]
	[InlineData(
		"simple.tag",
		new string[] { })]
	[InlineData(
		"simple.tag",
		new string[] { "tag", "other" })]
	[InlineData(
		"simple",
		new string[] { "simple.tag", "other" })]
	[InlineData(
		"color.green",
		new string[] { "simple", "other", "color.dark.green" })]
	[InlineData(
		"item.consumable.potion",
		new string[] { "item.consumable.potion.health" })]
	[InlineData(
		"tag",
		new string[] { "simple.tag", "color.green" })]
	public void Valid_tags_do_not_MatchAny_container(string tagKey, string[] containerKeys)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);

		TagContainer tagContainer = BuildContainerFromStrings(containerKeys);

		tag.MatchesAny(tagContainer).Should().BeFalse();
	}

	[Theory]
	[Trait("MatchesAny", "Single tag")]
	[InlineData("simple")]
	[InlineData("enemy.beast.boar")]
	[InlineData("item")]
	[InlineData("item.consumable.food.apple")]
	[InlineData("color.red")]
	public void Valid_tags_MatchAny_with_their_SingleTag_container(string tagKey)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);

#pragma warning disable CS8604 // Possible null reference argument.
		tag.MatchesAny(tag.GetSingleTagContainer()).Should().BeTrue();
#pragma warning restore CS8604 // Possible null reference argument.
	}

	[Theory]
	[Trait("MatchesAny", "Invalid tag")]
	[InlineData(
		"foo.bar",
		new string[] { })]
	[InlineData(
		"foo.bar",
		new string[] { "foo.bar", "other" })]
	[InlineData(
		"foo",
		new string[] { "simple.tag", "other" })]
	[InlineData(
		"bar",
		new string[] { "simple", "foo", "bar", "foo.bar" })]
	public void Invalid_tags_never_MatchAny_container(string tagKey, string[] containerKeys)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey, false);

		TagContainer tagContainer = BuildContainerFromStrings(containerKeys, false);

		tag.MatchesAny(tagContainer).Should().BeFalse();
	}

	[Theory]
	[Trait("MatchesAny", "Empty tag")]
	[InlineData((object)new string[] { })]
	[InlineData((object)new string[] { "tag", "other" })]
	[InlineData((object)new string[] { "simple.tag", "other" })]
	[InlineData((object)new string[] { "item.consumable.potion.health", "item.consumable.potion.stamina" })]
	[InlineData((object)new string[] { "enemy.humanoid.goblin", "color.dark" })]
	public void Empty_tag_never_MatchAny_container(string[] containerKeys)
	{
		TagContainer tagContainer = BuildContainerFromStrings(containerKeys, false);

		Tag.Empty.MatchesAny(tagContainer).Should().BeFalse();
	}

	[Theory]
	[Trait("MatchesAny", "Empty tag")]
	[InlineData("tag")]
	[InlineData("enemy.undead.ghoul")]
	[InlineData("item")]
	[InlineData("item.consumable.food.bread")]
	[InlineData("color.darl.red")]
	[InlineData("foo.bar")]
	public void No_tag_MatchAny_with_an_empty_container(string tagKey)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey, false);

		tag.MatchesAny(new TagContainer(_tagsManager)).Should().BeFalse();
	}

	[Theory]
	[Trait("MatchesAnyExact", "Valid tags")]
	[InlineData(
		"simple.tag",
		new string[] { "simple.tag" })]
	[InlineData(
		"simple.tag",
		new string[] { "simple.tag", "other" })]
	[InlineData(
		"color.green",
		new string[] { "simple.tag", "other", "color.green" })]
	[InlineData(
		"item.consumable.potion.health",
		new string[] { "item.consumable.potion.health" })]
	[InlineData(
		"item.consumable.potion.health",
		new string[] { "item", "color.green", "item.consumable.potion.health" })]
	public void Valid_tags_MatchAnyExact_container(string tagKey, string[] containerKeys)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);

		TagContainer tagContainer = BuildContainerFromStrings(containerKeys);

		tag.MatchesAnyExact(tagContainer).Should().BeTrue();
	}

	[Theory]
	[Trait("MatchesAnyExact", "Valid tags")]
	[InlineData(
		"simple.tag",
		new string[] { })]
	[InlineData(
		"simple.tag",
		new string[] { "simple", "other" })]
	[InlineData(
		"simple",
		new string[] { "simple.tag", "other" })]
	[InlineData(
		"color.green",
		new string[] { "simple", "other", "color.dark.green" })]
	[InlineData(
		"item.consumable.potion",
		new string[] { "item.consumable.potion.health", "item.consumable" })]
	[InlineData(
		"tag",
		new string[] { "simple.tag", "color.green" })]
	public void Valid_tags_do_not_MatchAnyExact_container(string tagKey, string[] containerKeys)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);

		TagContainer tagContainer = BuildContainerFromStrings(containerKeys);

		tag.MatchesAnyExact(tagContainer).Should().BeFalse();
	}

	[Theory]
	[Trait("MatchesAnyExact", "Single tag")]
	[InlineData("simple")]
	[InlineData("enemy.beast.boar")]
	[InlineData("item")]
	[InlineData("item.consumable.food.apple")]
	[InlineData("color.red")]
	public void Valid_tags_MatchAnyExact_with_their_SingleTag_container(string tagKey)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);

#pragma warning disable CS8604 // Possible null reference argument.
		tag.MatchesAnyExact(tag.GetSingleTagContainer()).Should().BeTrue();
#pragma warning restore CS8604 // Possible null reference argument.
	}

	[Theory]
	[Trait("MatchesAnyExact", "Invalid tag")]
	[InlineData(
		"foo.bar",
		new string[] { })]
	[InlineData(
		"foo.bar",
		new string[] { "foo.bar", "other" })]
	[InlineData(
		"foo",
		new string[] { "simple.tag", "other" })]
	[InlineData(
		"bar",
		new string[] { "simple", "foo", "bar", "foo.bar" })]
	public void Invalid_tags_never_MatchAnyExact_container(string tagKey, string[] containerKeys)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey, false);

		TagContainer tagContainer = BuildContainerFromStrings(containerKeys, false);

		tag.MatchesAnyExact(tagContainer).Should().BeFalse();
	}

	[Theory]
	[Trait("MatchesAnyExact", "Empty tag")]
	[InlineData((object)new string[] { })]
	[InlineData((object)new string[] { "tag", "other" })]
	[InlineData((object)new string[] { "simple.tag", "other" })]
	[InlineData((object)new string[] { "item.consumable.potion.health", "item.consumable.potion.stamina" })]
	[InlineData((object)new string[] { "enemy.humanoid.goblin", "color.dark" })]
	public void Empty_tag_never_MatchAnyExact_container(string[] containerKeys)
	{
		TagContainer tagContainer = BuildContainerFromStrings(containerKeys, false);

		Tag.Empty.MatchesAnyExact(tagContainer).Should().BeFalse();
	}

	[Theory]
	[Trait("MatchesAnyExact", "Empty tag")]
	[InlineData("tag")]
	[InlineData("enemy.undead.ghoul")]
	[InlineData("item")]
	[InlineData("item.consumable.food.bread")]
	[InlineData("color.dark.red")]
	[InlineData("foo.bar")]
	public void No_tag_MatchAnyExact_with_an_empty_container(string tagKey)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey, false);

		tag.MatchesAnyExact(new TagContainer(_tagsManager)).Should().BeFalse();
	}

	[Theory]
	[Trait("MatchesTagDepth", "Matches depth")]
	[InlineData("simple.tag", "simple.tag", 2)]
	[InlineData("simple.tag", "simple", 1)]
	[InlineData("item.consumable.food.bread", "item.consumable.food.bread", 4)]
	[InlineData("item.consumable.food.bread", "item.consumable.food", 3)]
	[InlineData("item.consumable.food.bread", "item.consumable", 2)]
	[InlineData("item.consumable.food.bread", "item.consumable.potion.health", 2)]
	[InlineData("item.consumable.food.bread", "item.equipment.weapon.sword", 1)]
	[InlineData("item.consumable.food.bread", "tag", 0)]
	[InlineData("color.dark.red", "color.dark.blue", 2)]
	public void Tags_match_depth(string tagKey1, string tagKey2, int depth)
	{
		var tag1 = Tag.RequestTag(_tagsManager, tagKey1);
		var tag2 = Tag.RequestTag(_tagsManager, tagKey2);

		tag1.MatchesTagDepth(tag2).Should().Be(depth);
	}

	[Theory]
	[Trait("MatchesTagDepth", "Empty tag")]
	[InlineData("tag")]
	[InlineData("simple.tag")]
	[InlineData("item.consumable.food.apple")]
	[InlineData("item.consumable.potion.health")]
	[InlineData("item.consumable.potion")]
	[InlineData("color.dark.red")]
	public void Empty_tag_always_match_depth_zero(string tagKey)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);

		tag.MatchesTagDepth(Tag.Empty).Should().Be(0);
		Tag.Empty.MatchesTagDepth(tag).Should().Be(0);
	}

	[Theory]
	[Trait("Equality", "Equals")]
	[InlineData("tag", "TAG")]
	[InlineData("simple.tag", "Simple.Tag")]
	[InlineData("item.equipment.weapon.dagger", "ITEM.equipment.WeAPoN.dagger")]
	[InlineData("ENEMY.HUMANOID", "enemy.humanoid")]
	public void Tags_are_equal(string tagKey1, string tagKey2)
	{
		var tag1 = Tag.RequestTag(_tagsManager, tagKey1);
		var tag2 = Tag.RequestTag(_tagsManager, tagKey2);

		tag1.Should().Be(tag2);
		(tag1 == tag2).Should().BeTrue();
		(tag1 != tag2).Should().BeFalse();
		tag1.Equals(tag2).Should().BeTrue();

		object tagObject1 = tag1;
		object tagObject2 = tag2;

		tagObject1.Should().Be(tagObject2);
		tagObject1.Equals(tag2).Should().BeTrue();
		tag1.Equals(tagObject2).Should().BeTrue();
		tag1.Should().Be(tagObject2);
	}

	[Theory]
	[Trait("Equality", "Different")]
	[InlineData("tag", "simple")]
	[InlineData("simple.tag", "simple")]
	[InlineData("simple.tag", "tag")]
	[InlineData("enemy.undead", "enemy.beast")]
	[InlineData("color.blue", "color.dark.blue")]
	public void Tags_are_different(string tagKey1, string tagKey2)
	{
		var tag1 = Tag.RequestTag(_tagsManager, tagKey1);
		var tag2 = Tag.RequestTag(_tagsManager, tagKey2);

		tag1.Should().NotBe(tag2);
		(tag1 != tag2).Should().BeTrue();
		(tag1 == tag2).Should().BeFalse();
		tag1.Equals(tag2).Should().BeFalse();

		object tagObject1 = tag1;
		object tagObject2 = tag2;

		tagObject1.Should().NotBe(tagObject2);
		tagObject1.Equals(tag2).Should().BeFalse();
		tag1.Equals(tagObject2).Should().BeFalse();
		tag1.Should().NotBe(tagObject2);
	}

	[Fact]
	[Trait("Equality", "Empty tag")]
	public void Empty_tags_are_the_same()
	{
		Tag tag1 = Tag.Empty;
		var tag2 = default(Tag);
		var tag3 = Tag.RequestTag(_tagsManager, StringKey.Empty, false);

		tag1.Should().Be(tag2);
		tag2.Should().Be(tag3);
		tag1.Should().Be(tag3);
	}

	[Theory]
	[Trait("ToString", "Lowercase")]
	[InlineData("TAG")]
	[InlineData("simple.tag")]
	[InlineData("Simple.Tag")]
	[InlineData("Item.Consumable.Food.Bread")]
	[InlineData("CoLoR.dArK.bLuE")]
	public void Tag_ToString_returns_lowercase_string(string tagKey)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);

		tag.ToString().Should().Be(tagKey.ToLowerInvariant());
	}

	[Fact]
	[Trait("ToString", "Empty tag")]
	public void Empty_tag_ToString_returns_empty_string()
	{
		Tag.Empty.ToString().Should().BeEmpty();
	}

	private static List<string> ExtractTagParents(string tagKey)
	{
		var parentTags = new List<string>();

		while (tagKey.Contains('.'))
		{
			var lastDotIndex = tagKey.LastIndexOf('.');

			if (lastDotIndex >= 0)
			{
				tagKey = tagKey[..lastDotIndex];
			}

			parentTags.Add(tagKey);
		}

		return parentTags;
	}

	private TagContainer BuildContainerFromStrings(string[] keys, bool errorIfNotFound = true)
	{
		var set = new HashSet<Tag>();

		foreach (var key in keys)
		{
			var tag = Tag.RequestTag(_tagsManager, key, errorIfNotFound);

			if (tag.IsValid)
			{
				set.Add(tag);
			}
		}

		return new TagContainer(_tagsManager, set);
	}
}
