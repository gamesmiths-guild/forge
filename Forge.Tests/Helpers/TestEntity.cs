// Copyright © 2025 Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayCues;
using Gamesmiths.Forge.GameplayEffects;
using Gamesmiths.Forge.GameplayTags;

namespace Gamesmiths.Forge.Tests.Helpers;

public class TestEntity : IForgeEntity
{
	public TestAttributeSet PlayerAttributeSet { get; }

	public Attributes Attributes { get; }

	public Forge.Core.GameplayTags GameplayTags { get; }

	public GameplayEffectsManager EffectsManager { get; }

	public TestEntity(GameplayTagsManager tagsManager, GameplayCuesManager cuesManager)
	{
		PlayerAttributeSet = new TestAttributeSet();
		var originalTags = new GameplayTagContainer(
			tagsManager,
			[
				GameplayTag.RequestTag(tagsManager, "enemy.undead.zombie"),
				GameplayTag.RequestTag(tagsManager, "color.green")
			]);

		EffectsManager = new(this, cuesManager);
		Attributes = new(PlayerAttributeSet);
		GameplayTags = new(originalTags);
	}
}
