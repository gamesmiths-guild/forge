// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

public class ModifierTagsComponentTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Theory]
	[Trait("Expiration", null)]
	[InlineData(new string[] { "color.dark.green" }, 10f)]
	[InlineData(new string[] { "color.red", "color.dark.red", "color" }, 10f)]
	[InlineData(new string[] { "color.green", "item.equipment.weapon.axe" }, 100f)]
	[InlineData(new string[] { "item.equipment.weapon.axe", "enemy.undead" }, 0.1f)]
	public void Duration_effect_adds_tags_temporarily(string[] tagKeys, float duration)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateDurationEffectData(tagKeys, duration);
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		var baseTagsContainer = new TagContainer(entity.Tags.BaseTags);
		TagContainer modifierTagsContainer = _tagsManager.RequestTagContainer(tagKeys);

		var validationTags = new HashSet<Tag>(baseTagsContainer.Tags);
		validationTags.UnionWith(TestUtils.StringToTag(_tagsManager, tagKeys));
		var validationContainer = new TagContainer(_tagsManager, validationTags);

		entity.EffectsManager.ApplyEffect(effect);
		entity.Tags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.Tags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();

		entity.EffectsManager.UpdateEffects(duration / 2);
		entity.Tags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.Tags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();

		entity.EffectsManager.UpdateEffects(duration / 2);
		entity.Tags.CombinedTags.Equals(baseTagsContainer).Should().BeTrue();
		entity.Tags.ModifierTags.IsEmpty.Should().BeTrue();
	}

	[Theory]
	[Trait("Multiple", null)]
	[InlineData(new string[] { "color.dark.green" }, 10f)]
	[InlineData(new string[] { "color.red", "color.dark.red", "color" }, 10f)]
	[InlineData(new string[] { "color.green", "item.equipment.weapon.axe" }, 100f)]
	[InlineData(new string[] { "item.equipment.weapon.axe", "enemy.undead" }, 0.1f)]
	public void Multiple_instances_keep_tags_until_all_expire(string[] tagKeys, float duration)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);

		EffectData effectData = CreateDurationEffectData(tagKeys, duration);
		var effect1 = new Effect(effectData, new EffectOwnership(entity, entity));
		var effect2 = new Effect(effectData, new EffectOwnership(entity, entity));

		var baseTagsContainer = new TagContainer(entity.Tags.BaseTags);
		TagContainer modifierTagsContainer = _tagsManager.RequestTagContainer(tagKeys);

		var validationTags = new HashSet<Tag>(baseTagsContainer.Tags);
		validationTags.UnionWith(TestUtils.StringToTag(_tagsManager, tagKeys));
		var validationContainer = new TagContainer(_tagsManager, validationTags);

		entity.EffectsManager.ApplyEffect(effect1);

		entity.Tags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.Tags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();

		entity.EffectsManager.UpdateEffects(duration / 2);

		entity.Tags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.Tags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();

		entity.EffectsManager.ApplyEffect(effect2);

		entity.Tags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.Tags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();

		entity.EffectsManager.UpdateEffects(duration / 2);

		entity.Tags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.Tags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();

		entity.EffectsManager.UpdateEffects(duration / 2);

		entity.Tags.CombinedTags.Equals(baseTagsContainer).Should().BeTrue();
		entity.Tags.ModifierTags.IsEmpty.Should().BeTrue();
	}

	[Theory]
	[Trait("Removal", null)]
	[InlineData((object)new string[] { "color.dark.green", "simple.tag" })]
	[InlineData((object)new string[] { "color.dark.green" })]
	[InlineData((object)new string[] { "color.red", "color.dark.red", "color" })]
	[InlineData((object)new string[] { "color.green", "item.equipment.weapon.axe" })]
	[InlineData((object)new string[] { "item.equipment.weapon.axe", "enemy.undead" })]
	public void Manual_removal_removes_tags_instantly(string[] tagKeys)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateInfiniteDurationEffectData(tagKeys);
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		var baseTagsContainer = new TagContainer(entity.Tags.BaseTags);
		TagContainer modifierTagsContainer = _tagsManager.RequestTagContainer(tagKeys);

		var validationTags = new HashSet<Tag>(baseTagsContainer.Tags);
		validationTags.UnionWith(TestUtils.StringToTag(_tagsManager, tagKeys));
		var validationContainer = new TagContainer(_tagsManager, validationTags);

		ActiveEffectHandle? activeEffectHandle = entity.EffectsManager.ApplyEffect(effect);

		entity.Tags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.Tags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();

		entity.EffectsManager.UpdateEffects(60f);
		entity.Tags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.Tags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();

		entity.EffectsManager.RemoveEffect(activeEffectHandle!);
		entity.Tags.CombinedTags.Equals(baseTagsContainer).Should().BeTrue();
		entity.Tags.ModifierTags.IsEmpty.Should().BeTrue();
	}

	[Theory]
	[Trait("Expiration", null)]
	[InlineData(new string[] { "color.dark.green" }, 10f)]
	[InlineData(new string[] { "color.red", "color.dark.red", "color" }, 10f)]
	[InlineData(new string[] { "color.green", "item.equipment.weapon.axe" }, 100f)]
	[InlineData(new string[] { "item.equipment.weapon.axe", "enemy.undead" }, 0.1f)]
	public void Expired_effect_can_be_reapplied(string[] tagKeys, float duration)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateDurationEffectData(tagKeys, duration);
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		var baseTagsContainer = new TagContainer(entity.Tags.BaseTags);
		TagContainer modifierTagsContainer = _tagsManager.RequestTagContainer(tagKeys);

		var validationTags = new HashSet<Tag>(baseTagsContainer.Tags);
		validationTags.UnionWith(TestUtils.StringToTag(_tagsManager, tagKeys));
		var validationContainer = new TagContainer(_tagsManager, validationTags);

		entity.EffectsManager.ApplyEffect(effect);
		entity.Tags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.Tags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();

		entity.EffectsManager.UpdateEffects(duration);
		entity.Tags.CombinedTags.Equals(baseTagsContainer).Should().BeTrue();
		entity.Tags.ModifierTags.IsEmpty.Should().BeTrue();

		entity.EffectsManager.ApplyEffect(effect);
		entity.Tags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.Tags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();
	}

	[Theory]
	[Trait("Multiple", null)]
	[InlineData(
		new string[] { "color.dark.green" },
		new string[] { "color.dark.green" },
		10f,
		15f)]
	[InlineData(
		new string[] { "color.dark.green" },
		new string[] { "color.red", "color.dark.red", "color" },
		10f,
		15f)]
	[InlineData(
		new string[] { "color.dark.green" },
		new string[] { "color.green", "color.dark.green", "color" },
		10f,
		10.5f)]
	[InlineData(
		new string[] { "color.green", "item.equipment.weapon.axe" },
		new string[] { "item.equipment.weapon.axe", "enemy.undead" },
		0.1f,
		100f)]
	public void Tags_from_different_effects_are_kept_until_expired(
		string[] tagKeys1,
		string[] tagKeys2,
		float effect1SmallerDuration,
		float effect2BiggerDuration)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);

		EffectData effectData1 = CreateDurationEffectData(tagKeys1, effect1SmallerDuration);
		EffectData effectData2 = CreateDurationEffectData(tagKeys2, effect2BiggerDuration);
		var effect1 = new Effect(effectData1, new EffectOwnership(entity, entity));
		var effect2 = new Effect(effectData2, new EffectOwnership(entity, entity));

		var baseTagsContainer = new TagContainer(entity.Tags.BaseTags);

		TagContainer effect1TagsContainer = _tagsManager.RequestTagContainer(tagKeys1);
		TagContainer effect2TagsContainer = _tagsManager.RequestTagContainer(tagKeys2);

		entity.EffectsManager.ApplyEffect(effect1);
		entity.EffectsManager.ApplyEffect(effect2);

		entity.Tags.CombinedTags.HasAllExact(baseTagsContainer).Should().BeTrue();
		entity.Tags.CombinedTags.HasAllExact(effect1TagsContainer).Should().BeTrue();
		entity.Tags.CombinedTags.HasAllExact(effect2TagsContainer).Should().BeTrue();
		entity.Tags.ModifierTags.HasAllExact(effect1TagsContainer).Should().BeTrue();
		entity.Tags.ModifierTags.HasAllExact(effect2TagsContainer).Should().BeTrue();

		entity.EffectsManager.UpdateEffects(effect1SmallerDuration);

		entity.Tags.CombinedTags.HasAllExact(baseTagsContainer).Should().BeTrue();
		entity.Tags.CombinedTags.HasAllExact(effect2TagsContainer).Should().BeTrue();
		entity.Tags.ModifierTags.HasAllExact(effect2TagsContainer).Should().BeTrue();

		entity.EffectsManager.UpdateEffects(effect2BiggerDuration - effect1SmallerDuration);

		entity.Tags.CombinedTags.Equals(baseTagsContainer).Should().BeTrue();
		entity.Tags.ModifierTags.IsEmpty.Should().BeTrue();
	}

	[Theory]
	[Trait("Stackable", null)]
	[InlineData(new string[] { "color.dark.green" }, 3)]
	[InlineData(new string[] { "color.red", "color.dark.red", "color" }, 5)]
	[InlineData(new string[] { "color.green", "item.equipment.weapon.axe" }, 1)]
	[InlineData(new string[] { "item.equipment.weapon.axe", "enemy.undead" }, 2)]
	public void Stackable_effects_keep_tags_until_completely_removed(string[] tagKeys, int stacks)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);

		EffectData effectData = CreateSimpleStackableEffectData(tagKeys, stacks);
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		var baseTagsContainer = new TagContainer(entity.Tags.BaseTags);
		TagContainer modifierTagsContainer = _tagsManager.RequestTagContainer(tagKeys);

		var validationTags = new HashSet<Tag>(baseTagsContainer.Tags);
		validationTags.UnionWith(TestUtils.StringToTag(_tagsManager, tagKeys));
		var validationContainer = new TagContainer(_tagsManager, validationTags);

		ActiveEffectHandle? activeEffectHandle = entity.EffectsManager.ApplyEffect(effect);

		entity.Tags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.Tags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();

		for (var i = 0; i < stacks - 1; i++)
		{
			entity.EffectsManager.RemoveEffect(activeEffectHandle!);
			entity.Tags.CombinedTags.Equals(validationContainer).Should().BeTrue();
			entity.Tags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();
		}

		entity.EffectsManager.RemoveEffect(activeEffectHandle!);
		entity.Tags.CombinedTags.Equals(baseTagsContainer).Should().BeTrue();
		entity.Tags.ModifierTags.IsEmpty.Should().BeTrue();
	}

	[Theory]
	[Trait("Stackable", null)]
	[InlineData(new string[] { "color.dark.green" }, 3)]
	[InlineData(new string[] { "color.red", "color.dark.red", "color" }, 5)]
	[InlineData(new string[] { "color.green", "item.equipment.weapon.axe" }, 1)]
	[InlineData(new string[] { "item.equipment.weapon.axe", "enemy.undead" }, 2)]
	public void Stackable_effects_removes_tags_when_forcibly_removed(string[] tagKeys, int stacks)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);

		EffectData effectData = CreateSimpleStackableEffectData(tagKeys, stacks);
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		var baseTagsContainer = new TagContainer(entity.Tags.BaseTags);
		TagContainer modifierTagsContainer = _tagsManager.RequestTagContainer(tagKeys);

		var validationTags = new HashSet<Tag>(baseTagsContainer.Tags);
		validationTags.UnionWith(TestUtils.StringToTag(_tagsManager, tagKeys));
		var validationContainer = new TagContainer(_tagsManager, validationTags);

		ActiveEffectHandle? activeEffectHandle = entity.EffectsManager.ApplyEffect(effect);

		entity.Tags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.Tags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();

		entity.EffectsManager.RemoveEffect(activeEffectHandle!, true);
		entity.Tags.CombinedTags.Equals(baseTagsContainer).Should().BeTrue();
		entity.Tags.ModifierTags.IsEmpty.Should().BeTrue();
	}

	private EffectData CreateDurationEffectData(string[] tagKeys, float duration)
	{
		HashSet<Tag> tags = TestUtils.StringToTag(_tagsManager, tagKeys);

		return new EffectData(
			"Test Effect",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(duration))),
			effectComponents:
			[
				new ModifierTagsEffectComponent(new TagContainer(_tagsManager, tags))
			]);
	}

	private EffectData CreateInfiniteDurationEffectData(string[] tagKeys)
	{
		HashSet<Tag> tags = TestUtils.StringToTag(_tagsManager, tagKeys);

		return new EffectData(
			"Test Effect",
			new DurationData(DurationType.Infinite),
			effectComponents:
			[
				new ModifierTagsEffectComponent(new TagContainer(_tagsManager, tags))
			]);
	}

	private EffectData CreateSimpleStackableEffectData(string[] tagKeys, int stacks)
	{
		HashSet<Tag> tags = TestUtils.StringToTag(_tagsManager, tagKeys);

		return new EffectData(
			"Test Effect",
			new DurationData(DurationType.Infinite),
			stackingData: new StackingData(
				new ScalableInt(stacks),
				new ScalableInt(stacks),
				StackPolicy.AggregateBySource,
				StackLevelPolicy.SegregateLevels,
				StackMagnitudePolicy.Sum,
				StackOverflowPolicy.DenyApplication,
				StackExpirationPolicy.RemoveSingleStackAndRefreshDuration),
			effectComponents:
			[
				new ModifierTagsEffectComponent(new TagContainer(_tagsManager, tags))
			]);
	}
}
