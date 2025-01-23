// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects;
using Gamesmiths.Forge.GameplayTags;

namespace Gamesmiths.Forge.Tests.Helpers;

public class TestEntity : IForgeEntity
{
	public TestAttributeSet PlayerAttributeSet { get; }

	public Attributes Attributes { get; }

	public Forge.Core.GameplayTags TagsManager { get; }

	public GameplayEffectsManager EffectsManager { get; }

	public TestEntity(GameplayTagsManager tagsManager)
	{
		PlayerAttributeSet = new TestAttributeSet();
		var originalTags = new GameplayTagContainer(
			tagsManager,
			[
				GameplayTag.RequestTag(tagsManager, "enemy.undead.zombie"),
				GameplayTag.RequestTag(tagsManager, "color.green")
			]);

		EffectsManager = new(this);
		Attributes = new(PlayerAttributeSet);
		TagsManager = new(originalTags);
	}
}
