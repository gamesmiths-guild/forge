// Copyright © 2024 Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects;
using Gamesmiths.Forge.GameplayEffects.Duration;
using Gamesmiths.Forge.GameplayEffects.Magnitudes;
using Gamesmiths.Forge.GameplayEffects.Modifiers;
using Gamesmiths.Forge.GameplayEffects.Periodic;
using Gamesmiths.Forge.GameplayEffects.Stacking;
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
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(effectMagnitude)))
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
							new AttributeCaptureDefinition(backingAttribute, AttributeCaptureSource.Source),
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
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(modifierMagnitude)))
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
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(modifierMagnitude)))
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
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(modifierMagnitude)))
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

	[Theory]
	[Trait("Periodic", null)]
	[InlineData(
		"TestAttributeSet.Attribute1",
		"TestAttributeSet.Attribute2",
		2f,
		1f,
		2f,
		1f,
		9,
		1f,
		17,
		2f,
		4,
		1f,
		29,
		6,
		29,
		1f,
		45)]
	[InlineData(
		"TestAttributeSet.Attribute2",
		"TestAttributeSet.Attribute5",
		2f,
		2f,
		2f,
		1f,
		18,
		1f,
		34,
		2f,
		7,
		1f,
		54,
		9,
		54,
		1f,
		78)]
	[InlineData(
		"TestAttributeSet.Attribute90",
		"TestAttributeSet.Attribute1",
		-1f,
		1f,
		1f,
		0.5f,
		89,
		1f,
		87,
		2f,
		3,
		1f,
		81,
		5,
		81,
		1f,
		71)]
	[InlineData(
		"TestAttributeSet.Attribute5",
		"TestAttributeSet.Attribute3",
		1f,
		0f,
		0f,
		1f,
		8,
		1f,
		11,
		-2f,
		1,
		1f,
		12,
		0,
		12,
		1f,
		12)]
	public void Non_snapshot_priodic_effect_with_attribute_based_magnitude_should_update_when_attribute_updates(
		string targetAttribute,
		string backingAttribute,
		float coefficient,
		float preMultiplyAdditiveValue,
		float postMultiplyAdditiveValue,
		float period,
		int firstTargetExpectedResult,
		float firstSimulatedPeriodOfTime,
		int secondTargetExpectedResult,
		float ownerBonusEffectMagnitude,
		int firstOwnerExpectedResult,
		float secondSimulatedPeriodOfTime,
		int thirdTargetExpectedResult,
		int secondOwnerExpectedResult,
		int fourthTargetExpectedResult,
		float thirdSimulatedPeriodOfTime,
		int fifthTargetExpectedResult)
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
						MagnitudeCalculationType.AttributeBased,
						attributeBasedFloat: new AttributeBasedFloat(
							new AttributeCaptureDefinition(backingAttribute, AttributeCaptureSource.Source, false),
							AttributeBasedFloatCalculationType.AttributeBaseValue,
							new ScalableFloat(coefficient),
							new ScalableFloat(preMultiplyAdditiveValue),
							new ScalableFloat(postMultiplyAdditiveValue))))
			],
			new DurationData(DurationType.Infinite),
			null,
			new PeriodicData(new ScalableFloat(period), true));

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(owner, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect);

		TestAttribute(target, targetAttribute, [firstTargetExpectedResult, firstTargetExpectedResult, 0, 0]);

		target.GameplayEffectsManager.UpdateEffects(firstSimulatedPeriodOfTime);

		TestAttribute(target, targetAttribute, [secondTargetExpectedResult, secondTargetExpectedResult, 0, 0]);

		var effectData2 = new GameplayEffectData(
			"Buff2",
			[
				new Modifier(
					backingAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(ownerBonusEffectMagnitude)))
			],
			new DurationData(DurationType.Instant),
			null,
			null);

		var effect2 = new GameplayEffect(
			effectData2,
			new GameplayEffectOwnership(owner, new Entity(_gameplayTagsManager)));

		owner.GameplayEffectsManager.ApplyEffect(effect2);

		TestAttribute(owner, backingAttribute, [firstOwnerExpectedResult, firstOwnerExpectedResult, 0, 0]);

		target.GameplayEffectsManager.UpdateEffects(secondSimulatedPeriodOfTime);

		TestAttribute(target, targetAttribute, [thirdTargetExpectedResult, thirdTargetExpectedResult, 0, 0]);

		owner.GameplayEffectsManager.ApplyEffect(effect2);

		TestAttribute(owner, backingAttribute, [secondOwnerExpectedResult, secondOwnerExpectedResult, 0, 0]);

		TestAttribute(target, targetAttribute, [fourthTargetExpectedResult, fourthTargetExpectedResult, 0, 0]);

		target.GameplayEffectsManager.UpdateEffects(thirdSimulatedPeriodOfTime);

		TestAttribute(target, targetAttribute, [fifthTargetExpectedResult, fifthTargetExpectedResult, 0, 0]);
	}

	[Theory]
	[Trait("Infinite", null)]
	[InlineData(
		"TestAttributeSet.Attribute1",
		"TestAttributeSet.Attribute2",
		2f,
		1f,
		2f,
		new int[] { 9, 1, 8, 0 },
		2f,
		4,
		new int[] { 13, 1, 12, 0 },
		6,
		new int[] { 17, 1, 16, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute2",
		"TestAttributeSet.Attribute5",
		2f,
		2f,
		2f,
		new int[] { 18, 2, 16, 0 },
		1f,
		6,
		new int[] { 20, 2, 18, 0 },
		7,
		new int[] { 22, 2, 20, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute90",
		"TestAttributeSet.Attribute1",
		-1f,
		1f,
		1f,
		new int[] { 89, 90, -1, 0 },
		2f,
		3,
		new int[] { 87, 90, -3, 0 },
		5,
		new int[] { 85, 90, -5, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute5",
		"TestAttributeSet.Attribute3",
		1f,
		0f,
		0f,
		new int[] { 8, 5, 3, 0 },
		-2f,
		1,
		new int[] { 6, 5, 1, 0 },
		0,
		new int[] { 5, 5, 0, 0 })]
	public void Non_periodic_non_snapshot_attribute_based_magnitude_updates_modifiers_when_attribute_updates(
		string targetAttribute,
		string backingAttribute,
		float coefficient,
		float preMultiplyAdditiveValue,
		float postMultiplyAdditiveValue,
		int[] firstTargetExpectedResults,
		float ownerBonusEffectMagnitude,
		int firstOwnerExpectedResult,
		int[] secondTargetExpectedResults,
		int secondOwnerExpectedResult,
		int[] thirdTargetExpectedResults)
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
						MagnitudeCalculationType.AttributeBased,
						attributeBasedFloat: new AttributeBasedFloat(
							new AttributeCaptureDefinition(backingAttribute, AttributeCaptureSource.Source, false),
							AttributeBasedFloatCalculationType.AttributeBaseValue,
							new ScalableFloat(coefficient),
							new ScalableFloat(preMultiplyAdditiveValue),
							new ScalableFloat(postMultiplyAdditiveValue))))
			],
			new DurationData(DurationType.Infinite),
			null,
			null);

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(owner, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect);

		TestAttribute(target, targetAttribute, firstTargetExpectedResults);

		var effectData2 = new GameplayEffectData(
			"Buff2",
			[
				new Modifier(
					backingAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(ownerBonusEffectMagnitude)))
			],
			new DurationData(DurationType.Instant),
			null,
			null);

		var effect2 = new GameplayEffect(
			effectData2,
			new GameplayEffectOwnership(owner, new Entity(_gameplayTagsManager)));

		owner.GameplayEffectsManager.ApplyEffect(effect2);

		TestAttribute(owner, backingAttribute, [firstOwnerExpectedResult, firstOwnerExpectedResult, 0, 0]);

		TestAttribute(target, targetAttribute, secondTargetExpectedResults);

		owner.GameplayEffectsManager.ApplyEffect(effect2);

		TestAttribute(owner, backingAttribute, [secondOwnerExpectedResult, secondOwnerExpectedResult, 0, 0]);

		TestAttribute(target, targetAttribute, thirdTargetExpectedResults);
	}

	[Theory]
	[Trait("Periodic", null)]
	[InlineData("TestAttributeSet.Attribute1", 10f, 3f, 1f, 32, 11, 1f, 21, 5f, 41)]
	[InlineData("TestAttributeSet.Attribute2", 2f, 5f, 0.5f, 64, 4, 4.9f, 22, 5f, 24)]
	[InlineData("TestAttributeSet.Attribute3", 3f, 60f, 30f, 128, 6, 29f, 6, 120f, 12)]
	[InlineData("TestAttributeSet.Attribute5", 5f, 3f, 10f, 16, 10, 1f, 10, 5f, 10)]
	[InlineData("TestAttributeSet.Attribute90", -10f, 0.5f, 0.1f, 32, 80, 1f, 30, 1f, 30)]
	public void Periodic_effect_modifies_base_attribute_value_and_expire_after_duration_time(
		string targetAttribute,
		float modifierMagnitude,
		float duration,
		float period,
		float simulatedFPS,
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
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(modifierMagnitude)))
			],
			new DurationData(DurationType.HasDuration, new ScalableFloat(duration)),
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
	[Trait("Stackable", null)]
	[InlineData(
		new int[] { 11, 1, 10, 0 },
		new int[] { 21, 1, 20, 0 },
		1,
		new object[] { new int[] { 2, 1, 0 } },
		"TestAttributeSet.Attribute1",
		10f,
		5,
		1,
		StackPolicy.AggregateByTarget,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.DenyApplication,
		StackExpirationPolicy.ClearEntireStack,
		StackOwnerDenialPolicy.AlwaysAllow,
		StackOwnerOverridePolicy.KeepCurrent)]
	[InlineData(
		new int[] { 11, 1, 10, 0 },
		new int[] { 11, 1, 10, 0 },
		1,
		new object[] { new int[] { 2, 1, 0 } },
		"TestAttributeSet.Attribute1",
		10f,
		5,
		1,
		StackPolicy.AggregateByTarget,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.DontStack,
		StackOverflowPolicy.DenyApplication,
		StackExpirationPolicy.ClearEntireStack,
		StackOwnerDenialPolicy.AlwaysAllow,
		StackOwnerOverridePolicy.KeepCurrent)]
	[InlineData(
		new int[] { 41, 1, 40, 0 },
		new int[] { 51, 1, 50, 0 },
		1,
		new object[] { new int[] { 5, 1, 0 } },
		"TestAttributeSet.Attribute1",
		10f,
		5,
		4,
		StackPolicy.AggregateByTarget,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.DenyApplication,
		StackExpirationPolicy.ClearEntireStack,
		StackOwnerDenialPolicy.AlwaysAllow,
		StackOwnerOverridePolicy.KeepCurrent)]
	public void Stackable_effect_from_two_different_sources_gives_expected_stack_data(
		int[] firstExpectedResults,
		int[] secondExpectedResults,
		int expectedStackDataCount,
		object[] expectedStackData,
		string targetAttribute,
		float modifierMagnitude,
		int stackLimit,
		int initialStack,
		StackPolicy stackPolicy,
		StackLevelPolicy stackLevelPolicy,
		StackMagnitudePolicy magnitudePolicy,
		StackOverflowPolicy overflowPolicy,
		StackExpirationPolicy expirationPolicy,
		StackOwnerDenialPolicy? ownerDenialPolicy = null,
		StackOwnerOverridePolicy? ownerOverridePolicy = null,
		StackOwnerOverrideStackCountPolicy? ownerOverrideStackCountPolicy = null,
		LevelComparison? levelDenialPolicy = null,
		LevelComparison? levelOverridePolicy = null,
		StackLevelOverrideStackCountPolicy? levelOverrideStackCountPolicy = null,
		StackApplicationRefreshPolicy? applicationRefreshPolicy = null,
		StackApplicationResetPeriodPolicy? applicationResetPeriodPolicy = null,
		bool? executeOnSuccessfulApplication = null)
	{
		var owner1 = new Entity(_gameplayTagsManager);
		var owner2 = new Entity(_gameplayTagsManager);
		var target = new Entity(_gameplayTagsManager);

		var effectData = new GameplayEffectData(
			"Buff",
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(modifierMagnitude)))
			],
			new DurationData(DurationType.Infinite),
			new StackingData(
				new ScalableInt(stackLimit),
				new ScalableInt(initialStack),
				stackPolicy,
				stackLevelPolicy,
				magnitudePolicy,
				overflowPolicy,
				expirationPolicy,
				ownerDenialPolicy,
				ownerOverridePolicy,
				ownerOverrideStackCountPolicy,
				levelDenialPolicy,
				levelOverridePolicy,
				levelOverrideStackCountPolicy,
				applicationRefreshPolicy,
				applicationResetPeriodPolicy,
				executeOnSuccessfulApplication),
			null);

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(owner1, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect);

		TestAttribute(target, targetAttribute, firstExpectedResults);

		var effect2 = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(owner2, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect2);

		TestAttribute(target, targetAttribute, secondExpectedResults);

		TestStackData(
			target.GameplayEffectsManager.GetEffectInfo(effectData),
			expectedStackDataCount,
			expectedStackData,
			owner1,
			owner2);
	}

	[Theory]
	[Trait("Stackable", null)]
	[InlineData(
		new int[] { 6, 1, 5, 0 },
		new int[] { 16, 1, 15, 0 },
		new int[] { 31, 1, 30, 0 },
		new int[] { 31, 1, 30, 0 },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		2,
		new object[] { new int[] { 1, 1, 0 }, new int[] { 1, 2, 1 } },
		3,
		new object[] { new int[] { 1, 1, 0 }, new int[] { 1, 2, 1 }, new int[] { 1, 3, 0 } },
		3,
		new object[] { new int[] { 1, 1, 0 }, new int[] { 2, 2, 1 }, new int[] { 1, 3, 0 } },
		"TestAttributeSet.Attribute1",
		5f,
		new float[] { 1, 2, 3 },
		5,
		1,
		StackPolicy.AggregateByTarget,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.DontStack,
		StackOverflowPolicy.DenyApplication,
		StackExpirationPolicy.ClearEntireStack,
		StackOwnerDenialPolicy.AlwaysAllow,
		StackOwnerOverridePolicy.KeepCurrent)]
	[InlineData(
		new int[] { 6, 1, 5, 0 },
		new int[] { 21, 1, 20, 0 },
		new int[] { 46, 1, 45, 0 },
		new int[] { 41, 1, 40, 0 },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 2, 2, 1 } },
		1,
		new object[] { new int[] { 3, 3, 0 } },
		1,
		new object[] { new int[] { 4, 2, 1 } },
		"TestAttributeSet.Attribute1",
		5f,
		new float[] { 1, 2, 3 },
		5,
		1,
		StackPolicy.AggregateByTarget,
		StackLevelPolicy.AggregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.DenyApplication,
		StackExpirationPolicy.ClearEntireStack,
		StackOwnerDenialPolicy.AlwaysAllow,
		StackOwnerOverridePolicy.Override,
		StackOwnerOverrideStackCountPolicy.IncreaseStacks,
		LevelComparison.None,
		LevelComparison.Lower | LevelComparison.Equal | LevelComparison.Higher,
		StackLevelOverrideStackCountPolicy.IncreaseStacks)]
	public void Stackable_effect_with_different_levels_gives_expected_stack_data(
		int[] firstExpectedResults,
		int[] secondExpectedResults,
		int[] thirdExpectedResults,
		int[] fourthExpectedResults,
		int firstExpectedStackDataCount,
		object[] firstExpectedStackData,
		int secondExpectedStackDataCount,
		object[] secondExpectedStackData,
		int thirdExpectedStackDataCount,
		object[] thirdExpectedStackData,
		int fourthExpectedStackDataCount,
		object[] fourthExpectedStackData,
		string targetAttribute,
		float modifierMagnitude,
		float[] modifierLevelMultipliers,
		int stackLimit,
		int initialStack,
		StackPolicy stackPolicy,
		StackLevelPolicy stackLevelPolicy,
		StackMagnitudePolicy magnitudePolicy,
		StackOverflowPolicy overflowPolicy,
		StackExpirationPolicy expirationPolicy,
		StackOwnerDenialPolicy? ownerDenialPolicy = null,
		StackOwnerOverridePolicy? ownerOverridePolicy = null,
		StackOwnerOverrideStackCountPolicy? ownerOverrideStackCountPolicy = null,
		LevelComparison? levelDenialPolicy = null,
		LevelComparison? levelOverridePolicy = null,
		StackLevelOverrideStackCountPolicy? levelOverrideStackCountPolicy = null,
		StackApplicationRefreshPolicy? applicationRefreshPolicy = null,
		StackApplicationResetPeriodPolicy? applicationResetPeriodPolicy = null,
		bool? executeOnSuccessfulApplication = null)
	{
		var owner1 = new Entity(_gameplayTagsManager);
		var owner2 = new Entity(_gameplayTagsManager);
		var target = new Entity(_gameplayTagsManager);

		var effectData = new GameplayEffectData(
			"Buff",
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(modifierMagnitude, new Curve(
							[
								new CurveKey(1, modifierLevelMultipliers[0]),
								new CurveKey(2, modifierLevelMultipliers[1]),
								new CurveKey(3, modifierLevelMultipliers[2])
							]))))
			],
			new DurationData(DurationType.Infinite),
			new StackingData(
				new ScalableInt(stackLimit),
				new ScalableInt(initialStack),
				stackPolicy,
				stackLevelPolicy,
				magnitudePolicy,
				overflowPolicy,
				expirationPolicy,
				ownerDenialPolicy,
				ownerOverridePolicy,
				ownerOverrideStackCountPolicy,
				levelDenialPolicy,
				levelOverridePolicy,
				levelOverrideStackCountPolicy,
				applicationRefreshPolicy,
				applicationResetPeriodPolicy,
				executeOnSuccessfulApplication),
			null);

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(owner1, new Entity(_gameplayTagsManager)));

		target.GameplayEffectsManager.ApplyEffect(effect);

		TestAttribute(target, targetAttribute, firstExpectedResults);

		TestStackData(
			target.GameplayEffectsManager.GetEffectInfo(effectData),
			firstExpectedStackDataCount,
			firstExpectedStackData,
			owner1,
			owner2);

		var effect2 = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(owner2, new Entity(_gameplayTagsManager)),
			2);

		target.GameplayEffectsManager.ApplyEffect(effect2);

		TestAttribute(target, targetAttribute, secondExpectedResults);

		TestStackData(
			target.GameplayEffectsManager.GetEffectInfo(effectData),
			secondExpectedStackDataCount,
			secondExpectedStackData,
			owner1,
			owner2);

		var effect3 = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(owner1, new Entity(_gameplayTagsManager)),
			3);

		target.GameplayEffectsManager.ApplyEffect(effect3);

		TestAttribute(target, targetAttribute, thirdExpectedResults);

		TestStackData(
			target.GameplayEffectsManager.GetEffectInfo(effectData),
			thirdExpectedStackDataCount,
			thirdExpectedStackData,
			owner1,
			owner2);

		target.GameplayEffectsManager.ApplyEffect(effect2);

		TestAttribute(target, targetAttribute, fourthExpectedResults);

		TestStackData(
			target.GameplayEffectsManager.GetEffectInfo(effectData),
			fourthExpectedStackDataCount,
			fourthExpectedStackData,
			owner1,
			owner2);
	}

	private static void TestStackData(
		IEnumerable<GameplayEffectStackInstanceData> stackData,
		int expectedStackDataCount,
		object[] expectedStackData,
		Entity entity1,
		Entity entity2)
	{
		stackData.Should().HaveCount(expectedStackDataCount);

		for (var i = 0; i < expectedStackDataCount; i++)
		{
			var expectedData = (int[])expectedStackData[i];

			stackData.ElementAt(i).StackCount.Should().Be(expectedData[0]);
			stackData.ElementAt(i).EffectLevel.Should().Be(expectedData[1]);
#pragma warning disable FAA0001 // Simplify Assertion
			stackData.ElementAt(i).Owner.Should().Be(expectedData[2] == 0 ? entity1 : entity2);
#pragma warning restore FAA0001 // Simplify Assertion
		}
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
