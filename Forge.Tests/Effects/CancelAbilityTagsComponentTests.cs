// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

public class CancelAbilityTagsComponentTests(TagsAndCuesFixture tagsAndCuesFixture)
	: IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("OnApplication", null)]
	public void Instant_effect_cancels_abilities_matching_the_with_tags()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);

		AbilityHandle red = ActivateAbility(entity, "Red", ["color.red"]);
		AbilityHandle green = ActivateAbility(entity, "Green", ["color.green"]);

		entity.EffectsManager.ApplyEffect(CreateCancelingEffect(
			entity,
			withTags: TagsOf("color.red"),
			withoutTags: null));

		red.IsActive.Should().BeFalse();
		green.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("OnApplication", null)]
	public void Abilities_carrying_the_without_tags_are_spared()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);

		AbilityHandle red = ActivateAbility(entity, "Red", ["color.red"]);
		AbilityHandle redAndBlue = ActivateAbility(entity, "RedAndBlue", ["color.red", "color.blue"]);
		AbilityHandle green = ActivateAbility(entity, "Green", ["color.green"]);

		entity.EffectsManager.ApplyEffect(CreateCancelingEffect(
			entity,
			withTags: TagsOf("color.red"),
			withoutTags: TagsOf("color.blue")));

		red.IsActive.Should().BeFalse();
		redAndBlue.IsActive.Should().BeTrue();
		green.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("OnApplication", null)]
	public void Without_tags_alone_cancels_everything_else()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);

		AbilityHandle red = ActivateAbility(entity, "Red", ["color.red"]);
		AbilityHandle blue = ActivateAbility(entity, "Blue", ["color.blue"]);

		entity.EffectsManager.ApplyEffect(CreateCancelingEffect(
			entity,
			withTags: null,
			withoutTags: TagsOf("color.blue")));

		red.IsActive.Should().BeFalse();
		blue.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("OnApplication", null)]
	public void Duration_effect_cancels_once_on_application_and_not_on_updates()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);

		AbilityHandle red = ActivateAbility(entity, "Red", ["color.red"]);

		entity.EffectsManager.ApplyEffect(CreateCancelingEffect(
			entity,
			withTags: TagsOf("color.red"),
			withoutTags: null,
			duration: 10f));

		red.IsActive.Should().BeFalse();

		// Re-activating mid-effect must stick: OnApplication does not fire again on update.
		red.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);

		entity.EffectsManager.UpdateEffects(5f);

		red.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("OnExecution", null)]
	public void Periodic_effect_cancels_on_each_execution()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);

		AbilityHandle red = ActivateAbility(entity, "Red", ["color.red"]);

		entity.EffectsManager.ApplyEffect(CreateCancelingEffect(
			entity,
			withTags: TagsOf("color.red"),
			withoutTags: null,
			duration: 10f,
			policy: CancelAbilityTagsPolicy.OnExecution,
			period: 1f));

		// ExecuteOnApplication is on, so the first execution happens right away.
		red.IsActive.Should().BeFalse();

		red.Activate(out _).Should().BeTrue();
		red.IsActive.Should().BeTrue();

		// No execution yet, so the re-activated ability survives.
		entity.EffectsManager.UpdateEffects(0.5f);
		red.IsActive.Should().BeTrue();

		// The next tick cancels it again.
		entity.EffectsManager.UpdateEffects(0.5f);
		red.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("OnExecution", null)]
	public void On_application_policy_does_not_fire_on_periodic_executions()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);

		AbilityHandle red = ActivateAbility(entity, "Red", ["color.red"]);

		entity.EffectsManager.ApplyEffect(CreateCancelingEffect(
			entity,
			withTags: TagsOf("color.red"),
			withoutTags: null,
			duration: 10f,
			policy: CancelAbilityTagsPolicy.OnApplication,
			period: 1f));

		red.IsActive.Should().BeFalse();

		red.Activate(out _).Should().BeTrue();

		entity.EffectsManager.UpdateEffects(3f);

		red.IsActive.Should().BeTrue();
	}

	// Both filters empty would mean "no filter" to EntityAbilities.CancelAbilities, which cancels every active
	// ability. The component treats that as a misconfiguration and cancels nothing instead.
	[Fact]
	[Trait("NoFilter", null)]
	public void Component_without_any_filter_cancels_nothing()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);

		AbilityHandle red = ActivateAbility(entity, "Red", ["color.red"]);
		AbilityHandle untagged = ActivateAbility(entity, "Untagged", []);

		entity.EffectsManager.ApplyEffect(CreateCancelingEffect(entity, withTags: null, withoutTags: null));

		red.IsActive.Should().BeTrue();
		untagged.IsActive.Should().BeTrue();

		entity.EffectsManager.ApplyEffect(CreateCancelingEffect(
			entity,
			withTags: new TagContainer(_tagsManager),
			withoutTags: new TagContainer(_tagsManager)));

		red.IsActive.Should().BeTrue();
		untagged.IsActive.Should().BeTrue();
	}

	private static Effect CreateCancelingEffect(
		TestEntity entity,
		TagContainer? withTags,
		TagContainer? withoutTags,
		float? duration = null,
		CancelAbilityTagsPolicy policy = CancelAbilityTagsPolicy.OnApplication,
		float? period = null)
	{
		DurationData durationData = duration.HasValue
			? new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(duration.Value)))
			: new DurationData(DurationType.Instant);

		PeriodicData? periodicData = period.HasValue
			? new PeriodicData(new ScalableFloat(period.Value), true, PeriodInhibitionRemovedPolicy.ResetPeriod)
			: null;

		var effectData = new EffectData(
			"Canceling Effect",
			durationData,
			periodicData: periodicData,
			effectComponents: [new CancelAbilityTagsEffectComponent(withTags, withoutTags, policy)]);

		return new Effect(effectData, new EffectOwnership(entity, entity));
	}

	private TagContainer TagsOf(params string[] tagKeys)
	{
		return new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, tagKeys));
	}

	// Grants a cost-free, cooldown-free ability carrying the given ability tags and activates it, so the cancellation
	// tests only have to reason about the tag filtering.
	private AbilityHandle ActivateAbility(TestEntity entity, string abilityName, string[] abilityTagKeys)
	{
		var abilityData = new AbilityData(
			abilityName,
			abilityTags: abilityTagKeys.Length > 0 ? TagsOf(abilityTagKeys) : null);

		AbilityHandle handle = entity.Abilities.GrantAbilityPermanently(abilityData, 1, LevelComparison.None, null);

		handle.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		handle.IsActive.Should().BeTrue();

		return handle;
	}
}
