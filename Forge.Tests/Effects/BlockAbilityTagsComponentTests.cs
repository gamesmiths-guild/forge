// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

public class BlockAbilityTagsComponentTests(TagsAndCuesFixture tagsAndCuesFixture)
	: IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Theory]
	[Trait("Blocking", null)]
	[InlineData(new string[] { "item.equipment.weapon.axe" }, 10f)]
	[InlineData(new string[] { "color.red", "color.dark.red" }, 0.1f)]
	[InlineData(new string[] { "enemy.undead.ghoul" }, 100f)]
	public void Duration_effect_blocks_matching_abilities_until_it_expires(string[] blockedTagKeys, float duration)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		AbilityHandle ability = GrantAbility(entity, "Blocked Ability", blockedTagKeys);

		ability.CanActivate(out _).Should().BeTrue();

		entity.EffectsManager.ApplyEffect(CreateBlockingEffect(entity, blockedTagKeys, duration));

		ability.CanActivate(out AbilityActivationFailures failureFlags).Should().BeFalse();
		failureFlags.Should().HaveFlag(AbilityActivationFailures.BlockedByTags);

		entity.EffectsManager.UpdateEffects(duration / 2);

		ability.CanActivate(out _).Should().BeFalse();

		entity.EffectsManager.UpdateEffects(duration / 2);

		ability.CanActivate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		entity.Abilities.BlockedAbilityTags.AllTags.IsEmpty.Should().BeTrue();
	}

	[Fact]
	[Trait("Blocking", null)]
	public void Blocked_ability_fails_to_actually_activate()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		AbilityHandle ability = GrantAbility(entity, "Blocked Ability", ["item.equipment.weapon.axe"]);

		entity.EffectsManager.ApplyEffect(CreateBlockingEffect(entity, ["item.equipment.weapon.axe"], 10f));

		ability.Activate(out AbilityActivationFailures failureFlags).Should().BeFalse();
		failureFlags.Should().HaveFlag(AbilityActivationFailures.BlockedByTags);
		ability.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Blocking", null)]
	public void Abilities_without_the_blocked_tags_are_unaffected()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		AbilityHandle blocked = GrantAbility(entity, "Blocked Ability", ["item.equipment.weapon.axe"]);
		AbilityHandle unrelated = GrantAbility(entity, "Unrelated Ability", ["item.equipment.weapon.sword"]);
		AbilityHandle untagged = GrantAbility(entity, "Untagged Ability", []);

		entity.EffectsManager.ApplyEffect(CreateBlockingEffect(entity, ["item.equipment.weapon.axe"], 10f));

		blocked.CanActivate(out AbilityActivationFailures failureFlags).Should().BeFalse();
		failureFlags.Should().HaveFlag(AbilityActivationFailures.BlockedByTags);

		unrelated.CanActivate(out _).Should().BeTrue();
		untagged.CanActivate(out _).Should().BeTrue();
	}

	[Fact]
	[Trait("Inhibition", null)]
	public void Blocks_lift_while_inhibited_and_restore_when_uninhibited()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		AbilityHandle ability = GrantAbility(entity, "Stunned Ability", ["item.equipment.weapon.axe"]);

		// The ongoing requirements are met while the target does not carry color.red, so the effect starts active.
		ActiveEffectHandle? blockingHandle = entity.EffectsManager.ApplyEffect(
			CreateInhibitableBlockingEffect(entity, ["item.equipment.weapon.axe"], TagsOf("color.red")));

		blockingHandle.Should().NotBeNull();
		blockingHandle!.IsInhibited.Should().BeFalse();
		ability.CanActivate(out AbilityActivationFailures failureFlags).Should().BeFalse();
		failureFlags.Should().HaveFlag(AbilityActivationFailures.BlockedByTags);

		// Granting color.red inhibits the blocking effect, which must drop its blocks.
		ActiveEffectHandle? inhibitor = ApplyTagEffect(entity, TagsOf("color.red"));

		blockingHandle.IsInhibited.Should().BeTrue();
		ability.CanActivate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		entity.Abilities.BlockedAbilityTags.AllTags.IsEmpty.Should().BeTrue();

		// Removing color.red un-inhibits the effect and the blocks come back.
		entity.EffectsManager.RemoveEffect(inhibitor!, true);

		blockingHandle.IsInhibited.Should().BeFalse();
		ability.CanActivate(out failureFlags).Should().BeFalse();
		failureFlags.Should().HaveFlag(AbilityActivationFailures.BlockedByTags);
	}

	[Fact]
	[Trait("Inhibition", null)]
	public void Effect_landing_inhibited_blocks_nothing()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		AbilityHandle ability = GrantAbility(entity, "Stunned Ability", ["item.equipment.weapon.axe"]);

		// color.red is already present, so the effect is inhibited from the moment it lands.
		ApplyTagEffect(entity, TagsOf("color.red"));

		ActiveEffectHandle? blockingHandle = entity.EffectsManager.ApplyEffect(
			CreateInhibitableBlockingEffect(entity, ["item.equipment.weapon.axe"], TagsOf("color.red")));

		blockingHandle.Should().NotBeNull();
		blockingHandle!.IsInhibited.Should().BeTrue();
		entity.Abilities.BlockedAbilityTags.AllTags.IsEmpty.Should().BeTrue();
		ability.CanActivate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
	}

	[Fact]
	[Trait("Multiple", null)]
	public void Blocks_persist_until_every_blocking_effect_is_removed()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		AbilityHandle ability = GrantAbility(entity, "Blocked Ability", ["item.equipment.weapon.axe"]);

		ActiveEffectHandle? first = entity.EffectsManager.ApplyEffect(
			CreateBlockingEffect(entity, ["item.equipment.weapon.axe"], null));
		ActiveEffectHandle? second = entity.EffectsManager.ApplyEffect(
			CreateBlockingEffect(entity, ["item.equipment.weapon.axe"], null));

		ability.CanActivate(out _).Should().BeFalse();

		entity.EffectsManager.RemoveEffect(first!, true);

		ability.CanActivate(out AbilityActivationFailures failureFlags).Should().BeFalse();
		failureFlags.Should().HaveFlag(AbilityActivationFailures.BlockedByTags);

		entity.EffectsManager.RemoveEffect(second!, true);

		ability.CanActivate(out _).Should().BeTrue();
		entity.Abilities.BlockedAbilityTags.AllTags.IsEmpty.Should().BeTrue();
	}

	private static ActiveEffectHandle? ApplyTagEffect(TestEntity entity, TagContainer tags)
	{
		var tagEffectData = new EffectData(
			"Tag Effect",
			new DurationData(DurationType.Infinite),
			effectComponents: [new ModifierTagsEffectComponent(tags)]);

		return entity.EffectsManager.ApplyEffect(new Effect(tagEffectData, new EffectOwnership(entity, entity)));
	}

	private TagContainer TagsOf(params string[] tagKeys)
	{
		return new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, tagKeys));
	}

	// Grants a cost-free, cooldown-free ability carrying the given ability tags, so the blocking tests only have to
	// reason about BlockedByTags.
	private AbilityHandle GrantAbility(TestEntity entity, string abilityName, string[] abilityTagKeys)
	{
		var abilityData = new AbilityData(
			abilityName,
			abilityTags: abilityTagKeys.Length > 0 ? TagsOf(abilityTagKeys) : null);

		return entity.Abilities.GrantAbilityPermanently(abilityData, 1, LevelComparison.None, null);
	}

	private Effect CreateBlockingEffect(TestEntity entity, string[] blockedTagKeys, float? duration)
	{
		DurationData durationData = duration.HasValue
			? new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(duration.Value)))
			: new DurationData(DurationType.Infinite);

		var effectData = new EffectData(
			"Blocking Effect",
			durationData,
			effectComponents: [new BlockAbilityTagsEffectComponent(TagsOf(blockedTagKeys))]);

		return new Effect(effectData, new EffectOwnership(entity, entity));
	}

	// A blocking effect whose ongoing requirements fail as soon as the target carries any of inhibitOnTags.
	private Effect CreateInhibitableBlockingEffect(
		TestEntity entity,
		string[] blockedTagKeys,
		TagContainer inhibitOnTags)
	{
		var effectData = new EffectData(
			"Inhibitable Blocking Effect",
			new DurationData(DurationType.Infinite),
			effectComponents:
			[
				new BlockAbilityTagsEffectComponent(TagsOf(blockedTagKeys)),
				new TargetTagRequirementsEffectComponent(
					ongoingTagRequirements: new TagRequirements(IgnoreTags: inhibitOnTags))
			]);

		return new Effect(effectData, new EffectOwnership(entity, entity));
	}
}
