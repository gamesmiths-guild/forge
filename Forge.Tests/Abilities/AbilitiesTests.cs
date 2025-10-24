// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Core;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Abilities;

public class AbilitiesTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Grant ability", null)]
	public void Ability_is_granted_successfully()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle.Should().NotBeNull();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle!.Activate();

		abilityHandle.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("Remove ability", null)]
	public void Removed_ability_is_deactivated_immediately()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out ActiveEffectHandle? effectHandle);

		abilityHandle.Should().NotBeNull();
		effectHandle.Should().NotBeNull();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle!.Activate();

		abilityHandle.IsActive.Should().BeTrue();

		entity.EffectsManager.UnapplyEffect(effectHandle!);

		entity.Abilities.GrantedAbilities.Should().BeEmpty();
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Remove ability", null)]
	public void Ability_is_only_removed_after_being_deactivated()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out ActiveEffectHandle? effectHandle,
			AbilityDeactivationPolicy.RemoveOnEnd,
			AbilityDeactivationPolicy.RemoveOnEnd);

		abilityHandle.Should().NotBeNull();
		effectHandle.Should().NotBeNull();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle!.Activate();

		abilityHandle.IsActive.Should().BeTrue();

		entity.EffectsManager.UnapplyEffect(effectHandle!);

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle.End();

		entity.Abilities.GrantedAbilities.Should().BeEmpty();
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Inhibit ability", null)]
	public void Ability_gets_inhibited_temporarily_while_granting_effect_is_inhibited()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		TagContainer? ignoreTags = Tag.RequestTag(_tagsManager, "Tag").GetSingleTagContainer();
		ignoreTags.Should().NotBeNull();

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			extraComponent: new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(
					IgnoreTags: ignoreTags)));

		abilityHandle.Should().NotBeNull();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle!.Activate();

		abilityHandle.IsActive.Should().BeTrue();

		ActiveEffectHandle? tagEffectHandle = CreateAndApplyTagEffect(entity, ignoreTags!);

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle.Activate();

		abilityHandle.IsActive.Should().BeFalse();
		abilityHandle.IsInhibited.Should().BeTrue();

		entity.EffectsManager.UnapplyEffect(tagEffectHandle!);

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle.Activate();

		abilityHandle.IsActive.Should().BeTrue();
		abilityHandle.IsInhibited.Should().BeFalse();
	}

	[Fact]
	[Trait("Remove ability", null)]
	public void Granted_ability_is_not_removed_when_deactivation_policy_is_ignore()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out ActiveEffectHandle? effectHandle,
			AbilityDeactivationPolicy.Ignore,
			AbilityDeactivationPolicy.Ignore);

		abilityHandle.Should().NotBeNull();
		effectHandle.Should().NotBeNull();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle!.Activate();

		abilityHandle.IsActive.Should().BeTrue();

		entity.EffectsManager.UnapplyEffect(effectHandle!);

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle.End();

		entity.Abilities.GrantedAbilities.Should().ContainSingle();
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Grant ability", null)]
	public void Ability_granted_by_multiple_effects_is_removed_only_when_all_granting_effects_are_removed()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		AbilityHandle? abilityHandle1 = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out ActiveEffectHandle? effectHandle1);

		AbilityHandle? abilityHandle2 = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out ActiveEffectHandle? effectHandle2);

		abilityHandle1.Should().NotBeNull();
		effectHandle1.Should().NotBeNull();
		abilityHandle2.Should().NotBeNull();
		effectHandle2.Should().NotBeNull();
		abilityHandle1.Should().Be(abilityHandle2);
		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		entity.EffectsManager.UnapplyEffect(effectHandle1!);

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		entity.EffectsManager.UnapplyEffect(effectHandle2!);

		entity.Abilities.GrantedAbilities.Should().BeEmpty();
	}

	[Fact]
	[Trait("Grant ability", null)]
	public void Ability_is_not_granted_if_target_has_blocking_tags()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		TagContainer? blockingTags = Tag.RequestTag(_tagsManager, "Tag").GetSingleTagContainer();
		blockingTags.Should().NotBeNull();

		var grantAbilityConfig = new GrantAbilityConfig(
			abilityData,
			new ScalableInt(1),
			AbilityDeactivationPolicy.CancelImmediately,
			AbilityDeactivationPolicy.CancelImmediately);

		var grantAbilityEffectData = new EffectData(
			"Grant Fireball",
			new DurationData(DurationType.Infinite),
			effectComponents:
			[
				new GrantAbilityEffectComponent([grantAbilityConfig]),
				new TargetTagRequirementsEffectComponent(applicationTagRequirements: new TagRequirements(IgnoreTags: blockingTags))
			]);

		var grantAbilityEffect = new Effect(
			grantAbilityEffectData,
			new EffectOwnership(entity, null));

		CreateAndApplyTagEffect(entity, blockingTags!);

		entity.EffectsManager.ApplyEffect(grantAbilityEffect);

		entity.Abilities.GrantedAbilities.Should().BeEmpty();
	}

	[Fact]
	[Trait("Grant ability", null)]
	public void Ability_granted_by_instant_effect_is_permanent()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		// Grant ability with an instant effect, making it permanent.
		AbilityHandle? permanentAbilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			durationData: new DurationData(DurationType.Instant));

		// Grant the same ability with a non-instant effect.
		AbilityHandle? temporaryAbilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out ActiveEffectHandle? temporaryEffectHandle);

		permanentAbilityHandle.Should().NotBeNull();
		temporaryAbilityHandle.Should().NotBeNull();
		temporaryEffectHandle.Should().NotBeNull();
		permanentAbilityHandle.Should().Be(temporaryAbilityHandle);
		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		// Remove the temporary effect.
		entity.EffectsManager.UnapplyEffect(temporaryEffectHandle!);

		// The ability should still be granted because of the initial permanent grant.
		entity.Abilities.GrantedAbilities.Should().ContainSingle();
	}

	[Fact]
	[Trait("Grant ability", null)]
	public void Ability_granted_by_late_instant_effect_is_permanent()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		// Grant the same ability with a non-instant effect.
		AbilityHandle? temporaryAbilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out ActiveEffectHandle? temporaryEffectHandle);

		temporaryAbilityHandle.Should().NotBeNull();
		temporaryEffectHandle.Should().NotBeNull();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		// Grant ability with an instant effect, making it permanent.
		AbilityHandle? permanentAbilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			durationData: new DurationData(DurationType.Instant));

		permanentAbilityHandle.Should().Be(temporaryAbilityHandle);

		// Remove the temporary effect.
		entity.EffectsManager.UnapplyEffect(temporaryEffectHandle!);

		// The ability should still be granted because of the initial permanent grant.
		entity.Abilities.GrantedAbilities.Should().ContainSingle();
	}

	[Fact]
	[Trait("Inhibit ability", null)]
	public void Ability_granted_by_instant_effect_is_not_inhibited()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		// Grant ability with an instant effect, making it permanent.
		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			durationData: new DurationData(DurationType.Instant));

		TagContainer? ignoreTags = Tag.RequestTag(_tagsManager, "Tag").GetSingleTagContainer();
		ignoreTags.Should().NotBeNull();

		// Grant the same ability with a non-instant, inhibitable effect.
		SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			extraComponent: new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(
					IgnoreTags: ignoreTags)));

		abilityHandle.Should().NotBeNull();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		// Inhibit the temporary effect by adding the tag.
		CreateAndApplyTagEffect(entity, ignoreTags!);

		// The ability should not be inhibited because it was granted permanently.
		abilityHandle!.IsInhibited.Should().BeFalse();
	}

	[Fact]
	[Trait("Inhibit ability", null)]
	public void Ability_granted_by_late_instant_effect_is_not_inhibited()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		TagContainer? ignoreTags = Tag.RequestTag(_tagsManager, "Tag").GetSingleTagContainer();
		ignoreTags.Should().NotBeNull();

		// Grant the same ability with a non-instant, inhibitable effect.
		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			extraComponent: new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(
					IgnoreTags: ignoreTags)));

		abilityHandle.Should().NotBeNull();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		// Inhibit the temporary effect by adding the tag.
		CreateAndApplyTagEffect(entity, ignoreTags!);

		// The ability should now be inhibited.
		abilityHandle!.IsInhibited.Should().BeTrue();

		// Grant ability with an instant effect, making it permanent and removing inhibition.
		SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			durationData: new DurationData(DurationType.Instant));

		// The ability should no longer be inhibited.
		abilityHandle.IsInhibited.Should().BeFalse();
	}

	[Fact]
	[Trait("Inhibit ability", null)]
	public void Ability_is_inhibited_only_when_all_granting_effects_are_inhibited()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		TagContainer? ignoreTags1 = Tag.RequestTag(_tagsManager, "Simple.Tag").GetSingleTagContainer();
		TagContainer? ignoreTags2 = Tag.RequestTag(_tagsManager, "Other.Tag").GetSingleTagContainer();
		ignoreTags1.Should().NotBeNull();
		ignoreTags2.Should().NotBeNull();

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			extraComponent: new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(
					IgnoreTags: ignoreTags1)));

		SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			extraComponent: new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(
					IgnoreTags: ignoreTags2)));

		abilityHandle.Should().NotBeNull();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();
		abilityHandle!.Activate();
		abilityHandle.IsActive.Should().BeTrue();

		// Inhibit the first effect.
		CreateAndApplyTagEffect(entity, ignoreTags1!);

		// Ability should not be inhibited yet, but it should be deactivated.
		abilityHandle.IsInhibited.Should().BeFalse();
		abilityHandle.IsActive.Should().BeFalse();

		// Inhibit the second effect.
		CreateAndApplyTagEffect(entity, ignoreTags2!);

		// Now the ability should be fully inhibited.
		abilityHandle.IsInhibited.Should().BeTrue();
	}

	[Fact]
	[Trait("Inhibit ability", null)]
	public void Inhibited_ability_becomes_active_if_new_non_inhibited_source_is_added()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		TagContainer? ignoreTags = Tag.RequestTag(_tagsManager, "Tag").GetSingleTagContainer();
		ignoreTags.Should().NotBeNull();

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			extraComponent: new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(
					IgnoreTags: ignoreTags)));

		abilityHandle.Should().NotBeNull();

		// Inhibit the ability.
		CreateAndApplyTagEffect(entity, ignoreTags!);

		abilityHandle!.IsInhibited.Should().BeTrue();

		// Add a new, non-inhibited source for the same ability.
		SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		// The ability should no longer be inhibited.
		abilityHandle.IsInhibited.Should().BeFalse();
	}

	[Fact]
	[Trait("Inhibit ability", null)]
	public void Inhibition_policy_RemoveOnEnd_inhibits_after_deactivation()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		TagContainer? ignoreTags = Tag.RequestTag(_tagsManager, "Tag").GetSingleTagContainer();
		ignoreTags.Should().NotBeNull();

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			grantedAbilityInhibitionPolicy: AbilityDeactivationPolicy.RemoveOnEnd,
			extraComponent: new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(
					IgnoreTags: ignoreTags)));

		abilityHandle.Should().NotBeNull();

		abilityHandle!.Activate();
		abilityHandle.IsActive.Should().BeTrue();

		// Inhibit the granting effect.
		CreateAndApplyTagEffect(entity, ignoreTags!);

		// With RemoveOnEnd policy, the ability is not inhibited while active.
		abilityHandle.IsInhibited.Should().BeFalse();
		abilityHandle.IsActive.Should().BeTrue();

		// End the ability.
		abilityHandle.End();

		// Now that it's no longer active, it should become inhibited.
		abilityHandle.IsActive.Should().BeFalse();
		abilityHandle.IsInhibited.Should().BeTrue();
	}

	[Fact]
	[Trait("Inhibit ability", null)]
	public void Inhibition_policy_Ignore_prevents_inhibition()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		TagContainer? ignoreTags = Tag.RequestTag(_tagsManager, "Tag").GetSingleTagContainer();
		ignoreTags.Should().NotBeNull();

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			grantedAbilityInhibitionPolicy: AbilityDeactivationPolicy.Ignore,
			extraComponent: new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(
					IgnoreTags: ignoreTags)));

		abilityHandle.Should().NotBeNull();

		abilityHandle!.Activate();

		// Inhibit the granting effect.
		CreateAndApplyTagEffect(entity, ignoreTags!);

		// With Ignore policy, the ability is never inhibited.
		abilityHandle.IsInhibited.Should().BeFalse();
		abilityHandle.IsActive.Should().BeTrue();

		abilityHandle.End();

		abilityHandle.IsInhibited.Should().BeFalse();
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Inhibit ability", null)]
	public void Effect_inhibited_at_application_grant_inhibited_abilities()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		TagContainer? ignoreTags = Tag.RequestTag(_tagsManager, "Tag").GetSingleTagContainer();

		CreateAndApplyTagEffect(entity, ignoreTags!);

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			extraComponent: new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(
					IgnoreTags: ignoreTags)));

		abilityHandle.Should().NotBeNull();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();
		abilityHandle!.IsInhibited.Should().BeTrue();
	}

	[Fact]
	[Trait("Grant ability", null)]
	public void Ability_level_is_set_correctly()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(5), // Grant at level 5
			out _);

		abilityHandle.Should().NotBeNull();
		abilityHandle!.Level.Should().Be(5);
	}

	[Fact]
	[Trait("Grant ability", null)]
	public void Ability_level_scales_with_curve()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var levelCurve = new Curve(
			[
				new CurveKey(1, 1f), // Effect level 1 -> Ability level 1
				new CurveKey(2, 3f), // Effect level 2 -> Ability level 3
				new CurveKey(3, 5f), // Effect level 3 -> Ability level 5
			]);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		// Grant ability with a scaling level based on the effect's level
		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1, levelCurve),
			out _,
			effectLevel: 2); // Granting effect is level 2

		abilityHandle.Should().NotBeNull();
		abilityHandle!.Level.Should().Be(3); // Curve should evaluate to 3
	}

	[Fact]
	[Trait("Grant ability", null)]
	public void Ability_level_override_policy_works_correctly()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		// Grant at level 2
		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(2),
			out _);

		abilityHandle.Should().NotBeNull();
		abilityHandle!.Level.Should().Be(2);

		// Grant again at level 3 with override policy for higher levels
		SetupAbility(
			entity,
			abilityData,
			new ScalableInt(3),
			out _,
			levelOverridePolicy: LevelComparison.Higher);

		abilityHandle.Level.Should().Be(3);

		// Grant again at level 1 with the same policy; level should not change
		SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			levelOverridePolicy: LevelComparison.Higher);

		abilityHandle.Level.Should().Be(3);

		// Grant again at level 5 with override policy for lower or equal levels
		SetupAbility(
			entity,
			abilityData,
			new ScalableInt(5),
			out _,
			levelOverridePolicy: LevelComparison.Lower | LevelComparison.Equal);

		abilityHandle.Level.Should().Be(3);
	}

	[Fact]
	[Trait("Grant ability", null)]
	public void Abilities_with_different_sources_are_separate_instances()
	{
		TestEntity entity1 = new(_tagsManager, _cuesManager);
		TestEntity entity2 = new(_tagsManager, _cuesManager);

		// Create two different AbilityData instances (differ by name)
		AbilityData abilityData1 = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		// Grant the first ability
		AbilityHandle? abilityHandle1 = SetupAbility(
			entity1,
			abilityData1,
			new ScalableInt(1),
			out _,
			sourceEntity: entity1);

		// Grant the second ability
		AbilityHandle? abilityHandle2 = SetupAbility(
			entity1,
			abilityData1,
			new ScalableInt(1),
			out _,
			sourceEntity: entity2);

		abilityHandle1.Should().NotBeNull();
		abilityHandle2.Should().NotBeNull();

		// The handles should be for different ability instances
		abilityHandle1.Should().NotBe(abilityHandle2);
		entity1.Abilities.GrantedAbilities.Should().HaveCount(2);

		// Activate one and ensure the other is not affected
		abilityHandle1!.Activate();
		abilityHandle1.IsActive.Should().BeTrue();
		abilityHandle2!.IsActive.Should().BeFalse();
	}

	private static AbilityData CreateAbiltyData(
		string abilityName,
		ScalableFloat cooldownDuration,
		string costAttribute,
		ScalableFloat costAmmount)
	{
		var costEffectData = new EffectData(
			"Fireball Cooldown",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, cooldownDuration)));

		var cooldownEffectData = new EffectData(
			"Fireball Cost",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					costAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, costAmmount))
			]);

		return new(
			abilityName,
			costEffectData,
			cooldownEffectData);
	}

	private static AbilityHandle? SetupAbility(
		TestEntity targetEntity,
		AbilityData abilityData,
		ScalableInt abilityLevelScaling,
		out ActiveEffectHandle? effectHandle,
		AbilityDeactivationPolicy grantedAbilityRemovalPolicy = AbilityDeactivationPolicy.CancelImmediately,
		AbilityDeactivationPolicy grantedAbilityInhibitionPolicy = AbilityDeactivationPolicy.CancelImmediately,
		IForgeEntity? sourceEntity = null,
		DurationData? durationData = null,
		IEffectComponent? extraComponent = null,
		int effectLevel = 1,
		LevelComparison levelOverridePolicy = LevelComparison.Higher)
	{
		GrantAbilityConfig grantAbilityConfig = new(
			abilityData,
			abilityLevelScaling,
			grantedAbilityRemovalPolicy,
			grantedAbilityInhibitionPolicy,
			levelOverridePolicy);

		Effect grantAbilityEffect = CreateAbilityApplierEffect(
			"Grant Fireball",
			grantAbilityConfig,
			sourceEntity,
			durationData,
			extraComponent,
			effectLevel);

		effectHandle = targetEntity.EffectsManager.ApplyEffect(grantAbilityEffect);

		targetEntity.Abilities.TryGetAbility(abilityData, out AbilityHandle? abilityHandle, sourceEntity);
		return abilityHandle;
	}

	private static Effect CreateAbilityApplierEffect(
		string effectName,
		GrantAbilityConfig grantAbilityConfig,
		IForgeEntity? sourceEntity,
		DurationData? durationData,
		IEffectComponent? extraComponent,
		int effectLevel)
	{
		durationData ??= new DurationData(DurationType.Infinite);

		List<IEffectComponent> effectComponents = [new GrantAbilityEffectComponent([grantAbilityConfig])];

		if (extraComponent is not null)
		{
			effectComponents.Add(extraComponent);
		}

		var grantAbilityEffectData = new EffectData(
			effectName,
			durationData.Value,
			effectComponents: [.. effectComponents]);

		return new Effect(
			grantAbilityEffectData,
			new EffectOwnership(null, sourceEntity),
			effectLevel);
	}

	private static ActiveEffectHandle? CreateAndApplyTagEffect(TestEntity entity, TagContainer tags)
	{
		var tagEffectData = new EffectData(
			"Tag Effect",
			new DurationData(DurationType.Infinite),
			effectComponents: [new ModifierTagsEffectComponent(tags)]);

		var tagEffect = new Effect(
			tagEffectData,
			new EffectOwnership(entity, null));

		return entity.EffectsManager.ApplyEffect(tagEffect);
	}
}
