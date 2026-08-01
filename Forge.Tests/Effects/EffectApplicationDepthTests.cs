// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

public class EffectApplicationDepthTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	// Mirrors EffectsManager.MaxApplicationDepth: the chain is cut once an application nests this deep.
	private const int MaxApplicationDepth = 16;

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("DepthGuard", null)]
	public void A_cycle_of_effects_applying_each_other_is_cut_off()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		Action act = () => ApplyCycle(target);

		// Without the guard this recurses until the stack gives out.
		act.Should().NotThrow();
		target.EffectsManager.GetActiveEffects().Should().HaveCount(MaxApplicationDepth);
	}

	[Fact]
	[Trait("DepthGuard", null)]
	public void The_cut_off_is_reported_when_validation_is_enabled()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		Validation.Enabled = true;

		try
		{
			Action act = () => ApplyCycle(target);

			act.Should().Throw<ValidationException>();
		}
		finally
		{
			Validation.Enabled = false;
		}
	}

	[Fact]
	[Trait("DepthGuard", null)]
	public void A_chain_that_ends_on_its_own_is_left_alone()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		EffectData lastData = CreateEffectData("Last", new ApplyOtherEffectComponent());

		target.EffectsManager.ApplyEffect(new Effect(
			CreateEffectData("First", new ApplyOtherEffectComponent { Other = lastData }),
			new EffectOwnership(target, target)));

		target.EffectsManager.GetActiveEffects().Should().HaveCount(2);
	}

	[Fact]
	[Trait("DepthGuard", null)]
	public void A_cycle_bouncing_between_two_entities_is_cut_off_as_well()
	{
		var first = new TestEntity(_tagsManager, _cuesManager);
		var second = new TestEntity(_tagsManager, _cuesManager);

		var pingComponent = new ApplyOtherEffectComponent { ApplyTo = second };
		var pongComponent = new ApplyOtherEffectComponent { ApplyTo = first };

		EffectData pingData = CreateEffectData("Ping", pingComponent);

		pingComponent.Other = CreateEffectData("Pong", pongComponent);
		pongComponent.Other = pingData;

		Action act = () => first.EffectsManager.ApplyEffect(
			new Effect(pingData, new EffectOwnership(first, first)));

		// The counter lives on each manager, but a cycle by definition comes back to one — and it comes back while the
		// first call is still on the stack, so the decrement in its finally has not run and the depth keeps climbing.
		act.Should().NotThrow();

		first.EffectsManager.GetActiveEffects().Should().HaveCount(MaxApplicationDepth);
		second.EffectsManager.GetActiveEffects().Should().HaveCount(MaxApplicationDepth);
	}

	private void ApplyCycle(TestEntity target)
	{
		// EffectData is immutable, so neither effect can name the other at construction. The components are wired up
		// afterwards, which is the only way to build a cycle at all — and the reason one is a configuration bug rather
		// than something a designer stumbles into.
		var pingComponent = new ApplyOtherEffectComponent();
		var pongComponent = new ApplyOtherEffectComponent();

		EffectData pingData = CreateEffectData("Ping", pingComponent);

		pingComponent.Other = CreateEffectData("Pong", pongComponent);
		pongComponent.Other = pingData;

		target.EffectsManager.ApplyEffect(new Effect(pingData, new EffectOwnership(target, target)));
	}

	private EffectData CreateEffectData(string name, IEffectComponent component)
	{
		return new EffectData(
			name,
			new DurationData(DurationType.Infinite),
			effectComponents: [component],
			effectTags: new TagContainer(_tagsManager));
	}

	private sealed class ApplyOtherEffectComponent : IEffectComponent
	{
		public EffectData? Other { get; set; }

		/// <summary>
		/// Gets or sets the entity to apply to, or <see langword="null"/> to apply back to the same one. Stands in for
		/// a component that can reach an entity of its own choosing, which is the only way to build a cascade that
		/// crosses managers.
		/// </summary>
		public IForgeEntity? ApplyTo { get; set; }

		public void OnEffectApplied(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
		{
			if (Other.HasValue)
			{
				(ApplyTo ?? target).EffectsManager.ApplyEffect(
					new Effect(Other.Value, effectEvaluatedData.Effect.Ownership));
			}
		}
	}
}
