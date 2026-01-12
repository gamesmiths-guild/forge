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
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Events;
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
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle.Should().NotBeNull();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		abilityHandle.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("Remove ability", null)]
	public void Removed_ability_is_deactivated_immediately()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
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

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		abilityHandle.IsActive.Should().BeTrue();

		entity.EffectsManager.RemoveEffect(effectHandle!);

		entity.Abilities.GrantedAbilities.Should().BeEmpty();
		abilityHandle.IsActive.Should().BeFalse();

		abilityHandle.Activate(out failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.InvalidHandler);
	}

	[Fact]
	[Trait("Remove ability", null)]
	public void Ability_is_removed_only_after_deactivation_when_granted_with_RemoveOnEnd()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
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

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		abilityHandle.IsActive.Should().BeTrue();

		entity.EffectsManager.RemoveEffect(effectHandle!);

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle.Cancel();

		entity.Abilities.GrantedAbilities.Should().BeEmpty();
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Remove ability", null)]
	public void Grants_with_different_remove_policies_work_independently()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out ActiveEffectHandle? effectHandle,
			AbilityDeactivationPolicy.RemoveOnEnd,
			AbilityDeactivationPolicy.RemoveOnEnd);

		AbilityHandle? abilityHandle2 = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out ActiveEffectHandle? effectHandle2,
			AbilityDeactivationPolicy.CancelImmediately,
			AbilityDeactivationPolicy.RemoveOnEnd);

		abilityHandle.Should().NotBeNull();
		abilityHandle2.Should().Be(abilityHandle);
		effectHandle.Should().NotBeNull();
		effectHandle2.Should().NotBeNull();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		abilityHandle.IsActive.Should().BeTrue();

		entity.EffectsManager.RemoveEffect(effectHandle!);
		entity.Abilities.GrantedAbilities.Should().ContainSingle();
		abilityHandle.IsActive.Should().BeTrue();

		entity.EffectsManager.RemoveEffect(effectHandle2!);

		entity.Abilities.GrantedAbilities.Should().BeEmpty();
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Remove ability", null)]
	public void CancelImmediately_takes_precedence_and_removes_the_ability_immediately()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out ActiveEffectHandle? effectHandle,
			AbilityDeactivationPolicy.RemoveOnEnd,
			AbilityDeactivationPolicy.Ignore);

		AbilityHandle? abilityHandle2 = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out ActiveEffectHandle? effectHandle2,
			AbilityDeactivationPolicy.CancelImmediately,
			AbilityDeactivationPolicy.Ignore);

		abilityHandle.Should().NotBeNull();
		abilityHandle2.Should().Be(abilityHandle);
		effectHandle.Should().NotBeNull();
		effectHandle2.Should().NotBeNull();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		abilityHandle.IsActive.Should().BeTrue();

		entity.EffectsManager.RemoveEffect(effectHandle!);
		entity.EffectsManager.RemoveEffect(effectHandle2!);
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
			[new ScalableFloat(3f)],
			["simple.tag"],
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

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		abilityHandle.IsActive.Should().BeTrue();

		ActiveEffectHandle? tagEffectHandle = CreateAndApplyTagEffect(entity, ignoreTags!);

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle.Activate(out failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.Inhibited);
		abilityHandle.IsActive.Should().BeFalse();
		abilityHandle.IsInhibited.Should().BeTrue();

		entity.EffectsManager.RemoveEffect(tagEffectHandle!);

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
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
			[new ScalableFloat(3f)],
			["simple.tag"],
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

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		abilityHandle.IsActive.Should().BeTrue();

		entity.EffectsManager.RemoveEffect(effectHandle!);

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle.Cancel();

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
			[new ScalableFloat(3f)],
			["simple.tag"],
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

		entity.EffectsManager.RemoveEffect(effectHandle1!);

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		entity.EffectsManager.RemoveEffect(effectHandle2!);

		entity.Abilities.GrantedAbilities.Should().BeEmpty();
	}

	[Fact]
	[Trait("Grant ability", null)]
	public void Ability_is_not_granted_if_target_has_blocking_tags()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
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
			[new ScalableFloat(3f)],
			["simple.tag"],
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
		entity.EffectsManager.RemoveEffect(temporaryEffectHandle!);

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
			[new ScalableFloat(3f)],
			["simple.tag"],
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
		entity.EffectsManager.RemoveEffect(temporaryEffectHandle!);

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
			[new ScalableFloat(3f)],
			["simple.tag"],
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
			[new ScalableFloat(3f)],
			["simple.tag"],
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
			[new ScalableFloat(3f)],
			["simple.tag"],
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
		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
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
			[new ScalableFloat(3f)],
			["simple.tag"],
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
			[new ScalableFloat(3f)],
			["simple.tag"],
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

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		abilityHandle.IsActive.Should().BeTrue();

		// Inhibit the granting effect.
		CreateAndApplyTagEffect(entity, ignoreTags!);

		// With RemoveOnEnd policy, the ability is not inhibited while active.
		abilityHandle.IsInhibited.Should().BeFalse();
		abilityHandle.IsActive.Should().BeTrue();

		// End the ability.
		abilityHandle.Cancel();

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
			[new ScalableFloat(3f)],
			["simple.tag"],
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

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);

		// Inhibit the granting effect.
		CreateAndApplyTagEffect(entity, ignoreTags!);

		// With Ignore policy, the ability is never inhibited.
		abilityHandle.IsInhibited.Should().BeFalse();
		abilityHandle.IsActive.Should().BeTrue();

		abilityHandle.Cancel();

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
			[new ScalableFloat(3f)],
			["simple.tag"],
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
	[Trait("Inhibit ability", null)]
	public void Grants_with_different_inhibit_policies_work_independently()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		TagContainer? ignoreTags = Tag.RequestTag(_tagsManager, "tag").GetSingleTagContainer();
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

		TagContainer? ignoreTags2 = Tag.RequestTag(_tagsManager, "simple.tag").GetSingleTagContainer();
		ignoreTags2.Should().NotBeNull();

		AbilityHandle? abilityHandle2 = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			grantedAbilityInhibitionPolicy: AbilityDeactivationPolicy.CancelImmediately,
			extraComponent: new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(
					IgnoreTags: ignoreTags2)));

		abilityHandle2.Should().NotBeNull();
		abilityHandle.Should().Be(abilityHandle2);

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		abilityHandle.IsActive.Should().BeTrue();
		abilityHandle2!.IsActive.Should().BeTrue();

		// Inhibit the granting effect with RemoveOnEnd policy.
		CreateAndApplyTagEffect(entity, ignoreTags!);

		// With RemoveOnEnd policy, the ability is not inhibited while active.
		abilityHandle.IsInhibited.Should().BeFalse();
		abilityHandle.IsActive.Should().BeTrue();
		abilityHandle2!.IsInhibited.Should().BeFalse();
		abilityHandle2.IsActive.Should().BeTrue();

		// Inhibit the second granting effect with CancelImmediately policy.
		CreateAndApplyTagEffect(entity, ignoreTags2!);

		// Now that it's no longer active, it should become inhibited.
		abilityHandle.IsActive.Should().BeFalse();
		abilityHandle.IsInhibited.Should().BeTrue();
		abilityHandle2.IsActive.Should().BeFalse();
		abilityHandle2.IsInhibited.Should().BeTrue();
	}

	[Fact]
	[Trait("Grant ability", null)]
	public void Ability_level_is_set_correctly()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
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
			[new ScalableFloat(3f)],
			["simple.tag"],
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
			[new ScalableFloat(3f)],
			["simple.tag"],
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
			[new ScalableFloat(3f)],
			["simple.tag"],
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
		abilityHandle1!.Activate(out AbilityActivationFailures failureFlags1).Should().BeTrue();
		failureFlags1.Should().Be(AbilityActivationFailures.None);
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
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		abilityHandle.IsActive.Should().BeTrue();

		abilityHandle.CommitCooldown();
		abilityHandle.Cancel();

		abilityHandle!.Activate(out failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.Cooldown);

		entity.EffectsManager.UpdateEffects(2f);

		abilityHandle!.Activate(out failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.Cooldown);

		entity.EffectsManager.UpdateEffects(1f);

		abilityHandle!.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
	}

	[Fact]
	[Trait("Cooldown", null)]
	public void Ability_wont_activate_until_last_cooldown_effect_is_removed()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f), new ScalableFloat(1f)],
			["simple.tag", "tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		abilityHandle.IsActive.Should().BeTrue();

		abilityHandle.CommitCooldown();
		abilityHandle.Cancel();

		abilityHandle!.Activate(out failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.Cooldown);

		entity.EffectsManager.UpdateEffects(2f);

		abilityHandle!.Activate(out failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.Cooldown);

		entity.EffectsManager.UpdateEffects(1f);

		abilityHandle!.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
	}

	[Fact]
	[Trait("Cooldown", null)]
	public void GetCooldownData_and_GetRemainingCooldownTime_return_correct_values()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f), new ScalableFloat(1f)],
			["simple.tag", "tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		abilityHandle.IsActive.Should().BeTrue();

		CooldownData[]? cooldownData = abilityHandle.GetCooldownData()!;
		cooldownData.Should().HaveCount(2);

		var simpleTag = Tag.RequestTag(_tagsManager, "simple.tag");
		var tag = Tag.RequestTag(_tagsManager, "tag");

		cooldownData[0].TotalTime.Should().Be(3f);
		cooldownData[0].RemainingTime.Should().Be(0f);
		cooldownData[0].CooldownTags.Should().Contain(simpleTag);

		cooldownData[1].TotalTime.Should().Be(1f);
		cooldownData[1].RemainingTime.Should().Be(0f);
		cooldownData[1].CooldownTags.Should().Contain(tag);

		abilityHandle.GetRemainingCooldownTime(simpleTag).Should().Be(0f);
		abilityHandle.GetRemainingCooldownTime(tag).Should().Be(0f);

		abilityHandle.CommitCooldown();
		abilityHandle.Cancel();

		abilityHandle!.Activate(out failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.Cooldown);

		cooldownData = abilityHandle.GetCooldownData()!;
		cooldownData.Should().HaveCount(2);

		cooldownData[0].TotalTime.Should().Be(3f);
		cooldownData[0].RemainingTime.Should().Be(3f);
		cooldownData[0].CooldownTags.Should().Contain(simpleTag);

		cooldownData[1].TotalTime.Should().Be(1f);
		cooldownData[1].RemainingTime.Should().Be(1f);
		cooldownData[1].CooldownTags.Should().Contain(tag);

		abilityHandle.GetRemainingCooldownTime(simpleTag).Should().Be(3f);
		abilityHandle.GetRemainingCooldownTime(tag).Should().Be(1f);

		entity.EffectsManager.UpdateEffects(0.5f);

		abilityHandle!.Activate(out failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.Cooldown);

		cooldownData = abilityHandle.GetCooldownData()!;
		cooldownData.Should().HaveCount(2);

		cooldownData[0].TotalTime.Should().Be(3f);
		cooldownData[0].RemainingTime.Should().Be(2.5f);
		cooldownData[0].CooldownTags.Should().Contain(simpleTag);

		cooldownData[1].TotalTime.Should().Be(1f);
		cooldownData[1].RemainingTime.Should().Be(0.5f);
		cooldownData[1].CooldownTags.Should().Contain(tag);

		abilityHandle.GetRemainingCooldownTime(simpleTag).Should().Be(2.5f);
		abilityHandle.GetRemainingCooldownTime(tag).Should().Be(0.5f);

		entity.EffectsManager.UpdateEffects(1f);

		cooldownData = abilityHandle.GetCooldownData()!;
		cooldownData.Should().HaveCount(2);

		cooldownData[0].TotalTime.Should().Be(3f);
		cooldownData[0].RemainingTime.Should().Be(1.5f);
		cooldownData[0].CooldownTags.Should().Contain(simpleTag);

		cooldownData[1].TotalTime.Should().Be(1f);
		cooldownData[1].RemainingTime.Should().Be(0f);
		cooldownData[1].CooldownTags.Should().Contain(tag);

		abilityHandle.GetRemainingCooldownTime(simpleTag).Should().Be(1.5f);
		abilityHandle.GetRemainingCooldownTime(tag).Should().Be(0f);

		entity.EffectsManager.UpdateEffects(2f);

		cooldownData = abilityHandle.GetCooldownData()!;
		cooldownData.Should().HaveCount(2);

		cooldownData[0].TotalTime.Should().Be(3f);
		cooldownData[0].RemainingTime.Should().Be(0f);
		cooldownData[0].CooldownTags.Should().Contain(simpleTag);

		cooldownData[1].TotalTime.Should().Be(1f);
		cooldownData[1].RemainingTime.Should().Be(0f);
		cooldownData[1].CooldownTags.Should().Contain(tag);

		abilityHandle.GetRemainingCooldownTime(simpleTag).Should().Be(0f);
		abilityHandle.GetRemainingCooldownTime(tag).Should().Be(0f);

		abilityHandle!.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
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
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(cost),
			retriggerInstancedAbility: true);

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		abilityHandle.IsActive.Should().BeTrue();

		abilityHandle.CommitCost();

		abilityHandle!.Activate(out failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.InsufficientResources);
	}

	[Fact]
	[Trait("OwnerTag requirements", null)]
	public void Ability_wont_activate_when_owner_missing_required_tag()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			activationRequiredTags: new TagContainer(
				_tagsManager, TestUtils.StringToTag(_tagsManager, ["tag"])));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.OwnerTagRequirements);
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("OwnerTag requirements", null)]
	public void Ability_wont_activate_when_owner_has_blocked_tag()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			activationBlockedTags: new TagContainer(
				_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.green"])));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.OwnerTagRequirements);
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
			[new ScalableFloat(3f)],
			["simple.tag"],
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

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.SourceTagRequirements);
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
			[new ScalableFloat(3f)],
			["simple.tag"],
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

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.SourceTagRequirements);
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("SourceTag requirements", null)]
	public void Ability_wont_activate_when_source_is_missing_but_has_required_tags()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
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

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.SourceTagRequirements);
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("SourceTag requirements", null)]
	public void Ability_activates_when_source_is_missing_and_has_blocked_tags()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
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

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
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
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			targetRequiredTags: new TagContainer(
				_tagsManager, TestUtils.StringToTag(_tagsManager, ["tag"])));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags, target).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.TargetTagRequirements);
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
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			targetBlockedTags: new TagContainer(
				_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.green"])));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags, target).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.TargetTagRequirements);
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("TargetTag requirements", null)]
	public void Ability_wont_activate_when_target_is_missing_but_has_required_tags()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			targetRequiredTags: new TagContainer(
				_tagsManager, TestUtils.StringToTag(_tagsManager, ["tag"])));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.TargetTagRequirements);
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("TargetTag requirements", null)]
	public void Ability_activates_when_target_is_missing_and_has_blocked_tags()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			targetBlockedTags: new TagContainer(
				_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.green"])));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		abilityHandle.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("BlockAbilitiesWithTag", null)]
	public void Ability_activation_blocks_other_abilities_with_blocked_tags()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData blockerAbilityData = CreateAbilityData(
			"Blocker ability",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			blockAbilitiesWithTag: new TagContainer(
				_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.red"])));

		AbilityData unblockedAbilityData = CreateAbilityData(
			"Unblocked ability",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			abilityTags: new TagContainer(
				_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.blue"])));

		AbilityData blockedAbilityData = CreateAbilityData(
			"Blocked ability",
			[new ScalableFloat(3f)],
			["simple.tag"],
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

		blockerAbilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		blockerAbilityHandle.IsActive.Should().BeTrue();

		unblockedAbilityHandle!.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		unblockedAbilityHandle.IsActive.Should().BeTrue();

		blockedAbilityHandle!.Activate(out failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.BlockedByTags);
		blockedAbilityHandle.IsActive.Should().BeFalse();

		blockerAbilityHandle!.Cancel();

		blockedAbilityHandle!.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		blockedAbilityHandle.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("Instancing", null)]
	public void Ability_PerEntity_no_retrigger_activated_twice_does_not_start_second_instance()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"PerEntity_NoRetrigger",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute1",
			new ScalableFloat(-1),
			instancingPolicy: AbilityInstancingPolicy.PerEntity,
			retriggerInstancedAbility: false);

		AbilityHandle? handle = SetupAbility(entity, abilityData, new ScalableInt(1), out _);
		handle.Should().NotBeNull();

		handle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		handle.IsActive.Should().BeTrue();

		// No retrigger, single instance.
		handle!.Activate(out failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.PersistentInstanceActive);
		handle.IsActive.Should().BeTrue();

		handle.Cancel();
		handle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Instancing", null)]
	public void Ability_PerEntity_retrigger_restarts_instance_and_fires_deactivated_once()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"PerEntity_Retrigger",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute2",
			new ScalableFloat(-1),
			instancingPolicy: AbilityInstancingPolicy.PerEntity,
			retriggerInstancedAbility: true);

		AbilityHandle? handle = SetupAbility(entity, abilityData, new ScalableInt(1), out _);
		handle.Should().NotBeNull();

		handle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		handle.IsActive.Should().BeTrue();

		// Retrigger replaces the running instance.
		handle!.Activate(out AbilityActivationFailures failureFlags2).Should().BeTrue();
		failureFlags2.Should().Be(AbilityActivationFailures.None);
		handle.IsActive.Should().BeTrue();

		// One End should fully deactivate because retrigger replaced the instance instead of stacking.
		handle.Cancel();
		handle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Instancing", null)]
	public void Ability_per_execution_multiple_activations_create_multiple_instances()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"PerExecution",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute3",
			new ScalableFloat(-1),
			instancingPolicy: AbilityInstancingPolicy.PerExecution);

		AbilityHandle? handle = SetupAbility(entity, abilityData, new ScalableInt(1), out _);
		handle.Should().NotBeNull();

		handle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		handle.IsActive.Should().BeTrue();

		handle!.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		handle.IsActive.Should().BeTrue();

		handle!.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		handle.IsActive.Should().BeTrue();

		// Cancel ends all instances.
		handle.Cancel();
		handle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Instancing", null)]
	public void Ability_Cancel_ends_all_instances()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"EndsMostRecent",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute5",
			new ScalableFloat(-1),
			instancingPolicy: AbilityInstancingPolicy.PerExecution);

		AbilityHandle? handle = SetupAbility(entity, abilityData, new ScalableInt(1), out _);
		handle.Should().NotBeNull();

		handle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		handle.IsActive.Should().BeTrue();

		handle!.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		handle.IsActive.Should().BeTrue();

		// One Cancel should fully deactivate if multiple instances exist.
		handle.Cancel();
		handle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Instancing", null)]
	public void Ability_CancelAbility_cancels_all_active_instances()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"CancelAllInstances",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute5",
			new ScalableFloat(-1),
			instancingPolicy: AbilityInstancingPolicy.PerExecution);

		AbilityHandle? handle = SetupAbility(entity, abilityData, new ScalableInt(1), out _);
		handle.Should().NotBeNull();

		handle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		handle.IsActive.Should().BeTrue();

		handle!.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		handle.IsActive.Should().BeTrue();

		handle!.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
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
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute3",
			new ScalableFloat(-1),
			cancelAbilitiesWithTag: cancelTags);

		AbilityData victim = CreateAbilityData(
			"Victim",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute5",
			new ScalableFloat(-1),
			abilityTags: new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.red"])));

		AbilityHandle? cancellerHandle = SetupAbility(entity, canceller, new ScalableInt(1), out _);
		AbilityHandle? victimHandle = SetupAbility(entity, victim, new ScalableInt(1), out _);

		victimHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		victimHandle.IsActive.Should().BeTrue();

		cancellerHandle!.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
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
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute3",
			new ScalableFloat(-1),
			cancelAbilitiesWithTag: cancelTags);

		AbilityData unrelated = CreateAbilityData(
			"Unrelated",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute5",
			new ScalableFloat(-1),
			abilityTags: new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.blue"])));

		AbilityHandle? cancellerHandle = SetupAbility(entity, canceller, new ScalableInt(1), out _);
		AbilityHandle? unrelatedHandle = SetupAbility(entity, unrelated, new ScalableInt(1), out _);

		unrelatedHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		unrelatedHandle.IsActive.Should().BeTrue();

		cancellerHandle!.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
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
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute1",
			new ScalableFloat(-1),
			cancelAbilitiesWithTag: cancelTags);

		AbilityData victim = CreateAbilityData(
			"VictimMulti",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute2",
			new ScalableFloat(-1),
			abilityTags: new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.red"])),
			instancingPolicy: AbilityInstancingPolicy.PerExecution);

		AbilityHandle? cancellerHandle = SetupAbility(entity, canceller, new ScalableInt(1), out _);
		AbilityHandle? victimHandle = SetupAbility(entity, victim, new ScalableInt(1), out _);

		victimHandle!.Activate(out AbilityActivationFailures failureFlagsA).Should().BeTrue();
		failureFlagsA.Should().Be(AbilityActivationFailures.None);
		victimHandle.IsActive.Should().BeTrue();

		cancellerHandle!.Activate(out AbilityActivationFailures failureFlagsB).Should().BeTrue();
		failureFlagsB.Should().Be(AbilityActivationFailures.None);
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
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute1",
			new ScalableFloat(-1),
			abilityTags: redTags,
			cancelAbilitiesWithTag: redTags);

		AbilityHandle? handle = SetupAbility(entity, selfCanceller, new ScalableInt(1), out _);

		handle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		handle.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("BlockAbilitiesWithTag", null)]
	public void Blocked_ability_tags_are_removed_after_all_instance_ends()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var redTags = new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.red"]));

		AbilityData blocker = CreateAbilityData(
			"BlockerMulti",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute1",
			new ScalableFloat(-1),
			instancingPolicy: AbilityInstancingPolicy.PerExecution,
			blockAbilitiesWithTag: redTags);

		AbilityData blocked = CreateAbilityData(
			"BlockedRed",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute5",
			new ScalableFloat(-1),
			abilityTags: redTags);

		AbilityHandle? blockerHandle = SetupAbility(entity, blocker, new ScalableInt(1), out _);
		AbilityHandle? blockedHandle = SetupAbility(entity, blocked, new ScalableInt(1), out _);

		blockerHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		blockerHandle.IsActive.Should().BeTrue();

		blockerHandle!.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		blockerHandle.IsActive.Should().BeTrue();

		// While any blocker instance active, blocked ability cannot activate.
		blockedHandle!.Activate(out failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.BlockedByTags);
		blockedHandle.IsActive.Should().BeFalse();

		// End all blocker instances.
		blockerHandle.Cancel();
		blockedHandle.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		blockedHandle.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("ActivationOwnedTags", null)]
	public void Activation_owned_tags_are_applied_on_activation_and_removed_on_Cancel()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var ownedTags = new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["tag"]));

		AbilityData abilityWithOwned = CreateAbilityData(
			"OwnedTagsAbility",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute5",
			new ScalableFloat(-1),
			activationOwnedTags: ownedTags);

		AbilityHandle? handle = SetupAbility(entity, abilityWithOwned, new ScalableInt(1), out _);

		handle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		entity.Tags.CombinedTags.HasAll(ownedTags).Should().BeTrue();
		handle.IsActive.Should().BeTrue();

		handle.Cancel();
		entity.Tags.CombinedTags.HasAny(ownedTags).Should().BeFalse();
	}

	[Fact]
	[Trait("ActivationOwnedTags", null)]
	public void Activation_owned_tags_are_applied_on_activation_and_removed_when_all_instances_ends()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var ownedTags = new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["tag"]));

		AbilityData abilityWithOwned = CreateAbilityData(
			"OwnedTagsAbility",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute5",
			new ScalableFloat(-1),
			instancingPolicy: AbilityInstancingPolicy.PerExecution,
			activationOwnedTags: ownedTags);

		AbilityHandle? handle = SetupAbility(entity, abilityWithOwned, new ScalableInt(1), out _);

		handle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		handle.IsActive.Should().BeTrue();

		handle!.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		handle.IsActive.Should().BeTrue();

		handle!.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		handle.IsActive.Should().BeTrue();

		entity.Tags.CombinedTags.HasAll(ownedTags).Should().BeTrue();

		handle.Cancel();
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
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute1",
			new ScalableFloat(-1),
			activationOwnedTags: buffTag);

		AbilityData requiresBuff = CreateAbilityData(
			"NeedsBuff",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute5",
			new ScalableFloat(-1),
			activationRequiredTags: buffTag);

		AbilityHandle? giverHandle = SetupAbility(entity, giver, new ScalableInt(1), out _);
		AbilityHandle? needsHandle = SetupAbility(entity, requiresBuff, new ScalableInt(1), out _);

		// Cannot activate without buff.
		needsHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.OwnerTagRequirements);
		needsHandle.IsActive.Should().BeFalse();

		// Gain buff, then can activate.
		giverHandle!.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		needsHandle.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);

		// Lose buff, then cannot activate again.
		giverHandle.Cancel();
		needsHandle.Cancel();
		needsHandle.Activate(out failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.OwnerTagRequirements);
	}

	[Fact]
	[Trait("Bookkeeping", null)]
	public void Granted_ability_is_removed_when_all_instances_end()
	{
		// Proxy via RemoveOnEnd semantics: ability is only removed after each instance ends once.
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"RemoveOnEndProxy",
			[new ScalableFloat(3f)],
			["simple.tag"],
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
		handle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		handle!.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);

		// Remove grant; ability should not be removed until all instances end.
		entity.EffectsManager.RemoveEffect(grantHandle!);

		// Still present because policy is RemoveOnEnd and still active.
		entity.Abilities.GrantedAbilities.Should().Contain(handle);

		// End all instances, remove grant.
		handle.Cancel();
		entity.Abilities.GrantedAbilities.Should().NotContain(handle);
	}

	[Fact]
	[Trait("Instancing", null)]
	public void Persistent_instance_reference_is_cleared_on_Cancel()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"PersistentCleared",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute3",
			new ScalableFloat(-1),
			instancingPolicy: AbilityInstancingPolicy.PerEntity);

		AbilityHandle? handle = SetupAbility(entity, abilityData, new ScalableInt(1), out _);
		handle.Should().NotBeNull();

		handle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		handle.Cancel();
		handle.IsActive.Should().BeFalse();

		// Should be able to activate again, implying the persistent instance was cleared.
		handle.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
	}

	[Fact]
	[Trait("CancelAbilitiesWithTag", null)]
	public void CancelAbilitiesWithTag_with_no_active_abilities_does_nothing()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var cancelTags = new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.red"]));

		AbilityData canceller = CreateAbilityData(
			"Canceller",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute2",
			new ScalableFloat(-1),
			cancelAbilitiesWithTag: cancelTags);

		AbilityData victim = CreateAbilityData(
			"Victim",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute3",
			new ScalableFloat(-1),
			abilityTags: new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.red"])));

		AbilityHandle? cancellerHandle = SetupAbility(entity, canceller, new ScalableInt(1), out _);
		AbilityHandle? victimHandle = SetupAbility(entity, victim, new ScalableInt(1), out _);

		// Victim is granted but inactive.
		victimHandle!.IsActive.Should().BeFalse();

		// Activating canceller should not affect inactive victim.
		cancellerHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
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
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute1",
			new ScalableFloat(-1),
			cancelAbilitiesWithTag: redTags,
			blockAbilitiesWithTag: blockBlue,
			activationOwnedTags: new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["tag"])));

		AbilityData victim = CreateAbilityData(
			"VictimOrder",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute2",
			new ScalableFloat(-1),
			abilityTags: redTags);

		AbilityHandle? cancellerHandle = SetupAbility(entity, canceller, new ScalableInt(1), out _);
		AbilityHandle? victimHandle = SetupAbility(entity, victim, new ScalableInt(1), out _);

		victimHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		cancellerHandle!.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);

		// Victim must be canceled; canceller remains active.
		victimHandle.IsActive.Should().BeFalse();
		cancellerHandle.IsActive.Should().BeTrue();
	}

	[Theory]
	[InlineData(ModifierOperation.FlatBonus, -1f, -1, 89)]
	[InlineData(ModifierOperation.FlatBonus, 1f, 1, 91)]
	[InlineData(ModifierOperation.PercentBonus, -0.1f, -9, 81)]
	[InlineData(ModifierOperation.PercentBonus, 0.1f, 9, 99)]
	[InlineData(ModifierOperation.Override, 1f, -89, 1)]
	[InlineData(ModifierOperation.Override, 98f, 8, 98)]
	[Trait("Ability cost", null)]
	public void Cost_effect_reports_configured_cost_and_applies_on_commit(
		ModifierOperation operation, float magnitude, int expectedCost, int finalValue)
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var costEffectData = new EffectData(
			"Fireball Cost",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					"TestAttributeSet.Attribute90",
					operation,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(magnitude)))
			]);

		AbilityData abilityData = new("Fireball", costEffectData);

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle.Should().NotBeNull();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		CostData[]? costData = abilityHandle!.GetCostData();
		costData.Should().ContainSingle();
		costData![0].Attribute.Should().Be("TestAttributeSet.Attribute90");
		costData![0].Cost.Should().Be(expectedCost);

		abilityHandle.GetCostForAttribute("TestAttributeSet.Attribute90").Should().Be(expectedCost);

		abilityHandle.CommitCost();
		entity.Attributes["TestAttributeSet.Attribute90"].CurrentValue.Should().Be(finalValue);
	}

	[Fact]
	[Trait("Ability cost", null)]
	public void Cost_effect_with_multiple_modifiers_reports_configured_cost_and_applies_on_commit()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var costEffectData = new EffectData(
			"Fireball Cost",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					"TestAttributeSet.Attribute90",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(1f))),
				new Modifier(
					"TestAttributeSet.Attribute90",
					ModifierOperation.PercentBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-0.1f)))
			]);

		AbilityData abilityData = new("Fireball", costEffectData);

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle.Should().NotBeNull();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		CostData[]? costData = abilityHandle!.GetCostData();
		costData.Should().ContainSingle();
		costData![0].Attribute.Should().Be("TestAttributeSet.Attribute90");
		costData![0].Cost.Should().Be(-9);

		abilityHandle.GetCostForAttribute("TestAttributeSet.Attribute90").Should().Be(-9);

		abilityHandle.CommitCost();
		entity.Attributes["TestAttributeSet.Attribute90"].CurrentValue.Should().Be(81);
	}

	[Fact]
	[Trait("Event", null)]
	public void Ability_gets_activated_by_proper_event_with_tag()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var triggerTag = Tag.RequestTag(_tagsManager, "color.red");

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			abilityTriggerData: AbilityTriggerData.ForEvent(triggerTag));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		var nonActivatingEventData = new EventData
		{
			EventTags = new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.blue"])),
			Source = entity,
			Target = entity,
		};

		entity.Events.Raise(in nonActivatingEventData);
		abilityHandle!.IsActive.Should().BeFalse();

		var activatingEventData = new EventData
		{
			EventTags = new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.red"])),
			Source = entity,
			Target = entity,
		};

		entity.Events.Raise(in activatingEventData);
		abilityHandle.IsActive.Should().BeTrue();

		abilityHandle.Cancel();
		abilityHandle!.IsActive.Should().BeFalse();

		entity.Events.Raise(in activatingEventData);
		abilityHandle.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("Event", null)]
	public void Ability_gets_activated_by_tag_addition()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var triggerTag = Tag.RequestTag(_tagsManager, "color.red");
		TagContainer? triggerTagContainer = triggerTag.GetSingleTagContainer();

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			abilityTriggerData: AbilityTriggerData.ForTagAdded(triggerTag));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle!.IsActive.Should().BeFalse();

		CreateAndApplyTagEffect(entity, triggerTagContainer!);

		abilityHandle!.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("Event", null)]
	public void Ability_gets_activated_while_tag_present()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var triggerTag = Tag.RequestTag(_tagsManager, "color.red");
		TagContainer? triggerTagContainer = triggerTag.GetSingleTagContainer();

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			abilityTriggerData: AbilityTriggerData.ForTagPresent(triggerTag));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle!.IsActive.Should().BeFalse();

		ActiveEffectHandle? effectHandle = CreateAndApplyTagEffect(entity, triggerTagContainer!);

		abilityHandle!.IsActive.Should().BeTrue();

		entity.EffectsManager.RemoveEffect(effectHandle!);

		abilityHandle!.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Ability Ended Event", null)]
	public void OnAbilityEnded_fires_when_ability_instance_is_canceled()
	{
		var targetEntity = new TestEntity(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Test Ability",
			[],
			[],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			instancingPolicy: AbilityInstancingPolicy.PerEntity);

		AbilityHandle? abilityHandle = SetupAbility(
			targetEntity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle.Should().NotBeNull();

		AbilityEndedData? capturedData = null;

		targetEntity.Abilities.OnAbilityEnded += x => { capturedData = x; };

		// Activate the ability
		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		abilityHandle.Cancel();

		// Verify event was fired
		capturedData.Should().NotBeNull();
		capturedData!.Value.Ability.Should().Be(abilityHandle);
		capturedData.Value.WasCanceled.Should().BeTrue();
	}

	[Fact]
	[Trait("Grant ability", null)]
	public void Ability_is_granted_and_activated_once()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		AbilityHandle? abilityHandle = entity.Abilities.GrantAbilityAndActivateOnce(
			abilityData,
			1,
			LevelComparison.None,
			out AbilityActivationFailures failureFlags,
			entity,
			entity);

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		failureFlags.Should().Be(AbilityActivationFailures.None);
		abilityHandle!.IsActive.Should().BeTrue();

		abilityHandle.Cancel();
		entity.Abilities.GrantedAbilities.Should().BeEmpty();
	}

	[Fact]
	[Trait("Grant ability", null)]
	public void Ability_fails_to_be_granted_and_activated_once()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-100));

		AbilityHandle? abilityHandle = entity.Abilities.GrantAbilityAndActivateOnce(
			abilityData,
			1,
			LevelComparison.None,
			out AbilityActivationFailures failureFlags,
			entity,
			entity);

		entity.Abilities.GrantedAbilities.Should().BeEmpty();
		failureFlags.Should().Be(AbilityActivationFailures.InsufficientResources);
		abilityHandle.Should().BeNull();
	}

	[Fact]
	[Trait("Failure reason", null)]
	public void Failure_reason_contains_all_failureFlags_reasons()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-50),
			retriggerInstancedAbility: true);

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _);

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		abilityHandle.IsActive.Should().BeTrue();

		abilityHandle.CommitAbility();

		abilityHandle!.Activate(out failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.InsufficientResources |
			AbilityActivationFailures.Cooldown);
	}

	[Fact]
	[Trait("Grant ability", null)]
	public void Periodic_effects_grants_ability_while_active()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out ActiveEffectHandle? effectHandle,
			durationData: new DurationData(DurationType.Infinite),
			periodicData: new PeriodicData(new ScalableFloat(1f), true, PeriodInhibitionRemovedPolicy.ResetPeriod));

		abilityHandle.Should().NotBeNull();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		abilityHandle.IsActive.Should().BeTrue();

		entity.EffectsManager.UpdateEffects(10f);

		abilityHandle.Should().NotBeNull();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		entity.EffectsManager.RemoveEffect(effectHandle!);

		entity.Abilities.GrantedAbilities.Should().BeEmpty();
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("TryActivateOnGrant", null)]
	public void Duration_effect_with_TryActivateOnGrant_activates_ability_immediately()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			tryActivateOnGrant: true);

		abilityHandle.Should().NotBeNull();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();
		abilityHandle!.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("TryActivateOnGrant", null)]
	public void Instant_effect_with_TryActivateOnGrant_activates_ability_immediately()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			durationData: new DurationData(DurationType.Instant),
			tryActivateOnGrant: true);

		abilityHandle.Should().NotBeNull();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();
		abilityHandle!.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("TryActivateOnGrant", null)]
	public void Effect_inhibited_at_grant_does_not_try_to_activate_ability()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		TagContainer? ignoreTags = Tag.RequestTag(_tagsManager, "Tag").GetSingleTagContainer();
		ignoreTags.Should().NotBeNull();

		// Apply the inhibiting tag before granting the ability
		CreateAndApplyTagEffect(entity, ignoreTags!);

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			extraComponent: new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(IgnoreTags: ignoreTags)),
			tryActivateOnGrant: true);

		abilityHandle.Should().NotBeNull();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();
		abilityHandle!.IsInhibited.Should().BeTrue();
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("TryActivateOnEnable", null)]
	public void Effect_enabled_from_inhibition_with_TryActivateOnEnable_activates_ability()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		TagContainer? ignoreTags = Tag.RequestTag(_tagsManager, "Tag").GetSingleTagContainer();
		ignoreTags.Should().NotBeNull();

		// Apply the inhibiting tag before granting the ability
		ActiveEffectHandle? tagEffectHandle = CreateAndApplyTagEffect(entity, ignoreTags!);

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			extraComponent: new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(IgnoreTags: ignoreTags)),
			tryActivateOnEnable: true);

		abilityHandle.Should().NotBeNull();
		abilityHandle!.IsInhibited.Should().BeTrue();
		abilityHandle.IsActive.Should().BeFalse();

		// Remove the inhibiting tag to enable the effect
		entity.EffectsManager.RemoveEffect(tagEffectHandle!);

		abilityHandle.IsInhibited.Should().BeFalse();
		abilityHandle.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("TryActivateOnEnable", null)]
	public void Effect_enabled_from_inhibition_without_TryActivateOnEnable_does_not_activate_ability()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		TagContainer? ignoreTags = Tag.RequestTag(_tagsManager, "Tag").GetSingleTagContainer();
		ignoreTags.Should().NotBeNull();

		// Apply the inhibiting tag before granting the ability
		ActiveEffectHandle? tagEffectHandle = CreateAndApplyTagEffect(entity, ignoreTags!);

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			extraComponent: new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(IgnoreTags: ignoreTags)),
			tryActivateOnEnable: false);

		abilityHandle.Should().NotBeNull();
		abilityHandle!.IsInhibited.Should().BeTrue();
		abilityHandle.IsActive.Should().BeFalse();

		// Remove the inhibiting tag to enable the effect
		entity.EffectsManager.RemoveEffect(tagEffectHandle!);

		abilityHandle.IsInhibited.Should().BeFalse();
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("TryActivateOnGrant", null)]
	public void TryActivateOnGrant_does_not_activate_if_CanActivate_fails()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		// Create ability that requires a tag that the entity doesn't have
		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			activationRequiredTags: new TagContainer(
				_tagsManager, TestUtils.StringToTag(_tagsManager, ["tag"])));

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			tryActivateOnGrant: true);

		abilityHandle.Should().NotBeNull();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		// Ability should be granted but not active because CanActivate fails (required tag)
		abilityHandle!.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("TryActivateOnGrant", null)]
	public void Both_TryActivateOnGrant_and_TryActivateOnEnable_work_together()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		TagContainer? ignoreTags = Tag.RequestTag(_tagsManager, "Tag").GetSingleTagContainer();
		ignoreTags.Should().NotBeNull();

		// Grant ability without inhibition should activate on grant
		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			extraComponent: new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(IgnoreTags: ignoreTags)),
			tryActivateOnGrant: true,
			tryActivateOnEnable: true);

		abilityHandle.Should().NotBeNull();
		abilityHandle!.IsActive.Should().BeTrue();

		// Cancel the ability
		abilityHandle.Cancel();
		abilityHandle.IsActive.Should().BeFalse();

		// Inhibit the effect
		ActiveEffectHandle? tagEffectHandle = CreateAndApplyTagEffect(entity, ignoreTags!);
		abilityHandle.IsInhibited.Should().BeTrue();

		// Remove inhibition - should activate on enable
		entity.EffectsManager.RemoveEffect(tagEffectHandle!);
		abilityHandle.IsInhibited.Should().BeFalse();
		abilityHandle.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("TryActivateOnEnable", null)]
	public void TryActivateOnEnable_does_not_activate_if_CanActivate_fails()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		TagContainer? requiredTag = Tag.RequestTag(_tagsManager, "other.tag").GetSingleTagContainer();
		requiredTag.Should().NotBeNull();

		// Create ability that requires a tag that the entity doesn't have
		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			[new ScalableFloat(3f)],
			["simple.tag"],
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1),
			activationRequiredTags: requiredTag);

		TagContainer? ignoreTags = Tag.RequestTag(_tagsManager, "Tag").GetSingleTagContainer();
		ignoreTags.Should().NotBeNull();

		// Apply the inhibiting tag before granting the ability
		ActiveEffectHandle? tagEffectHandle = CreateAndApplyTagEffect(entity, ignoreTags!);

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			extraComponent: new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(IgnoreTags: ignoreTags)),
			tryActivateOnEnable: true);

		abilityHandle.Should().NotBeNull();
		abilityHandle!.IsInhibited.Should().BeTrue();
		abilityHandle.IsActive.Should().BeFalse();

		// Remove the inhibiting tag to enable the effect
		entity.EffectsManager.RemoveEffect(tagEffectHandle!);

		// Ability should not activate because CanActivate fails (missing required tag)
		abilityHandle.IsInhibited.Should().BeFalse();
		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Cleanup", null)]
	public void Removed_ability_with_TagAdded_trigger_does_not_activate_after_removal()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var triggerTag = Tag.RequestTag(_tagsManager, "simple.tag");

		var activationCount = 0;

		AbilityData abilityData = new(
			"Triggered Ability",
			instancingPolicy: AbilityInstancingPolicy.PerEntity,
			abilityTriggerData: AbilityTriggerData.ForTagAdded(triggerTag),
			behaviorFactory: () => new CountingAbilityBehavior(() => activationCount++));

		// Grant the ability via effect
		var grantConfig = new GrantAbilityConfig(
			abilityData,
			new ScalableInt(1),
			AbilityDeactivationPolicy.CancelImmediately,
			AbilityDeactivationPolicy.CancelImmediately);

		var grantEffectData = new EffectData(
			"Grant Ability",
			new DurationData(DurationType.Infinite),
			effectComponents: [new GrantAbilityEffectComponent([grantConfig])]);

		var effect = new Effect(grantEffectData, new EffectOwnership(entity, entity));
		ActiveEffectHandle? effectHandle = entity.EffectsManager.ApplyEffect(effect);

		AbilityHandle abilityHandle = effectHandle!.GetComponent<GrantAbilityEffectComponent>()!.GrantedAbilities[0];
		abilityHandle.IsValid.Should().BeTrue();

		// Create an effect that adds the trigger tag
		TagContainer? triggerTagContainer = triggerTag.GetSingleTagContainer();
		var tagEffectData = new EffectData(
			"Add Trigger Tag",
			new DurationData(DurationType.Infinite),
			effectComponents: [new ModifierTagsEffectComponent(triggerTagContainer!)]);

		var tagEffect = new Effect(tagEffectData, new EffectOwnership(entity, entity));

		// Add tag to trigger the ability
		ActiveEffectHandle? tagEffectHandle = entity.EffectsManager.ApplyEffect(tagEffect);
		activationCount.Should().Be(1);

		// Remove the tag
		entity.EffectsManager.RemoveEffect(tagEffectHandle!);

		// Remove the granting effect (removes the ability)
		entity.EffectsManager.RemoveEffect(effectHandle!);
		abilityHandle.IsValid.Should().BeFalse();

		// Add tag again, this should NOT trigger the ability since it's been removed
		entity.EffectsManager.ApplyEffect(tagEffect);
		activationCount.Should().Be(1); // Should still be 1, not 2
	}

	[Fact]
	[Trait("Cleanup", null)]
	public void Removed_ability_with_TagPresent_trigger_does_not_activate_after_removal()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var triggerTag = Tag.RequestTag(_tagsManager, "simple.tag");

		var activationCount = 0;

		AbilityData abilityData = new(
			"Triggered Ability",
			instancingPolicy: AbilityInstancingPolicy.PerEntity,
			abilityTriggerData: AbilityTriggerData.ForTagPresent(triggerTag),
			behaviorFactory: () => new CountingAbilityBehavior(() => activationCount++));

		// Grant the ability via effect
		var grantConfig = new GrantAbilityConfig(
			abilityData,
			new ScalableInt(1),
			AbilityDeactivationPolicy.CancelImmediately,
			AbilityDeactivationPolicy.CancelImmediately);

		var grantEffectData = new EffectData(
			"Grant Ability",
			new DurationData(DurationType.Infinite),
			effectComponents: [new GrantAbilityEffectComponent([grantConfig])]);

		var effect = new Effect(grantEffectData, new EffectOwnership(entity, entity));
		ActiveEffectHandle? effectHandle = entity.EffectsManager.ApplyEffect(effect);

		AbilityHandle abilityHandle = effectHandle!.GetComponent<GrantAbilityEffectComponent>()!.GrantedAbilities[0];
		abilityHandle.IsValid.Should().BeTrue();

		// Create an effect that adds the trigger tag
		TagContainer? triggerTagContainer = triggerTag.GetSingleTagContainer();
		var tagEffectData = new EffectData(
			"Add Trigger Tag",
			new DurationData(DurationType.Infinite),
			effectComponents: [new ModifierTagsEffectComponent(triggerTagContainer!)]);

		var tagEffect = new Effect(tagEffectData, new EffectOwnership(entity, entity));

		// Add tag to trigger the ability
		ActiveEffectHandle? tagEffectHandle = entity.EffectsManager.ApplyEffect(tagEffect);
		activationCount.Should().Be(1);

		// Remove the tag (this will cancel the ability for TagPresent)
		entity.EffectsManager.RemoveEffect(tagEffectHandle!);

		// Remove the granting effect (removes the ability)
		entity.EffectsManager.RemoveEffect(effectHandle!);
		abilityHandle.IsValid.Should().BeFalse();

		// Add tag again, this should NOT trigger the ability since it's been removed
		entity.EffectsManager.ApplyEffect(tagEffect);
		activationCount.Should().Be(1); // Should still be 1, not 2
	}

	[Fact]
	[Trait("Cleanup", null)]
	public void Removed_ability_with_Event_trigger_does_not_activate_after_removal()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var eventTag = Tag.RequestTag(_tagsManager, "simple.tag");

		var activationCount = 0;

		AbilityData abilityData = new(
			"Triggered Ability",
			instancingPolicy: AbilityInstancingPolicy.PerEntity,
			abilityTriggerData: AbilityTriggerData.ForEvent(eventTag),
			behaviorFactory: () => new CountingAbilityBehavior(() => activationCount++));

		// Grant the ability via effect
		var grantConfig = new GrantAbilityConfig(
			abilityData,
			new ScalableInt(1),
			AbilityDeactivationPolicy.CancelImmediately,
			AbilityDeactivationPolicy.CancelImmediately);

		var grantEffectData = new EffectData(
			"Grant Ability",
			new DurationData(DurationType.Infinite),
			effectComponents: [new GrantAbilityEffectComponent([grantConfig])]);

		var effect = new Effect(grantEffectData, new EffectOwnership(entity, entity));
		ActiveEffectHandle? effectHandle = entity.EffectsManager.ApplyEffect(effect);

		AbilityHandle abilityHandle = effectHandle!.GetComponent<GrantAbilityEffectComponent>()!.GrantedAbilities[0];
		abilityHandle.IsValid.Should().BeTrue();

		// Raise event to trigger the ability
		entity.Events.Raise(new EventData
		{
			EventTags = eventTag.GetSingleTagContainer()!,
			Source = entity,
			Target = entity,
		});
		activationCount.Should().Be(1);

		// Remove the granting effect (removes the ability)
		entity.EffectsManager.RemoveEffect(effectHandle!);
		abilityHandle.IsValid.Should().BeFalse();

		// Raise event again, this should NOT trigger the ability since it's been removed
		entity.Events.Raise(new EventData
		{
			EventTags = eventTag.GetSingleTagContainer()!,
			Source = entity,
			Target = entity,
		});

		activationCount.Should().Be(1); // Should still be 1, not 2
	}

	[Fact]
	[Trait("Cleanup", null)]
	public void Removed_ability_with_typed_Event_trigger_does_not_activate_after_removal()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		var eventTag = Tag.RequestTag(_tagsManager, "simple.tag");

		var activationCount = 0;

		AbilityData abilityData = new(
			"Triggered Ability",
			instancingPolicy: AbilityInstancingPolicy.PerEntity,
			abilityTriggerData: AbilityTriggerData.ForEvent<int>(eventTag),
			behaviorFactory: () => new CountingAbilityBehavior(() => activationCount++));

		// Grant the ability via effect
		var grantConfig = new GrantAbilityConfig(
			abilityData,
			new ScalableInt(1),
			AbilityDeactivationPolicy.CancelImmediately,
			AbilityDeactivationPolicy.CancelImmediately);

		var grantEffectData = new EffectData(
			"Grant Ability",
			new DurationData(DurationType.Infinite),
			effectComponents: [new GrantAbilityEffectComponent([grantConfig])]);

		var effect = new Effect(grantEffectData, new EffectOwnership(entity, entity));
		ActiveEffectHandle? effectHandle = entity.EffectsManager.ApplyEffect(effect);

		AbilityHandle abilityHandle = effectHandle!.GetComponent<GrantAbilityEffectComponent>()!.GrantedAbilities[0];
		abilityHandle.IsValid.Should().BeTrue();

		// Raise typed event to trigger the ability
		entity.Events.Raise(new EventData<int>
		{
			EventTags = eventTag.GetSingleTagContainer()!,
			Source = entity,
			Target = entity,
			Payload = 42,
		});
		activationCount.Should().Be(1);

		// Remove the granting effect (removes the ability)
		entity.EffectsManager.RemoveEffect(effectHandle!);
		abilityHandle.IsValid.Should().BeFalse();

		// Raise typed event again, this should NOT trigger the ability since it's been removed
		entity.Events.Raise(new EventData<int>
		{
			EventTags = eventTag.GetSingleTagContainer()!,
			Source = entity,
			Target = entity,
			Payload = 100,
		});

		activationCount.Should().Be(1); // Should still be 1, not 2
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
		PeriodicData? periodicData = null,
		IEffectComponent? extraComponent = null,
		int effectLevel = 1,
		bool tryActivateOnGrant = false,
		bool tryActivateOnEnable = false,
		LevelComparison levelOverridePolicy = LevelComparison.Higher)
	{
		GrantAbilityConfig grantAbilityConfig = new(
			abilityData,
			abilityLevelScaling,
			grantedAbilityRemovalPolicy,
			grantedAbilityInhibitionPolicy,
			tryActivateOnGrant,
			tryActivateOnEnable,
			levelOverridePolicy);

		Effect grantAbilityEffect = CreateAbilityApplierEffect(
			"Grant Ability Effect",
			grantAbilityConfig,
			sourceEntity,
			durationData,
			periodicData,
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
		PeriodicData? periodicData,
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
			periodicData: periodicData,
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
		ScalableFloat[] cooldownDurations,
		string[] cooldownTags,
		string costAttribute,
		ScalableFloat costAmount,
		TagContainer? abilityTags = null,
		AbilityInstancingPolicy instancingPolicy = AbilityInstancingPolicy.PerEntity,
		bool retriggerInstancedAbility = false,
		AbilityTriggerData? abilityTriggerData = null,
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
		var cooldownEffectData = new EffectData[cooldownDurations.Length];

		for (var i = 0; i < cooldownDurations.Length; i++)
		{
			cooldownEffectData[i] = new EffectData(
				"Fireball Cooldown",
				new DurationData(
					DurationType.HasDuration,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, cooldownDurations[i])),
				effectComponents:
				[
					new ModifierTagsEffectComponent(
						new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, [cooldownTags[i]])))
				]);
		}

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
			abilityTags: abilityTags,
			instancingPolicy: instancingPolicy,
			retriggerInstancedAbility: retriggerInstancedAbility,
			abilityTriggerData: abilityTriggerData,
			cancelAbilitiesWithTag: cancelAbilitiesWithTag,
			blockAbilitiesWithTag: blockAbilitiesWithTag,
			activationOwnedTags: activationOwnedTags,
			activationRequiredTags: activationRequiredTags,
			activationBlockedTags: activationBlockedTags,
			sourceRequiredTags: sourceRequiredTags,
			sourceBlockedTags: sourceBlockedTags,
			targetRequiredTags: targetRequiredTags,
			targetBlockedTags: targetBlockedTags);
	}

	private sealed class CountingAbilityBehavior(Action onStarted) : IAbilityBehavior
	{
		private readonly Action _onStarted = onStarted;

		public void OnStarted(AbilityBehaviorContext context)
		{
			_onStarted();
			context.InstanceHandle.End();
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
		}
	}
}
