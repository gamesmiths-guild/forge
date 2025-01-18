// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects;
using Gamesmiths.Forge.GameplayTags;
using Attribute = Gamesmiths.Forge.Attributes.Attribute;

namespace Gamesmiths.Forge.Tests.Helpers;

public class TestEntity : IForgeEntity
{
	public TestAttributeSet PlayerAttributeSet { get; }

	public GameplayEffectsManager GameplayEffectsManager { get; }

	public List<AttributeSet> AttributeSets { get; }

	public Dictionary<StringKey, Attribute> Attributes { get; }

	public GameplayTagContainer GameplayTags { get; }

	public TestEntity(GameplayTagsManager tagsManager)
	{
		GameplayEffectsManager = new(this);
		AttributeSets = [];
		Attributes = [];
		GameplayTags = new(tagsManager);

		PlayerAttributeSet = new TestAttributeSet();
		((IForgeEntity)this).AddAttributeSet(PlayerAttributeSet);
	}
}
