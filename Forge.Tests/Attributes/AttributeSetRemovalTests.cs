// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Attributes;

public class AttributeSetRemovalTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string KeptAttribute = "TestAttributeSet.Attribute1000";
	private const string DepartingAttribute = "VitalAttributeSet.CurrentHealth";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Removal", null)]
	public void Removing_a_set_that_is_not_present_reports_failure()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);

		entity.Attributes.RemoveAttributeSet(new VitalAttributeSet()).Should().BeFalse();
		entity.Attributes.AttributeSets.Should().ContainSingle();
	}

	[Fact]
	[Trait("Removal", null)]
	public void Removing_a_set_detaches_its_attributes_and_keeps_the_others()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var vitalSet = new VitalAttributeSet();

		entity.Attributes.AddAttributeSet(vitalSet);
		entity.Attributes.AttributeSets.Should().HaveCount(2);
		entity.Attributes.TryGetAttribute(DepartingAttribute, out _).Should().BeTrue();

		entity.Attributes.RemoveAttributeSet(vitalSet).Should().BeTrue();

		entity.Attributes.AttributeSets.Should().ContainSingle();
		entity.Attributes.TryGetAttribute(DepartingAttribute, out _).Should().BeFalse();
		entity.Attributes.TryGetAttribute(KeptAttribute, out _).Should().BeTrue();
	}

	[Fact]
	[Trait("Removal", null)]
	public void An_effect_keeps_the_modifiers_for_the_attributes_that_remain()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var vitalSet = new VitalAttributeSet();

		entity.Attributes.AddAttributeSet(vitalSet);
		entity.EffectsManager.ApplyEffect(CreateCrossSetEffect(entity)).Should().NotBeNull();

		entity.Attributes[KeptAttribute].CurrentValue.Should().Be(10);
		entity.Attributes[DepartingAttribute].CurrentValue.Should().Be(90);

		entity.Attributes.RemoveAttributeSet(vitalSet).Should().BeTrue();

		// The effect survives with its remaining modifier still applied.
		entity.EffectsManager.GetActiveEffects().Should().ContainSingle();
		entity.Attributes[KeptAttribute].CurrentValue.Should().Be(10);
	}

	[Fact]
	[Trait("Removal", null)]
	public void Re_adding_a_set_reapplies_the_modifier_exactly_once()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var vitalSet = new VitalAttributeSet();

		entity.Attributes.AddAttributeSet(vitalSet);
		entity.EffectsManager.ApplyEffect(CreateCrossSetEffect(entity)).Should().NotBeNull();

		entity.Attributes[DepartingAttribute].CurrentValue.Should().Be(90);

		entity.Attributes.RemoveAttributeSet(vitalSet).Should().BeTrue();
		entity.Attributes.AddAttributeSet(vitalSet);

		// 90 and not 80: the modifier was unwound on the way out, so coming back applies it once, not twice.
		entity.Attributes[DepartingAttribute].CurrentValue.Should().Be(90);
	}

	[Fact]
	[Trait("Removal", null)]
	public void A_set_removed_while_an_effect_is_active_comes_back_clean_once_the_effect_is_gone()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var vitalSet = new VitalAttributeSet();

		entity.Attributes.AddAttributeSet(vitalSet);

		ActiveEffectHandle? handle = entity.EffectsManager.ApplyEffect(CreateCrossSetEffect(entity));
		handle.Should().NotBeNull();
		entity.Attributes[DepartingAttribute].CurrentValue.Should().Be(90);

		entity.Attributes.RemoveAttributeSet(vitalSet).Should().BeTrue();

		// Removing the effect while the set is detached must not leave anything behind on the departed attribute.
		entity.EffectsManager.RemoveEffect(handle!);

		entity.Attributes.AddAttributeSet(vitalSet);

		entity.Attributes[DepartingAttribute].CurrentValue.Should().Be(100);
		entity.Attributes[DepartingAttribute].Modifier.Should().Be(0);
	}

	[Fact]
	[Trait("Removal", null)]
	public void Adding_a_set_mid_life_lets_an_active_effect_start_modifying_it()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);

		// Applied while the entity does not have the set at all, so the modifier is skipped.
		entity.EffectsManager.ApplyEffect(CreateCrossSetEffect(entity)).Should().NotBeNull();
		entity.Attributes[KeptAttribute].CurrentValue.Should().Be(10);
		entity.Attributes.TryGetAttribute(DepartingAttribute, out _).Should().BeFalse();

		entity.Attributes.AddAttributeSet(new VitalAttributeSet());

		// The active effect picks the new attribute up instead of waiting for something else to re-evaluate it.
		entity.Attributes[DepartingAttribute].CurrentValue.Should().Be(90);
	}

	[Fact]
	[Trait("Removal", null)]
	public void The_last_change_to_a_departing_attribute_still_reaches_its_listeners()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var vitalSet = new VitalAttributeSet();

		entity.Attributes.AddAttributeSet(vitalSet);

		int observed = 0;
		entity.Attributes[DepartingAttribute].OnValueChanged += (_, change) => observed += change;

		entity.EffectsManager.ApplyEffect(CreateCrossSetEffect(entity)).Should().NotBeNull();
		observed.Should().Be(-10);

		entity.Attributes.RemoveAttributeSet(vitalSet).Should().BeTrue();

		// Unwinding the modifier on the way out is itself a change, and it has to be flushed before the attribute is
		// detached rather than left pending on an object nothing enumerates any more.
		observed.Should().Be(0);
	}

	[Fact]
	[Trait("Removal", null)]
	public void Removing_a_set_raises_the_membership_events()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var vitalSet = new VitalAttributeSet();

		var added = new List<AttributeSet>();
		var removed = new List<AttributeSet>();

		entity.Attributes.OnAttributeSetAdded += added.Add;
		entity.Attributes.OnAttributeSetRemoved += removed.Add;

		entity.Attributes.AddAttributeSet(vitalSet);
		entity.Attributes.RemoveAttributeSet(vitalSet).Should().BeTrue();

		added.Should().ContainSingle().Which.Should().BeSameAs(vitalSet);
		removed.Should().ContainSingle().Which.Should().BeSameAs(vitalSet);
	}

	[Fact]
	[Trait("Removal", null)]
	public void An_ongoing_requirement_on_a_departing_attribute_inhibits_its_effect()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var vitalSet = new VitalAttributeSet();

		entity.Attributes.AddAttributeSet(vitalSet);

		var effectData = new EffectData(
			"Gated Buff",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					KeptAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10)))
			],
			effectComponents:
			[
				new AttributeRequirementsEffectComponent(
					ongoingRequirements: [new AttributeRequirement(DepartingAttribute, MinValue: 1)])
			]);

		ActiveEffectHandle? handle = entity.EffectsManager.ApplyEffect(
			new Effect(effectData, new EffectOwnership(entity, entity)));

		handle.Should().NotBeNull();
		handle!.IsInhibited.Should().BeFalse();
		entity.Attributes[KeptAttribute].CurrentValue.Should().Be(10);

		entity.Attributes.RemoveAttributeSet(vitalSet).Should().BeTrue();

		// A requirement naming an attribute the entity does not have is never met, and nothing else would re-check it
		// once the attribute that used to drive it is detached.
		handle.IsInhibited.Should().BeTrue();
		entity.Attributes[KeptAttribute].CurrentValue.Should().Be(0);

		entity.Attributes.AddAttributeSet(vitalSet);

		handle.IsInhibited.Should().BeFalse();
		entity.Attributes[KeptAttribute].CurrentValue.Should().Be(10);
	}

	[Fact]
	[Trait("Removal", null)]
	public void An_accumulator_stops_at_its_total_when_its_attribute_leaves_and_resumes_when_it_returns()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var vitalSet = new VitalAttributeSet();
		var tallyTag = Tag.RequestTag(_tagsManager, "simple.tag");

		entity.Attributes.AddAttributeSet(vitalSet);

		// The effect drains the tracked attribute by 10 on every tick, and the accumulator tallies those losses.
		var effectData = new EffectData(
			"Tally",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					DepartingAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-10)))
			],
			periodicData: new PeriodicData(
				new ScalableFloat(1),
				true,
				PeriodInhibitionRemovedPolicy.NeverReset),
			effectComponents:
			[
				new AttributeAccumulatorEffectComponent(DepartingAttribute, tallyTag, AccumulationPolicy.Losses)
			]);

		ActiveEffectHandle? handle = entity.EffectsManager.ApplyEffect(
			new Effect(effectData, new EffectOwnership(entity, entity)));

		handle.Should().NotBeNull();

		AttributeAccumulatorEffectComponent accumulator =
			handle!.GetComponent<AttributeAccumulatorEffectComponent>()!;

		accumulator.Total.Should().Be(10);

		// The set leaves mid-flight: the running total stands, and nothing throws on the orphaned attribute.
		FluentActions.Invoking(() => entity.Attributes.RemoveAttributeSet(vitalSet)).Should().NotThrow();

		entity.EffectsManager.UpdateEffects(1);
		entity.EffectsManager.GetActiveEffects().Should().ContainSingle();

		// The total is a record of what already happened, so it survives the attribute going away — and nothing is
		// added while there is no attribute to drain.
		accumulator.Total.Should().Be(10);

		// A *different* set instance supplying the same keys, so the attribute objects are new ones. That is what makes
		// the rebind load-bearing: a component still holding the old instance would tally nothing from here on.
		entity.Attributes.AddAttributeSet(new VitalAttributeSet());
		entity.EffectsManager.UpdateEffects(1);

		accumulator.Total.Should().Be(20);
	}

	[Fact]
	[Trait("Removal", null)]
	public void An_ability_cost_charged_against_a_departing_attribute_makes_it_uncastable()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var vitalSet = new VitalAttributeSet();

		entity.Attributes.AddAttributeSet(vitalSet);

		var costEffectData = new EffectData(
			"Health Cost",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					DepartingAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-10)))
			]);

		var abilityData = new AbilityData("Bloodcast", costEffectData);

		AbilityHandle handle = entity.Abilities.GrantAbilityPermanently(
			abilityData,
			1,
			LevelComparison.None,
			sourceEntity: null);

		handle.TryActivate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		handle.Cancel();

		entity.Attributes.RemoveAttributeSet(vitalSet).Should().BeTrue();

		// A cost that can never be paid is refused rather than quietly skipped, so this is the one place where a
		// missing attribute fails loudly instead of being ignored.
		handle.TryActivate(out failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.InsufficientResources);

		entity.Attributes.AddAttributeSet(vitalSet);

		handle.TryActivate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
	}

	[Fact]
	[Trait("Removal", null)]
	public void An_effect_whose_duration_is_backed_by_a_departing_attribute_expires()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var vitalSet = new VitalAttributeSet();

		entity.Attributes.AddAttributeSet(vitalSet);

		// Duration reads the departing attribute live, so losing it re-evaluates the duration to zero.
		var effectData = new EffectData(
			"Timed Buff",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(
					MagnitudeCalculationType.AttributeBased,
					attributeBasedFloat: new AttributeBasedFloat(
						new AttributeCaptureDefinition(
							DepartingAttribute,
							AttributeCaptureSource.Target,
							Snapshot: false),
						AttributeCalculationType.CurrentValue,
						new ScalableFloat(1),
						new ScalableFloat(0),
						new ScalableFloat(0)))),
			[
				new Modifier(
					KeptAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10)))
			]);

		entity.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(entity, entity)))
			.Should().NotBeNull();

		entity.EffectsManager.GetActiveEffects().Should().ContainSingle();
		entity.Attributes[KeptAttribute].CurrentValue.Should().Be(10);

		entity.Attributes.RemoveAttributeSet(vitalSet).Should().BeTrue();

		// Its duration is now zero, so it must expire rather than run on for the time it had left.
		entity.EffectsManager.GetActiveEffects().Should().BeEmpty();
		entity.Attributes[KeptAttribute].CurrentValue.Should().Be(0);
	}

	[Fact]
	[Trait("Removal", null)]
	public void Adding_a_set_whose_key_collides_leaves_the_entity_untouched()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);

		entity.EffectsManager.ApplyEffect(CreateCrossSetEffect(entity)).Should().NotBeNull();
		entity.Attributes[KeptAttribute].CurrentValue.Should().Be(10);

		// Keys derive from the set's runtime type name, so a second TestAttributeSet collides with the one the entity
		// already has. That has to be refused before anything is unwound, not halfway through the rebuild.
		FluentActions.Invoking(() => entity.Attributes.AddAttributeSet(new TestAttributeSet()))
			.Should().Throw<ArgumentException>();

		entity.Attributes.AttributeSets.Should().ContainSingle();
		entity.Attributes[KeptAttribute].CurrentValue.Should().Be(10);
		entity.EffectsManager.GetActiveEffects().Should().ContainSingle();
	}

	[Fact]
	[Trait("Removal", null)]
	public void Adding_a_set_that_already_satisfies_a_removal_requirement_removes_the_effect()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Dispellable",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					KeptAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10)))
			],
			effectComponents:
			[
				new AttributeRequirementsEffectComponent(
					removalRequirements: [new AttributeRequirement(DepartingAttribute, MinValue: 1)])
			]);

		entity.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(entity, entity)))
			.Should().NotBeNull();

		entity.EffectsManager.GetActiveEffects().Should().ContainSingle();

		// CurrentHealth arrives at 100, which already meets the removal requirement. Subscribing does not itself raise
		// a value change, so the membership change is the only chance to notice.
		entity.Attributes.AddAttributeSet(new VitalAttributeSet());

		entity.EffectsManager.GetActiveEffects().Should().BeEmpty();
	}

	[Fact]
	[Trait("Cross entity", null)]
	public void An_effect_on_another_entity_reevaluates_when_its_source_loses_the_captured_attribute()
	{
		var source = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);
		var vitalSet = new VitalAttributeSet();

		source.Attributes.AddAttributeSet(vitalSet);

		// The effect lives on the target but reads the *source's* health, live, to size its modifier.
		var effectData = new EffectData(
			"Sympathetic Buff",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					KeptAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.AttributeBased,
						attributeBasedFloat: new AttributeBasedFloat(
							new AttributeCaptureDefinition(
								DepartingAttribute,
								AttributeCaptureSource.Source,
								Snapshot: false),
							AttributeCalculationType.CurrentValue,
							new ScalableFloat(1),
							new ScalableFloat(0),
							new ScalableFloat(0))))
			]);

		target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(source, source)))
			.Should().NotBeNull();

		target.Attributes[KeptAttribute].CurrentValue.Should().Be(100);

		// Changing the *source's* sets has to reach an effect living on a different entity's manager.
		source.Attributes.RemoveAttributeSet(vitalSet).Should().BeTrue();

		target.Attributes[KeptAttribute].CurrentValue.Should().Be(0);

		source.Attributes.AddAttributeSet(vitalSet);

		target.Attributes[KeptAttribute].CurrentValue.Should().Be(100);
	}

	[Fact]
	[Trait("Cross entity", null)]
	public void A_source_requirement_rebinds_when_the_source_entity_changes_sets()
	{
		var source = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);
		var vitalSet = new VitalAttributeSet();

		source.Attributes.AddAttributeSet(vitalSet);

		var effectData = new EffectData(
			"Gated By Source",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					KeptAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10)))
			],
			effectComponents:
			[
				new SourceAttributeRequirementsEffectComponent(
					ongoingRequirements: [new AttributeRequirement(DepartingAttribute, MinValue: 1)],
					ownershipEntity: OwnershipEntity.Source)
			]);

		ActiveEffectHandle? handle = target.EffectsManager.ApplyEffect(
			new Effect(effectData, new EffectOwnership(source, source)));

		handle.Should().NotBeNull();
		handle!.IsInhibited.Should().BeFalse();
		target.Attributes[KeptAttribute].CurrentValue.Should().Be(10);

		// The requirement watches the source, so the source losing the attribute must inhibit an effect that lives on
		// the target. Only the dependent registry can carry that across.
		source.Attributes.RemoveAttributeSet(vitalSet).Should().BeTrue();

		handle.IsInhibited.Should().BeTrue();
		target.Attributes[KeptAttribute].CurrentValue.Should().Be(0);

		source.Attributes.AddAttributeSet(vitalSet);

		handle.IsInhibited.Should().BeFalse();
		target.Attributes[KeptAttribute].CurrentValue.Should().Be(10);
	}

	[Fact]
	[Trait("Cross entity", null)]
	public void A_source_requirement_ignores_set_changes_on_the_ownership_entity_it_does_not_watch()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var source = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var ownerVitalSet = new VitalAttributeSet();
		var sourceVitalSet = new VitalAttributeSet();

		owner.Attributes.AddAttributeSet(ownerVitalSet);
		source.Attributes.AddAttributeSet(sourceVitalSet);

		// Owner and source are different entities, and the requirement names the source.
		var effectData = new EffectData(
			"Gated By Source",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					KeptAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10)))
			],
			effectComponents:
			[
				new SourceAttributeRequirementsEffectComponent(
					ongoingRequirements: [new AttributeRequirement(DepartingAttribute, MinValue: 1)],
					ownershipEntity: OwnershipEntity.Source)
			]);

		ActiveEffectHandle? handle = target.EffectsManager.ApplyEffect(
			new Effect(effectData, new EffectOwnership(owner, source)));

		handle.Should().NotBeNull();
		handle!.IsInhibited.Should().BeFalse();

		// The owner is not what this component watches, so its sets changing must leave the effect alone.
		owner.Attributes.RemoveAttributeSet(ownerVitalSet).Should().BeTrue();

		handle.IsInhibited.Should().BeFalse();
		target.Attributes[KeptAttribute].CurrentValue.Should().Be(10);

		// The source is, so its sets changing must reach it.
		source.Attributes.RemoveAttributeSet(sourceVitalSet).Should().BeTrue();

		handle.IsInhibited.Should().BeTrue();
		target.Attributes[KeptAttribute].CurrentValue.Should().Be(0);
	}

	[Fact]
	[Trait("Cross entity", null)]
	public void An_effect_registers_only_with_the_entity_its_component_names()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var source = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var ownerVitalSet = new VitalAttributeSet();
		var sourceVitalSet = new VitalAttributeSet();

		owner.Attributes.AddAttributeSet(ownerVitalSet);
		source.Attributes.AddAttributeSet(sourceVitalSet);

		var probe = new MembershipProbeComponent(AttributeCaptureSource.Source);

		var effectData = new EffectData(
			"Probed",
			new DurationData(DurationType.Infinite),
			effectComponents: [probe]);

		target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(owner, source)))
			.Should().NotBeNull();

		// The probe names the source, so a change on the owner must not reach it at all — not merely be ignored by a
		// guard inside the component, but never be delivered, so the effect is not rebuilt for nothing.
		owner.Attributes.RemoveAttributeSet(ownerVitalSet).Should().BeTrue();

		probe.NotificationCount.Should().Be(0);

		source.Attributes.RemoveAttributeSet(sourceVitalSet).Should().BeTrue();

		probe.NotificationCount.Should().Be(1);
	}

	[Fact]
	[Trait("Cross entity", null)]
	public void A_removed_effect_stops_being_a_dependent_of_the_entity_it_read()
	{
		var source = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);
		var vitalSet = new VitalAttributeSet();

		source.Attributes.AddAttributeSet(vitalSet);

		var effectData = new EffectData(
			"Sympathetic Buff",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					KeptAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.AttributeBased,
						attributeBasedFloat: new AttributeBasedFloat(
							new AttributeCaptureDefinition(
								DepartingAttribute,
								AttributeCaptureSource.Source,
								Snapshot: false),
							AttributeCalculationType.CurrentValue,
							new ScalableFloat(1),
							new ScalableFloat(0),
							new ScalableFloat(0))))
			]);

		ActiveEffectHandle? handle = target.EffectsManager.ApplyEffect(
			new Effect(effectData, new EffectOwnership(source, source)));

		handle.Should().NotBeNull();

		target.EffectsManager.RemoveEffect(handle!);
		target.Attributes[KeptAttribute].CurrentValue.Should().Be(0);

		// The registration has to come off with the effect, or the source keeps rebuilding a dead one forever.
		FluentActions.Invoking(() => source.Attributes.RemoveAttributeSet(vitalSet)).Should().NotThrow();

		target.Attributes[KeptAttribute].CurrentValue.Should().Be(0);
		target.EffectsManager.GetActiveEffects().Should().BeEmpty();
	}

	[Fact]
	[Trait("Removal", null)]
	public void The_attribute_sets_collection_is_exposed_read_only()
	{
		// Pins the declared type rather than the runtime one: the guarantee is that a caller cannot add or remove a
		// set without a deliberate cast, matching how EntityAbilities exposes its granted abilities.
		typeof(EntityAttributes).GetProperty(nameof(EntityAttributes.AttributeSets))!
			.PropertyType.Should().Be<IReadOnlyList<AttributeSet>>();
	}

	// An infinite effect straddling two sets: one modifier on an attribute the entity keeps, one on an attribute that
	// leaves with the set under test.
	private static Effect CreateCrossSetEffect(TestEntity entity)
	{
		var effectData = new EffectData(
			"Cross Set Buff",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					KeptAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10))),
				new Modifier(
					DepartingAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-10)))
			]);

		return new Effect(effectData, new EffectOwnership(entity, entity));
	}

	private sealed class MembershipProbeComponent(AttributeCaptureSource watchedSource) : IEffectComponent
	{
		public AttributeCaptureSource WatchedAttributeSource { get; } = watchedSource;

		public int NotificationCount { get; private set; }

		// Deliberately shares the instance so the test can read the count off the object it passed in.
		public IEffectComponent CreateInstance()
		{
			return this;
		}

		public void OnAttributeMembershipChanged(
			IForgeEntity changedEntity,
			in ActiveEffectEvaluatedData activeEffectEvaluatedData)
		{
			NotificationCount++;
		}
	}
}
