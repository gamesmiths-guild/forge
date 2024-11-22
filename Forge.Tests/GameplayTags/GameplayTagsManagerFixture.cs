// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.GameplayTags;

namespace Gamesmiths.Forge.Tests.GameplayTags;

public class GameplayTagsManagerFixture
{
	public GameplayTagsManager GameplayTagsManager { get; }

	public GameplayTagsManagerFixture()
	{
		GameplayTagsManager = new GameplayTagsManager([
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
			"color.dark.blue"
		]);
	}
}
