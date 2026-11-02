// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Benchmarks;

internal sealed class BenchmarkEntity : IForgeEntity
{
	public BenchmarkAttributeSet AttributeSet { get; }

	public EntityAttributes Attributes { get; }

	public EntityTags Tags { get; }

	public EffectsManager EffectsManager { get; }

	public CuesManager CuesManager { get; }

	public EntityAbilities Abilities { get; }

	public EventManager Events { get; }

	public Variables SharedVariables { get; } = new Variables();

	public DeterministicRandom? Random { get; set; }

	public EntityInput? Input { get; set; }

	public IEntityMover? Mover { get; set; }

	public IEntityStateHook? StateHook { get; set; }

	public BenchmarkEntity(TagsManager tagsManager, CuesManager cuesManager, int seed)
	{
		AttributeSet = new BenchmarkAttributeSet();
		var baseTags = new TagContainer(tagsManager, [Tag.RequestTag(tagsManager, "enemy.undead")]);

		EffectsManager = new(this, cuesManager);
		CuesManager = cuesManager;
		Attributes = new(this, AttributeSet);
		Tags = new(baseTags);
		Abilities = new(this);
		Events = new();
		Random = new DeterministicRandom((ulong)seed);
	}
}
