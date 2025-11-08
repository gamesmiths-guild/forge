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

		AbilityData abilityData = CreateAbilityData(
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

		abilityHandle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		abilityHandle.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("Remove ability", null)]
	public void Removed_ability_is_deactivated_immediately()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
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

		abilityHandle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
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

		AbilityData abilityData = CreateAbilityData(
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

		abilityHandle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
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

		AbilityData abilityData = CreateAbilityData(
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

		abilityHandle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		abilityHandle.IsActive.Should().BeTrue();

		ActiveEffectHandle? tagEffectHandle = CreateAndApplyTagEffect(entity, ignoreTags!);

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle.Activate(out activationResult).Should().BeFalse();
		activationResult.Should().Be(AbilityActivationResult.FailedInhibition);
		abilityHandle.IsActive.Should().BeFalse();
		abilityHandle.IsInhibited.Should().BeTrue();

		entity.EffectsManager.UnapplyEffect(tagEffectHandle!);

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle.Activate(out activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		abilityHandle.IsActive.Should().BeTrue();
		abilityHandle.IsInhibited.Should().BeFalse();
	}

	[Fact]
	[Trait("Remove ability", null)]
	public void Granted_ability_is_not_removed_when_deactivation_policy_is_ignore()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
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

		abilityHandle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
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

		AbilityData abilityData = CreateAbilityData(
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

		AbilityData abilityData = CreateAbilityData(
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

		AbilityData abilityData = CreateAbilityData(
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

		AbilityData abilityData = CreateAbilityData(
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

		AbilityData abilityData = CreateAbilityData(
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

		AbilityData abilityData = CreateAbilityData(
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

		AbilityData abilityData = CreateAbilityData(
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
		abilityHandle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
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

		AbilityData abilityData = CreateAbilityData(
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

		AbilityData abilityData = CreateAbilityData(
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

		abilityHandle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
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

		AbilityData abilityData = CreateAbilityData(
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

		abilityHandle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);

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

		AbilityData abilityData = CreateAbilityData(
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

		AbilityData abilityData = CreateAbilityData(
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

		AbilityData abilityData = CreateAbilityData(
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

		AbilityData abilityData = CreateAbilityData(
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
		AbilityData abilityData1 = CreateAbilityData(
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
		abilityHandle1!.Activate(out AbilityActivationResult activationResult1).Should().BeTrue();
		activationResult1.Should().Be(AbilityActivationResult.Success);
		abilityHandle1.IsActive.Should().BeTrue();
		abilityHandle2!.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Cooldown", null)]
	public void Ability_wont_activate_while_cooldown_is_active()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		abilityHandle.IsActive.Should().BeTrue();

		abilityHandle.CommitCooldown();
		abilityHandle.End();

		abilityHandle!.Activate(out activationResult).Should().BeFalse();
		activationResult.Should().Be(AbilityActivationResult.FailedCooldown);

		entity.EffectsManager.UpdateEffects(2f);

		abilityHandle!.Activate(out activationResult).Should().BeFalse();
		activationResult.Should().Be(AbilityActivationResult.FailedCooldown);

		entity.EffectsManager.UpdateEffects(1f);

		abilityHandle!.Activate(out activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
	}

	[Theory]
	[Trait("Cost", null)]
	[InlineData(5)]
	[InlineData(-50)]
	public void Ability_wont_activate_if_cant_afford_cost(int cost)
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(cost),
			retriggerInstancedAbility: true);

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		abilityHandle.IsActive.Should().BeTrue();

		abilityHandle.CommitCost();

		abilityHandle!.Activate(out activationResult).Should().BeFalse();
		activationResult.Should().Be(AbilityActivationResult.FailedInsufficientResources);
	}

	[Fact]
	[Trait("OwnerTag requirements", null)]
	public void Ability_wont_activate_when_owner_missing_required_tag()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			activationRequiredTags: new TagContainer(
				_tagsManager, TestUtils.StringToTag(_tagsManager, ["tag"])));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle!.Activate(out AbilityActivationResult activationResult).Should().BeFalse();
		activationResult.Should().Be(AbilityActivationResult.FailedOwnerTagRequirements);
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("OwnerTag requirements", null)]
	public void Ability_wont_activate_when_owner_has_blocked_tag()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			activationBlockedTags: new TagContainer(
				_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.green"])));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle!.Activate(out AbilityActivationResult activationResult).Should().BeFalse();
		activationResult.Should().Be(AbilityActivationResult.FailedOwnerTagRequirements);
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("SourceTag requirements", null)]
	public void Ability_wont_activate_when_source_missing_required_tag()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);
		TestEntity source = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			sourceRequiredTags: new TagContainer(
				_tagsManager, TestUtils.StringToTag(_tagsManager, ["tag"])));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			sourceEntity: source);

		abilityHandle!.Activate(out AbilityActivationResult activationResult).Should().BeFalse();
		activationResult.Should().Be(AbilityActivationResult.FailedSourceTagRequirements);
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("SourceTag requirements", null)]
	public void Ability_wont_activate_when_source_has_blocked_tag()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);
		TestEntity source = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			sourceBlockedTags: new TagContainer(
				_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.green"])));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			sourceEntity: source);

		abilityHandle!.Activate(out AbilityActivationResult activationResult).Should().BeFalse();
		activationResult.Should().Be(AbilityActivationResult.FailedSourceTagRequirements);
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("SourceTag requirements", null)]
	public void Ability_wont_activate_when_source_is_missing_but_has_required_tags()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			sourceRequiredTags: new TagContainer(
				_tagsManager, TestUtils.StringToTag(_tagsManager, ["tag"])));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			sourceEntity: null);

		abilityHandle!.Activate(out AbilityActivationResult activationResult).Should().BeFalse();
		activationResult.Should().Be(AbilityActivationResult.FailedSourceTagRequirements);
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("SourceTag requirements", null)]
	public void Ability_activates_when_source_is_missing_and_has_blocked_tags()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			sourceBlockedTags: new TagContainer(
				_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.green"])));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			sourceEntity: null);

		abilityHandle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		abilityHandle.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("TargetTag requirements", null)]
	public void Ability_wont_activate_when_target_missing_required_tag()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);
		TestEntity target = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			targetRequiredTags: new TagContainer(
				_tagsManager, TestUtils.StringToTag(_tagsManager, ["tag"])));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle!.Activate(out AbilityActivationResult activationResult, target).Should().BeFalse();
		activationResult.Should().Be(AbilityActivationResult.FailedTargetTagRequirements);
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("TargetTag requirements", null)]
	public void Ability_wont_activate_when_target_has_blocked_tag()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);
		TestEntity target = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			targetBlockedTags: new TagContainer(
				_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.green"])));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle!.Activate(out AbilityActivationResult activationResult, target).Should().BeFalse();
		activationResult.Should().Be(AbilityActivationResult.FailedTargetTagRequirements);
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("TargetTag requirements", null)]
	public void Ability_wont_activate_when_target_is_missing_but_has_required_tags()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			targetRequiredTags: new TagContainer(
				_tagsManager, TestUtils.StringToTag(_tagsManager, ["tag"])));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle!.Activate(out AbilityActivationResult activationResult).Should().BeFalse();
		activationResult.Should().Be(AbilityActivationResult.FailedTargetTagRequirements);
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("TargetTag requirements", null)]
	public void Ability_activates_when_target_is_missing_and_has_blocked_tags()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			targetBlockedTags: new TagContainer(
				_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.green"])));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		abilityHandle.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("BlockAbilitiesWithTag", null)]
	public void Ability_activation_blocks_other_abilities_with_blocked_tags()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData blockerAbilityData = CreateAbilityData(
			"Blocker ability",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			blockAbilitiesWithTag: new TagContainer(
				_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.red"])));

		AbilityData unblockedAbilityData = CreateAbilityData(
			"Unblocked ability",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			abilityTags: new TagContainer(
				_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.blue"])));

		AbilityData blockedAbilityData = CreateAbilityData(
			"Blocked ability",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			abilityTags: new TagContainer(
				_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.red"])));

		AbilityHandle? blockerAbilityHandle = SetupAbility(
			entity,
			blockerAbilityData,
			new ScalableInt(1),
			out _);

		AbilityHandle? unblockedAbilityHandle = SetupAbility(
			entity,
			unblockedAbilityData,
			new ScalableInt(1),
			out _);

		AbilityHandle? blockedAbilityHandle = SetupAbility(
			entity,
			blockedAbilityData,
			new ScalableInt(1),
			out _);

		blockerAbilityHandle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		blockerAbilityHandle.IsActive.Should().BeTrue();

		unblockedAbilityHandle!.Activate(out activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		unblockedAbilityHandle.IsActive.Should().BeTrue();

		blockedAbilityHandle!.Activate(out activationResult).Should().BeFalse();
		activationResult.Should().Be(AbilityActivationResult.FailedBlockedByTags);
		blockedAbilityHandle.IsActive.Should().BeFalse();

		blockerAbilityHandle!.End();

		blockedAbilityHandle!.Activate(out activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		blockedAbilityHandle.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("Instancing", null)]
	public void Ability_PerEntity_no_retrigger_activated_twice_does_not_start_second_instance()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"PerEntity_NoRetrigger",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute1",
			new ScalableFloat(-1),
			instancingPolicy: AbilityInstancingPolicy.PerEntity,
			retriggerInstancedAbility: false);

		AbilityHandle? handle = SetupAbility(entity, abilityData, new ScalableInt(1), out _);
		handle.Should().NotBeNull();

		handle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		handle.IsActive.Should().BeTrue();

		// No retrigger, single instance.
		handle!.Activate(out activationResult).Should().BeFalse();
		activationResult.Should().Be(AbilityActivationResult.FailedPersistentInstanceActive);
		handle.IsActive.Should().BeTrue();

		handle.End();
		handle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Instancing", null)]
	public void Ability_PerEntity_retrigger_restarts_instance_and_fires_deactivated_once()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"PerEntity_Retrigger",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute2",
			new ScalableFloat(-1),
			instancingPolicy: AbilityInstancingPolicy.PerEntity,
			retriggerInstancedAbility: true);

		AbilityHandle? handle = SetupAbility(entity, abilityData, new ScalableInt(1), out _);
		handle.Should().NotBeNull();

		handle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		handle.IsActive.Should().BeTrue();

		// Retrigger replaces the running instance.
		handle!.Activate(out AbilityActivationResult activationResult2).Should().BeTrue();
		activationResult2.Should().Be(AbilityActivationResult.Success);
		handle.IsActive.Should().BeTrue();

		// One End should fully deactivate because retrigger replaced the instance instead of stacking.
		handle.End();
		handle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Instancing", null)]
	public void Ability_per_execution_multiple_activations_create_multiple_instances()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"PerExecution",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute3",
			new ScalableFloat(-1),
			instancingPolicy: AbilityInstancingPolicy.PerExecution);

		AbilityHandle? handle = SetupAbility(entity, abilityData, new ScalableInt(1), out _);
		handle.Should().NotBeNull();

		handle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		handle.IsActive.Should().BeTrue();

		handle!.Activate(out activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		handle.IsActive.Should().BeTrue();

		handle!.Activate(out activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		handle.IsActive.Should().BeTrue();

		// End most recent instance only; still active until all are ended.
		handle.End();
		handle.IsActive.Should().BeTrue();

		handle.End();
		handle.IsActive.Should().BeTrue();

		handle.End();
		handle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Instancing", null)]
	public void Ability_End_ends_most_recent_instance_only()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"EndsMostRecent",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute5",
			new ScalableFloat(-1),
			instancingPolicy: AbilityInstancingPolicy.PerExecution);

		AbilityHandle? handle = SetupAbility(entity, abilityData, new ScalableInt(1), out _);
		handle.Should().NotBeNull();

		handle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		handle.IsActive.Should().BeTrue();

		handle!.Activate(out activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		handle.IsActive.Should().BeTrue();

		// One End should not fully deactivate if multiple instances exist.
		handle.End();
		handle.IsActive.Should().BeTrue();

		// Second End ends the remaining instance.
		handle.End();
		handle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Instancing", null)]
	public void Ability_CancelAbility_cancels_all_active_instances()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"CancelAllInstances",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute5",
			new ScalableFloat(-1),
			instancingPolicy: AbilityInstancingPolicy.PerExecution);

		AbilityHandle? handle = SetupAbility(entity, abilityData, new ScalableInt(1), out _);
		handle.Should().NotBeNull();

		handle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		handle.IsActive.Should().BeTrue();

		handle!.Activate(out activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		handle.IsActive.Should().BeTrue();

		handle!.Activate(out activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		handle.IsActive.Should().BeTrue();

		handle.Cancel();

		handle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("CancelAbilitiesWithTag", null)]
	public void CancelAbilitiesWithTag_cancels_all_matching_active_abilities_on_activation()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var cancelTags = new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.red"]));

		AbilityData canceller = CreateAbilityData(
			"Canceller",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute3",
			new ScalableFloat(-1),
			cancelAbilitiesWithTag: cancelTags);

		AbilityData victim = CreateAbilityData(
			"Victim",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute5",
			new ScalableFloat(-1),
			abilityTags: new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.red"])));

		AbilityHandle? cancellerHandle = SetupAbility(entity, canceller, new ScalableInt(1), out _);
		AbilityHandle? victimHandle = SetupAbility(entity, victim, new ScalableInt(1), out _);

		victimHandle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		victimHandle.IsActive.Should().BeTrue();

		cancellerHandle!.Activate(out activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		cancellerHandle.IsActive.Should().BeTrue();

		victimHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("CancelAbilitiesWithTag", null)]
	public void CancelAbilitiesWithTag_does_not_cancel_unrelated_abilities()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var cancelTags = new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.red"]));

		AbilityData canceller = CreateAbilityData(
			"Canceller2",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute3",
			new ScalableFloat(-1),
			cancelAbilitiesWithTag: cancelTags);

		AbilityData unrelated = CreateAbilityData(
			"Unrelated",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute5",
			new ScalableFloat(-1),
			abilityTags: new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.blue"])));

		AbilityHandle? cancellerHandle = SetupAbility(entity, canceller, new ScalableInt(1), out _);
		AbilityHandle? unrelatedHandle = SetupAbility(entity, unrelated, new ScalableInt(1), out _);

		unrelatedHandle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		unrelatedHandle.IsActive.Should().BeTrue();

		cancellerHandle!.Activate(out activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		cancellerHandle.IsActive.Should().BeTrue();

		unrelatedHandle.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("CancelAbilitiesWithTag", null)]
	public void CancelAbilitiesWithTag_cancels_all_instances_of_matching_abilities()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var cancelTags = new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.red"]));

		AbilityData canceller = CreateAbilityData(
			"Canceller3",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute1",
			new ScalableFloat(-1),
			cancelAbilitiesWithTag: cancelTags);

		AbilityData victim = CreateAbilityData(
			"VictimMulti",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute2",
			new ScalableFloat(-1),
			abilityTags: new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.red"])),
			instancingPolicy: AbilityInstancingPolicy.PerExecution);

		AbilityHandle? cancellerHandle = SetupAbility(entity, canceller, new ScalableInt(1), out _);
		AbilityHandle? victimHandle = SetupAbility(entity, victim, new ScalableInt(1), out _);

		victimHandle!.Activate(out AbilityActivationResult activationResultA).Should().BeTrue();
		activationResultA.Should().Be(AbilityActivationResult.Success);
		victimHandle.IsActive.Should().BeTrue();

		cancellerHandle!.Activate(out AbilityActivationResult activationResultB).Should().BeTrue();
		activationResultB.Should().Be(AbilityActivationResult.Success);
		cancellerHandle.IsActive.Should().BeTrue();

		victimHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("CancelAbilitiesWithTag", null)]
	public void CancelAbilitiesWithTag_does_not_cancel_self_on_activation()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var redTags = new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.red"]));

		AbilityData selfCanceller = CreateAbilityData(
			"SelfCanceller",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute1",
			new ScalableFloat(-1),
			abilityTags: redTags,
			cancelAbilitiesWithTag: redTags);

		AbilityHandle? handle = SetupAbility(entity, selfCanceller, new ScalableInt(1), out _);

		handle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		handle.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("BlockAbilitiesWithTag", null)]
	public void Blocked_ability_tags_are_removed_only_after_last_instance_ends()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var redTags = new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.red"]));

		AbilityData blocker = CreateAbilityData(
			"BlockerMulti",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute1",
			new ScalableFloat(-1),
			instancingPolicy: AbilityInstancingPolicy.PerExecution,
			blockAbilitiesWithTag: redTags);

		AbilityData blocked = CreateAbilityData(
			"BlockedRed",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute5",
			new ScalableFloat(-1),
			abilityTags: redTags);

		AbilityHandle? blockerHandle = SetupAbility(entity, blocker, new ScalableInt(1), out _);
		AbilityHandle? blockedHandle = SetupAbility(entity, blocked, new ScalableInt(1), out _);

		blockerHandle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		blockerHandle.IsActive.Should().BeTrue();

		blockerHandle!.Activate(out activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		blockerHandle.IsActive.Should().BeTrue();

		// While any blocker instance active, blocked ability cannot activate.
		blockedHandle!.Activate(out activationResult).Should().BeFalse();
		activationResult.Should().Be(AbilityActivationResult.FailedBlockedByTags);
		blockedHandle.IsActive.Should().BeFalse();

		// End one blocker instance; still blocked.
		blockerHandle.End();
		blockedHandle.Activate(out activationResult).Should().BeFalse();
		activationResult.Should().Be(AbilityActivationResult.FailedBlockedByTags);
		blockedHandle.IsActive.Should().BeFalse();

		// End last blocker instance; now unblocked.
		blockerHandle.End();
		blockedHandle.Activate(out activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		blockedHandle.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("ActivationOwnedTags", null)]
	public void Activation_owned_tags_are_applied_on_activation_and_removed_on_end()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var ownedTags = new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["tag"]));

		AbilityData abilityWithOwned = CreateAbilityData(
			"OwnedTagsAbility",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute5",
			new ScalableFloat(-1),
			activationOwnedTags: ownedTags);

		AbilityHandle? handle = SetupAbility(entity, abilityWithOwned, new ScalableInt(1), out _);

		handle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		entity.Tags.CombinedTags.HasAll(ownedTags).Should().BeTrue();
		handle.IsActive.Should().BeTrue();

		handle.End();
		entity.Tags.CombinedTags.HasAny(ownedTags).Should().BeFalse();
	}

	[Fact]
	[Trait("ActivationOwnedTags", null)]
	public void Activation_owned_tags_are_applied_on_activation_and_removed_after_last_instance_ends()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var ownedTags = new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["tag"]));

		AbilityData abilityWithOwned = CreateAbilityData(
			"OwnedTagsAbility",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute5",
			new ScalableFloat(-1),
			instancingPolicy: AbilityInstancingPolicy.PerExecution,
			activationOwnedTags: ownedTags);

		AbilityHandle? handle = SetupAbility(entity, abilityWithOwned, new ScalableInt(1), out _);

		handle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		handle.IsActive.Should().BeTrue();

		handle!.Activate(out activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		handle.IsActive.Should().BeTrue();

		handle!.Activate(out activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		handle.IsActive.Should().BeTrue();

		entity.Tags.CombinedTags.HasAll(ownedTags).Should().BeTrue();

		handle.End();
		entity.Tags.CombinedTags.HasAll(ownedTags).Should().BeTrue();

		handle.End();
		handle.End();
		entity.Tags.CombinedTags.HasAny(ownedTags).Should().BeFalse();
	}

	[Fact]
	[Trait("ActivationOwnedTags", null)]
	public void Activation_owned_tags_enable_other_ability_only_while_active()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var buffTag = new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["tag"]));

		AbilityData giver = CreateAbilityData(
			"Giver",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute1",
			new ScalableFloat(-1),
			activationOwnedTags: buffTag);

		AbilityData requiresBuff = CreateAbilityData(
			"NeedsBuff",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute5",
			new ScalableFloat(-1),
			activationRequiredTags: buffTag);

		AbilityHandle? giverHandle = SetupAbility(entity, giver, new ScalableInt(1), out _);
		AbilityHandle? needsHandle = SetupAbility(entity, requiresBuff, new ScalableInt(1), out _);

		// Cannot activate without buff.
		needsHandle!.Activate(out AbilityActivationResult activationResult).Should().BeFalse();
		activationResult.Should().Be(AbilityActivationResult.FailedOwnerTagRequirements);
		needsHandle.IsActive.Should().BeFalse();

		// Gain buff, then can activate.
		giverHandle!.Activate(out activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		needsHandle.Activate(out activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);

		// Lose buff, then cannot activate again.
		giverHandle.End();
		needsHandle.End();
		needsHandle.Activate(out activationResult).Should().BeFalse();
		activationResult.Should().Be(AbilityActivationResult.FailedOwnerTagRequirements);
	}

	[Fact]
	[Trait("Bookkeeping", null)]
	public void OnAbilityDeactivated_is_fired_once_per_instance_end()
	{
		// Proxy via RemoveOnEnd semantics: ability is only removed after each instance ends once.
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"RemoveOnEndProxy",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute1",
			new ScalableFloat(-1),
			instancingPolicy: AbilityInstancingPolicy.PerExecution);

		AbilityHandle? handle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out ActiveEffectHandle? grantHandle,
			AbilityDeactivationPolicy.RemoveOnEnd,
			AbilityDeactivationPolicy.RemoveOnEnd);

		handle.Should().NotBeNull();
		grantHandle.Should().NotBeNull();

		// Activate twice to simulate two instances.
		handle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		handle!.Activate(out activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);

		// Remove grant; ability should not be removed until all instances end.
		entity.EffectsManager.UnapplyEffect(grantHandle!);

		// Still present because policy is RemoveOnEnd and still active.
		entity.Abilities.GrantedAbilities.Should().Contain(handle);

		// End one instance; still granted, one more end needed.
		handle.End();
		entity.Abilities.GrantedAbilities.Should().Contain(handle);

		// End last instance; now removed.
		handle.End();
		entity.Abilities.GrantedAbilities.Should().NotContain(handle);
	}

	[Fact]
	[Trait("Instancing", null)]
	public void Persistent_instance_reference_is_cleared_on_end()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"PersistentCleared",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute3",
			new ScalableFloat(-1),
			instancingPolicy: AbilityInstancingPolicy.PerEntity);

		AbilityHandle? handle = SetupAbility(entity, abilityData, new ScalableInt(1), out _);
		handle.Should().NotBeNull();

		handle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		handle.End();
		handle.IsActive.Should().BeFalse();

		// Should be able to activate again, implying the persistent instance was cleared.
		handle.Activate(out activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
	}

	[Fact]
	[Trait("CancelAbilitiesWithTag", null)]
	public void CancelAbilitiesWithTag_with_no_active_abilities_does_nothing()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var cancelTags = new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.red"]));

		AbilityData canceller = CreateAbilityData(
			"Canceller",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute2",
			new ScalableFloat(-1),
			cancelAbilitiesWithTag: cancelTags);

		AbilityData victim = CreateAbilityData(
			"Victim",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute3",
			new ScalableFloat(-1),
			abilityTags: new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.red"])));

		AbilityHandle? cancellerHandle = SetupAbility(entity, canceller, new ScalableInt(1), out _);
		AbilityHandle? victimHandle = SetupAbility(entity, victim, new ScalableInt(1), out _);

		// Victim is granted but inactive.
		victimHandle!.IsActive.Should().BeFalse();

		// Activating canceller should not affect inactive victim.
		cancellerHandle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		victimHandle.IsActive.Should().BeFalse();
		entity.Abilities.GrantedAbilities.Should().Contain(victimHandle);
	}

	[Fact]
	[Trait("CancelAbilitiesWithTag", null)]
	public void CancelAbilitiesWithTag_executes_before_applying_blocking_or_activation_tags()
	{
		// Approximated: ensure canceller activates and cancels victim reliably.
		TestEntity entity = new(_tagsManager, _cuesManager);

		var redTags = new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.red"]));
		var blockBlue = new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.blue"]));

		AbilityData canceller = CreateAbilityData(
			"CancellerOrder",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute1",
			new ScalableFloat(-1),
			cancelAbilitiesWithTag: redTags,
			blockAbilitiesWithTag: blockBlue,
			activationOwnedTags: new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["tag"])));

		AbilityData victim = CreateAbilityData(
			"VictimOrder",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute2",
			new ScalableFloat(-1),
			abilityTags: redTags);

		AbilityHandle? cancellerHandle = SetupAbility(entity, canceller, new ScalableInt(1), out _);
		AbilityHandle? victimHandle = SetupAbility(entity, victim, new ScalableInt(1), out _);

		victimHandle!.Activate(out AbilityActivationResult activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
		cancellerHandle!.Activate(out activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);

		// Victim must be canceled; canceller remains active.
		victimHandle.IsActive.Should().BeFalse();
		cancellerHandle.IsActive.Should().BeTrue();
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
			"Grant Ability Effect",
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

	private AbilityData CreateAbilityData(
		string abilityName,
		ScalableFloat cooldownDuration,
		string costAttribute,
		ScalableFloat costAmount,
		TagContainer? abilityTags = null,
		AbilityInstancingPolicy instancingPolicy = AbilityInstancingPolicy.PerEntity,
		bool retriggerInstancedAbility = false,
		TagContainer? cancelAbilitiesWithTag = null,
		TagContainer? blockAbilitiesWithTag = null,
		TagContainer? activationOwnedTags = null,
		TagContainer? activationRequiredTags = null,
		TagContainer? activationBlockedTags = null,
		TagContainer? sourceRequiredTags = null,
		TagContainer? sourceBlockedTags = null,
		TagContainer? targetRequiredTags = null,
		TagContainer? targetBlockedTags = null)
	{
		var cooldownEffectData = new EffectData(
			"Fireball Cooldown",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, cooldownDuration)),
			effectComponents:
			[
				new ModifierTagsEffectComponent(new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["simple.tag"])))
			]);

		var costEffectData = new EffectData(
			"Fireball Cost",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					costAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, costAmount))
			]);

		return new(
			abilityName,
			costEffectData,
			cooldownEffectData,
			AbilityTags: abilityTags,
			InstancingPolicy: instancingPolicy,
			RetriggerInstancedAbility: retriggerInstancedAbility,
			CancelAbilitiesWithTag: cancelAbilitiesWithTag,
			BlockAbilitiesWithTag: blockAbilitiesWithTag,
			ActivationOwnedTags: activationOwnedTags,
			ActivationRequiredTags: activationRequiredTags,
			ActivationBlockedTags: activationBlockedTags,
			SourceRequiredTags: sourceRequiredTags,
			SourceBlockedTags: sourceBlockedTags,
			TargetRequiredTags: targetRequiredTags,
			TargetBlockedTags: targetBlockedTags);
	}
}
