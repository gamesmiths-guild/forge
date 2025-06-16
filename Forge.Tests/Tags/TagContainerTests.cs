// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Tags;

public class TagContainerTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	public enum QueryType
	{
		AllTagsMatch = 0,
		AnyTagsMatch = 1,
		NoTagsMatch = 2,
	}

	private readonly TagsManager _tagsManager = fixture.TagsManager;

	[Theory]
	[Trait("Initialization", "Request")]
	[InlineData("simple")]
	[InlineData("Simple.Tag")]
	public void Container_created_with_tag_contains_given_tag(string tagKey)
	{
		var tag = Tag.RequestTag(_tagsManager, tagKey);
		var tagContainer = new TagContainer(tag);

		tagContainer.IsEmpty.Should().BeFalse();
		tagContainer.HasTag(tag).Should().BeTrue();
		tagContainer.Count.Should().Be(1);
	}

	[Fact]
	[Trait("Initialization", "Empty container")]
	public void Freshly_created_container_is_empty()
	{
		var tagContainer = new TagContainer(_tagsManager);

		tagContainer.IsEmpty.Should().BeTrue();
	}

	[Theory]
	[Trait("Initialization", "Tag set")]
	[InlineData((object)new string[] { "tag" })]
	[InlineData((object)new string[] { "tag", "simple.tag", "other.tag" })]
	[InlineData((object)new string[] { "enemy.undead.zombie", "enemy.undead.skeleton", "enemy.undead.ghoul" })]
	[InlineData((object)new string[] { "item.consumable.potion.health", "color.red" })]
	[InlineData((object)new string[] { "item.equipment.weapon.sword", "item.equipment.weapon" })]
	public void Container_created_with_tag_set_contains_given_tags(string[] tagKeys)
	{
		var tagSet = new HashSet<Tag>();

		foreach (var tagKey in tagKeys)
		{
			tagSet.Add(Tag.RequestTag(_tagsManager, tagKey));
		}

		var container = new TagContainer(_tagsManager, tagSet);

		container.IsEmpty.Should().BeFalse();
		container.Count.Should().Be(tagSet.Count);

		foreach (var tagKey in tagKeys)
		{
			var tag = Tag.RequestTag(_tagsManager, tagKey);
			container.HasTag(tag).Should().BeTrue();
		}
	}

	[Theory]
	[Trait("Serialization", "Serialize")]
	[InlineData(
		new string[] { "tag" },
		new byte[] { 0, 1, 39, 0 })]
	[InlineData(
		new string[] { "tag", "simple.tag", "other.tag" },
		new byte[] { 0, 3, 39, 0, 38, 0, 36, 0 })]
	[InlineData(
		new string[] { "color", "color.blue", "color.dark.blue" },
		new byte[] { 0, 3, 0, 0, 1, 0, 3, 0 })]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" },
		new byte[] { 0, 4, 34, 0, 31, 0, 30, 0, 21, 0 })]
	[InlineData(
		new string[] { "item.consumable.potion.health", "color.red" },
		new byte[] { 0, 2, 27, 0, 7, 0 })]
	public void Container_with_registered_tags_serializes_successfully_with_valid_net_index(
		string[] tagKeys, byte[] netIndexStream)
	{
		TagContainer container = _tagsManager.RequestTagContainer(tagKeys);

		TagContainer.NetSerialize(_tagsManager, container, out var containerStream);

		containerStream[0].Should().Be(0);
		containerStream.Should().HaveCount((tagKeys.Length * 2) + 2);

		containerStream.Should().BeEquivalentTo(netIndexStream);
	}

	[Fact]
	[Trait("Serialization", "Serialize")]
	public void Empty_container_serializes_successfully()
	{
		var tagContainer = new TagContainer(_tagsManager);

		TagContainer.NetSerialize(_tagsManager, tagContainer, out var containerStream)
			.Should()
			.BeTrue();

		containerStream[0].Should().Be(1);
		containerStream.Should().ContainSingle();
	}

	[Theory]
	[Trait("Serialization", "Deserialize")]
	[InlineData(
		new byte[] { 0, 1, 39, 0 },
		new string[] { "tag" })]
	[InlineData(
		new byte[] { 0, 3, 39, 0, 38, 0, 36, 0 },
		new string[] { "tag", "simple.tag", "other.tag" })]
	[InlineData(
		new byte[] { 0, 3, 0, 0, 1, 0, 3, 0 },
		new string[] { "color", "color.blue", "color.dark.blue" })]
	[InlineData(
		new byte[] { 0, 4, 34, 0, 31, 0, 30, 0, 21, 0 },
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" })]
	[InlineData(
		new byte[] { 0, 2, 27, 0, 7, 0 },
		new string[] { "item.consumable.potion.health", "color.red" })]
	[InlineData(
		new byte[] { 0, 2, 45, 0, 39, 0 }, // 45 is invalid tag net index
		new string[] { "tag" })]
	public void Container_deserializes_successfully(byte[] netIndexStream, string[] tagKeys)
	{
		TagContainer.NetDeserialize(
			_tagsManager,
			netIndexStream,
			out TagContainer deserializedContainer).Should().BeTrue();

		TagContainer tagContainerCheck = _tagsManager.RequestTagContainer(tagKeys);

		deserializedContainer.Should().BeEquivalentTo(tagContainerCheck);
	}

	[Fact]
	[Trait("Serialization", "Deserialize")]
	public void Empty_container_deserializes_successfully()
	{
		TagContainer.NetDeserialize(
			_tagsManager,
			[1],
			out TagContainer deserializedContainer).Should().BeTrue();

		deserializedContainer.Should().BeEquivalentTo(new TagContainer(_tagsManager));
	}

	[Fact]
	[Trait("Serialization", "Deserialize")]
	public void Container_deserializes_successfully_ignoring_invalid_tags()
	{
		TagContainer.NetDeserialize(
			_tagsManager,
			[0, 2, (byte)_tagsManager.InvalidTagNetIndex, 0, 39, 0],
			out TagContainer deserializedContainer).Should().BeTrue();

		var tagContainerCheck = new TagContainer(Tag.RequestTag(_tagsManager, "tag"));

		deserializedContainer.Should().BeEquivalentTo(tagContainerCheck);
	}

	[Fact]
	[Trait("Serialization", "Deserialize")]
	public void Container_throws_exception_when_deserializing_higher_than_InvalidTagNetIndex_values()
	{
		Action act = () =>
		{
			TagContainer.NetDeserialize(
				_tagsManager,
				[0, 2, (byte)(_tagsManager.InvalidTagNetIndex + 1), 0, 39, 0],
				out TagContainer _);
		};

		act.Should().Throw<InvalidTagNetIndexException>().WithMessage("Received invalid tag net index*");
	}

	[Theory]
	[Trait("HasTag", "True")]
	[InlineData(
		new string[] { "tag" },
		"tag")]
	[InlineData(
		new string[] { "tag", "other.tag" },
		"tag")]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" },
		"item.equipment")]
	[InlineData(
		new string[] { "item.consumable.potion.health", "color.red" },
		"color.red")]
	[InlineData(
		new string[] { "item.consumable.potion.stamina", "color.green" },
		"color")]
	[InlineData(
		new string[] { "item.consumable.potion.stamina", "color.green" },
		"item.consumable.potion")]
	[InlineData(
		new string[] { "item.consumable.potion.stamina", "color.green" },
		"item")]
	public void Container_should_have_tag(string[] tagKeys, string tagKey)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(tagKeys);

		var tag = Tag.RequestTag(_tagsManager, tagKey);

		tagContainer.HasTag(tag).Should().BeTrue();
	}

	[Theory]
	[Trait("HasTag", "False")]
	[InlineData(
		new string[] { "tag" },
		"other")]
	[InlineData(
		new string[] { "tag", "other.tag" },
		"simple.tag")]
	[InlineData(
		new string[] { "item.equipment.weapon", "item.equipment", "item" },
		"item.equipment.weapon.sword")]
	[InlineData(
		new string[] { "item.consumable.potion.health", "color.dark" },
		"color.dark.red")]
	[InlineData(
		new string[] { "item.consumable.potion.stamina", "color.green" },
		"color.dark.green")]
	public void Container_should_not_have_tag(string[] tagKeys, string tagKey)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(tagKeys);

		var tag = Tag.RequestTag(_tagsManager, tagKey);

		tagContainer.HasTag(tag).Should().BeFalse();
	}

	[Fact]
	[Trait("HasTag", "Empty tag")]
	public void Container_does_not_have_Empty_tag()
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(["tag", "simple.tag", "other.tag"]);

		tagContainer.HasTag(Tag.Empty).Should().BeFalse();
	}

	[Fact]
	[Trait("HasTag", "Empty container")]
	public void Empty_container_never_have_any_tags()
	{
		var tagContainer = new TagContainer(_tagsManager);

		tagContainer.HasTag(Tag.RequestTag(_tagsManager, "tag")).Should().BeFalse();
		tagContainer.HasTag(Tag.Empty).Should().BeFalse();
	}

	[Theory]
	[Trait("HasTagExact", "True")]
	[InlineData(
		new string[] { "tag" },
		"tag")]
	[InlineData(
		new string[] { "tag", "other.tag" },
		"tag")]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" },
		"item.equipment")]
	[InlineData(
		new string[] { "item.consumable.potion.health", "color.red" },
		"color.red")]
	[InlineData(
		new string[] { "item.consumable.potion.stamina", "color.green" },
		"color.green")]
	[InlineData(
		new string[] { "item.consumable.potion.stamina", "color.green" },
		"item.consumable.potion.stamina")]
	public void Container_should_have_exact_tag(string[] tagKeys, string tagKey)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(tagKeys);

		var tag = Tag.RequestTag(_tagsManager, tagKey);

		tagContainer.HasTagExact(tag).Should().BeTrue();
	}

	[Theory]
	[Trait("HasTagExact", "False")]
	[InlineData(
		new string[] { "tag" },
		"other")]
	[InlineData(
		new string[] { "tag", "other.tag" },
		"simple.tag")]
	[InlineData(
		new string[] { "item.equipment.weapon", "item.equipment", "item" },
		"item.equipment.weapon.sword")]
	[InlineData(
		new string[] { "item.consumable.potion.health", "color.dark" },
		"color.dark.red")]
	[InlineData(
		new string[] { "item.consumable.potion.stamina", "color.green" },
		"color.dark.green")]
	[InlineData(
		new string[] { "item.consumable.potion.stamina", "color.green" },
		"color")]
	[InlineData(
		new string[] { "item.consumable.potion.stamina", "color.green" },
		"item.consumable.potion")]
	[InlineData(
		new string[] { "item.consumable.potion.stamina", "color.green" },
		"item")]
	public void Container_should_not_have_exact_tag(string[] tagKeys, string tagKey)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(tagKeys);

		var tag = Tag.RequestTag(_tagsManager, tagKey);

		tagContainer.HasTagExact(tag).Should().BeFalse();
	}

	[Fact]
	[Trait("HasTagExact", "Empty tag")]
	public void Container_does_not_have_exactly_Empty_tag()
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(["tag", "simple.tag", "other.tag"]);

		tagContainer.HasTagExact(Tag.Empty).Should().BeFalse();
	}

	[Fact]
	[Trait("HasTagExact", "Empty container")]
	public void Empty_container_never_have_exactly_any_tags()
	{
		var tagContainer = new TagContainer(_tagsManager);

		tagContainer.HasTagExact(Tag.RequestTag(_tagsManager, "tag")).Should().BeFalse();
		tagContainer.HasTagExact(Tag.Empty).Should().BeFalse();
	}

	[Theory]
	[Trait("HasAny", "True")]
	[InlineData(
		new string[] { "tag" },
		new string[] { "tag" })]
	[InlineData(
		new string[] { "tag", "other.tag" },
		new string[] { "tag" })]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" },
		new string[] { "item.equipment.weapon.sword", "item.equipment", "item" })]
	[InlineData(
		new string[] { "item.consumable.potion.stamina", "color.green" },
		new string[] { "color.green" })]
	[InlineData(
		new string[] { "item.consumable.potion.health", "color.red" },
		new string[] { "color" })]
	public void Container_A_has_any_container_B_tag(string[] tagKeysA, string[] tagKeysB)
	{
		TagContainer tagContainerA = _tagsManager.RequestTagContainer(tagKeysA);
		TagContainer tagContainerB = _tagsManager.RequestTagContainer(tagKeysB);

		tagContainerA.HasAny(tagContainerB).Should().BeTrue();
	}

	[Theory]
	[Trait("HasAny", "False")]
	[InlineData(
		new string[] { "tag" },
		new string[] { "other" })]
	[InlineData(
		new string[] { "tag", "other.tag" },
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" })]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" },
		new string[] { "item.consumable.potion.stamina", "color.green" })]
	[InlineData(
		new string[] { "item.consumable.potion.stamina", "color.green" },
		new string[] { "tag", "other.tag", "simple.tag" })]
	[InlineData(
		new string[] { "item.consumable.potion.health" },
		new string[] { "color" })]
	public void Container_A_does_not_have_any_container_B_tag(string[] tagKeysA, string[] tagKeysB)
	{
		TagContainer tagContainerA = _tagsManager.RequestTagContainer(tagKeysA);
		TagContainer tagContainerB = _tagsManager.RequestTagContainer(tagKeysB);

		tagContainerA.HasAny(tagContainerB).Should().BeFalse();
	}

	[Fact]
	[Trait("HasAny", "Empty container")]
	public void Container_does_not_have_any_empty_container_tags()
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(["tag", "simple.tag", "other.tag"]);
		var emptyContainer = new TagContainer(_tagsManager);

		tagContainer.HasAny(emptyContainer).Should().BeFalse();
	}

	[Fact]
	[Trait("HasAny", "Empty container")]
	public void Empty_container_does_not_have_any_container_tags()
	{
		var emptyContainer = new TagContainer(_tagsManager);
		TagContainer tagContainer = _tagsManager.RequestTagContainer(["tag", "simple.tag", "other.tag"]);

		emptyContainer.HasAny(tagContainer).Should().BeFalse();
	}

	[Theory]
	[Trait("HasAnyExact", "True")]
	[InlineData(
		new string[] { "tag" },
		new string[] { "tag" })]
	[InlineData(
		new string[] { "tag", "other.tag" },
		new string[] { "tag" })]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" },
		new string[] { "item.equipment.weapon.sword", "item.equipment", "item" })]
	[InlineData(
		new string[] { "item.consumable.potion.stamina", "color.green" },
		new string[] { "color.green" })]
	[InlineData(
		new string[] { "item.consumable.potion.health", "color.red" },
		new string[] { "item.consumable.potion.health" })]
	public void Container_A_has_any_exact_container_B_tag(string[] tagKeysA, string[] tagKeysB)
	{
		TagContainer tagContainerA = _tagsManager.RequestTagContainer(tagKeysA);
		TagContainer tagContainerB = _tagsManager.RequestTagContainer(tagKeysB);

		tagContainerA.HasAnyExact(tagContainerB).Should().BeTrue();
	}

	[Theory]
	[Trait("HasAnyExact", "False")]
	[InlineData(
		new string[] { "tag" },
		new string[] { "other" })]
	[InlineData(
		new string[] { "tag", "other.tag" },
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" })]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" },
		new string[] { "item.consumable.potion.stamina", "color.green" })]
	[InlineData(
		new string[] { "item.consumable.potion.stamina", "color.green" },
		new string[] { "tag", "other.tag", "simple.tag" })]
	[InlineData(
		new string[] { "item.consumable.potion.health" },
		new string[] { "color" })]
	[InlineData(
		new string[] { "item.consumable.potion.health", "color.red" },
		new string[] { "color" })]
	[InlineData(
		new string[] { "item.consumable.potion.health", "color.red" },
		new string[] { "item.consumable.potion" })]
	public void Container_A_does_not_have_any_exact_container_B_tag(string[] tagKeysA, string[] tagKeysB)
	{
		TagContainer tagContainerA = _tagsManager.RequestTagContainer(tagKeysA);
		TagContainer tagContainerB = _tagsManager.RequestTagContainer(tagKeysB);

		tagContainerA.HasAnyExact(tagContainerB).Should().BeFalse();
	}

	[Fact]
	[Trait("HasAnyExact", "Empty container")]
	public void Container_does_not_have_any_exact_empty_container_tags()
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(["tag", "simple.tag", "other.tag"]);
		var emptyContainer = new TagContainer(_tagsManager);

		tagContainer.HasAnyExact(emptyContainer).Should().BeFalse();
	}

	[Fact]
	[Trait("HasAnyExact", "Empty container")]
	public void Empty_container_does_not_have_exact_any_container_tags()
	{
		var emptyContainer = new TagContainer(_tagsManager);
		TagContainer tagContainer = _tagsManager.RequestTagContainer(["tag", "simple.tag", "other.tag"]);

		emptyContainer.HasAnyExact(tagContainer).Should().BeFalse();
	}

	[Theory]
	[Trait("HasAll", "True")]
	[InlineData(
		new string[] { "tag", "simple.tag", "other.tag" },
		new string[] { "tag" })]
	[InlineData(
		new string[] { "tag", "simple.tag", "other.tag" },
		new string[] { "tag", "simple", "other.tag" })]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" },
		new string[] { "item.equipment.weapon.sword", "item.equipment", "item" })]
	[InlineData(
		new string[] { "item.consumable.potion.stamina", "color.green" },
		new string[] { "color.green" })]
	[InlineData(
		new string[] { "item.consumable.potion.health", "color.red" },
		new string[] { "color" })]
	public void Container_A_has_all_container_B_tag(string[] tagKeysA, string[] tagKeysB)
	{
		TagContainer tagContainerA = _tagsManager.RequestTagContainer(tagKeysA);
		TagContainer tagContainerB = _tagsManager.RequestTagContainer(tagKeysB);

		tagContainerA.HasAll(tagContainerB).Should().BeTrue();
	}

	[Theory]
	[Trait("HasAll", "False")]
	[InlineData(
		new string[] { "tag" },
		new string[] { "other" })]
	[InlineData(
		new string[] { "tag", "other.tag" },
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" })]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" },
		new string[] { "item.consumable.potion.stamina", "color.green" })]
	[InlineData(
		new string[] { "item.consumable.potion.stamina", "color.green" },
		new string[] { "tag", "other.tag", "simple.tag" })]
	[InlineData(
		new string[] { "item.consumable.potion.health" },
		new string[] { "color" })]
	[InlineData(
		new string[] { "tag", "simple.tag", "other.tag" },
		new string[] { "color.dark.red" })]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" },
		new string[] { "item.equipment.weapon.sword", "item.equipment", "item", "tag" })]
	[InlineData(
		new string[] { "item.consumable.potion.stamina", "color.green" },
		new string[] { "color.dark.green" })]
	[InlineData(
		new string[] { "item.consumable.potion.health", "color.red" },
		new string[] { "color.dark.red" })]
	public void Container_A_does_not_have_all_container_B_tag(string[] tagKeysA, string[] tagKeysB)
	{
		TagContainer tagContainerA = _tagsManager.RequestTagContainer(tagKeysA);
		TagContainer tagContainerB = _tagsManager.RequestTagContainer(tagKeysB);

		tagContainerA.HasAll(tagContainerB).Should().BeFalse();
	}

	[Fact]
	[Trait("HasAll", "Empty container")]
	public void Container_has_all_empty_container_tags()
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(["tag", "simple.tag", "other.tag"]);
		var emptyContainer = new TagContainer(_tagsManager);

		tagContainer.HasAll(emptyContainer).Should().BeTrue();
	}

	[Fact]
	[Trait("HasAll", "Empty container")]
	public void Empty_container_does_not_have_all_container_tags()
	{
		var emptyContainer = new TagContainer(_tagsManager);
		TagContainer tagContainer = _tagsManager.RequestTagContainer(["tag", "simple.tag", "other.tag"]);

		emptyContainer.HasAll(tagContainer).Should().BeFalse();
	}

	[Theory]
	[Trait("HasAllExact", "True")]
	[InlineData(
		new string[] { "tag", "simple.tag", "other.tag" },
		new string[] { "tag" })]
	[InlineData(
		new string[] { "tag", "simple.tag", "other.tag" },
		new string[] { "tag", "simple.tag", "other.tag" })]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" },
		new string[] { "item.equipment.weapon.sword", "item.equipment", "item" })]
	[InlineData(
		new string[] { "item.consumable.potion.stamina", "color.green" },
		new string[] { "color.green" })]
	[InlineData(
		new string[] { "item.consumable.potion.health", "color.red" },
		new string[] { "item.consumable.potion.health" })]
	public void Container_A_has_all_exact_container_B_tag(string[] tagKeysA, string[] tagKeysB)
	{
		TagContainer tagContainerA = _tagsManager.RequestTagContainer(tagKeysA);
		TagContainer tagContainerB = _tagsManager.RequestTagContainer(tagKeysB);

		tagContainerA.HasAllExact(tagContainerB).Should().BeTrue();
	}

	[Theory]
	[Trait("HasAllExact", "False")]
	[InlineData(
		new string[] { "tag" },
		new string[] { "other" })]
	[InlineData(
		new string[] { "tag", "other.tag" },
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" })]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" },
		new string[] { "item.consumable.potion.stamina", "color.green" })]
	[InlineData(
		new string[] { "item.consumable.potion.stamina", "color.green" },
		new string[] { "tag", "other.tag", "simple.tag" })]
	[InlineData(
		new string[] { "item.consumable.potion.health" },
		new string[] { "color" })]
	[InlineData(
		new string[] { "tag", "simple.tag", "other.tag" },
		new string[] { "color.dark.red" })]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" },
		new string[] { "item.equipment.weapon.sword", "item.equipment", "item", "tag" })]
	[InlineData(
		new string[] { "item.consumable.potion.stamina", "color.green" },
		new string[] { "item.consumable.potion" })]
	[InlineData(
		new string[] { "item.consumable.potion.health", "color.red" },
		new string[] { "color" })]
	public void Container_A_does_not_have_all_exact_container_B_tag(string[] tagKeysA, string[] tagKeysB)
	{
		TagContainer tagContainerA = _tagsManager.RequestTagContainer(tagKeysA);
		TagContainer tagContainerB = _tagsManager.RequestTagContainer(tagKeysB);

		tagContainerA.HasAllExact(tagContainerB).Should().BeFalse();
	}

	[Fact]
	[Trait("HasAllExact", "Empty container")]
	public void Container_has_all_exact_empty_container_tags()
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(["tag", "simple.tag", "other.tag"]);
		var emptyContainer = new TagContainer(_tagsManager);

		tagContainer.HasAllExact(emptyContainer).Should().BeTrue();
	}

	[Fact]
	[Trait("HasAllExact", "Empty container")]
	public void Empty_container_does_not_have_exact_all_container_tags()
	{
		var emptyContainer = new TagContainer(_tagsManager);
		TagContainer tagContainer = _tagsManager.RequestTagContainer(["tag", "simple.tag", "other.tag"]);

		emptyContainer.HasAllExact(tagContainer).Should().BeFalse();
	}

	[Theory]
	[Trait("Filter", "Has tags")]
	[InlineData(
		new string[] { "tag", "other.tag" },
		new string[] { "other.tag", "simple.tag" },
		new string[] { "other.tag" })]
	[InlineData(
		new string[] { "tag", "other.tag" },
		new string[] { "other", "simple.tag" },
		new string[] { "other.tag" })]
	[InlineData(
		new string[] { "tag", "simple.tag", "other.tag" },
		new string[] { "tag", "simple.tag", "other.tag" },
		new string[] { "tag", "simple.tag", "other.tag" })]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" },
		new string[] { "item.equipment.weapon.sword", "item.equipment", "item", "tag" },
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" })]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" },
		new string[] { "item", "tag" },
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" })]
	public void Container_A_filter_container_B_has_result_tags(
		string[] tagKeysA,
		string[] tagKeysB,
		string[] tagKeysResult)
	{
		TagContainer tagContainerA = _tagsManager.RequestTagContainer(tagKeysA);
		TagContainer tagContainerB = _tagsManager.RequestTagContainer(tagKeysB);

		TagContainer filteredContainer = tagContainerA.Filter(tagContainerB);

		TagContainer validationContainer = _tagsManager.RequestTagContainer(tagKeysResult);

		filteredContainer.Should().BeEquivalentTo(validationContainer);
	}

	[Theory]
	[Trait("Filter", "Has no tags")]
	[InlineData(
		new string[] { "tag", "other.tag" },
		new string[] { "simple.tag" })]
	[InlineData(
		new string[] { "other", "simple.tag" },
		new string[] { "tag", "other.tag" })]
	[InlineData(
		new string[] { "tag", "other.tag" },
		new string[] { "color.dark.blue", "color.blue" })]
	[InlineData(
		new string[] { "tag", "simple.tag", "other.tag" },
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" })]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" },
		new string[] { "enemy.undead.zombie", "enemy.beast.boar", "enemy.humanoid.orc" })]
	public void Container_A_filter_container_B_has_no_tags(string[] tagKeysA, string[] tagKeysB)
	{
		TagContainer tagContainerA = _tagsManager.RequestTagContainer(tagKeysA);
		TagContainer tagContainerB = _tagsManager.RequestTagContainer(tagKeysB);

		TagContainer filteredContainer = tagContainerA.Filter(tagContainerB);

		filteredContainer.IsEmpty.Should().BeTrue();
		filteredContainer.Should().BeEmpty();
	}

	[Fact]
	[Trait("Filter", "Empty container")]
	public void Container_filter_empty_container_is_empty()
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(["tag", "simple.tag", "other.tag"]);
		var emptyContainer = new TagContainer(_tagsManager);

		TagContainer filteredContainer = tagContainer.Filter(emptyContainer);

		filteredContainer.IsEmpty.Should().BeTrue();
		filteredContainer.Should().BeEmpty();
	}

	[Fact]
	[Trait("Filter", "Empty container")]
	public void Empty_container_filter_container_is_empty()
	{
		var emptyContainer = new TagContainer(_tagsManager);
		TagContainer tagContainer = _tagsManager.RequestTagContainer(["tag", "simple.tag", "other.tag"]);

		TagContainer filteredContainer = emptyContainer.Filter(tagContainer);

		filteredContainer.IsEmpty.Should().BeTrue();
		filteredContainer.Should().BeEmpty();
	}

	[Theory]
	[Trait("FilterExact", "Has tags")]
	[InlineData(
		new string[] { "tag", "other.tag" },
		new string[] { "other.tag", "simple.tag" },
		new string[] { "other.tag" })]
	[InlineData(
		new string[] { "tag", "other.tag" },
		new string[] { "other", "tag" },
		new string[] { "tag" })]
	[InlineData(
		new string[] { "tag", "simple.tag", "other.tag" },
		new string[] { "tag", "simple.tag", "other.tag" },
		new string[] { "tag", "simple.tag", "other.tag" })]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" },
		new string[] { "item.equipment.weapon.sword", "item.equipment", "item", "tag" },
		new string[] { "item.equipment.weapon.sword", "item.equipment", "item" })]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" },
		new string[] { "item", "tag" },
		new string[] { "item" })]
	public void Container_A_filter_exact_container_B_has_result_tags(
		string[] tagKeysA,
		string[] tagKeysB,
		string[] tagKeysResult)
	{
		TagContainer tagContainerA = _tagsManager.RequestTagContainer(tagKeysA);
		TagContainer tagContainerB = _tagsManager.RequestTagContainer(tagKeysB);

		TagContainer filteredContainer = tagContainerA.FilterExact(tagContainerB);

		TagContainer validationContainer = _tagsManager.RequestTagContainer(tagKeysResult);

		filteredContainer.Should().BeEquivalentTo(validationContainer);
	}

	[Theory]
	[Trait("FilterExact", "Has no tags")]
	[InlineData(
		new string[] { "tag", "other.tag" },
		new string[] { "simple.tag" })]
	[InlineData(
		new string[] { "tag", "other.tag" },
		new string[] { "other", "simple.tag" })]
	[InlineData(
		new string[] { "other", "simple.tag" },
		new string[] { "tag", "other.tag" })]
	[InlineData(
		new string[] { "tag", "other.tag" },
		new string[] { "color.dark.blue", "color.blue" })]
	[InlineData(
		new string[] { "tag", "simple.tag", "other.tag" },
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" })]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" },
		new string[] { "enemy.undead.zombie", "enemy.beast.boar", "enemy.humanoid.orc" })]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment" },
		new string[] { "item" })]
	public void Container_A_filter_exact_container_B_has_no_tags(string[] tagKeysA, string[] tagKeysB)
	{
		TagContainer tagContainerA = _tagsManager.RequestTagContainer(tagKeysA);
		TagContainer tagContainerB = _tagsManager.RequestTagContainer(tagKeysB);

		TagContainer filteredContainer = tagContainerA.FilterExact(tagContainerB);

		filteredContainer.IsEmpty.Should().BeTrue();
		filteredContainer.Should().BeEmpty();
	}

	[Fact]
	[Trait("FilterExact", "Empty container")]
	public void Container_filter_exact_empty_container_is_empty()
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(["tag", "simple.tag", "other.tag"]);
		var emptyContainer = new TagContainer(_tagsManager);

		TagContainer filteredContainer = tagContainer.FilterExact(emptyContainer);

		filteredContainer.IsEmpty.Should().BeTrue();
		filteredContainer.Should().BeEmpty();
	}

	[Fact]
	[Trait("FilterExact", "Empty container")]
	public void Empty_container_filter_exact_container_is_empty()
	{
		var emptyContainer = new TagContainer(_tagsManager);
		TagContainer tagContainer = _tagsManager.RequestTagContainer(["tag", "simple.tag", "other.tag"]);

		TagContainer filteredContainer = emptyContainer.FilterExact(tagContainer);

		filteredContainer.IsEmpty.Should().BeTrue();
		filteredContainer.Should().BeEmpty();
	}

	[Theory]
	[Trait("MatchesQuery", "Matches")]
	[InlineData(
		new string[] { "tag", "simple.tag", "other.tag" },
		QueryType.AllTagsMatch,
		new string[] { "tag", "simple.tag", "other.tag" })]
	[InlineData(
		new string[] { "tag", "simple.tag", "other.tag" },
		QueryType.AllTagsMatch,
		new string[] { "simple", "other", "tag" })]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment" },
		QueryType.AnyTagsMatch,
		new string[] { "item.equipment.weapon.sword", "simple.tag" })]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment" },
		QueryType.NoTagsMatch,
		new string[] { "simple", "other", "tag" })]
	public void Container_matches_query(string[] containerTagKeys, QueryType queryType, string[] queryTagKeys)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(containerTagKeys);

		TagQuery query = BuildSimpleTagQuery(queryType, queryTagKeys);

		tagContainer.MatchesQuery(query).Should().BeTrue();
	}

	[Theory]
	[Trait("MatchesQuery", "Doesn't matches")]
	[InlineData(
		new string[] { "tag", "simple.tag", "other.tag" },
		QueryType.AllTagsMatch,
		new string[] { "tag", "simple.tag", "other.tag", "enemy.undead.zombie" })]
	[InlineData(
		new string[] { "tag", "simple.tag", "other.tag" },
		QueryType.AllTagsMatch,
		new string[] { "simple", "other", "tag", "enemy" })]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment" },
		QueryType.AnyTagsMatch,
		new string[] { "enemy.undead.vampire", "color.red" })]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment" },
		QueryType.NoTagsMatch,
		new string[] { "simple", "other", "tag", "item" })]
	public void Container_does_not_match_query(string[] containerTagKeys, QueryType queryType, string[] queryTagKeys)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(containerTagKeys);

		TagQuery query = BuildSimpleTagQuery(queryType, queryTagKeys);

		tagContainer.MatchesQuery(query).Should().BeFalse();
	}

	[Theory]
	[Trait("MatchesQuery", "Empty container")]
	[InlineData(
		QueryType.NoTagsMatch,
		new string[] { "tag", "simple.tag", "other.tag", "enemy.undead.zombie" })]
	[InlineData(
		QueryType.NoTagsMatch,
		new string[] { "simple", "other", "tag", "enemy" })]
	[InlineData(
		QueryType.NoTagsMatch,
		new string[] { "enemy.undead.vampire", "color.red" })]
	[InlineData(
		QueryType.NoTagsMatch,
		new string[] { "simple", "other", "tag", "item" })]
	public void Empty_container_matches_query(QueryType queryType, string[] queryTagKeys)
	{
		var emptyContainer = new TagContainer(_tagsManager);

		TagQuery query = BuildSimpleTagQuery(queryType, queryTagKeys);

		emptyContainer.MatchesQuery(query).Should().BeTrue();
	}

	[Theory]
	[Trait("MatchesQuery", "Empty container")]
	[InlineData(
		QueryType.AnyTagsMatch,
		new string[] { "tag", "simple.tag", "other.tag", "enemy.undead.zombie" })]
	[InlineData(
		QueryType.AnyTagsMatch,
		new string[] { "simple", "other", "tag", "enemy" })]
	[InlineData(
		QueryType.AllTagsMatch,
		new string[] { "enemy.undead.vampire", "color.red" })]
	[InlineData(
		QueryType.AllTagsMatch,
		new string[] { "simple", "other", "tag", "item" })]
	public void Empty_container_does_not_match_query(QueryType queryType, string[] queryTagKeys)
	{
		var emptyContainer = new TagContainer(_tagsManager);

		TagQuery query = BuildSimpleTagQuery(queryType, queryTagKeys);

		emptyContainer.MatchesQuery(query).Should().BeFalse();
	}

	[Theory]
	[Trait("ToString", "String")]
	[InlineData(
		new string[] { "simple.tag" },
		"'simple.tag'")]
	[InlineData(
		new string[] { "tag", "simple.tag", "other.tag" },
		"'tag', 'simple.tag', 'other.tag'")]
	[InlineData(
		new string[] { "item.equipment.weapon.sword", "item.equipment.weapon", "item.equipment", "item" },
		"'item.equipment.weapon.sword', 'item.equipment.weapon', 'item.equipment', 'item'")]
	public void Container_ToString_returns_expected_string(string[] tagKeys, string output)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(tagKeys);

		tagContainer.ToString().Should().Be(output);
	}

	[Fact]
	[Trait("ToString", "Empty container")]
	public void EmptyContainer_ToString_returns_empty_string()
	{
		var emptyContainer = new TagContainer(_tagsManager);

		emptyContainer.ToString().Should().BeNullOrEmpty();
	}

	[Theory]
	[Trait("Equality", "Equals")]
	[InlineData(
		new string[] { "simple.tag" },
		new string[] { "simple.tag" })]
	[InlineData(
		new string[] { "tag", "simple.tag", "other.tag" },
		new string[] { "tag", "simple.tag", "other.tag" })]
	[InlineData(
		new string[] { "enemy.undead.zombie", "enemy.undead.skeleton", "enemy.undead.ghoul" },
		new string[] { "enemy.undead.zombie", "enemy.undead.skeleton", "enemy.undead.ghoul" })]
	public void Containers_are_equal(string[] tagKeysA, string[] tagKeysB)
	{
		TagContainer tagContainerA = _tagsManager.RequestTagContainer(tagKeysA);
		TagContainer tagContainerB = _tagsManager.RequestTagContainer(tagKeysB);

		tagContainerA.Should().BeEquivalentTo(tagContainerB);
		(tagContainerA == tagContainerB).Should().BeFalse();
		(tagContainerA != tagContainerB).Should().BeTrue();
		tagContainerA.Equals(tagContainerB).Should().BeTrue();

		object tagObject1 = tagContainerA;
		object tagObject2 = tagContainerB;

		tagObject1.Should().Be(tagObject2);
		tagObject1.Equals(tagContainerB).Should().BeTrue();
		tagContainerA.Equals(tagObject2).Should().BeTrue();
	}

	[Theory]
	[Trait("Equality", "Different")]
	[InlineData(
		new string[] { "simple.tag" },
		new string[] { "simple" })]
	[InlineData(
		new string[] { "simple.tag" },
		new string[] { "tag" })]
	[InlineData(
		new string[] { "tag", "simple.tag", "other.tag" },
		new string[] { "enemy.undead.zombie", "enemy.undead.skeleton", "enemy.undead.ghoul" })]
	[InlineData(
		new string[] { "enemy.undead.zombie", "enemy.undead.skeleton", "enemy.undead.ghoul" },
		new string[] { "tag", "simple.tag", "other.tag" })]
	public void Containers_are_not_equal(string[] tagKeysA, string[] tagKeysB)
	{
		TagContainer tagContainerA = _tagsManager.RequestTagContainer(tagKeysA);
		TagContainer tagContainerB = _tagsManager.RequestTagContainer(tagKeysB);

		tagContainerA.Should().NotBeEquivalentTo(tagContainerB);
		(tagContainerA == tagContainerB).Should().BeFalse();
		(tagContainerA != tagContainerB).Should().BeTrue();
		tagContainerA.Equals(tagContainerB).Should().BeFalse();

		object tagObject1 = tagContainerA;
		object tagObject2 = tagContainerB;

		tagObject1.Should().NotBe(tagObject2);
		tagObject1.Equals(tagContainerB).Should().BeFalse();
		tagContainerA.Equals(tagObject2).Should().BeFalse();
	}

	[Theory]
	[Trait("Equality", "Clone")]
	[InlineData((object)new string[] { "tag" })]
	[InlineData((object)new string[] { "tag", "simple.tag", "other.tag" })]
	[InlineData((object)new string[] { "enemy.undead.zombie", "enemy.undead.skeleton", "enemy.undead.ghoul" })]
	[InlineData((object)new string[] { "item.consumable.potion.health", "color.red" })]
	[InlineData((object)new string[] { "item.equipment.weapon.sword", "item.equipment.weapon" })]
	public void Container_created_from_another_container_should_be_equal(string[] tagKeys)
	{
		TagContainer tagContainer = _tagsManager.RequestTagContainer(tagKeys);
		var cloneContainer = new TagContainer(tagContainer);

		tagContainer.Should().BeEquivalentTo(cloneContainer);
		tagContainer.Equals(cloneContainer).Should().BeTrue();
	}

	[Fact]
	[Trait("Equality", "Empty container")]
	public void Empty_containers_is_equal_empty_container()
	{
		var emptyContainerA = new TagContainer(_tagsManager);
		var emptyContainerB = new TagContainer(_tagsManager);

		emptyContainerA.Should().BeEquivalentTo(emptyContainerB);
		emptyContainerA.Equals(emptyContainerB).Should().BeTrue();
	}

	private TagQuery BuildSimpleTagQuery(QueryType queryType, string[] queryTagKeys)
	{
		var query = new TagQuery();

		var queryExpression = new TagQueryExpression(_tagsManager);

		switch (queryType)
		{
			case QueryType.NoTagsMatch:
				queryExpression.NoTagsMatch();
				break;

			case QueryType.AnyTagsMatch:
				queryExpression.AnyTagsMatch();
				break;

			case QueryType.AllTagsMatch:
				queryExpression.AllTagsMatch();
				break;
		}

		foreach (var tagKey in queryTagKeys)
		{
			var tag = Tag.RequestTag(_tagsManager, tagKey);
			queryExpression.AddTag(tag);
		}

		query.Build(queryExpression);

		return query;
	}
}
