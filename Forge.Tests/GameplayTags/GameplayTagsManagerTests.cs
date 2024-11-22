// Copyright © 2024 Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.GameplayTags;

namespace Gamesmiths.Forge.Tests.GameplayTags;

public class GameplayTagsManagerTests
{
	[Theory]
	[Trait("Initialization", "Valid tags")]
	[InlineData(
		new string[]
		{
			"tag",
		},
		1)]
	[InlineData(
		new string[]
		{
			"tag",
			"simple",
			"simple.tag",
			"other.tag",
		},
		5)]
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
		8)]
	[InlineData(
		new string[]
		{
			"tag",
			"simple",
			"simple.tag",
			"other.tag",
			"enemy.undead.zombie",
			"enemy.undead.skeleton",
			"enemy.undead.ghoul",
			"enemy.undead.vampire",
			"enemy.beast.wolf",
			"enemy.beast.wolf.gray",
			"enemy.beast.boar",
			"enemy.humanoid.goblin",
			"enemy.humanoid.orc",
			"item.consumable.potion.health",
			"item.consumable.potion.mana",
			"item.consumable.potion.stamina",
			"item.consumable.food.apple",
			"item.consumable.food.bread",
			"item.equipment.weapon.sword",
			"item.equipment.weapon.dagger",
			"item.equipment.weapon.axe",
			"color.red",
			"color.green",
			"color.blue",
			"color.dark.red",
			"color.dark.green",
			"color.dark.blue",
		},
		40)]
	[InlineData(new string[] { }, 0)]
	public void Tags_manager_initializes_with_correct_values(string[] managerTags, int nodeCount)
	{
		var tagsManager = new GameplayTagsManager(managerTags);

		tagsManager.NodesCount.Should().Be(nodeCount);
		tagsManager.InvalidTagNetIndex.Should().Be((ushort)(nodeCount + 1));
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
		var tagsManager = new GameplayTagsManager(managerTags);

		tagsManager.ToString().Should().Be(output);
	}
}
