// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects;
using Gamesmiths.Forge.GameplayTags;

namespace Gamesmiths.Forge.Tests.Helpers;

public class TestEntity : IForgeEntity
{
	public TestAttributeSet PlayerAttributeSet { get; }

	public GameplayEffectsManager GameplayEffectsManager { get; }

	public Attributes Attributes { get; }

	public GameplayTagContainer GameplayTags { get; }

	public TestEntity(GameplayTagsManager tagsManager)
	{
		PlayerAttributeSet = new TestAttributeSet();

		GameplayEffectsManager = new(this);
		Attributes = new Attributes(PlayerAttributeSet);
		GameplayTags = new(tagsManager);
	}
}
