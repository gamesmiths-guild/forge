// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Tests.Helpers;

public class TestEntity : IForgeEntity
{
	public TestAttributeSet PlayerAttributeSet { get; }

	public EntityAttributes Attributes { get; }

	public EntityTags Tags { get; }

	public EffectsManager EffectsManager { get; }

	public TestEntity(TagsManager tagsManager, CuesManager cuesManager)
	{
		PlayerAttributeSet = new TestAttributeSet();
		var originalTags = new TagContainer(
			tagsManager,
			[
				Tag.RequestTag(tagsManager, "enemy.undead.zombie"),
				Tag.RequestTag(tagsManager, "color.green")
			]);

		EffectsManager = new(this, cuesManager);
		Attributes = new(PlayerAttributeSet);
		Tags = new(originalTags);
	}
}
