// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Tests.Helpers;

public class VitalTestEntity : IForgeEntity
{
	public VitalAttributeSet VitalAttributeSet { get; }

	public EntityAttributes Attributes { get; }

	public EntityTags Tags { get; }

	public EffectsManager EffectsManager { get; }

	public EntityAbilities Abilities { get; }

	public EventManager Events { get; }

	public Variables SharedVariables { get; } = new Variables();

	public VitalTestEntity(TagsManager tagsManager, CuesManager cuesManager)
	{
		VitalAttributeSet = new VitalAttributeSet();
		var originalTags = new TagContainer(tagsManager, []);

		EffectsManager = new(this, cuesManager);
		Attributes = new(VitalAttributeSet);
		Tags = new(originalTags);
		Abilities = new(this);
		Events = new();
	}
}
