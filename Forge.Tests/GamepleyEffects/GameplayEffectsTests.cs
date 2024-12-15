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

		TestAttribute(target, targetAttribute, [expectedResult, expectedResult, 0, 0]);
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

		TestAttribute(target, targetAttribute, [expectedResult, expectedResult, 0, 0]);
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

		TestAttribute(target, targetAttribute, [firstExpectedResult, firstExpectedResult, 0, 0]);

		var effectData2 = new GameplayEffectData(
			"Rank Up",
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.PercentBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(secondEffectPercentMagnitude)))
			],
			new DurationData(DurationType.Instant),
			null,
			null);

		var effect2 = new GameplayEffect(
			effectData2,
			new GameplayEffectOwnership(owner, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect2);

		TestAttribute(target, targetAttribute, [secondExpectedResult, secondExpectedResult, 0, 0]);

		var effectData3 = new GameplayEffectData(
			"Rank Down",
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.PercentBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(thirdEffectPercentMagnitude)))
			],
			new DurationData(DurationType.Instant),
			null,
			null);

		var effect3 = new GameplayEffect(
			effectData3,
			new GameplayEffectOwnership(owner, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect3);

		TestAttribute(target, targetAttribute, [thirdExpectedResult, thirdExpectedResult, 0, 0]);

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

		TestAttribute(target, targetAttribute, [fourthExpectedResult, fourthExpectedResult, 0, 0]);
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

		TestAttribute(target, targetAttribute, [firstExpectedResult, firstExpectedResult, 0, 0]);

		effect.LevelUp();

		target.GameplayEffectsManager.ApplyEffect(effect);

		TestAttribute(target, targetAttribute, [secondExpectedResult, secondExpectedResult, 0, 0]);
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

		TestAttribute(target, targetAttribute, firstExpectedResults);

		effect.LevelUp();

		TestAttribute(target, targetAttribute, secondExpectedResult);
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

		TestAttribute(target, targetAttribute, [firstExpectedResult, firstExpectedResult, 0, 0]);

		target.GameplayEffectsManager.UpdateEffects(firstTimeUpdateDelta);

		TestAttribute(target, targetAttribute, [secondExpectedResult, secondExpectedResult, 0, 0]);

		effect.LevelUp();

		target.GameplayEffectsManager.UpdateEffects(secondTimeUpdateDelta);

		TestAttribute(target, targetAttribute, [thirdExpectedResult, thirdExpectedResult, 0, 0]);
	}

	[Theory]
	[Trait("Infinite", null)]
	[InlineData("TestAttributeSet.Attribute1", 10, 32f, 5f, new int[] { 11, 1, 10, 0 })]
	[InlineData("TestAttributeSet.Attribute2", 1, 60f, 0.1f, new int[] { 3, 2, 1, 0 })]
	[InlineData("TestAttributeSet.Attribute3", -10, 15f, 120f, new int[] { 0, 3, -10, -7 })]
	[InlineData("TestAttributeSet.Attribute5", 20, 33f, 999f, new int[] { 25, 5, 20, 0 })]
	[InlineData("TestAttributeSet.Attribute90", 100, 1f, 60f, new int[] { 99, 90, 100, 91 })]
	public void Inifinite_effect_modify_attribute_modifier_value(
		string targetAttribute,
		float modifierBaseMagnitude,
		float simulatedFPS,
		float totalSimulatedTime,
		int[] expectedResults)
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
						new ScalableFloat(modifierBaseMagnitude)))
			],
			new DurationData(DurationType.Infinite),
			null,
			null);

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(owner, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect);

		TestAttribute(target, targetAttribute, expectedResults);

		for (var i = 0f; i < simulatedFPS * totalSimulatedTime; i++)
		{
			target.GameplayEffectsManager.UpdateEffects(1f / simulatedFPS);
		}

		TestAttribute(target, targetAttribute, expectedResults);
	}

	[Theory]
	[Trait("Infinite", null)]
	[InlineData(
		"TestAttributeSet.Attribute1",
		4,
		new int[] { 5, 1, 4, 0 },
		4f,
		new int[] { 25, 1, 24, 0 },
		-0.66f,
		new int[] { 21, 1, 20, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute2",
		40,
		new int[] { 42, 2, 40, 0 },
		0.5f,
		new int[] { 63, 2, 61, 0 },
		-0.5f,
		new int[] { 42, 2, 40, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute3",
		47,
		new int[] { 50, 3, 47, 0 },
		-0.5f,
		new int[] { 25, 3, 22, 0 },
		-0.2f,
		new int[] { 15, 3, 12, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute5",
		45,
		new int[] { 50, 5, 45, 0 },
		1f,
		new int[] { 99, 5, 95, 1 },
		-1.5f,
		new int[] { 25, 5, 20, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute90",
		-10,
		new int[] { 80, 90, -10, 0 },
		-0.1f,
		new int[] { 72, 90, -18, 0 },
		-10f,
		new int[] { 0, 90, -818, -728 })]
	public void Infinite_effect_of_different_operations_update_modifier_value_accordingly(
		string targetAttribute,
		float firstEffectModifierBaseMagnitude,
		int[] firstExpectedResults,
		float secondEffectModifierBaseMagnitude,
		int[] secondExpectedResults,
		float thirdEffectModifierBaseMagnitude,
		int[] thirdExpectedResults)
	{
		var owner = new Entity(_gameplayTagsManager);
		var target = new Entity(_gameplayTagsManager);

		var effectData = new GameplayEffectData(
			"Buff1",
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(firstEffectModifierBaseMagnitude)))
			],
			new DurationData(DurationType.Infinite),
			null,
			null);

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(owner, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect);

		TestAttribute(target, targetAttribute, firstExpectedResults);

		var effectData2 = new GameplayEffectData(
			"Buff2",
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.PercentBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(secondEffectModifierBaseMagnitude)))
			],
			new DurationData(DurationType.Infinite),
			null,
			null);

		var effect2 = new GameplayEffect(
			effectData2,
			new GameplayEffectOwnership(owner, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect2);

		TestAttribute(target, targetAttribute, secondExpectedResults);

		var effectData3 = new GameplayEffectData(
			"Buff3",
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.PercentBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(thirdEffectModifierBaseMagnitude)))
			],
			new DurationData(DurationType.Infinite),
			null,
			null);

		var effect3 = new GameplayEffect(
			effectData3,
			new GameplayEffectOwnership(owner, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect3);

		TestAttribute(target, targetAttribute, thirdExpectedResults);
	}

	[Theory]
	[Trait("Infinite", null)]
	[InlineData(
		"TestAttributeSet.Attribute1",
		ModifierOperation.FlatBonus,
		4,
		0,
		ModifierOperation.PercentBonus,
		4f,
		0,
		ModifierOperation.PercentBonus,
		-0.66f,
		1,
		new int[] { 8, 1, 7, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute2",
		ModifierOperation.FlatBonus,
		8,
		1,
		ModifierOperation.PercentBonus,
		2f,
		0,
		ModifierOperation.PercentBonus,
		2f,
		1,
		new int[] { 42, 2, 40, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute3",
		ModifierOperation.FlatBonus,
		17,
		0,
		ModifierOperation.PercentBonus,
		1f,
		0,
		ModifierOperation.PercentBonus,
		-1f,
		1,
		new int[] { 0, 3, -3, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute5",
		ModifierOperation.FlatBonus,
		20,
		1,
		ModifierOperation.PercentBonus,
		-1f,
		0,
		ModifierOperation.PercentBonus,
		0.5f,
		1,
		new int[] { 30, 5, 25, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute90",
		ModifierOperation.FlatBonus,
		-10,
		0,
		ModifierOperation.PercentBonus,
		-1f,
		0,
		ModifierOperation.PercentBonus,
		1f,
		1,
		new int[] { 0, 90, -90, 0 })]
	public void Infinite_effect_computes_channels_accordingly(
		string targetAttribute,
		ModifierOperation firstModifierOperationType,
		float firstModifierBaseMagnitude,
		int firstChannel,
		ModifierOperation secondModifierOperationType,
		float secondModifierBaseMagnitude,
		int secondChannel,
		ModifierOperation thirdModifierOperationType,
		float thirdModifierBaseMagnitude,
		int thirdChannel,
		int[] expectedResults)
	{
		var owner = new Entity(_gameplayTagsManager);
		var target = new Entity(_gameplayTagsManager);

		var effectData = new GameplayEffectData(
			"Buff",
			[
				new Modifier(
					targetAttribute,
					firstModifierOperationType,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(firstModifierBaseMagnitude)),
					firstChannel),
				new Modifier(
					targetAttribute,
					secondModifierOperationType,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(secondModifierBaseMagnitude)),
					secondChannel),
				new Modifier(
					targetAttribute,
					thirdModifierOperationType,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(thirdModifierBaseMagnitude)),
					thirdChannel)
			],
			new DurationData(DurationType.Infinite),
			null,
			null);

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(owner, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect);

		TestAttribute(target, targetAttribute, expectedResults);
	}

	[Theory]
	[Trait("Duration", null)]
	[InlineData("TestAttributeSet.Attribute1", 10, 10f, 32, 5f, new int[] { 11, 1, 10, 0 }, 5f, 1)]
	[InlineData("TestAttributeSet.Attribute2", 10, 60f, 1, 59f, new int[] { 12, 2, 10, 0 }, 2f, 2)]
	[InlineData("TestAttributeSet.Attribute3", 10, 0.1f, 60, 0.05f, new int[] { 13, 3, 10, 0 }, 0.05f, 3)]
	[InlineData("TestAttributeSet.Attribute5", 10, 1f, 300, 0.5f, new int[] { 15, 5, 10, 0 }, 300f, 5)]
	[InlineData("TestAttributeSet.Attribute90", 10, 600f, 64, 1f, new int[] { 99, 90, 10, 1 }, 599f, 90)]
	public void Duration_effect_modifies_attribute_modifier_value_and_expire_after_duration_time(
		string targetAttribute,
		float modifierMagnitude,
		float baseDuration,
		float simulatedFPS,
		float unexpiringPeriodOfTime,
		int[] firstExpectedResults,
		float expiringPeriodOfTime,
		int lastExpectedResult)
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
						new ScalableFloat(modifierMagnitude)))
			],
			new DurationData(DurationType.HasDuration, new ScalableFloat(baseDuration)),
			null,
			null);

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(owner, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect);

		TestAttribute(target, targetAttribute, firstExpectedResults);

		for (var i = 0; i < simulatedFPS * unexpiringPeriodOfTime; i++)
		{
			target.GameplayEffectsManager.UpdateEffects(1f / simulatedFPS);
		}

		TestAttribute(target, targetAttribute, firstExpectedResults);

		for (var i = 0; i < simulatedFPS * expiringPeriodOfTime; i++)
		{
			target.GameplayEffectsManager.UpdateEffects(1 / simulatedFPS);
		}

		TestAttribute(target, targetAttribute, [lastExpectedResult, lastExpectedResult, 0, 0]);
	}

	[Theory]
	[Trait("Periodic", null)]
	[InlineData("TestAttributeSet.Attribute1", 10, 32, 1f, 11, 1f, 21, 5f, 71)]
	[InlineData("TestAttributeSet.Attribute2", 1, 64, 0.5f, 3, 1.66f, 6, 20f, 46)]
	[InlineData("TestAttributeSet.Attribute3", 3, 128, 10f, 6, 9f, 6, 60f, 24)]
	[InlineData("TestAttributeSet.Attribute5", 90, 1, 1f, 95, 9f, 99, 10f, 99)]
	[InlineData("TestAttributeSet.Attribute90", -5, 32, 0.01f, 85, 0.05f, 60, 10f, 0)]
	public void Periodic_effect_modifies_base_attribute_value(
		string targetAttribute,
		float modifierMagnitude,
		float simulatedFPS,
		float period,
		int firstExpectedResult,
		float firstPeriodOfTime,
		int secondExpectedResult,
		float secondPeriodOfTime,
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
						new ScalableFloat(modifierMagnitude)))
			],
			new DurationData(DurationType.Infinite),
			null,
			new PeriodicData(new ScalableFloat(period), true));

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(owner, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect);

		TestAttribute(target, targetAttribute, [firstExpectedResult, firstExpectedResult, 0, 0]);

		target.GameplayEffectsManager.UpdateEffects(firstPeriodOfTime);

		TestAttribute(target, targetAttribute, [secondExpectedResult, secondExpectedResult, 0, 0]);

		for (var i = 0; i < simulatedFPS * secondPeriodOfTime; i++)
		{
			target.GameplayEffectsManager.UpdateEffects(1 / simulatedFPS);
		}

		TestAttribute(target, targetAttribute, [thirdExpectedResult, thirdExpectedResult, 0, 0]);
	}

	[Theory]
	[Trait("Periodic", null)]
	[InlineData("TestAttributeSet.Attribute1", 10, 1f, 11, 1f, 21)]
	[InlineData("TestAttributeSet.Attribute2", 1, 0.1f, 3, 1f, 13)]
	[InlineData("TestAttributeSet.Attribute3", 10, 60f, 13, 59f, 13)]
	[InlineData("TestAttributeSet.Attribute5", 5, 1f, 10, 15.9f, 85)]
	[InlineData("TestAttributeSet.Attribute90", -1, 0.5f, 89, 60f, 0)]
	public void Snapshot_periodic_effect_modifies_base_attribute_with_same_value_even_after_level_up(
		string targetAttribute,
		float modifierMagnitude,
		float period,
		int firstExpectedResult,
		float simulatedPeriodOfTime,
		int secondExpectedResult)
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
						new ScalableFloat(modifierMagnitude)))
			],
			new DurationData(DurationType.Infinite),
			null,
			new PeriodicData(new ScalableFloat(period), true));

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(owner, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect);

		TestAttribute(target, targetAttribute, [firstExpectedResult, firstExpectedResult, 0, 0]);

		effect.LevelUp();

		target.GameplayEffectsManager.UpdateEffects(simulatedPeriodOfTime);

		TestAttribute(target, targetAttribute, [secondExpectedResult, secondExpectedResult, 0, 0]);
	}

	private static void TestAttribute(Entity target, string targetAttribute, int[] expectedResults)
	{
		target.Attributes[targetAttribute].CurrentValue.Should().Be(expectedResults[0]);
		target.Attributes[targetAttribute].BaseValue.Should().Be(expectedResults[1]);
		target.Attributes[targetAttribute].Modifier.Should().Be(expectedResults[2]);
		target.Attributes[targetAttribute].Overflow.Should().Be(expectedResults[3]);
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
			Attribute2 = InitializeAttribute(nameof(Attribute2), 2, 0, 99, 2);
			Attribute3 = InitializeAttribute(nameof(Attribute3), 3, 0, 99, 2);
			Attribute5 = InitializeAttribute(nameof(Attribute5), 5, 0, 99, 2);
			Attribute90 = InitializeAttribute(nameof(Attribute90), 90, 0, 99, 2);
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
