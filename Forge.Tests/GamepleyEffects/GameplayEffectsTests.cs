// Copyright © 2024 Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects;
using Gamesmiths.Forge.GameplayEffects.Duration;
using Gamesmiths.Forge.GameplayEffects.Magnitudes;
using Gamesmiths.Forge.GameplayEffects.Modifiers;
using Gamesmiths.Forge.GameplayEffects.Periodic;
using Gamesmiths.Forge.GameplayTags;
using Gamesmiths.Forge.Tests.GameplayTags;
using Attribute = Gamesmiths.Forge.Attributes.Attribute;

namespace Gamesmiths.Forge.Tests.GamepleyEffects;

public class GameplayEffectsTests(GameplayTagsManagerFixture fixture) : IClassFixture<GameplayTagsManagerFixture>
{
	private readonly GameplayTagsManager _gameplayTagsManager = fixture.GameplayTagsManager;

	[Theory]
	[Trait("Instant", null)]
	[InlineData("TestAttributeSet.Attribute1", 10, 11)]
	[InlineData("TestAttributeSet.Attribute1", -10, 0)]
	[InlineData("TestAttributeSet.Attribute90", 25, 99)]
	[InlineData("TestAttributeSet.Attribute3", 20, 23)]
	[InlineData("TestAttributeSet.Attribute5", -2, 3)]
	public void Instant_effect_modifies_attribute_base_value(
		string targetAttribute,
		int effectMagnitude,
		int expectedResult)
	{
		var owner = new Entity(_gameplayTagsManager);
		var target = new Entity(_gameplayTagsManager);

		var effectData = new GameplayEffectData(
			"Level Up",
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(effectMagnitude)))
			],
			new DurationData(DurationType.Instant),
			null,
			null);

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(
				new Entity(_gameplayTagsManager),
				owner));

		target.GameplayEffectsManager.ApplyEffect(effect);

		target.Attributes[targetAttribute].CurrentValue.Should().Be(expectedResult);
		target.Attributes[targetAttribute].BaseValue.Should().Be(expectedResult);
		target.Attributes[targetAttribute].Modifier.Should().Be(0);
		target.Attributes[targetAttribute].Overflow.Should().Be(0);
	}

	[Theory]
	[Trait("Instant", null)]
	[InlineData("TestAttributeSet.Attribute1", "TestAttributeSet.Attribute1", 2, 1, 2, 7)]
	[InlineData("TestAttributeSet.Attribute1", "TestAttributeSet.Attribute2", 2, 1, 2, 9)]
	[InlineData("TestAttributeSet.Attribute90", "TestAttributeSet.Attribute5", -1, 5, 2, 82)]
	[InlineData("TestAttributeSet.Attribute3", "TestAttributeSet.Attribute5", 1.5f, 2.2f, 3.5f, 17)]
	public void Attribute_based_effect_modifies_values_based_on_source_attribute(
		string targetAttribute,
		string backingAttribute,
		float coefficient,
		float preMultiplyAdditiveValue,
		float postMultiplyAdditiveValue,
		int expectedResult)
	{
		var owner = new Entity(_gameplayTagsManager);
		var target = new Entity(_gameplayTagsManager);

		var effectData = new GameplayEffectData(
			"Level Up",
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.AttributeBased,
						attributeBasedFloat: new AttributeBasedFloat(
							new AttributeCaptureDefinition(
								backingAttribute,
								AttributeCaptureSource.Source),
							AttributeBasedFloatCalculationType.AttributeBaseValue,
							new ScalableFloat(coefficient),
							new ScalableFloat(preMultiplyAdditiveValue),
							new ScalableFloat(postMultiplyAdditiveValue))))
			],
			new DurationData(DurationType.Instant),
			null,
			null);

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(owner, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect);

		target.Attributes[targetAttribute].CurrentValue.Should().Be(expectedResult);
		target.Attributes[targetAttribute].BaseValue.Should().Be(expectedResult);
		target.Attributes[targetAttribute].Modifier.Should().Be(0);
		target.Attributes[targetAttribute].Overflow.Should().Be(0);
	}

	[Theory]
	[Trait("Instant", null)]
	[InlineData("TestAttributeSet.Attribute1", 4, 5, 4, 25, -0.66f, 8, 42, 42)]
	[InlineData("TestAttributeSet.Attribute2", 8, 10, 2, 30, -0.66f, 10, 99, 99)]
	[InlineData("TestAttributeSet.Attribute3", 20, 23, 0.5f, 34, 1, 68, -10, 0)]
	[InlineData("TestAttributeSet.Attribute90", 90, 99, 0.3f, 99, 0f, 99, 100, 99)]
	public void Multiple_instant_effects_of_different_operations_modify_base_value_accordingly(
		string targetAttribute,
		float firstEffectFlatMagnitude,
		int firstExpectedResult,
		float secondEffectPercentMagnitude,
		int secondExpectedResult,
		float thirdEffectPercentMagnitude,
		int thirdExpectedResult,
		float fourthEffectOverrideMagnitude,
		int fourthExpectedResult)
	{
		var owner = new Entity(_gameplayTagsManager);
		var target = new Entity(_gameplayTagsManager);

		var effectData = new GameplayEffectData(
			"Level Up",
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(firstEffectFlatMagnitude)))
			],
			new DurationData(DurationType.Instant),
			null,
			null);

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(owner, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect);

		target.Attributes[targetAttribute].CurrentValue.Should().Be(firstExpectedResult);
		target.Attributes[targetAttribute].BaseValue.Should().Be(firstExpectedResult);
		target.Attributes[targetAttribute].Modifier.Should().Be(0);
		target.Attributes[targetAttribute].Overflow.Should().Be(0);

		var effectData2 = new GameplayEffectData(
			"Rank Up",
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.PercentBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(secondEffectPercentMagnitude))) // 400% bonus
			],
			new DurationData(DurationType.Instant),
			null,
			null);

		var effect2 = new GameplayEffect(
			effectData2,
			new GameplayEffectOwnership(owner, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect2);

		target.Attributes[targetAttribute].CurrentValue.Should().Be(secondExpectedResult);
		target.Attributes[targetAttribute].BaseValue.Should().Be(secondExpectedResult);
		target.Attributes[targetAttribute].Modifier.Should().Be(0);
		target.Attributes[targetAttribute].Overflow.Should().Be(0);

		var effectData3 = new GameplayEffectData(
			"Rank Down",
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.PercentBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(thirdEffectPercentMagnitude))) // Divides by 3 (66% reduction)
			],
			new DurationData(DurationType.Instant),
			null,
			null);

		var effect3 = new GameplayEffect(
			effectData3,
			new GameplayEffectOwnership(owner, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect3);

		target.Attributes[targetAttribute].CurrentValue.Should().Be(thirdExpectedResult);
		target.Attributes[targetAttribute].BaseValue.Should().Be(thirdExpectedResult);
		target.Attributes[targetAttribute].Modifier.Should().Be(0);
		target.Attributes[targetAttribute].Overflow.Should().Be(0);

		var effectData4 = new GameplayEffectData(
			"Rank Fix",
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.Override,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(fourthEffectOverrideMagnitude)))
			],
			new DurationData(DurationType.Instant),
			null,
			null);

		var effect4 = new GameplayEffect(
			effectData4,
			new GameplayEffectOwnership(owner, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect4);

		target.Attributes[targetAttribute].CurrentValue.Should().Be(fourthExpectedResult);
		target.Attributes[targetAttribute].BaseValue.Should().Be(fourthExpectedResult);
		target.Attributes[targetAttribute].Modifier.Should().Be(0);
		target.Attributes[targetAttribute].Overflow.Should().Be(0);
	}

	[Theory]
	[Trait("Instant", null)]
	[InlineData("TestAttributeSet.Attribute1", 10, 1, 2, 11, 31)]
	[InlineData("TestAttributeSet.Attribute2", 5, 1, 2.5f, 7, 19)]
	[InlineData("TestAttributeSet.Attribute3", 40, 1, 0.2f, 43, 51)]
	[InlineData("TestAttributeSet.Attribute5", 10, 1, -1, 15, 5)]
	[InlineData("TestAttributeSet.Attribute90", 1, 2, 1, 92, 93)]
	public void Modifiers_of_different_level_effects_apply_different_modifiers(
		string targetAttribute,
		float modifierBaseMagnitude,
		float modifierLevel1Multiplier,
		float modifierLevel2Multiplier,
		int firstExpectedResult,
		int secondExpectedResult)
	{
		var owner = new Entity(_gameplayTagsManager);
		var target = new Entity(_gameplayTagsManager);

		var effectData = new GameplayEffectData(
			"Level Up",
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(modifierBaseMagnitude, new Curve(
							[
								new CurveKey(1, modifierLevel1Multiplier),
								new CurveKey(2, modifierLevel2Multiplier),
							]))))
			],
			new DurationData(DurationType.Instant),
			null,
			null);

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(owner, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect);

		target.Attributes[targetAttribute].CurrentValue.Should().Be(firstExpectedResult);
		target.Attributes[targetAttribute].BaseValue.Should().Be(firstExpectedResult);
		target.Attributes[targetAttribute].Modifier.Should().Be(0);
		target.Attributes[targetAttribute].Overflow.Should().Be(0);

		effect.LevelUp();

		target.GameplayEffectsManager.ApplyEffect(effect);

		target.Attributes[targetAttribute].CurrentValue.Should().Be(secondExpectedResult);
		target.Attributes[targetAttribute].BaseValue.Should().Be(secondExpectedResult);
		target.Attributes[targetAttribute].Modifier.Should().Be(0);
		target.Attributes[targetAttribute].Overflow.Should().Be(0);
	}

	[Theory]
	[Trait("Duration", null)]
	[InlineData("TestAttributeSet.Attribute1", 10, 1, 2, new int[] { 11, 1, 10, 0 }, new int[] { 21, 1, 20, 0 })]
	[InlineData("TestAttributeSet.Attribute2", 5, 1, 2.5f, new int[] { 7, 2, 5, 0 }, new int[] { 14, 2, 12, 0 })]
	[InlineData("TestAttributeSet.Attribute3", 40, 1, 0.2f, new int[] { 43, 3, 40, 0 }, new int[] { 11, 3, 8, 0 })]
	[InlineData("TestAttributeSet.Attribute5", 10, 1, -1, new int[] { 15, 5, 10, 0 }, new int[] { 0, 5, -10, -5 })]
	[InlineData("TestAttributeSet.Attribute90", 1, 2, 1, new int[] { 92, 90, 2, 0 }, new int[] { 91, 90, 1, 0 })]
	[InlineData("TestAttributeSet.Attribute90", 5, 2, 4, new int[] { 99, 90, 10, 1 }, new int[] { 99, 90, 20, 11 })]
	public void Non_snapshot_level_effect_updates_value_on_level_up(
		string targetAttribute,
		float modifierBaseMagnitude,
		float modifierLevel1Multiplier,
		float modifierLevel2Multiplier,
		int[] firstExpectedResults,
		int[] secondExpectedResult)
	{
		var owner = new Entity(_gameplayTagsManager);
		var target = new Entity(_gameplayTagsManager);

		var effectData = new GameplayEffectData(
			"Level Up",
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(modifierBaseMagnitude, new Curve(
							[
								new CurveKey(1, modifierLevel1Multiplier),
								new CurveKey(2, modifierLevel2Multiplier),
							]))))
			],
			new DurationData(DurationType.Infinite),
			null,
			null,
			false);

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(owner, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect);

		target.Attributes[targetAttribute].CurrentValue.Should().Be(firstExpectedResults[0]);
		target.Attributes[targetAttribute].BaseValue.Should().Be(firstExpectedResults[1]);
		target.Attributes[targetAttribute].Modifier.Should().Be(firstExpectedResults[2]);
		target.Attributes[targetAttribute].Overflow.Should().Be(firstExpectedResults[3]);

		effect.LevelUp();

		target.Attributes[targetAttribute].CurrentValue.Should().Be(secondExpectedResult[0]);
		target.Attributes[targetAttribute].BaseValue.Should().Be(secondExpectedResult[1]);
		target.Attributes[targetAttribute].Modifier.Should().Be(secondExpectedResult[2]);
		target.Attributes[targetAttribute].Overflow.Should().Be(secondExpectedResult[3]);
	}

	[Theory]
	[Trait("Periodic", null)]
	[InlineData("TestAttributeSet.Attribute1", 1, 1, 2, 1, 1, 1, 2, 1, 3, 1, 5)]
	[InlineData("TestAttributeSet.Attribute1", 10, 1, 2, 1, 1, 0.5f, 11, 1.1f, 21, 1.5f, 61)]
	[InlineData("TestAttributeSet.Attribute2", 5, 1, 2, 1, 1, 4, 7, 2.5f, 17, 2.5f, 27)]
	[InlineData("TestAttributeSet.Attribute3", 10, 2, 1, 0.5f, 1, 0.5f, 23, 0.4f, 23, 0.1f, 33)]
	[InlineData("TestAttributeSet.Attribute5", 5, 1, 0.2f, 1, 1, 0.1f, 10, 3.1f, 25, 1.79f, 34)]
	[InlineData("TestAttributeSet.Attribute90", -1, 1, 2, 0.1f, 1, 1, 89, 2f, 69, 100f, 0)]
	public void Non_snapshot_level_periodic_effect_updates_scalable_float_values_on_level_up(
		string targetAttribute,
		float modifierBaseMagnitude,
		float modifierLevel1Multiplier,
		float modifierLevel2Multiplier,
		float periodicBaseValue,
		float periodicLevel1Multiplier,
		float periodicLevel2Multiplier,
		int firstExpectedResult,
		float firstTimeUpdateDelta,
		int secondExpectedResult,
		float secondTimeUpdateDelta,
		int thirdExpectedResult)
	{
		var owner = new Entity(_gameplayTagsManager);
		var target = new Entity(_gameplayTagsManager);

		var effectData = new GameplayEffectData(
			"Buff",
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(modifierBaseMagnitude, new Curve(
							[
								new CurveKey(1, modifierLevel1Multiplier),
								new CurveKey(2, modifierLevel2Multiplier),
							]))))
			],
			new DurationData(DurationType.Infinite),
			null,
			new PeriodicData(
				new ScalableFloat(periodicBaseValue, new Curve(
					[
						new CurveKey(1, periodicLevel1Multiplier),
						new CurveKey(2, periodicLevel2Multiplier),
					])),
				true),
			false);

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(owner, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect);

		target.Attributes[targetAttribute].CurrentValue.Should().Be(firstExpectedResult);
		target.Attributes[targetAttribute].BaseValue.Should().Be(firstExpectedResult);
		target.Attributes[targetAttribute].Modifier.Should().Be(0);
		target.Attributes[targetAttribute].Overflow.Should().Be(0);

		target.GameplayEffectsManager.UpdateEffects(firstTimeUpdateDelta);

		target.Attributes[targetAttribute].CurrentValue.Should().Be(secondExpectedResult);
		target.Attributes[targetAttribute].BaseValue.Should().Be(secondExpectedResult);
		target.Attributes[targetAttribute].Modifier.Should().Be(0);
		target.Attributes[targetAttribute].Overflow.Should().Be(0);

		effect.LevelUp();

		target.GameplayEffectsManager.UpdateEffects(secondTimeUpdateDelta);

		target.Attributes[targetAttribute].CurrentValue.Should().Be(thirdExpectedResult);
		target.Attributes[targetAttribute].BaseValue.Should().Be(thirdExpectedResult);
		target.Attributes[targetAttribute].Modifier.Should().Be(0);
		target.Attributes[targetAttribute].Overflow.Should().Be(0);
	}

	private class TestAttributeSet : AttributeSet
	{
		public Attribute Attribute1 { get; }

		public Attribute Attribute2 { get; }

		public Attribute Attribute3 { get; }

		public Attribute Attribute5 { get; }

		public Attribute Attribute90 { get; }

		public TestAttributeSet()
		{
			Attribute1 = InitializeAttribute(nameof(Attribute1), 1, 0, 99, 2);
			Attribute2 = InitializeAttribute(nameof(Attribute2), 2, 0, 99);
			Attribute3 = InitializeAttribute(nameof(Attribute3), 3, 0, 99);
			Attribute5 = InitializeAttribute(nameof(Attribute5), 5, 0, 99);
			Attribute90 = InitializeAttribute(nameof(Attribute90), 90, 0, 99);
		}
	}

	private class Entity : IForgeEntity
	{
		public TestAttributeSet PlayerAttributeSet { get; }

		public GameplayEffectsManager GameplayEffectsManager { get; }

		public List<AttributeSet> AttributeSets { get; }

		public Dictionary<StringKey, Attribute> Attributes { get; }

		public GameplayTagContainer GameplayTags { get; }

		public Entity(GameplayTagsManager tagsManager)
		{
			GameplayEffectsManager = new(this);
			AttributeSets = [];
			Attributes = [];
			GameplayTags = new(tagsManager);

			PlayerAttributeSet = new TestAttributeSet();
			((IForgeEntity)this).AddAttributeSet(PlayerAttributeSet);
		}
	}
}
