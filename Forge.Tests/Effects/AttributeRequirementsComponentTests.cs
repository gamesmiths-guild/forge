// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

public class AttributeRequirementsComponentTests(TagsAndCuesFixture tagsAndCuesFixture)
	: IClassFixture<TagsAndCuesFixture>
{
	private const string HealthAttribute = "VitalAttributeSet.CurrentHealth";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Application", null)]
	public void Application_is_denied_while_the_attribute_is_outside_the_bounds()
	{
		var entity = new VitalTestEntity(_tagsManager, _cuesManager);

		// "Execute" only lands on a target at or below 50 health.
		Effect gatedEffect = CreateGatedEffect(
			entity,
			applicationRequirements: [new AttributeRequirement(HealthAttribute, MaxValue: 50)]);

		entity.EffectsManager.ApplyEffect(gatedEffect).Should().BeNull();

		ChangeHealth(entity, -60);
		entity.VitalAttributeSet.CurrentHealth.CurrentValue.Should().Be(40);

		entity.EffectsManager.ApplyEffect(gatedEffect).Should().NotBeNull();
	}

	[Fact]
	[Trait("Application", null)]
	public void Percent_of_max_bounds_compare_against_the_attributes_max()
	{
		var entity = new VitalTestEntity(_tagsManager, _cuesManager);

		// Max health is 100, so 50% is 50.
		Effect gatedEffect = CreateGatedEffect(
			entity,
			applicationRequirements:
			[
				new AttributeRequirement(
					HealthAttribute,
					MaxValue: 50,
					ThresholdType: AttributeThresholdType.PercentOfMax)
			]);

		entity.EffectsManager.ApplyEffect(gatedEffect).Should().BeNull();

		ChangeHealth(entity, -51);
		entity.VitalAttributeSet.CurrentHealth.CurrentValue.Should().Be(49);

		entity.EffectsManager.ApplyEffect(gatedEffect).Should().NotBeNull();
	}

	[Fact]
	[Trait("Application", null)]
	public void A_requirement_naming_a_missing_attribute_is_never_met()
	{
		var entity = new VitalTestEntity(_tagsManager, _cuesManager);

		Effect gatedEffect = CreateGatedEffect(
			entity,
			applicationRequirements: [new AttributeRequirement("VitalAttributeSet.NotAnAttribute", MinValue: 0)]);

		entity.EffectsManager.ApplyEffect(gatedEffect).Should().BeNull();
	}

	[Fact]
	[Trait("Application", null)]
	public void Requirements_within_a_bucket_are_and_combined()
	{
		var entity = new VitalTestEntity(_tagsManager, _cuesManager);

		// Health at or below 50, and vitality at or above 10.
		Effect gatedEffect = CreateGatedEffect(
			entity,
			applicationRequirements:
			[
				new AttributeRequirement(HealthAttribute, MaxValue: 50),
				new AttributeRequirement("VitalAttributeSet.Vitality", MinValue: 10)
			]);

		ChangeHealth(entity, -60);
		entity.EffectsManager.ApplyEffect(gatedEffect).Should().NotBeNull();

		// Dropping vitality below 10 breaks the second requirement, so a further application is denied.
		ChangeAttribute(entity, "VitalAttributeSet.Vitality", -5);
		entity.EffectsManager.ApplyEffect(gatedEffect).Should().BeNull();
	}

	[Fact]
	[Trait("Ongoing", null)]
	public void Ongoing_requirements_toggle_inhibition_as_the_attribute_crosses_the_threshold()
	{
		var entity = new VitalTestEntity(_tagsManager, _cuesManager);

		// A "Bloodlust" aura that only works while health is at or below 50.
		Effect auraEffect = CreateGatedEffect(
			entity,
			ongoingRequirements: [new AttributeRequirement(HealthAttribute, MaxValue: 50)]);

		ActiveEffectHandle? handle = entity.EffectsManager.ApplyEffect(auraEffect);

		handle.Should().NotBeNull();
		handle!.IsInhibited.Should().BeTrue();

		ChangeHealth(entity, -60);
		handle.IsInhibited.Should().BeFalse();

		ChangeHealth(entity, 60);
		handle.IsInhibited.Should().BeTrue();
	}

	[Fact]
	[Trait("Removal", null)]
	public void Removal_requirements_force_removal_when_met()
	{
		var entity = new VitalTestEntity(_tagsManager, _cuesManager);

		// A shield that shatters once health drops to 25 or below.
		Effect shieldEffect = CreateGatedEffect(
			entity,
			removalRequirements: [new AttributeRequirement(HealthAttribute, MaxValue: 25)]);

		ActiveEffectHandle? handle = entity.EffectsManager.ApplyEffect(shieldEffect);

		handle.Should().NotBeNull();
		handle!.IsValid.Should().BeTrue();

		ChangeHealth(entity, -50);
		handle.IsValid.Should().BeTrue();

		ChangeHealth(entity, -30);
		handle.IsValid.Should().BeFalse();
	}

	[Fact]
	[Trait("Removal", null)]
	public void Application_is_denied_when_the_removal_requirements_are_already_met()
	{
		var entity = new VitalTestEntity(_tagsManager, _cuesManager);

		Effect shieldEffect = CreateGatedEffect(
			entity,
			removalRequirements: [new AttributeRequirement(HealthAttribute, MaxValue: 25)]);

		ChangeHealth(entity, -80);
		entity.VitalAttributeSet.CurrentHealth.CurrentValue.Should().Be(20);

		entity.EffectsManager.ApplyEffect(shieldEffect).Should().BeNull();
	}

	[Fact]
	[Trait("Removal", null)]
	public void Attribute_changes_after_removal_no_longer_affect_the_effect()
	{
		var entity = new VitalTestEntity(_tagsManager, _cuesManager);

		Effect auraEffect = CreateGatedEffect(
			entity,
			ongoingRequirements: [new AttributeRequirement(HealthAttribute, MaxValue: 50)]);

		ActiveEffectHandle? handle = entity.EffectsManager.ApplyEffect(auraEffect);
		handle.Should().NotBeNull();

		entity.EffectsManager.RemoveEffect(handle!, true);
		handle!.IsValid.Should().BeFalse();

		// The component must have unsubscribed; this would otherwise touch a freed handle.
		ChangeHealth(entity, -60);
		ChangeHealth(entity, 60);
	}

	private static void ChangeAttribute(VitalTestEntity entity, string attribute, int amount)
	{
		var damageEffectData = new EffectData(
			"Attribute Change",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					attribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(amount)))
			]);

		entity.EffectsManager.ApplyEffect(
			new Effect(damageEffectData, new EffectOwnership(entity, entity)));
	}

	private static void ChangeHealth(VitalTestEntity entity, int amount)
	{
		ChangeAttribute(entity, HealthAttribute, amount);
	}

	// An infinite, modifier-free effect whose only job is to carry the requirements component.
	private static Effect CreateGatedEffect(
		VitalTestEntity entity,
		AttributeRequirement[]? applicationRequirements = null,
		AttributeRequirement[]? removalRequirements = null,
		AttributeRequirement[]? ongoingRequirements = null)
	{
		var effectData = new EffectData(
			"Gated Effect",
			new DurationData(DurationType.Infinite),
			effectComponents:
			[
				new AttributeRequirementsEffectComponent(
					applicationRequirements,
					removalRequirements,
					ongoingRequirements)
			]);

		return new Effect(effectData, new EffectOwnership(entity, entity));
	}
}
