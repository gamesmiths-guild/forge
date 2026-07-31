// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

public class SourceAttributeRequirementsComponentTests(TagsAndCuesFixture tagsAndCuesFixture)
	: IClassFixture<TagsAndCuesFixture>
{
	private const string HealthAttribute = "VitalAttributeSet.CurrentHealth";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Application", null)]
	public void Application_is_gated_on_the_sources_attributes_not_the_targets()
	{
		var target = new VitalTestEntity(_tagsManager, _cuesManager);
		var healthyAttacker = new VitalTestEntity(_tagsManager, _cuesManager);
		var woundedAttacker = new VitalTestEntity(_tagsManager, _cuesManager);

		ChangeHealth(woundedAttacker, -60);

		// A desperate strike that only lands while the *attacker* is at or below 50 health.
		AttributeRequirement[] requirements = [new AttributeRequirement(HealthAttribute, MaxValue: 50)];

		target.EffectsManager
			.ApplyEffect(CreateSourceGatedEffect(healthyAttacker, applicationRequirements: requirements))
			.Should().BeNull();

		target.EffectsManager
			.ApplyEffect(CreateSourceGatedEffect(woundedAttacker, applicationRequirements: requirements))
			.Should().NotBeNull();
	}

	[Fact]
	[Trait("Application", null)]
	public void The_targets_own_attributes_do_not_satisfy_a_source_requirement()
	{
		var target = new VitalTestEntity(_tagsManager, _cuesManager);
		var attacker = new VitalTestEntity(_tagsManager, _cuesManager);

		// The target is wounded; the attacker is not.
		ChangeHealth(target, -60);

		target.EffectsManager
			.ApplyEffect(CreateSourceGatedEffect(
				attacker,
				applicationRequirements: [new AttributeRequirement(HealthAttribute, MaxValue: 50)]))
			.Should().BeNull();
	}

	[Fact]
	[Trait("Ongoing", null)]
	public void Ongoing_source_attributes_toggle_inhibition()
	{
		var target = new VitalTestEntity(_tagsManager, _cuesManager);
		var caster = new VitalTestEntity(_tagsManager, _cuesManager);

		// A channelled beam that suppresses itself while its caster is at or below 50 health.
		ActiveEffectHandle? handle = target.EffectsManager.ApplyEffect(CreateSourceGatedEffect(
			caster,
			ongoingRequirements: [new AttributeRequirement(HealthAttribute, MinValue: 51)]));

		handle.Should().NotBeNull();
		handle!.IsInhibited.Should().BeFalse();

		ChangeHealth(caster, -60);
		handle.IsInhibited.Should().BeTrue();

		ChangeHealth(caster, 60);
		handle.IsInhibited.Should().BeFalse();
	}

	[Fact]
	[Trait("Ongoing", null)]
	public void The_targets_attribute_changes_do_not_toggle_a_source_requirement()
	{
		var target = new VitalTestEntity(_tagsManager, _cuesManager);
		var caster = new VitalTestEntity(_tagsManager, _cuesManager);

		ActiveEffectHandle? handle = target.EffectsManager.ApplyEffect(CreateSourceGatedEffect(
			caster,
			ongoingRequirements: [new AttributeRequirement(HealthAttribute, MinValue: 51)]));

		handle.Should().NotBeNull();
		handle!.IsInhibited.Should().BeFalse();

		// Wounding the target must not affect a requirement that reads the source.
		ChangeHealth(target, -60);
		handle.IsInhibited.Should().BeFalse();
	}

	[Fact]
	[Trait("Removal", null)]
	public void Removal_source_attributes_force_removal()
	{
		var target = new VitalTestEntity(_tagsManager, _cuesManager);
		var caster = new VitalTestEntity(_tagsManager, _cuesManager);

		// The link breaks once the caster drops to 25 health or below.
		ActiveEffectHandle? handle = target.EffectsManager.ApplyEffect(CreateSourceGatedEffect(
			caster,
			removalRequirements: [new AttributeRequirement(HealthAttribute, MaxValue: 25)]));

		handle.Should().NotBeNull();
		handle!.IsValid.Should().BeTrue();

		ChangeHealth(caster, -50);
		handle.IsValid.Should().BeTrue();

		ChangeHealth(caster, -30);
		handle.IsValid.Should().BeFalse();
	}

	[Fact]
	[Trait("OwnershipEntity", null)]
	public void Owner_mode_reads_the_owner_instead_of_the_source()
	{
		var target = new VitalTestEntity(_tagsManager, _cuesManager);
		var owner = new VitalTestEntity(_tagsManager, _cuesManager);
		var source = new VitalTestEntity(_tagsManager, _cuesManager);

		ChangeHealth(owner, -60);

		AttributeRequirement[] requirements = [new AttributeRequirement(HealthAttribute, MaxValue: 50)];

		// Reading the source, who is at full health, denies the application.
		target.EffectsManager
			.ApplyEffect(CreateEffect(new EffectOwnership(owner, source), applicationRequirements: requirements))
			.Should().BeNull();

		// Reading the owner, who is wounded, allows it.
		target.EffectsManager
			.ApplyEffect(CreateEffect(
				new EffectOwnership(owner, source),
				applicationRequirements: requirements,
				ownershipEntity: OwnershipEntity.Owner))
			.Should().NotBeNull();
	}

	[Fact]
	[Trait("NullSource", null)]
	public void A_null_source_satisfies_nothing()
	{
		var target = new VitalTestEntity(_tagsManager, _cuesManager);

		target.EffectsManager
			.ApplyEffect(CreateEffect(
				new EffectOwnership(null, null),
				applicationRequirements: [new AttributeRequirement(HealthAttribute, MinValue: 0)]))
			.Should().BeNull();
	}

	[Fact]
	[Trait("NullSource", null)]
	public void A_null_source_leaves_an_empty_bucket_alone()
	{
		var target = new VitalTestEntity(_tagsManager, _cuesManager);

		// Nothing is required of the source, so a missing one is not an obstacle.
		target.EffectsManager
			.ApplyEffect(CreateEffect(new EffectOwnership(null, null)))
			.Should().NotBeNull();
	}

	private static void ChangeHealth(VitalTestEntity entity, int amount)
	{
		var damageEffectData = new EffectData(
			"Attribute Change",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					HealthAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(amount)))
			]);

		entity.EffectsManager.ApplyEffect(
			new Effect(damageEffectData, new EffectOwnership(entity, entity)));
	}

	private static Effect CreateEffect(
		EffectOwnership ownership,
		AttributeRequirement[]? applicationRequirements = null,
		AttributeRequirement[]? removalRequirements = null,
		AttributeRequirement[]? ongoingRequirements = null,
		OwnershipEntity ownershipEntity = OwnershipEntity.Source)
	{
		var effectData = new EffectData(
			"Source Gated Effect",
			new DurationData(DurationType.Infinite),
			effectComponents:
			[
				new SourceAttributeRequirementsEffectComponent(
					applicationRequirements,
					removalRequirements,
					ongoingRequirements,
					ownershipEntity)
			]);

		return new Effect(effectData, ownership);
	}

	private static Effect CreateSourceGatedEffect(
		IForgeEntity source,
		AttributeRequirement[]? applicationRequirements = null,
		AttributeRequirement[]? removalRequirements = null,
		AttributeRequirement[]? ongoingRequirements = null)
	{
		return CreateEffect(
			new EffectOwnership(source, source),
			applicationRequirements,
			removalRequirements,
			ongoingRequirements);
	}
}
