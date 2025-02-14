// Copyright © 2024 Gamesmiths Guild.

using System.Diagnostics;
using FluentAssertions;
using Gamesmiths.Forge.GameplayEffects;
using Gamesmiths.Forge.GameplayEffects.Components;
using Gamesmiths.Forge.GameplayEffects.Duration;
using Gamesmiths.Forge.GameplayEffects.Magnitudes;
using Gamesmiths.Forge.GameplayEffects.Stacking;
using Gamesmiths.Forge.GameplayTags;
using Gamesmiths.Forge.Tests.GameplayTags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.GamepleyEffects;

public class ModifierTagsComponentTests(GameplayTagsManagerFixture fixture)
	: IClassFixture<GameplayTagsManagerFixture>
{
	private readonly GameplayTagsManager _gameplayTagsManager = fixture.GameplayTagsManager;

	[Theory]
	[Trait("Expiration", null)]
	[InlineData(new string[] { "color.dark.green" }, 10f)]
	[InlineData(new string[] { "color.red", "color.dark.red", "color" }, 10f)]
	[InlineData(new string[] { "color.green", "item.equipment.weapon.axe" }, 100f)]
	[InlineData(new string[] { "item.equipment.weapon.axe", "enemy.undead" }, 0.1f)]
	public void Duration_effect_adds_tags_temporarily(string[] tagKeys, float duration)
	{
		var entity = new TestEntity(_gameplayTagsManager);
		GameplayEffectData effectData = CreateDurationEffectData(tagKeys, duration);
		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

		var baseTagsContainer = new GameplayTagContainer(entity.GameplayTags.BaseTags);
		GameplayTagContainer modifierTagsContainer = _gameplayTagsManager.RequestTagContainer(tagKeys);

		var validationTags = new HashSet<GameplayTag>(baseTagsContainer.GameplayTags);
		validationTags.UnionWith(TestUtils.StringToGameplayTag(_gameplayTagsManager, tagKeys));
		var validationContainer = new GameplayTagContainer(_gameplayTagsManager, validationTags);

		entity.EffectsManager.ApplyEffect(effect);
		entity.GameplayTags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.GameplayTags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();

		entity.EffectsManager.UpdateEffects(duration / 2);
		entity.GameplayTags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.GameplayTags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();

		entity.EffectsManager.UpdateEffects(duration / 2);
		entity.GameplayTags.CombinedTags.Equals(baseTagsContainer).Should().BeTrue();
		entity.GameplayTags.ModifierTags.IsEmpty.Should().BeTrue();
	}

	[Theory]
	[Trait("Multiple", null)]
	[InlineData(new string[] { "color.dark.green" }, 10f)]
	[InlineData(new string[] { "color.red", "color.dark.red", "color" }, 10f)]
	[InlineData(new string[] { "color.green", "item.equipment.weapon.axe" }, 100f)]
	[InlineData(new string[] { "item.equipment.weapon.axe", "enemy.undead" }, 0.1f)]
	public void Multiple_instances_keep_tags_until_all_expire(string[] tagKeys, float duration)
	{
		var entity = new TestEntity(_gameplayTagsManager);

		GameplayEffectData effectData = CreateDurationEffectData(tagKeys, duration);
		var effect1 = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));
		var effect2 = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

		var baseTagsContainer = new GameplayTagContainer(entity.GameplayTags.BaseTags);
		GameplayTagContainer modifierTagsContainer = _gameplayTagsManager.RequestTagContainer(tagKeys);

		var validationTags = new HashSet<GameplayTag>(baseTagsContainer.GameplayTags);
		validationTags.UnionWith(TestUtils.StringToGameplayTag(_gameplayTagsManager, tagKeys));
		var validationContainer = new GameplayTagContainer(_gameplayTagsManager, validationTags);

		entity.EffectsManager.ApplyEffect(effect1);

		entity.GameplayTags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.GameplayTags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();

		entity.EffectsManager.UpdateEffects(duration / 2);

		entity.GameplayTags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.GameplayTags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();

		entity.EffectsManager.ApplyEffect(effect2);

		entity.GameplayTags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.GameplayTags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();

		entity.EffectsManager.UpdateEffects(duration / 2);

		entity.GameplayTags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.GameplayTags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();

		entity.EffectsManager.UpdateEffects(duration / 2);

		entity.GameplayTags.CombinedTags.Equals(baseTagsContainer).Should().BeTrue();
		entity.GameplayTags.ModifierTags.IsEmpty.Should().BeTrue();
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
		var entity = new TestEntity(_gameplayTagsManager);
		GameplayEffectData effectData = CreateInfiniteDurationEffectData(tagKeys);
		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

		var baseTagsContainer = new GameplayTagContainer(entity.GameplayTags.BaseTags);
		GameplayTagContainer modifierTagsContainer = _gameplayTagsManager.RequestTagContainer(tagKeys);

		var validationTags = new HashSet<GameplayTag>(baseTagsContainer.GameplayTags);
		validationTags.UnionWith(TestUtils.StringToGameplayTag(_gameplayTagsManager, tagKeys));
		var validationContainer = new GameplayTagContainer(_gameplayTagsManager, validationTags);

		ActiveGameplayEffectHandle? activeEffectHandle = entity.EffectsManager.ApplyEffect(effect);
		Debug.Assert(activeEffectHandle is not null, "Effect handle should have a value.");

		entity.GameplayTags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.GameplayTags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();

		entity.EffectsManager.UpdateEffects(60f);
		entity.GameplayTags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.GameplayTags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();

		entity.EffectsManager.UnapplyEffect(activeEffectHandle);
		entity.GameplayTags.CombinedTags.Equals(baseTagsContainer).Should().BeTrue();
		entity.GameplayTags.ModifierTags.IsEmpty.Should().BeTrue();
	}

	[Theory]
	[Trait("Expiration", null)]
	[InlineData(new string[] { "color.dark.green" }, 10f)]
	[InlineData(new string[] { "color.red", "color.dark.red", "color" }, 10f)]
	[InlineData(new string[] { "color.green", "item.equipment.weapon.axe" }, 100f)]
	[InlineData(new string[] { "item.equipment.weapon.axe", "enemy.undead" }, 0.1f)]
	public void Expired_effect_can_be_reapplied(string[] tagKeys, float duration)
	{
		var entity = new TestEntity(_gameplayTagsManager);
		GameplayEffectData effectData = CreateDurationEffectData(tagKeys, duration);
		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

		var baseTagsContainer = new GameplayTagContainer(entity.GameplayTags.BaseTags);
		GameplayTagContainer modifierTagsContainer = _gameplayTagsManager.RequestTagContainer(tagKeys);

		var validationTags = new HashSet<GameplayTag>(baseTagsContainer.GameplayTags);
		validationTags.UnionWith(TestUtils.StringToGameplayTag(_gameplayTagsManager, tagKeys));
		var validationContainer = new GameplayTagContainer(_gameplayTagsManager, validationTags);

		entity.EffectsManager.ApplyEffect(effect);
		entity.GameplayTags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.GameplayTags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();

		entity.EffectsManager.UpdateEffects(duration);
		entity.GameplayTags.CombinedTags.Equals(baseTagsContainer).Should().BeTrue();
		entity.GameplayTags.ModifierTags.IsEmpty.Should().BeTrue();

		entity.EffectsManager.ApplyEffect(effect);
		entity.GameplayTags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.GameplayTags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();
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
		var entity = new TestEntity(_gameplayTagsManager);

		GameplayEffectData effectData1 = CreateDurationEffectData(tagKeys1, effect1SmallerDuration);
		GameplayEffectData effectData2 = CreateDurationEffectData(tagKeys2, effect2BiggerDuration);
		var effect1 = new GameplayEffect(effectData1, new GameplayEffectOwnership(entity, entity));
		var effect2 = new GameplayEffect(effectData2, new GameplayEffectOwnership(entity, entity));

		var baseTagsContainer = new GameplayTagContainer(entity.GameplayTags.BaseTags);

		GameplayTagContainer effect1TagsContainer = _gameplayTagsManager.RequestTagContainer(tagKeys1);
		GameplayTagContainer effect2TagsContainer = _gameplayTagsManager.RequestTagContainer(tagKeys2);

		entity.EffectsManager.ApplyEffect(effect1);
		entity.EffectsManager.ApplyEffect(effect2);

		entity.GameplayTags.CombinedTags.HasAllExact(baseTagsContainer).Should().BeTrue();
		entity.GameplayTags.CombinedTags.HasAllExact(effect1TagsContainer).Should().BeTrue();
		entity.GameplayTags.CombinedTags.HasAllExact(effect2TagsContainer).Should().BeTrue();
		entity.GameplayTags.ModifierTags.HasAllExact(effect1TagsContainer).Should().BeTrue();
		entity.GameplayTags.ModifierTags.HasAllExact(effect2TagsContainer).Should().BeTrue();

		entity.EffectsManager.UpdateEffects(effect1SmallerDuration);

		entity.GameplayTags.CombinedTags.HasAllExact(baseTagsContainer).Should().BeTrue();
		entity.GameplayTags.CombinedTags.HasAllExact(effect2TagsContainer).Should().BeTrue();
		entity.GameplayTags.ModifierTags.HasAllExact(effect2TagsContainer).Should().BeTrue();

		entity.EffectsManager.UpdateEffects(effect2BiggerDuration - effect1SmallerDuration);

		entity.GameplayTags.CombinedTags.Equals(baseTagsContainer).Should().BeTrue();
		entity.GameplayTags.ModifierTags.IsEmpty.Should().BeTrue();
	}

	[Theory]
	[Trait("Stackable", null)]
	[InlineData(new string[] { "color.dark.green" }, 3)]
	[InlineData(new string[] { "color.red", "color.dark.red", "color" }, 5)]
	[InlineData(new string[] { "color.green", "item.equipment.weapon.axe" }, 1)]
	[InlineData(new string[] { "item.equipment.weapon.axe", "enemy.undead" }, 2)]
	public void Stackable_effects_keep_tags_until_completely_removed(string[] tagKeys, int stacks)
	{
		var entity = new TestEntity(_gameplayTagsManager);

		GameplayEffectData effectData = CreateSimpleStackableEffectData(tagKeys, stacks);
		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

		var baseTagsContainer = new GameplayTagContainer(entity.GameplayTags.BaseTags);
		GameplayTagContainer modifierTagsContainer = _gameplayTagsManager.RequestTagContainer(tagKeys);

		var validationTags = new HashSet<GameplayTag>(baseTagsContainer.GameplayTags);
		validationTags.UnionWith(TestUtils.StringToGameplayTag(_gameplayTagsManager, tagKeys));
		var validationContainer = new GameplayTagContainer(_gameplayTagsManager, validationTags);

		ActiveGameplayEffectHandle? activeEffectHandle = entity.EffectsManager.ApplyEffect(effect);
		Debug.Assert(activeEffectHandle is not null, "Effect handle should have a value.");

		entity.GameplayTags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.GameplayTags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();

		for (var i = 0; i < stacks - 1; i++)
		{
			entity.EffectsManager.UnapplyEffect(activeEffectHandle);
			entity.GameplayTags.CombinedTags.Equals(validationContainer).Should().BeTrue();
			entity.GameplayTags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();
		}

		entity.EffectsManager.UnapplyEffect(activeEffectHandle);
		entity.GameplayTags.CombinedTags.Equals(baseTagsContainer).Should().BeTrue();
		entity.GameplayTags.ModifierTags.IsEmpty.Should().BeTrue();
	}

	[Theory]
	[Trait("Stackable", null)]
	[InlineData(new string[] { "color.dark.green" }, 3)]
	[InlineData(new string[] { "color.red", "color.dark.red", "color" }, 5)]
	[InlineData(new string[] { "color.green", "item.equipment.weapon.axe" }, 1)]
	[InlineData(new string[] { "item.equipment.weapon.axe", "enemy.undead" }, 2)]
	public void Stackable_effects_removes_tags_when_forcibly_removed(string[] tagKeys, int stacks)
	{
		var entity = new TestEntity(_gameplayTagsManager);

		GameplayEffectData effectData = CreateSimpleStackableEffectData(tagKeys, stacks);
		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

		var baseTagsContainer = new GameplayTagContainer(entity.GameplayTags.BaseTags);
		GameplayTagContainer modifierTagsContainer = _gameplayTagsManager.RequestTagContainer(tagKeys);

		var validationTags = new HashSet<GameplayTag>(baseTagsContainer.GameplayTags);
		validationTags.UnionWith(TestUtils.StringToGameplayTag(_gameplayTagsManager, tagKeys));
		var validationContainer = new GameplayTagContainer(_gameplayTagsManager, validationTags);

		ActiveGameplayEffectHandle? activeEffectHandle = entity.EffectsManager.ApplyEffect(effect);
		Debug.Assert(activeEffectHandle is not null, "Effect handle should have a value.");

		entity.GameplayTags.CombinedTags.Equals(validationContainer).Should().BeTrue();
		entity.GameplayTags.ModifierTags.Equals(modifierTagsContainer).Should().BeTrue();

		entity.EffectsManager.UnapplyEffect(activeEffectHandle, true);
		entity.GameplayTags.CombinedTags.Equals(baseTagsContainer).Should().BeTrue();
		entity.GameplayTags.ModifierTags.IsEmpty.Should().BeTrue();
	}

	private GameplayEffectData CreateDurationEffectData(string[] tagKeys, float duration)
	{
		HashSet<GameplayTag> tags = TestUtils.StringToGameplayTag(_gameplayTagsManager, tagKeys);

		return new GameplayEffectData(
			"Test Effect",
			[],
			new DurationData(DurationType.HasDuration, new ScalableFloat(duration)),
			null,
			null,
			gameplayEffectComponents:
			[
				new ModifierTagsEffectComponent(new GameplayTagContainer(_gameplayTagsManager, tags))
			]);
	}

	private GameplayEffectData CreateInfiniteDurationEffectData(string[] tagKeys)
	{
		HashSet<GameplayTag> tags = TestUtils.StringToGameplayTag(_gameplayTagsManager, tagKeys);

		return new GameplayEffectData(
			"Test Effect",
			[],
			new DurationData(DurationType.Infinite),
			null,
			null,
			gameplayEffectComponents:
			[
				new ModifierTagsEffectComponent(new GameplayTagContainer(_gameplayTagsManager, tags))
			]);
	}

	private GameplayEffectData CreateSimpleStackableEffectData(string[] tagKeys, int stacks)
	{
		HashSet<GameplayTag> tags = TestUtils.StringToGameplayTag(_gameplayTagsManager, tagKeys);

		return new GameplayEffectData(
			"Test Effect",
			[],
			new DurationData(DurationType.Infinite),
			new StackingData(
				new ScalableInt(stacks),
				new ScalableInt(stacks),
				StackPolicy.AggregateBySource,
				StackLevelPolicy.SegregateLevels,
				StackMagnitudePolicy.Sum,
				StackOverflowPolicy.DenyApplication,
				StackExpirationPolicy.RemoveSingleStackAndRefreshDuration),
			null,
			gameplayEffectComponents:
			[
				new ModifierTagsEffectComponent(new GameplayTagContainer(_gameplayTagsManager, tags))
			]);
	}
}
