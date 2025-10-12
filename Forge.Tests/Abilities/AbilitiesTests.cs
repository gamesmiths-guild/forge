// Copyright © Gamesmiths Guild.

using System.Diagnostics;
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
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Abilities;

public class AbilitiesTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Grant ability", null)]
	public void Abilitie_are_granted_successfully()
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

		Debug.Assert(abilityHandle is not null, "abilityHandle is not null.");
		Debug.Assert(effectHandle is not null, "effectHandle is not null.");

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle.Activate();

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

		Debug.Assert(abilityHandle is not null, "abilityHandle is not null.");
		Debug.Assert(effectHandle is not null, "effectHandle is not null.");

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle.Activate();

		abilityHandle.IsActive.Should().BeTrue();

		entity.EffectsManager.UnapplyEffect(effectHandle);

		entity.Abilities.GrantedAbilities.Should().BeEmpty();

		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Grant ability", null)]
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

		Debug.Assert(abilityHandle is not null, "abilityHandle is not null.");
		Debug.Assert(effectHandle is not null, "effectHandle is not null.");

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle.Activate();

		abilityHandle.IsActive.Should().BeTrue();

		entity.EffectsManager.UnapplyEffect(effectHandle);

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle.End();

		entity.Abilities.GrantedAbilities.Should().BeEmpty();

		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Inhibit ability", null)]
	public void Inhibited_effect_inhibites_ability_temporarily()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		TagContainer? ignoreTags = Tag.RequestTag(_tagsManager, "Tag").GetSingleTagContainer();

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out ActiveEffectHandle? effectHandle,
			extraComponent: new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(
					IgnoreTags: ignoreTags)));

		Debug.Assert(abilityHandle is not null, "abilityHandle is not null.");
		Debug.Assert(effectHandle is not null, "effectHandle is not null.");
		Debug.Assert(ignoreTags is not null, "ignoreTags is not null.");

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle.Activate();

		abilityHandle.IsActive.Should().BeTrue();

		var tagEffectData = new EffectData(
			"Tag Effect",
			new DurationData(DurationType.Infinite),
			effectComponents: [new ModifierTagsEffectComponent(ignoreTags)]);

		var tagEffect = new Effect(
			tagEffectData,
			new EffectOwnership(entity, null));

		ActiveEffectHandle? tagEffectHandle = entity.EffectsManager.ApplyEffect(tagEffect);

		Debug.Assert(tagEffectHandle is not null, "tagEffectHandle is not null.");

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle.Activate();

		abilityHandle.IsActive.Should().BeFalse();
		abilityHandle.IsInhibited.Should().BeTrue();

		entity.EffectsManager.UnapplyEffect(tagEffectHandle);

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle.Activate();

		abilityHandle.IsActive.Should().BeTrue();
		abilityHandle.IsInhibited.Should().BeFalse();
	}

	[Fact]
	[Trait("Grant ability", null)]
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

		Debug.Assert(abilityHandle is not null, "abilityHandle is not null.");
		Debug.Assert(effectHandle is not null, "effectHandle is not null.");

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle.Activate();

		abilityHandle.IsActive.Should().BeTrue();

		entity.EffectsManager.UnapplyEffect(effectHandle);

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle.End();

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		abilityHandle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Grant ability", null)]
	public void Ability_granted_by_multiple_effects_is_removed_only_when_all_effects_are_removed()
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

		Debug.Assert(abilityHandle1 is not null, "abilityHandle1 is not null.");
		Debug.Assert(effectHandle1 is not null, "effectHandle1 is not null.");
		Debug.Assert(abilityHandle2 is not null, "abilityHandle2 is not null.");
		Debug.Assert(effectHandle2 is not null, "effectHandle2 is not null.");

		abilityHandle1.Should().Be(abilityHandle2);

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		entity.EffectsManager.UnapplyEffect(effectHandle1);

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		entity.EffectsManager.UnapplyEffect(effectHandle2);

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

		Debug.Assert(blockingTags is not null, "blockingTags is not null.");

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

		var tagEffectData = new EffectData(
			"Tag Effect",
			new DurationData(DurationType.Infinite),
			effectComponents: [new ModifierTagsEffectComponent(blockingTags)]);

		var tagEffect = new Effect(
			tagEffectData,
			new EffectOwnership(entity, null));

		entity.EffectsManager.ApplyEffect(tagEffect);

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

		Debug.Assert(permanentAbilityHandle is not null, "permanentAbilityHandle is not null.");
		Debug.Assert(temporaryAbilityHandle is not null, "temporaryAbilityHandle is not null.");
		Debug.Assert(temporaryEffectHandle is not null, "temporaryEffectHandle is not null.");

		permanentAbilityHandle.Should().Be(temporaryAbilityHandle);
		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		// Remove the temporary effect.
		entity.EffectsManager.UnapplyEffect(temporaryEffectHandle);

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

		Debug.Assert(temporaryAbilityHandle is not null, "temporaryAbilityHandle is not null.");
		Debug.Assert(temporaryEffectHandle is not null, "temporaryEffectHandle is not null.");

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
		entity.EffectsManager.UnapplyEffect(temporaryEffectHandle);

		// The ability should still be granted because of the initial permanent grant.
		entity.Abilities.GrantedAbilities.Should().ContainSingle();
	}

	[Fact]
	[Trait("Grant ability", null)]
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

		// Grant the same ability with a non-instant, inhibitable effect.
		SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out ActiveEffectHandle? temporaryEffectHandle,
			extraComponent: new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(
					IgnoreTags: ignoreTags)));

		Debug.Assert(abilityHandle is not null, "abilityHandle is not null.");
		Debug.Assert(temporaryEffectHandle is not null, "temporaryEffectHandle is not null.");
		Debug.Assert(ignoreTags is not null, "ignoreTags is not null.");

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		// Inhibit the temporary effect by adding the tag.
		var tagEffectData = new EffectData(
			"Tag Effect",
			new DurationData(DurationType.Infinite),
			effectComponents: [new ModifierTagsEffectComponent(ignoreTags)]);
		var tagEffect = new Effect(tagEffectData, new EffectOwnership(entity, null));
		entity.EffectsManager.ApplyEffect(tagEffect);

		// The ability should not be inhibited because it was granted permanently.
		abilityHandle.IsInhibited.Should().BeFalse();
	}

	[Fact]
	[Trait("Grant ability", null)]
	public void Ability_granted_by_late_instant_effect_is_not_inhibited()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		TagContainer? ignoreTags = Tag.RequestTag(_tagsManager, "Tag").GetSingleTagContainer();

		// Grant the same ability with a non-instant, inhibitable effect.
		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out ActiveEffectHandle? temporaryEffectHandle,
			extraComponent: new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(
					IgnoreTags: ignoreTags)));

		Debug.Assert(abilityHandle is not null, "abilityHandle is not null.");
		Debug.Assert(temporaryEffectHandle is not null, "temporaryEffectHandle is not null.");
		Debug.Assert(ignoreTags is not null, "ignoreTags is not null.");

		entity.Abilities.GrantedAbilities.Should().ContainSingle();

		// Inhibit the temporary effect by adding the tag.
		var tagEffectData = new EffectData(
			"Tag Effect",
			new DurationData(DurationType.Infinite),
			effectComponents: [new ModifierTagsEffectComponent(ignoreTags)]);
		var tagEffect = new Effect(tagEffectData, new EffectOwnership(entity, null));
		entity.EffectsManager.ApplyEffect(tagEffect);

		// The ability should not be inhibited because it was granted permanently.
		abilityHandle.IsInhibited.Should().BeTrue();

		// Grant ability with an instant effect, making it permanent.
		SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			durationData: new DurationData(DurationType.Instant));

		// The ability should not be inhibited because it was granted permanently.
		abilityHandle.IsInhibited.Should().BeFalse();
	}

	[Fact]
	[Trait("Grant ability", null)]
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

		Debug.Assert(abilityHandle is not null, "abilityHandle is not null.");
		Debug.Assert(ignoreTags1 is not null, "ignoreTags1 is not null.");
		Debug.Assert(ignoreTags2 is not null, "ignoreTags2 is not null.");

		entity.Abilities.GrantedAbilities.Should().ContainSingle();
		abilityHandle.Activate();
		abilityHandle.IsActive.Should().BeTrue();

		// Inhibit the first effect.
		var tagEffect1 = new Effect(
			new EffectData("Tag Effect 1", new DurationData(DurationType.Infinite), effectComponents: [new ModifierTagsEffectComponent(ignoreTags1)]),
			new EffectOwnership(entity, null));
		entity.EffectsManager.ApplyEffect(tagEffect1);

		// Ability should not be inhibited yet.
		abilityHandle.IsInhibited.Should().BeFalse();
		abilityHandle.IsActive.Should().BeFalse(); // It deactivates because one source is inhibited.

		// Inhibit the second effect.
		var tagEffect2 = new Effect(
			new EffectData("Tag Effect 2", new DurationData(DurationType.Infinite), effectComponents: [new ModifierTagsEffectComponent(ignoreTags2)]),
			new EffectOwnership(entity, null));
		entity.EffectsManager.ApplyEffect(tagEffect2);

		// Now the ability should be fully inhibited.
		abilityHandle.IsInhibited.Should().BeTrue();
	}

	[Fact]
	[Trait("Grant ability", null)]
	public void Inhibited_ability_becomes_active_if_new_non_inhibited_source_is_added()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		TagContainer? ignoreTags = Tag.RequestTag(_tagsManager, "Tag").GetSingleTagContainer();

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			extraComponent: new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(
					IgnoreTags: ignoreTags)));

		Debug.Assert(abilityHandle is not null, "abilityHandle is not null.");
		Debug.Assert(ignoreTags is not null, "ignoreTags is not null.");

		// Inhibit the ability.
		var tagEffect = new Effect(
			new EffectData("Tag Effect", new DurationData(DurationType.Infinite), effectComponents: [new ModifierTagsEffectComponent(ignoreTags)]),
			new EffectOwnership(entity, null));
		entity.EffectsManager.ApplyEffect(tagEffect);

		abilityHandle.IsInhibited.Should().BeTrue();

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
	[Trait("Grant ability", null)]
	public void Inhibition_policy_RemoveOnEnd_inhibits_after_deactivation()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		TagContainer? ignoreTags = Tag.RequestTag(_tagsManager, "Tag").GetSingleTagContainer();

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			grantedAbilityInhibitionPolicy: AbilityDeactivationPolicy.RemoveOnEnd,
			extraComponent: new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(
					IgnoreTags: ignoreTags)));

		Debug.Assert(abilityHandle is not null, "abilityHandle is not null.");
		Debug.Assert(ignoreTags is not null, "ignoreTags is not null.");

		abilityHandle.Activate();
		abilityHandle.IsActive.Should().BeTrue();

		// Inhibit the granting effect.
		var tagEffect = new Effect(
			new EffectData("Tag Effect", new DurationData(DurationType.Infinite), effectComponents: [new ModifierTagsEffectComponent(ignoreTags)]),
			new EffectOwnership(entity, null));
		entity.EffectsManager.ApplyEffect(tagEffect);

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
	[Trait("Grant ability", null)]
	public void Inhibition_policy_Ignore_prevents_inhibition()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);

		AbilityData abilityData = CreateAbiltyData(
			"Fireball",
			new ScalableFloat(3f),
			"TestAttributeSet.Attribute90",
			new ScalableFloat(-1));

		TagContainer? ignoreTags = Tag.RequestTag(_tagsManager, "Tag").GetSingleTagContainer();

		AbilityHandle? abilityHandle = SetupAbility(
			entity,
			abilityData,
			new ScalableInt(1),
			out _,
			grantedAbilityInhibitionPolicy: AbilityDeactivationPolicy.Ignore,
			extraComponent: new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(
					IgnoreTags: ignoreTags)));

		Debug.Assert(abilityHandle is not null, "abilityHandle is not null.");
		Debug.Assert(ignoreTags is not null, "ignoreTags is not null.");

		abilityHandle.Activate();

		// Inhibit the granting effect.
		var tagEffect = new Effect(
			new EffectData("Tag Effect", new DurationData(DurationType.Infinite), effectComponents: [new ModifierTagsEffectComponent(ignoreTags)]),
			new EffectOwnership(entity, null));
		entity.EffectsManager.ApplyEffect(tagEffect);

		// With Ignore policy, the ability is never inhibited.
		abilityHandle.IsInhibited.Should().BeFalse();
		abilityHandle.IsActive.Should().BeTrue();

		abilityHandle.End();

		abilityHandle.IsInhibited.Should().BeFalse();
		abilityHandle.IsActive.Should().BeFalse();
	}

	private static AbilityData CreateAbiltyData(
		string abilityName,
		ScalableFloat cooldownDuration,
		string costAttribute,
		ScalableFloat costAmmount)
	{
		var costEffectData = new EffectData(
			"Fireball Cooldown",
			new DurationData(DurationType.HasDuration, cooldownDuration));

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
		TestEntity entity,
		AbilityData abilityData,
		ScalableInt abilityLevelScaling,
		out ActiveEffectHandle? effectHandle,
		AbilityDeactivationPolicy grantedAbilityRemovalPolicy = AbilityDeactivationPolicy.CancelImmediately,
		AbilityDeactivationPolicy grantedAbilityInhibitionPolicy = AbilityDeactivationPolicy.CancelImmediately,
		DurationData? durationData = null,
		IEffectComponent? extraComponent = null)
	{
		GrantAbilityConfig grantAbilityConfig = new(
			abilityData,
			abilityLevelScaling,
			grantedAbilityRemovalPolicy,
			grantedAbilityInhibitionPolicy);

		Effect grantAbilityEffect = CreateAbilityApplierEffect(
			"Grant Fireball",
			grantAbilityConfig,
			entity,
			durationData,
			extraComponent);

		effectHandle = entity.EffectsManager.ApplyEffect(grantAbilityEffect);

		return entity.Abilities.GrantedAbilities.FirstOrDefault();
	}

	private static Effect CreateAbilityApplierEffect(
		string effectName,
		GrantAbilityConfig grantAbilityConfig,
		IForgeEntity source,
		DurationData? durationData,
		IEffectComponent? extraComponent)
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
			new EffectOwnership(source, null));
	}
}
