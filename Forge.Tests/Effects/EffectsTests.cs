// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Calculator;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Core;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

public class EffectsTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Theory]
	[Trait("Instant", null)]
	[InlineData("TestAttributeSet.Attribute1", 10, 11)]
	[InlineData("TestAttributeSet.Attribute1", -10, 0)]
	[InlineData("TestAttributeSet.Attribute90", 25, 99)]
	[InlineData("TestAttributeSet.Attribute3", 20, 23)]
	[InlineData("TestAttributeSet.Attribute5", -2, 3)]
	[InlineData("Invalid.Attribute", 10, 0)]
	public void Instant_effect_modifies_attribute_base_value(
		string targetAttribute,
		int effectMagnitude,
		int expectedResult)
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Level Up",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(effectMagnitude)))
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(
				new TestEntity(_tagsManager, _cuesManager),
				owner));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, [expectedResult, expectedResult, 0, 0]);
	}

	[Theory]
	[Trait("Instant", null)]
	[InlineData("TestAttributeSet.Attribute1", "TestAttributeSet.Attribute1", 2, 1, 2, 7)]
	[InlineData("TestAttributeSet.Attribute1", "TestAttributeSet.Attribute2", 2, 1, 2, 9)]
	[InlineData("TestAttributeSet.Attribute90", "TestAttributeSet.Attribute5", -1, 5, 2, 82)]
	[InlineData("TestAttributeSet.Attribute3", "TestAttributeSet.Attribute5", 1.5f, 2.2f, 3.5f, 17)]
	[InlineData("Invalid.Attribute", "Invalid.Attribute", 2, 1, 2, 0)]
	[InlineData("TestAttributeSet.Attribute1", "Invalid.Attribute", 2, 1, 2, 1)]
	[InlineData("Invalid.Attribute", "TestAttributeSet.Attribute1", 2, 1, 2, 0)]
	public void Attribute_based_effect_modifies_values_based_on_source_attribute(
		string targetAttribute,
		string backingAttribute,
		float coefficient,
		float preMultiplyAdditiveValue,
		float postMultiplyAdditiveValue,
		int expectedResult)
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Level Up",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.AttributeBased,
						attributeBasedFloat: new AttributeBasedFloat(
							new AttributeCaptureDefinition(backingAttribute, AttributeCaptureSource.Source),
							AttributeCalculationType.BaseValue,
							new ScalableFloat(coefficient),
							new ScalableFloat(preMultiplyAdditiveValue),
							new ScalableFloat(postMultiplyAdditiveValue))))
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, [expectedResult, expectedResult, 0, 0]);
	}

	[Theory]
	[Trait("Instant", null)]
	[InlineData("TestAttributeSet.Attribute1", "TestAttributeSet.Attribute1", 2, 1, 2, 5)]
	[InlineData("TestAttributeSet.Attribute1", "TestAttributeSet.Attribute2", 2, 1, 2, 4)]
	[InlineData("TestAttributeSet.Attribute3", "TestAttributeSet.Attribute5", 1.5f, 2.2f, 3.5f, 4)]
	[InlineData("TestAttributeSet.Attribute90", "TestAttributeSet.Attribute5", -1, 5, 2, 94)]
	[InlineData("Invalid.Attribute", "Invalid.Attribute", 2, 1, 2, 0)]
	[InlineData("TestAttributeSet.Attribute1", "Invalid.Attribute", 2, 1, 2, 1)]
	[InlineData("Invalid.Attribute", "TestAttributeSet.Attribute1", 2, 1, 2, 0)]
	public void Attribute_based_effect_with_curve_modifies_values_based_on_curve_lookup(
		string targetAttribute,
		string backingAttribute,
		float coefficient,
		float preMultiplyAdditiveValue,
		float postMultiplyAdditiveValue,
		int expectedResult)
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Level Up",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.AttributeBased,
						attributeBasedFloat: new AttributeBasedFloat(
							new AttributeCaptureDefinition(backingAttribute, AttributeCaptureSource.Source),
							AttributeCalculationType.BaseValue,
							new ScalableFloat(coefficient),
							new ScalableFloat(preMultiplyAdditiveValue),
							new ScalableFloat(postMultiplyAdditiveValue),
							LookupCurve: new Curve(
								[
									new CurveKey(6, 4),
									new CurveKey(8, 3),
									new CurveKey(14, 1)
								]))))
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, [expectedResult, expectedResult, 0, 0]);
	}

	[Theory]
	[Trait("Instant", null)]
	[InlineData("TestAttributeSet.Attribute1", 4, 5, 4, 25, -0.66f, 8, 42, 42)]
	[InlineData("TestAttributeSet.Attribute2", 8, 10, 2, 30, -0.66f, 10, 99, 99)]
	[InlineData("TestAttributeSet.Attribute3", 20, 23, 0.5f, 34, 1, 68, -10, 0)]
	[InlineData("TestAttributeSet.Attribute90", 90, 99, 0.3f, 99, 0f, 99, 100, 99)]
	[InlineData("Invalid.Attribute", 4, 0, 4, 0, -0.66f, 0, 42, 0)]
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
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Level Up",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(firstEffectFlatMagnitude)))
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, [firstExpectedResult, firstExpectedResult, 0, 0]);

		var effectData2 = new EffectData(
			"Rank Up",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.PercentBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(secondEffectPercentMagnitude)))
			]);

		var effect2 = new Effect(
			effectData2,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect2);

		TestUtils.TestAttribute(target, targetAttribute, [secondExpectedResult, secondExpectedResult, 0, 0]);

		var effectData3 = new EffectData(
			"Rank Down",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.PercentBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(thirdEffectPercentMagnitude)))
			]);

		var effect3 = new Effect(
			effectData3,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect3);

		TestUtils.TestAttribute(target, targetAttribute, [thirdExpectedResult, thirdExpectedResult, 0, 0]);

		var effectData4 = new EffectData(
			"Rank Fix",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.Override,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(fourthEffectOverrideMagnitude)))
			]);

		var effect4 = new Effect(
			effectData4,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect4);

		TestUtils.TestAttribute(target, targetAttribute, [fourthExpectedResult, fourthExpectedResult, 0, 0]);
	}

	[Theory]
	[Trait("Instant", null)]
	[InlineData("TestAttributeSet.Attribute1", 10, 1, 2, 11, 31)]
	[InlineData("TestAttributeSet.Attribute2", 5, 1, 2.5f, 7, 19)]
	[InlineData("TestAttributeSet.Attribute3", 40, 1, 0.2f, 43, 51)]
	[InlineData("TestAttributeSet.Attribute5", 10, 1, -1, 15, 5)]
	[InlineData("TestAttributeSet.Attribute90", 1, 2, 1, 92, 93)]
	[InlineData("Invalid.Attribute", 10, 1, 2, 0, 0)]
	public void Modifiers_of_different_level_effects_apply_different_modifiers(
		string targetAttribute,
		float modifierBaseMagnitude,
		float modifierLevel1Multiplier,
		float modifierLevel2Multiplier,
		int firstExpectedResult,
		int secondExpectedResult)
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Level Up",
			new DurationData(DurationType.Instant),
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
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, [firstExpectedResult, firstExpectedResult, 0, 0]);

		effect.LevelUp();

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, [secondExpectedResult, secondExpectedResult, 0, 0]);
	}

	[Theory]
	[Trait("Override", null)]
	[InlineData("TestAttributeSet.Attribute1", 10, 12, new int[] { 11, 1, 10, 0 }, new int[] { 12, 1, 11, 0 })]
	[InlineData("TestAttributeSet.Attribute2", 5, 3, new int[] { 7, 2, 5, 0 }, new int[] { 3, 2, 1, 0 })]
	[InlineData("TestAttributeSet.Attribute3", 40, 1, new int[] { 43, 3, 40, 0 }, new int[] { 1, 3, -2, 0 })]
	[InlineData("TestAttributeSet.Attribute5", 10, -10, new int[] { 15, 5, 10, 0 }, new int[] { 0, 5, -15, -10 })]
	[InlineData("TestAttributeSet.Attribute90", 1, 15, new int[] { 91, 90, 1, 0 }, new int[] { 15, 90, -75, 0 })]
	[InlineData("TestAttributeSet.Attribute90", 1, -40, new int[] { 91, 90, 1, 0 }, new int[] { 0, 90, -130, -40 })]
	[InlineData("Invalid.Attribute", 10, 12, new int[] { }, new int[] { })]
	public void Override_values_are_applied_temporarily(
		string targetAttribute,
		float flatMagnitude,
		float overrideMagnitude,
		int[] firstExpectedResults,
		int[] secondExpectedResult)
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var flatBonusEffectData = new EffectData(
			"FlatBonus",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(flatMagnitude)))
			]);

		var overrideEffectData = new EffectData(
			"Override effect",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.Override,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(overrideMagnitude)))
			]);

		var effect1 = new Effect(
			flatBonusEffectData,
			new EffectOwnership(owner, owner));

		var effect2 = new Effect(
			overrideEffectData,
			new EffectOwnership(owner, owner));

		target.EffectsManager.ApplyEffect(effect1);

		TestUtils.TestAttribute(target, targetAttribute, firstExpectedResults);

		ActiveEffectHandle? activeEffect2handle = target.EffectsManager.ApplyEffect(effect2);

		TestUtils.TestAttribute(target, targetAttribute, secondExpectedResult);

		target.EffectsManager.UnapplyEffect(activeEffect2handle!);

		TestUtils.TestAttribute(target, targetAttribute, firstExpectedResults);
	}

	[Theory]
	[Trait("Override", null)]
	[InlineData(
		"TestAttributeSet.Attribute1",
		10,
		0,
		11,
		0,
		12,
		0,
		13,
		0,
		new int[] { 10, 1, 9, 0 },
		new int[] { 11, 1, 10, 0 },
		new int[] { 12, 1, 11, 0 },
		new int[] { 13, 1, 12, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute1",
		10,
		1,
		11,
		0,
		12,
		0,
		13,
		0,
		new int[] { 10, 1, 9, 0 },
		new int[] { 10, 1, 9, 0 },
		new int[] { 10, 1, 9, 0 },
		new int[] { 10, 1, 9, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute1",
		10,
		1,
		11,
		0,
		12,
		0,
		13,
		1,
		new int[] { 10, 1, 9, 0 },
		new int[] { 10, 1, 9, 0 },
		new int[] { 10, 1, 9, 0 },
		new int[] { 13, 1, 12, 0 })]
	[InlineData(
		"Invalid.Attribute",
		10,
		1,
		11,
		0,
		12,
		0,
		13,
		1,
		new int[] { },
		new int[] { },
		new int[] { },
		new int[] { })]
	public void Multiple_override_values_are_applied_and_removed_correctly(
		string targetAttribute,
		float overrideMagnitude1,
		int channel1,
		float overrideMagnitude2,
		int channel2,
		float overrideMagnitude3,
		int channel3,
		float overrideMagnitude4,
		int channel4,
		int[] expectedResults1,
		int[] expectedResults2,
		int[] expectedResults3,
		int[] expectedResults4)
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		EffectData effectData1 =
			CreateOverrideEffect("Override effect 1", targetAttribute, overrideMagnitude1, channel1);
		EffectData effectData2 =
			CreateOverrideEffect("Override effect 2", targetAttribute, overrideMagnitude2, channel2);
		EffectData effectData3 =
			CreateOverrideEffect("Override effect 3", targetAttribute, overrideMagnitude3, channel3);
		EffectData effectData4 =
			CreateOverrideEffect("Override effect 4", targetAttribute, overrideMagnitude4, channel4);

		var effect1 = new Effect(effectData1, new EffectOwnership(owner, owner));
		var effect2 = new Effect(effectData2, new EffectOwnership(owner, owner));
		var effect3 = new Effect(effectData3, new EffectOwnership(owner, owner));
		var effect4 = new Effect(effectData4, new EffectOwnership(owner, owner));

		target.EffectsManager.ApplyEffect(effect1);
		TestUtils.TestAttribute(target, targetAttribute, expectedResults1); // 1

		ActiveEffectHandle? activeEffect2Handle1 = target.EffectsManager.ApplyEffect(effect2);
		TestUtils.TestAttribute(target, targetAttribute, expectedResults2); // 1,2

		ActiveEffectHandle? activeEffect3Handle = target.EffectsManager.ApplyEffect(effect3);
		TestUtils.TestAttribute(target, targetAttribute, expectedResults3); // 1,2,3

		ActiveEffectHandle? activeEffect4Handle = target.EffectsManager.ApplyEffect(effect4);
		TestUtils.TestAttribute(target, targetAttribute, expectedResults4); // 1,2,3,4

		ActiveEffectHandle? activeEffect1Handle = target.EffectsManager.ApplyEffect(effect1);

		TestUtils.TestAttribute(target, targetAttribute, expectedResults1); // 1,2,3,4,1

		target.EffectsManager.UnapplyEffect(activeEffect1Handle!);
		TestUtils.TestAttribute(target, targetAttribute, expectedResults4); // 1,2,3,4

		target.EffectsManager.UnapplyEffect(activeEffect2Handle1!);
		TestUtils.TestAttribute(target, targetAttribute, expectedResults4); // 1,3,4

		target.EffectsManager.UnapplyEffect(activeEffect4Handle!);
		TestUtils.TestAttribute(target, targetAttribute, expectedResults3); // 1,3

		ActiveEffectHandle? activeEffect2Handle2 = target.EffectsManager.ApplyEffect(effect2);
		TestUtils.TestAttribute(target, targetAttribute, expectedResults2); // 1,3,2

		target.EffectsManager.UnapplyEffect(activeEffect3Handle!);
		TestUtils.TestAttribute(target, targetAttribute, expectedResults2); // 1,2

		target.EffectsManager.UnapplyEffect(activeEffect2Handle2!);
		TestUtils.TestAttribute(target, targetAttribute, expectedResults1); // 1

		static EffectData CreateOverrideEffect(
			string effectName,
			string targetAttribute,
			float overrideMagnitude,
			int channel)
		{
			return new EffectData(
				effectName,
				new DurationData(DurationType.Infinite),
				[
					new Modifier(
						targetAttribute,
						ModifierOperation.Override,
						new ModifierMagnitude(
							MagnitudeCalculationType.ScalableFloat,
							new ScalableFloat(overrideMagnitude)),
						channel)
				]);
		}
	}

	[Theory]
	[Trait("Duration", null)]
	[InlineData("TestAttributeSet.Attribute1", 10, 1, 2, new int[] { 11, 1, 10, 0 }, new int[] { 21, 1, 20, 0 })]
	[InlineData("TestAttributeSet.Attribute2", 5, 1, 2.5f, new int[] { 7, 2, 5, 0 }, new int[] { 14, 2, 12, 0 })]
	[InlineData("TestAttributeSet.Attribute3", 40, 1, 0.2f, new int[] { 43, 3, 40, 0 }, new int[] { 11, 3, 8, 0 })]
	[InlineData("TestAttributeSet.Attribute5", 10, 1, -1, new int[] { 15, 5, 10, 0 }, new int[] { 0, 5, -10, -5 })]
	[InlineData("TestAttributeSet.Attribute90", 1, 2, 1, new int[] { 92, 90, 2, 0 }, new int[] { 91, 90, 1, 0 })]
	[InlineData("TestAttributeSet.Attribute90", 5, 2, 4, new int[] { 99, 90, 10, 1 }, new int[] { 99, 90, 20, 11 })]
	[InlineData("Invalid.Attribute", 10, 1, 2, new int[] { }, new int[] { })]
	public void Non_snapshot_level_effect_updates_value_on_level_up(
		string targetAttribute,
		float modifierBaseMagnitude,
		float modifierLevel1Multiplier,
		float modifierLevel2Multiplier,
		int[] firstExpectedResults,
		int[] secondExpectedResult)
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Level Up",
			new DurationData(DurationType.Infinite),
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
			snapshotLevel: false);

		var effect = new Effect(
			effectData,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, firstExpectedResults);

		effect.LevelUp();

		TestUtils.TestAttribute(target, targetAttribute, secondExpectedResult);
	}

	[Theory]
	[Trait("Periodic", null)]
	[InlineData("TestAttributeSet.Attribute1", 1, 1, 2, 1, 1, 1, 2, 1, 3, 1, 5)]
	[InlineData("TestAttributeSet.Attribute1", 10, 1, 2, 1, 1, 0.5f, 11, 1.1f, 21, 1.5f, 61)]
	[InlineData("TestAttributeSet.Attribute2", 5, 1, 2, 1, 1, 4, 7, 2.5f, 17, 2.5f, 27)]
	[InlineData("TestAttributeSet.Attribute3", 10, 2, 1, 0.5f, 1, 0.5f, 23, 0.4f, 23, 0.1f, 33)]
	[InlineData("TestAttributeSet.Attribute5", 5, 1, 0.2f, 1, 1, 0.1f, 10, 3.1f, 25, 1.79f, 34)]
	[InlineData("TestAttributeSet.Attribute90", -1, 1, 2, 0.1f, 1, 1, 89, 2f, 69, 100f, 0)]
	[InlineData("Invalid.Attribute", 1, 1, 2, 1, 1, 1, 0, 1, 0, 1, 0)]
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
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Buff",
			new DurationData(DurationType.Infinite),
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
			periodicData: new PeriodicData(
				new ScalableFloat(periodicBaseValue, new Curve(
					[
						new CurveKey(1, periodicLevel1Multiplier),
						new CurveKey(2, periodicLevel2Multiplier),
					])),
				true,
				PeriodInhibitionRemovedPolicy.NeverReset),
			snapshotLevel: false);

		var effect = new Effect(
			effectData,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, [firstExpectedResult, firstExpectedResult, 0, 0]);

		target.EffectsManager.UpdateEffects(firstTimeUpdateDelta);

		TestUtils.TestAttribute(target, targetAttribute, [secondExpectedResult, secondExpectedResult, 0, 0]);

		effect.LevelUp();

		target.EffectsManager.UpdateEffects(secondTimeUpdateDelta);

		TestUtils.TestAttribute(target, targetAttribute, [thirdExpectedResult, thirdExpectedResult, 0, 0]);
	}

	[Theory]
	[Trait("Periodic", null)]
	[InlineData("TestAttributeSet.Attribute1", 0, 1, 1)]
	[InlineData("TestAttributeSet.Attribute1", 1, 0, 1)]
	[InlineData("TestAttributeSet.Attribute1", 1, 1, 0)]
	[InlineData("TestAttributeSet.Attribute1", -1, 1, 1)]
	[InlineData("TestAttributeSet.Attribute1", 1, -1, 1)]
	[InlineData("TestAttributeSet.Attribute1", 1, 1, -1)]
	public void Periodic_effect_with_invalid_period_throws_exception(
		string targetAttribute,
		float periodicBaseValue,
		float periodicLevel1Multiplier,
		float periodicLevel2Multiplier)
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Buff",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(1)))
			],
			periodicData: new PeriodicData(
				new ScalableFloat(periodicBaseValue, new Curve(
					[
						new CurveKey(1, periodicLevel1Multiplier),
						new CurveKey(2, periodicLevel2Multiplier),
					])),
				true,
				PeriodInhibitionRemovedPolicy.NeverReset),
			snapshotLevel: false);

		var effect = new Effect(
			effectData,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		Action act = () =>
		{
			target.EffectsManager.ApplyEffect(effect);
			target.EffectsManager.UpdateEffects(1);
			effect.LevelUp();
		};

		act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*greater than zero*");
	}

	[Theory]
	[Trait("Infinite", null)]
	[InlineData("TestAttributeSet.Attribute1", 10, 32f, 5f, new int[] { 11, 1, 10, 0 })]
	[InlineData("TestAttributeSet.Attribute2", 1, 60f, 0.1f, new int[] { 3, 2, 1, 0 })]
	[InlineData("TestAttributeSet.Attribute3", -10, 15f, 120f, new int[] { 0, 3, -10, -7 })]
	[InlineData("TestAttributeSet.Attribute5", 20, 33f, 999f, new int[] { 25, 5, 20, 0 })]
	[InlineData("TestAttributeSet.Attribute90", 100, 1f, 60f, new int[] { 99, 90, 100, 91 })]
	[InlineData("Invalid.Attribute", 100, 1f, 60f, new int[] { })]
	public void Infinite_effect_modify_attribute_modifier_value(
		string targetAttribute,
		float modifierBaseMagnitude,
		float simulatedFPS,
		float totalSimulatedTime,
		int[] expectedResults)
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Buff",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(modifierBaseMagnitude)))
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, expectedResults);

		for (var i = 0f; i < simulatedFPS * totalSimulatedTime; i++)
		{
			target.EffectsManager.UpdateEffects(1f / simulatedFPS);
		}

		TestUtils.TestAttribute(target, targetAttribute, expectedResults);
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
	[InlineData(
		"Invalid.Attribute",
		4,
		new int[] { },
		4f,
		new int[] { },
		-0.66f,
		new int[] { })]
	public void Infinite_effect_of_different_operations_update_modifier_value_accordingly(
		string targetAttribute,
		float firstEffectModifierBaseMagnitude,
		int[] firstExpectedResults,
		float secondEffectModifierBaseMagnitude,
		int[] secondExpectedResults,
		float thirdEffectModifierBaseMagnitude,
		int[] thirdExpectedResults)
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Buff1",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(firstEffectModifierBaseMagnitude)))
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, firstExpectedResults);

		var effectData2 = new EffectData(
			"Buff2",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.PercentBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(secondEffectModifierBaseMagnitude)))
			]);

		var effect2 = new Effect(
			effectData2,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect2);

		TestUtils.TestAttribute(target, targetAttribute, secondExpectedResults);

		var effectData3 = new EffectData(
			"Buff3",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.PercentBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(thirdEffectModifierBaseMagnitude)))
			]);

		var effect3 = new Effect(
			effectData3,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect3);

		TestUtils.TestAttribute(target, targetAttribute, thirdExpectedResults);
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
	[InlineData(
		"Invalid.Attribute",
		ModifierOperation.FlatBonus,
		4,
		0,
		ModifierOperation.PercentBonus,
		4f,
		0,
		ModifierOperation.PercentBonus,
		-0.66f,
		1,
		new int[] { })]
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
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Buff",
			new DurationData(DurationType.Infinite),
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
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, expectedResults);
	}

	[Theory]
	[Trait("Duration", null)]
	[InlineData(10f, "TestAttributeSet.Attribute1", 10, 32, 5f, new int[] { 11, 1, 10, 0 }, 5f, 1)]
	[InlineData(60f, "TestAttributeSet.Attribute2", 10, 1, 59f, new int[] { 12, 2, 10, 0 }, 2f, 2)]
	[InlineData(0.1f, "TestAttributeSet.Attribute3", 10, 60, 0.05f, new int[] { 13, 3, 10, 0 }, 0.05f, 3)]
	[InlineData(1f, "TestAttributeSet.Attribute5", 10, 300, 0.5f, new int[] { 15, 5, 10, 0 }, 300f, 5)]
	[InlineData(600f, "TestAttributeSet.Attribute90", 10, 64, 1f, new int[] { 99, 90, 10, 1 }, 599f, 90)]
	[InlineData(10f, "Invalid.Attribute", 10, 32, 5f, new int[] { }, 5f, 0)]
	public void Duration_effect_modifies_attribute_modifier_value_and_expire_after_duration_time(
		float baseDuration,
		string targetAttribute,
		float modifierMagnitude,
		float simulatedFPS,
		float unexpiringPeriodOfTime,
		int[] firstExpectedResults,
		float expiringPeriodOfTime,
		int lastExpectedResult)
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Buff",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(baseDuration))),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(modifierMagnitude)))
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, firstExpectedResults);

		for (var i = 0; i < simulatedFPS * unexpiringPeriodOfTime; i++)
		{
			target.EffectsManager.UpdateEffects(1f / simulatedFPS);
		}

		TestUtils.TestAttribute(target, targetAttribute, firstExpectedResults);

		for (var i = 0; i < simulatedFPS * expiringPeriodOfTime; i++)
		{
			target.EffectsManager.UpdateEffects(1 / simulatedFPS);
		}

		TestUtils.TestAttribute(target, targetAttribute, [lastExpectedResult, lastExpectedResult, 0, 0]);
	}

	[Theory]
	[Trait("Periodic", null)]
	[InlineData("TestAttributeSet.Attribute1", 10, 32, 1f, 11, 1f, 21, 5f, 71)]
	[InlineData("TestAttributeSet.Attribute2", 1, 64, 0.5f, 3, 1.66f, 6, 20f, 46)]
	[InlineData("TestAttributeSet.Attribute3", 3, 128, 10f, 6, 9f, 6, 60f, 24)]
	[InlineData("TestAttributeSet.Attribute5", 90, 1, 1f, 95, 9f, 99, 10f, 99)]
	[InlineData("TestAttributeSet.Attribute90", -5, 32, 0.01f, 85, 0.05f, 60, 10f, 0)]
	[InlineData("Invalid.Attribute", 10, 32, 1f, 0, 1f, 0, 5f, 0)]
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
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Buff",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(modifierMagnitude)))
			],
			periodicData: new PeriodicData(new ScalableFloat(period), true, PeriodInhibitionRemovedPolicy.NeverReset));

		var effect = new Effect(
			effectData,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, [firstExpectedResult, firstExpectedResult, 0, 0]);

		target.EffectsManager.UpdateEffects(firstPeriodOfTime);

		TestUtils.TestAttribute(target, targetAttribute, [secondExpectedResult, secondExpectedResult, 0, 0]);

		for (var i = 0; i < simulatedFPS * secondPeriodOfTime; i++)
		{
			target.EffectsManager.UpdateEffects(1 / simulatedFPS);
		}

		TestUtils.TestAttribute(target, targetAttribute, [thirdExpectedResult, thirdExpectedResult, 0, 0]);
	}

	[Theory]
	[Trait("Periodic", null)]
	[InlineData("TestAttributeSet.Attribute1", 10, 1f, 11, 1f, 21)]
	[InlineData("TestAttributeSet.Attribute2", 1, 0.1f, 3, 1f, 13)]
	[InlineData("TestAttributeSet.Attribute3", 10, 60f, 13, 59f, 13)]
	[InlineData("TestAttributeSet.Attribute5", 5, 1f, 10, 15.9f, 85)]
	[InlineData("TestAttributeSet.Attribute90", -1, 0.5f, 89, 60f, 0)]
	[InlineData("Invalid.Attribute", 10, 1f, 0, 1f, 0)]
	public void Snapshot_periodic_effect_modifies_base_attribute_with_same_value_even_after_level_up(
		string targetAttribute,
		float modifierMagnitude,
		float period,
		int firstExpectedResult,
		float simulatedPeriodOfTime,
		int secondExpectedResult)
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Buff",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(modifierMagnitude)))
			],
			periodicData: new PeriodicData(new ScalableFloat(period), true, PeriodInhibitionRemovedPolicy.NeverReset));

		var effect = new Effect(
			effectData,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, [firstExpectedResult, firstExpectedResult, 0, 0]);

		effect.LevelUp();

		target.EffectsManager.UpdateEffects(simulatedPeriodOfTime);

		TestUtils.TestAttribute(target, targetAttribute, [secondExpectedResult, secondExpectedResult, 0, 0]);
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
	[InlineData(
		"Invalid.Attribute",
		"Invalid.Attribute",
		2f,
		1f,
		2f,
		1f,
		0,
		1f,
		0,
		2f,
		0,
		1f,
		0,
		0,
		0,
		1f,
		0)]
	[InlineData(
		"TestAttributeSet.Attribute1",
		"Invalid.Attribute",
		2f,
		1f,
		2f,
		1f,
		1,
		1f,
		1,
		2f,
		0,
		1f,
		1,
		0,
		1,
		1f,
		1)]
	[InlineData(
		"Invalid.Attribute",
		"TestAttributeSet.Attribute2",
		2f,
		1f,
		2f,
		1f,
		0,
		1f,
		0,
		2f,
		4,
		1f,
		0,
		6,
		0,
		1f,
		0)]
	public void Non_snapshot_periodic_effect_with_attribute_based_magnitude_should_update_when_attribute_updates(
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
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Buff",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.AttributeBased,
						attributeBasedFloat: new AttributeBasedFloat(
							new AttributeCaptureDefinition(backingAttribute, AttributeCaptureSource.Source, false),
							AttributeCalculationType.BaseValue,
							new ScalableFloat(coefficient),
							new ScalableFloat(preMultiplyAdditiveValue),
							new ScalableFloat(postMultiplyAdditiveValue))))
			],
			periodicData: new PeriodicData(new ScalableFloat(period), true, PeriodInhibitionRemovedPolicy.NeverReset));

		var effect = new Effect(
			effectData,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, [firstTargetExpectedResult, firstTargetExpectedResult, 0, 0]);

		target.EffectsManager.UpdateEffects(firstSimulatedPeriodOfTime);

		TestUtils.TestAttribute(target, targetAttribute, [secondTargetExpectedResult, secondTargetExpectedResult, 0, 0]);

		var effectData2 = new EffectData(
			"Buff2",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					backingAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(ownerBonusEffectMagnitude)))
			]);

		var effect2 = new Effect(
			effectData2,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		owner.EffectsManager.ApplyEffect(effect2);

		TestUtils.TestAttribute(owner, backingAttribute, [firstOwnerExpectedResult, firstOwnerExpectedResult, 0, 0]);

		target.EffectsManager.UpdateEffects(secondSimulatedPeriodOfTime);

		TestUtils.TestAttribute(target, targetAttribute, [thirdTargetExpectedResult, thirdTargetExpectedResult, 0, 0]);

		owner.EffectsManager.ApplyEffect(effect2);

		TestUtils.TestAttribute(owner, backingAttribute, [secondOwnerExpectedResult, secondOwnerExpectedResult, 0, 0]);

		TestUtils.TestAttribute(target, targetAttribute, [fourthTargetExpectedResult, fourthTargetExpectedResult, 0, 0]);

		target.EffectsManager.UpdateEffects(thirdSimulatedPeriodOfTime);

		TestUtils.TestAttribute(target, targetAttribute, [fifthTargetExpectedResult, fifthTargetExpectedResult, 0, 0]);
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
	[InlineData(
		"Invalid.Attribute",
		"Invalid.Attribute",
		2f,
		1f,
		2f,
		new int[] { },
		2f,
		0,
		new int[] { },
		0,
		new int[] { })]
	[InlineData(
		"TestAttributeSet.Attribute1",
		"Invalid.Attribute",
		2f,
		1f,
		2f,
		new int[] { 1, 1, 0, 0 },
		2f,
		0,
		new int[] { 1, 1, 0, 0 },
		0,
		new int[] { 1, 1, 0, 0 })]
	[InlineData(
		"Invalid.Attribute",
		"TestAttributeSet.Attribute2",
		2f,
		1f,
		2f,
		new int[] { },
		2f,
		4,
		new int[] { },
		6,
		new int[] { })]
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
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Buff",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.AttributeBased,
						attributeBasedFloat: new AttributeBasedFloat(
							new AttributeCaptureDefinition(backingAttribute, AttributeCaptureSource.Source, false),
							AttributeCalculationType.BaseValue,
							new ScalableFloat(coefficient),
							new ScalableFloat(preMultiplyAdditiveValue),
							new ScalableFloat(postMultiplyAdditiveValue))))
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, firstTargetExpectedResults);

		var effectData2 = new EffectData(
			"Buff2",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					backingAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(ownerBonusEffectMagnitude)))
			]);

		var effect2 = new Effect(
			effectData2,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		owner.EffectsManager.ApplyEffect(effect2);

		TestUtils.TestAttribute(owner, backingAttribute, [firstOwnerExpectedResult, firstOwnerExpectedResult, 0, 0]);

		TestUtils.TestAttribute(target, targetAttribute, secondTargetExpectedResults);

		owner.EffectsManager.ApplyEffect(effect2);

		TestUtils.TestAttribute(owner, backingAttribute, [secondOwnerExpectedResult, secondOwnerExpectedResult, 0, 0]);

		TestUtils.TestAttribute(target, targetAttribute, thirdTargetExpectedResults);
	}

	[Theory]
	[Trait("Periodic", null)]
	[InlineData(3f, "TestAttributeSet.Attribute1", 10f, 1f, 32, 11, 1f, 21, 5f, 41)]
	[InlineData(5f, "TestAttributeSet.Attribute2", 2f, 0.5f, 64, 4, 4.9f, 22, 5f, 24)]
	[InlineData(60f, "TestAttributeSet.Attribute3", 3f, 30f, 128, 6, 29f, 6, 120f, 12)]
	[InlineData(3f, "TestAttributeSet.Attribute5", 5f, 10f, 16, 10, 1f, 10, 5f, 10)]
	[InlineData(0.5f, "TestAttributeSet.Attribute90", -10f, 0.1f, 32, 80, 1f, 30, 1f, 30)]
	[InlineData(3f, "Invalid.Attribute", 10f, 1f, 32, 0, 1f, 0, 5f, 0)]
	public void Periodic_effect_modifies_base_attribute_value_and_expire_after_duration_time(
		float duration,
		string targetAttribute,
		float modifierMagnitude,
		float period,
		float simulatedFPS,
		int firstExpectedResult,
		float firstPeriodOfTime,
		int secondExpectedResult,
		float secondPeriodOfTime,
		int thirdExpectedResult)
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Buff",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(duration))),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(modifierMagnitude)))
			],
			periodicData: new PeriodicData(new ScalableFloat(period), true, PeriodInhibitionRemovedPolicy.NeverReset));

		var effect = new Effect(
			effectData,
			new EffectOwnership(owner, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, [firstExpectedResult, firstExpectedResult, 0, 0]);

		target.EffectsManager.UpdateEffects(firstPeriodOfTime);

		TestUtils.TestAttribute(target, targetAttribute, [secondExpectedResult, secondExpectedResult, 0, 0]);

		for (var i = 0; i < simulatedFPS * secondPeriodOfTime; i++)
		{
			target.EffectsManager.UpdateEffects(1 / simulatedFPS);
		}

		TestUtils.TestAttribute(target, targetAttribute, [thirdExpectedResult, thirdExpectedResult, 0, 0]);
	}

	[Theory]
	[Trait("Stackable", null)]
	[InlineData(
		new int[] { 11, 1, 10, 0 },
		new int[] { 21, 1, 20, 0 },
		1,
		new object[] { new int[] { 1, 1, 0 } },
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
		new object[] { new int[] { 1, 1, 0 } },
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
		new object[] { new int[] { 4, 1, 0 } },
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
	[InlineData(
		new int[] { 4, 1, 3, 0 },
		new int[] { 4, 1, 3, 0 },
		1,
		new object[] { new int[] { 3, 1, 0 } },
		1,
		new object[] { new int[] { 3, 1, 0 } },
		"TestAttributeSet.Attribute1",
		1,
		3,
		3,
		StackPolicy.AggregateByTarget,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.DenyApplication,
		StackExpirationPolicy.ClearEntireStack,
		StackOwnerDenialPolicy.AlwaysAllow,
		StackOwnerOverridePolicy.Override,
		StackOwnerOverrideStackCountPolicy.IncreaseStacks)]
	[InlineData(
		new int[] { 4, 1, 3, 0 },
		new int[] { 4, 1, 3, 0 },
		1,
		new object[] { new int[] { 3, 1, 0 } },
		1,
		new object[] { new int[] { 3, 1, 1 } },
		"TestAttributeSet.Attribute1",
		1,
		3,
		3,
		StackPolicy.AggregateByTarget,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.AllowApplication,
		StackExpirationPolicy.ClearEntireStack,
		StackOwnerDenialPolicy.AlwaysAllow,
		StackOwnerOverridePolicy.Override,
		StackOwnerOverrideStackCountPolicy.IncreaseStacks)]
	[InlineData(
		new int[] { },
		new int[] { },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 2, 1, 0 } },
		"Invalid.Attribute",
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
		new int[] { },
		new int[] { },
		1,
		new object[] { new int[] { 3, 1, 0 } },
		1,
		new object[] { new int[] { 3, 1, 1 } },
		"Invalid.Attribute",
		1,
		3,
		3,
		StackPolicy.AggregateByTarget,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.AllowApplication,
		StackExpirationPolicy.ClearEntireStack,
		StackOwnerDenialPolicy.AlwaysAllow,
		StackOwnerOverridePolicy.Override,
		StackOwnerOverrideStackCountPolicy.IncreaseStacks)]
	public void Stackable_effect_from_two_different_sources_gives_expected_stack_data(
		int[] firstExpectedResults,
		int[] secondExpectedResults,
		int firstExpectedStackDataCount,
		object[] firstExpectedStackData,
		int secondExpectedStackDataCount,
		object[] secondExpectedStackData,
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
		var owner1 = new TestEntity(_tagsManager, _cuesManager);
		var owner2 = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Buff",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(modifierMagnitude)))
			],
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
				executeOnSuccessfulApplication));

		var effect = new Effect(
			effectData,
			new EffectOwnership(owner1, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, firstExpectedResults);

		TestUtils.TestStackData(
			target.EffectsManager.GetEffectInfo(effectData),
			firstExpectedStackDataCount,
			firstExpectedStackData,
			owner1,
			owner2);

		var effect2 = new Effect(
			effectData,
			new EffectOwnership(owner2, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect2);

		TestUtils.TestAttribute(target, targetAttribute, secondExpectedResults);

		TestUtils.TestStackData(
			target.EffectsManager.GetEffectInfo(effectData),
			secondExpectedStackDataCount,
			secondExpectedStackData,
			owner1,
			owner2);
	}

	[Theory]
	[Trait("Stackable", null)]

	// Effects with stack level policy Segregate don't stack.
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

	// Effects with stack level policy Aggregate stack and recalculate.
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

	// Effect denies stack from different owner.
	[InlineData(
		new int[] { 6, 1, 5, 0 },
		new int[] { 6, 1, 5, 0 },
		new int[] { 31, 1, 30, 0 },
		new int[] { 31, 1, 30, 0 },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 2, 3, 0 } },
		1,
		new object[] { new int[] { 2, 3, 0 } },
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
		StackOwnerDenialPolicy.DenyIfDifferent,
		null,
		null,
		LevelComparison.None,
		LevelComparison.Lower | LevelComparison.Equal | LevelComparison.Higher,
		StackLevelOverrideStackCountPolicy.IncreaseStacks)]

	// Effect denies stacks from lower levels.
	[InlineData(
		new int[] { 6, 1, 5, 0 },
		new int[] { 21, 1, 20, 0 },
		new int[] { 46, 1, 45, 0 },
		new int[] { 46, 1, 45, 0 },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 2, 2, 1 } },
		1,
		new object[] { new int[] { 3, 3, 0 } },
		1,
		new object[] { new int[] { 3, 3, 0 } },
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
		LevelComparison.Lower,
		LevelComparison.Equal | LevelComparison.Higher,
		StackLevelOverrideStackCountPolicy.IncreaseStacks)]
	[InlineData(
		new int[] { },
		new int[] { },
		new int[] { },
		new int[] { },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		2,
		new object[] { new int[] { 1, 1, 0 }, new int[] { 1, 2, 1 } },
		3,
		new object[] { new int[] { 1, 1, 0 }, new int[] { 1, 2, 1 }, new int[] { 1, 3, 0 } },
		3,
		new object[] { new int[] { 1, 1, 0 }, new int[] { 2, 2, 1 }, new int[] { 1, 3, 0 } },
		"Invalid.Attribute",
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
		var owner1 = new TestEntity(_tagsManager, _cuesManager);
		var owner2 = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Buff",
			new DurationData(DurationType.Infinite),
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
				executeOnSuccessfulApplication));

		var effect = new Effect(
			effectData,
			new EffectOwnership(owner1, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, firstExpectedResults);

		TestUtils.TestStackData(
			target.EffectsManager.GetEffectInfo(effectData),
			firstExpectedStackDataCount,
			firstExpectedStackData,
			owner1,
			owner2);

		var effect2 = new Effect(
			effectData,
			new EffectOwnership(owner2, new TestEntity(_tagsManager, _cuesManager)),
			2);

		target.EffectsManager.ApplyEffect(effect2);

		TestUtils.TestAttribute(target, targetAttribute, secondExpectedResults);

		TestUtils.TestStackData(
			target.EffectsManager.GetEffectInfo(effectData),
			secondExpectedStackDataCount,
			secondExpectedStackData,
			owner1,
			owner2);

		var effect3 = new Effect(
			effectData,
			new EffectOwnership(owner1, new TestEntity(_tagsManager, _cuesManager)),
			3);

		target.EffectsManager.ApplyEffect(effect3);

		TestUtils.TestAttribute(target, targetAttribute, thirdExpectedResults);

		TestUtils.TestStackData(
			target.EffectsManager.GetEffectInfo(effectData),
			thirdExpectedStackDataCount,
			thirdExpectedStackData,
			owner1,
			owner2);

		target.EffectsManager.ApplyEffect(effect2);

		TestUtils.TestAttribute(target, targetAttribute, fourthExpectedResults);

		TestUtils.TestStackData(
			target.EffectsManager.GetEffectInfo(effectData),
			fourthExpectedStackDataCount,
			fourthExpectedStackData,
			owner1,
			owner2);
	}

	[Theory]
	[Trait("Stackable", null)]

	// Effect updates overrides stacks with new owner.
	[InlineData(
		new int[] { 3, 1, 2, 0 },
		new int[] { 5, 1, 4, 0 },
		new int[] { 6, 1, 5, 0 },
		new int[] { 3, 1, 2, 0 },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 2, 1, 0 } },
		1,
		new object[] { new int[] { 1, 1, 1 } },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		"TestAttributeSet.Attribute1",
		"TestAttributeSet.Attribute2",
		3,
		AttributeCaptureSource.Source,
		AttributeCalculationType.CurrentValue,
		new float[] { 1, 0, 0 },
		5,
		1,
		StackPolicy.AggregateByTarget,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.DenyApplication,
		StackExpirationPolicy.ClearEntireStack,
		StackOwnerDenialPolicy.AlwaysAllow,
		StackOwnerOverridePolicy.Override,
		StackOwnerOverrideStackCountPolicy.ResetStacks)]
	[InlineData(
		new int[] { },
		new int[] { },
		new int[] { },
		new int[] { },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 2, 1, 0 } },
		1,
		new object[] { new int[] { 1, 1, 1 } },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		"Invalid.Attribute",
		"Invalid.Attribute",
		3,
		AttributeCaptureSource.Source,
		AttributeCalculationType.CurrentValue,
		new float[] { 1, 0, 0 },
		5,
		1,
		StackPolicy.AggregateByTarget,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.DenyApplication,
		StackExpirationPolicy.ClearEntireStack,
		StackOwnerDenialPolicy.AlwaysAllow,
		StackOwnerOverridePolicy.Override,
		StackOwnerOverrideStackCountPolicy.ResetStacks)]
	[InlineData(
		new int[] { 1, 1, 0, 0 },
		new int[] { 1, 1, 0, 0 },
		new int[] { 1, 1, 0, 0 },
		new int[] { 1, 1, 0, 0 },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 2, 1, 0 } },
		1,
		new object[] { new int[] { 1, 1, 1 } },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		"TestAttributeSet.Attribute1",
		"Invalid.Attribute",
		3,
		AttributeCaptureSource.Source,
		AttributeCalculationType.CurrentValue,
		new float[] { 1, 0, 0 },
		5,
		1,
		StackPolicy.AggregateByTarget,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.DenyApplication,
		StackExpirationPolicy.ClearEntireStack,
		StackOwnerDenialPolicy.AlwaysAllow,
		StackOwnerOverridePolicy.Override,
		StackOwnerOverrideStackCountPolicy.ResetStacks)]
	[InlineData(
		new int[] { },
		new int[] { },
		new int[] { },
		new int[] { },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 2, 1, 0 } },
		1,
		new object[] { new int[] { 1, 1, 1 } },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		"Invalid.Attribute",
		"TestAttributeSet.Attribute2",
		3,
		AttributeCaptureSource.Source,
		AttributeCalculationType.CurrentValue,
		new float[] { 1, 0, 0 },
		5,
		1,
		StackPolicy.AggregateByTarget,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.DenyApplication,
		StackExpirationPolicy.ClearEntireStack,
		StackOwnerDenialPolicy.AlwaysAllow,
		StackOwnerOverridePolicy.Override,
		StackOwnerOverrideStackCountPolicy.ResetStacks)]
	public void Stackable_attribute_based_effect_with_different_sources_gives_expected_stack_data(
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
		string backingAttribute,
		int backingAttributeMagnitudeChange,
		AttributeCaptureSource attributeCaptureSource,
		AttributeCalculationType attributeCalculationType,
		float[] attributeBasedFloatFormulaParameters,
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
		var owner1 = new TestEntity(_tagsManager, _cuesManager);
		var owner2 = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		// Setup Owner2 by changing his attributes.
		var levelUpEffectData = new EffectData(
			"Level Up",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					backingAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(backingAttributeMagnitudeChange)))
			]);

		var levelUpEffect = new Effect(
			levelUpEffectData,
			new EffectOwnership(owner2, new TestEntity(_tagsManager, _cuesManager)));

		owner2.EffectsManager.ApplyEffect(levelUpEffect);

		// Setup test effect.
		var effectData = new EffectData(
			"Buff",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.AttributeBased,
						attributeBasedFloat: new AttributeBasedFloat(
							new AttributeCaptureDefinition(
								backingAttribute,
								attributeCaptureSource,
								false),
							attributeCalculationType,
							new ScalableFloat(attributeBasedFloatFormulaParameters[0]),
							new ScalableFloat(attributeBasedFloatFormulaParameters[1]),
							new ScalableFloat(attributeBasedFloatFormulaParameters[2]))))
			],
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
				executeOnSuccessfulApplication));

		var effect = new Effect(
			effectData,
			new EffectOwnership(owner1, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, firstExpectedResults);

		TestUtils.TestStackData(
			target.EffectsManager.GetEffectInfo(effectData),
			firstExpectedStackDataCount,
			firstExpectedStackData,
			owner1,
			owner2);

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, secondExpectedResults);

		TestUtils.TestStackData(
			target.EffectsManager.GetEffectInfo(effectData),
			secondExpectedStackDataCount,
			secondExpectedStackData,
			owner1,
			owner2);

		var effect2 = new Effect(
			effectData,
			new EffectOwnership(owner2, new TestEntity(_tagsManager, _cuesManager)));

		target.EffectsManager.ApplyEffect(effect2);

		TestUtils.TestAttribute(target, targetAttribute, thirdExpectedResults);

		TestUtils.TestStackData(
			target.EffectsManager.GetEffectInfo(effectData),
			thirdExpectedStackDataCount,
			thirdExpectedStackData,
			owner1,
			owner2);

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, fourthExpectedResults);

		TestUtils.TestStackData(
			target.EffectsManager.GetEffectInfo(effectData),
			fourthExpectedStackDataCount,
			fourthExpectedStackData,
			owner1,
			owner2);
	}

	[Theory]
	[Trait("Stackable", null)]

	[InlineData(
		new int[] { 4, 4, 0, 0 },
		new int[] { 64, 64, 0, 0 },
		new int[] { 67, 67, 0, 0 },
		new int[] { 99, 99, 0, 0 },
		1,
		new object[] { new int[] { 3, 1, 0 } },
		0,
		new object[] { },
		1,
		new object[] { new int[] { 3, 1, 0 } },
		0,
		new object[] { },
		10f,
		"TestAttributeSet.Attribute1",
		1,
		1f,
		40f,
		40f,
		3,
		3,
		StackPolicy.AggregateBySource,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.DenyApplication,
		StackExpirationPolicy.RemoveSingleStackAndRefreshDuration,
		null,
		null,
		null,
		null,
		null,
		null,
		StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication,
		StackApplicationResetPeriodPolicy.ResetOnSuccessfulApplication,
		false)]
	[InlineData(
		new int[] { 4, 4, 0, 0 },
		new int[] { 40, 40, 0, 0 },
		new int[] { 40, 40, 0, 0 },
		new int[] { 99, 99, 0, 0 },
		1,
		new object[] { new int[] { 3, 1, 0 } },
		1,
		new object[] { new int[] { 2, 1, 0 } },
		1,
		new object[] { new int[] { 3, 1, 0 } },
		0,
		new object[] { },
		10f,
		"TestAttributeSet.Attribute1",
		1,
		1f,
		13.54f,
		43.54f,
		3,
		3,
		StackPolicy.AggregateBySource,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.DenyApplication,
		StackExpirationPolicy.RemoveSingleStackAndRefreshDuration,
		null,
		null,
		null,
		null,
		null,
		null,
		StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication,
		StackApplicationResetPeriodPolicy.ResetOnSuccessfulApplication,
		false)]
	[InlineData(
		new int[] { 4, 4, 0, 0 },
		new int[] { 13, 13, 0, 0 },
		new int[] { 13, 13, 0, 0 },
		new int[] { 34, 34, 0, 0 },
		1,
		new object[] { new int[] { 3, 1, 0 } },
		1,
		new object[] { new int[] { 3, 1, 0 } },
		1,
		new object[] { new int[] { 3, 1, 0 } },
		0,
		new object[] { },
		10f,
		"TestAttributeSet.Attribute1",
		1,
		1f,
		3.54f,
		13.54f,
		3,
		3,
		StackPolicy.AggregateBySource,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.DenyApplication,
		StackExpirationPolicy.ClearEntireStack,
		null,
		null,
		null,
		null,
		null,
		null,
		StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication,
		StackApplicationResetPeriodPolicy.ResetOnSuccessfulApplication,
		false)]
	[InlineData(
		new int[] { 2, 2, 0, 0 },
		new int[] { 11, 11, 0, 0 },
		new int[] { 11, 11, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 2, 1, 0 } },
		0,
		new object[] { },
		10f,
		"TestAttributeSet.Attribute1",
		1,
		1f,
		9f,
		30f,
		3,
		1,
		StackPolicy.AggregateBySource,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.DenyApplication,
		StackExpirationPolicy.RemoveSingleStackAndRefreshDuration,
		null,
		null,
		null,
		null,
		null,
		null,
		StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication,
		StackApplicationResetPeriodPolicy.ResetOnSuccessfulApplication,
		false)]
	[InlineData(
		new int[] { 2, 2, 0, 0 },
		new int[] { 11, 11, 0, 0 },
		new int[] { 11, 11, 0, 0 },
		new int[] { 23, 23, 0, 0 },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 2, 1, 0 } },
		0,
		new object[] { },
		10f,
		"TestAttributeSet.Attribute1",
		1,
		1f,
		9f,
		30f,
		3,
		1,
		StackPolicy.AggregateBySource,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.DenyApplication,
		StackExpirationPolicy.RemoveSingleStackAndRefreshDuration,
		null,
		null,
		null,
		null,
		null,
		null,
		StackApplicationRefreshPolicy.NeverRefresh,
		StackApplicationResetPeriodPolicy.ResetOnSuccessfulApplication,
		false)]
	[InlineData(
		new int[] { 2, 2, 0, 0 },
		new int[] { 11, 11, 0, 0 },
		new int[] { 11, 11, 0, 0 },
		new int[] { 21, 21, 0, 0 },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		0,
		new object[] { },
		10f,
		"TestAttributeSet.Attribute1",
		1,
		1f,
		9.5f,
		20f,
		1,
		1,
		StackPolicy.AggregateBySource,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.AllowApplication,
		StackExpirationPolicy.RemoveSingleStackAndRefreshDuration,
		null,
		null,
		null,
		null,
		null,
		null,
		StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication,
		StackApplicationResetPeriodPolicy.ResetOnSuccessfulApplication,
		false)]
	[InlineData(
		new int[] { 2, 2, 0, 0 },
		new int[] { 11, 11, 0, 0 },
		new int[] { 11, 11, 0, 0 },
		new int[] { 11, 11, 0, 0 },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		0,
		new object[] { },
		10f,
		"TestAttributeSet.Attribute1",
		1,
		1f,
		9.5f,
		20f,
		1,
		1,
		StackPolicy.AggregateBySource,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.AllowApplication,
		StackExpirationPolicy.RemoveSingleStackAndRefreshDuration,
		null,
		null,
		null,
		null,
		null,
		null,
		StackApplicationRefreshPolicy.NeverRefresh,
		StackApplicationResetPeriodPolicy.ResetOnSuccessfulApplication,
		false)]
	[InlineData(
		new int[] { 2, 2, 0, 0 },
		new int[] { 11, 11, 0, 0 },
		new int[] { 11, 11, 0, 0 },
		new int[] { 12, 12, 0, 0 },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		0,
		new object[] { },
		10f,
		"TestAttributeSet.Attribute1",
		1,
		1f,
		9.5f,
		20f,
		1,
		1,
		StackPolicy.AggregateBySource,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.AllowApplication,
		StackExpirationPolicy.RemoveSingleStackAndRefreshDuration,
		null,
		null,
		null,
		null,
		null,
		null,
		StackApplicationRefreshPolicy.NeverRefresh,
		StackApplicationResetPeriodPolicy.NeverReset,
		false)]
	[InlineData(
		new int[] { 2, 2, 0, 0 },
		new int[] { 3, 3, 0, 0 },
		new int[] { 5, 5, 0, 0 },
		new int[] { 7, 7, 0, 0 },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 2, 1, 0 } },
		1,
		new object[] { new int[] { 2, 1, 0 } },
		10f,
		"TestAttributeSet.Attribute1",
		1f,
		1f,
		1.3f,
		0.8f,
		3,
		1,
		StackPolicy.AggregateBySource,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.DenyApplication,
		StackExpirationPolicy.RemoveSingleStackAndRefreshDuration,
		null,
		null,
		null,
		null,
		null,
		null,
		StackApplicationRefreshPolicy.NeverRefresh,
		StackApplicationResetPeriodPolicy.NeverReset,
		true)]
	[InlineData(
		new int[] { },
		new int[] { },
		new int[] { },
		new int[] { },
		1,
		new object[] { new int[] { 3, 1, 0 } },
		0,
		new object[] { },
		1,
		new object[] { new int[] { 3, 1, 0 } },
		0,
		new object[] { },
		10f,
		"Invalid.Attribute",
		1,
		1f,
		40f,
		40f,
		3,
		3,
		StackPolicy.AggregateBySource,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.DenyApplication,
		StackExpirationPolicy.RemoveSingleStackAndRefreshDuration,
		null,
		null,
		null,
		null,
		null,
		null,
		StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication,
		StackApplicationResetPeriodPolicy.ResetOnSuccessfulApplication,
		false)]
	[InlineData(
		new int[] { },
		new int[] { },
		new int[] { },
		new int[] { },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		1,
		new object[] { new int[] { 2, 1, 0 } },
		1,
		new object[] { new int[] { 2, 1, 0 } },
		10f,
		"Invalid.Attribute",
		1f,
		1f,
		1.3f,
		0.8f,
		3,
		1,
		StackPolicy.AggregateBySource,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.DenyApplication,
		StackExpirationPolicy.RemoveSingleStackAndRefreshDuration,
		null,
		null,
		null,
		null,
		null,
		null,
		StackApplicationRefreshPolicy.NeverRefresh,
		StackApplicationResetPeriodPolicy.NeverReset,
		true)]
	public void Stackable_periodic_effect_with_duration_updates_correctly(
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
		float effectDuration,
		string targetAttribute,
		int modifierMagnitude,
		float effectPeriod,
		float firstPeriodOfTime,
		float secondPeriodOfTime,
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
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Buff",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(effectDuration))),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(modifierMagnitude)))
			],
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
			new PeriodicData(new ScalableFloat(effectPeriod), true, PeriodInhibitionRemovedPolicy.NeverReset));

		var effect = new Effect(
			effectData,
			new EffectOwnership(owner, owner));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, firstExpectedResults);

		TestUtils.TestStackData(
			target.EffectsManager.GetEffectInfo(effectData),
			firstExpectedStackDataCount,
			firstExpectedStackData,
			owner,
			target);

		target.EffectsManager.UpdateEffects(firstPeriodOfTime);

		TestUtils.TestAttribute(target, targetAttribute, secondExpectedResults);

		TestUtils.TestStackData(
			target.EffectsManager.GetEffectInfo(effectData),
			secondExpectedStackDataCount,
			secondExpectedStackData,
			owner,
			target);

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, thirdExpectedResults);

		TestUtils.TestStackData(
			target.EffectsManager.GetEffectInfo(effectData),
			thirdExpectedStackDataCount,
			thirdExpectedStackData,
			owner,
			target);

		target.EffectsManager.UpdateEffects(secondPeriodOfTime);

		TestUtils.TestAttribute(target, targetAttribute, fourthExpectedResults);

		TestUtils.TestStackData(
			target.EffectsManager.GetEffectInfo(effectData),
			fourthExpectedStackDataCount,
			fourthExpectedStackData,
			owner,
			target);
	}

	[Theory]
	[Trait("Stackable", null)]
	[InlineData(
		new int[] { 31, 1, 30, 0 },
		new int[] { 21, 1, 20, 0 },
		new int[] { 11, 1, 10, 0 },
		new int[] { 1, 1, 0, 0 },
		1,
		new object[] { new int[] { 3, 1, 0 } },
		1,
		new object[] { new int[] { 2, 1, 0 } },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		0,
		new object[] { },
		"TestAttributeSet.Attribute1",
		10,
		3,
		3,
		false,
		StackPolicy.AggregateBySource,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.DenyApplication,
		StackExpirationPolicy.RemoveSingleStackAndRefreshDuration,
		null,
		null,
		null,
		null,
		null,
		null,
		null)]
	[InlineData(
		new int[] { 31, 1, 30, 0 },
		new int[] { 1, 1, 0, 0 },
		new int[] { 1, 1, 0, 0 },
		new int[] { 1, 1, 0, 0 },
		1,
		new object[] { new int[] { 3, 1, 0 } },
		0,
		new object[] { },
		0,
		new object[] { },
		0,
		new object[] { },
		"TestAttributeSet.Attribute1",
		10,
		3,
		3,
		false,
		StackPolicy.AggregateBySource,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.DenyApplication,
		StackExpirationPolicy.ClearEntireStack,
		null,
		null,
		null,
		null,
		null,
		null,
		null)]
	[InlineData(
		new int[] { 31, 1, 30, 0 },
		new int[] { 1, 1, 0, 0 },
		new int[] { 1, 1, 0, 0 },
		new int[] { 1, 1, 0, 0 },
		1,
		new object[] { new int[] { 3, 1, 0 } },
		0,
		new object[] { },
		0,
		new object[] { },
		0,
		new object[] { },
		"TestAttributeSet.Attribute1",
		10,
		3,
		3,
		true,
		StackPolicy.AggregateBySource,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.DenyApplication,
		StackExpirationPolicy.RemoveSingleStackAndRefreshDuration,
		null,
		null,
		null,
		null,
		null,
		null,
		null)]
	[InlineData(
		new int[] { },
		new int[] { },
		new int[] { },
		new int[] { },
		1,
		new object[] { new int[] { 3, 1, 0 } },
		1,
		new object[] { new int[] { 2, 1, 0 } },
		1,
		new object[] { new int[] { 1, 1, 0 } },
		0,
		new object[] { },
		"Invalid.Attribute",
		10,
		3,
		3,
		false,
		StackPolicy.AggregateBySource,
		StackLevelPolicy.SegregateLevels,
		StackMagnitudePolicy.Sum,
		StackOverflowPolicy.DenyApplication,
		StackExpirationPolicy.RemoveSingleStackAndRefreshDuration,
		null,
		null,
		null,
		null,
		null,
		null,
		null)]
	public void Infinite_stackable_effect_unnaplies_correctly(
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
		int modifierMagnitude,
		int stackLimit,
		int initialStack,
		bool forceUnapply,
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
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Buff",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(modifierMagnitude)))
			],
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
				executeOnSuccessfulApplication));

		var effect = new Effect(
			effectData,
			new EffectOwnership(owner, owner));

		ActiveEffectHandle? effectHandle = target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, firstExpectedResults);

		TestUtils.TestStackData(
			target.EffectsManager.GetEffectInfo(effectData),
			firstExpectedStackDataCount,
			firstExpectedStackData,
			owner,
			target);

		target.EffectsManager.UnapplyEffect(effectHandle!, forceUnapply);

		TestUtils.TestAttribute(target, targetAttribute, secondExpectedResults);

		TestUtils.TestStackData(
			target.EffectsManager.GetEffectInfo(effectData),
			secondExpectedStackDataCount,
			secondExpectedStackData,
			owner,
			target);

		target.EffectsManager.UnapplyEffect(effectHandle!, forceUnapply);

		TestUtils.TestAttribute(target, targetAttribute, thirdExpectedResults);

		TestUtils.TestStackData(
			target.EffectsManager.GetEffectInfo(effectData),
			thirdExpectedStackDataCount,
			thirdExpectedStackData,
			owner,
			target);

		target.EffectsManager.UnapplyEffect(effectHandle!, forceUnapply);

		TestUtils.TestAttribute(target, targetAttribute, fourthExpectedResults);

		TestUtils.TestStackData(
			target.EffectsManager.GetEffectInfo(effectData),
			fourthExpectedStackDataCount,
			fourthExpectedStackData,
			owner,
			target);
	}

	[Theory]
	[Trait("Duration", null)]
	[InlineData(
		"TestAttributeSet.Attribute1",
		80f,
		new int[] { 81, 1, 80, 0 },
		new int[] { 99, 1, 160, 62 })]
	[InlineData(
		"TestAttributeSet.Attribute2",
		10f,
		new int[] { 12, 2, 10, 0 },
		new int[] { 22, 2, 20, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute3",
		-10f,
		new int[] { 0, 3, -10, -7 },
		new int[] { 0, 3, -20, -17 })]
	[InlineData(
		"TestAttributeSet.Attribute5",
		200f,
		new int[] { 99, 5, 200, 106 },
		new int[] { 99, 5, 400, 306 })]
	[InlineData(
		"TestAttributeSet.Attribute90",
		-45,
		new int[] { 45, 90, -45, 0 },
		new int[] { 0, 90, -90, 0 })]
	[InlineData(
		"Invalid.Attribute",
		80f,
		new int[] { },
		new int[] { })]
	public void Unapply_duration_effect_restores_original_attribute_values(
		string targetAttribute,
		float effectMagnitude,
		int[] firstExpectedResults,
		int[] secondExpectedResults)
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Buff",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(effectMagnitude)))
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(owner, owner));

		var originalCurrentValue = 0;
		var originalBaseValue = 0;
		var originalModifier = 0;
		var originalOverflow = 0;

		if (target.Attributes.WithKeys()
			.Any(x => x.FullKey.Equals(targetAttribute, StringComparison.OrdinalIgnoreCase)))
		{
			originalCurrentValue = target.Attributes[targetAttribute].CurrentValue;
			originalBaseValue = target.Attributes[targetAttribute].BaseValue;
			originalModifier = target.Attributes[targetAttribute].Modifier;
			originalOverflow = target.Attributes[targetAttribute].Overflow;
		}

		ActiveEffectHandle? activeEffectHandle1 = target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, firstExpectedResults);

		ActiveEffectHandle? activeEffectHandle2 = target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, secondExpectedResults);

		target.EffectsManager.UnapplyEffect(activeEffectHandle1!);

		TestUtils.TestAttribute(target, targetAttribute, firstExpectedResults);

		target.EffectsManager.UnapplyEffect(activeEffectHandle2!);

		TestUtils.TestAttribute(
			target,
			targetAttribute,
			[originalCurrentValue, originalBaseValue, originalModifier, originalOverflow]);
	}

	[Theory]
	[Trait("Instant", null)]
	[InlineData("TestAttributeSet.Attribute1", 10, 11)]
	[InlineData("TestAttributeSet.Attribute1", -10, 0)]
	[InlineData("TestAttributeSet.Attribute90", 25, 99)]
	[InlineData("TestAttributeSet.Attribute3", 20, 23)]
	[InlineData("TestAttributeSet.Attribute5", -2, 3)]
	[InlineData("Invalid.Attribute", 10, 0)]
	public void Set_by_caller_magnitude_modifies_attribute_accordingly(
		string targetAttribute,
		int effectMagnitude,
		int expectedResult)
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var setByCallerTag = Tag.RequestTag(_tagsManager, "tag");

		var effectData = new EffectData(
			"Level Up",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.SetByCaller,
						setByCallerFloat: new SetByCallerFloat(setByCallerTag)))
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(
				new TestEntity(_tagsManager, _cuesManager),
				owner));

		effect.SetSetByCallerMagnitude(setByCallerTag, effectMagnitude);

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, [expectedResult, expectedResult, 0, 0]);
	}

	[Fact]
	[Trait("Instant", null)]
	public void Simple_effects_with_null_ownership_applies_modifiers_successfully()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Level Up",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10)))
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(null, null));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 11, 0, 0]);
	}

	[Fact]
	[Trait("Instant", null)]
	public void Attribute_based_modifier_with_null_ownership_does_not_apply_changes()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Level Up",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.AttributeBased,
						attributeBasedFloat: new AttributeBasedFloat(
							new AttributeCaptureDefinition(
								"TestAttributeSet.Attribute5",
								AttributeCaptureSource.Source),
							AttributeCalculationType.CurrentValue,
							new ScalableFloat(1),
							new ScalableFloat(0),
							new ScalableFloat(0)))),
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(null, null));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
	}

	[Fact]
	[Trait("Duration", null)]
	public void Attribute_based_duration_uses_source_attribute_to_set_expiration()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		// Owner TestAttributeSet.Attribute5 base is 5. Duration = 5 * 2 = 10 seconds.
		var durationMagnitude = new ModifierMagnitude(
			MagnitudeCalculationType.AttributeBased,
			attributeBasedFloat: new AttributeBasedFloat(
				new AttributeCaptureDefinition("TestAttributeSet.Attribute5", AttributeCaptureSource.Source),
				AttributeCalculationType.BaseValue,
				new ScalableFloat(2f), // coefficient
				new ScalableFloat(0f), // pre-add
				new ScalableFloat(0f))); // post-add

		var effectData = new EffectData(
			"AB Duration",
			new DurationData(DurationType.HasDuration, durationMagnitude),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10)))
			]);

		var effect = new Effect(effectData, new EffectOwnership(owner, owner));

		// Apply and verify modifier is applied
		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);

		// Before expiration
		target.EffectsManager.UpdateEffects(9.9f);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);

		// Cross expiration (10s total)
		target.EffectsManager.UpdateEffects(10f - 9.9f);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
	}

	[Fact]
	[Trait("Duration", null)]
	public void Set_by_caller_duration_sets_expiration_from_runtime_value()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var durationTag = Tag.RequestTag(_tagsManager, "simple.tag");

		var durationMagnitude = new ModifierMagnitude(
			MagnitudeCalculationType.SetByCaller,
			setByCallerFloat: new SetByCallerFloat(durationTag));

		var effectData = new EffectData(
			"SBC Duration",
			new DurationData(DurationType.HasDuration, durationMagnitude),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10)))
			]);

		var effect = new Effect(effectData, new EffectOwnership(owner, owner));

		// Set duration to 0.5 seconds at runtime
		effect.SetSetByCallerMagnitude(durationTag, 0.5f);

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);

		// Before expiration
		target.EffectsManager.UpdateEffects(0.49f);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);

		// Cross expiration
		target.EffectsManager.UpdateEffects(0.5f - 0.49f);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
	}

	[Fact]
	[Trait("Duration", null)]
	public void Duration_uses_custom_calculator_to_set_expiration()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var durationMagnitude = new ModifierMagnitude(
			MagnitudeCalculationType.CustomCalculatorClass,
			customCalculationBasedFloat: new CustomCalculationBasedFloat(
				new DurationFromSourceAttributeCalculator(),
				new ScalableFloat(1f), // coefficient
				new ScalableFloat(0f), // pre-add
				new ScalableFloat(0f))); // post-add

		var effectData = new EffectData(
			"CC Duration",
			new DurationData(DurationType.HasDuration, durationMagnitude),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10)))
			]);

		var effect = new Effect(effectData, new EffectOwnership(owner, owner));

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);

		// Before expiration (1.0s)
		target.EffectsManager.UpdateEffects(0.99f);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);

		// Cross expiration
		target.EffectsManager.UpdateEffects(1f - 0.99f);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
	}

	[Fact]
	[Trait("Duration", null)]
	public void Attribute_based_duration_updates_duration_with_attribute_changes()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		// Owner TestAttributeSet.Attribute5 base is 5. Duration = 5 * 2 = 10 seconds.
		var durationMagnitude = new ModifierMagnitude(
			MagnitudeCalculationType.AttributeBased,
			attributeBasedFloat: new AttributeBasedFloat(
				new AttributeCaptureDefinition("TestAttributeSet.Attribute5", AttributeCaptureSource.Source, false),
				AttributeCalculationType.BaseValue,
				new ScalableFloat(2f), // coefficient
				new ScalableFloat(0f), // pre-add
				new ScalableFloat(0f))); // post-add

		var effectData = new EffectData(
			"AB Duration",
			new DurationData(DurationType.HasDuration, durationMagnitude),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10)))
			]);

		var effect = new Effect(effectData, new EffectOwnership(owner, owner));

		var buffEffectData = new EffectData(
				"Buff Attribute5",
				new DurationData(DurationType.Instant),
				[
					new Modifier(
						"TestAttributeSet.Attribute5",
						ModifierOperation.FlatBonus,
						new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(5)))
				]);

		var buffEffect = new Effect(buffEffectData, new EffectOwnership(owner, owner));

		// Apply and verify modifier is applied
		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);

		// Before expiration
		target.EffectsManager.UpdateEffects(9.9f);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);

		owner.EffectsManager.ApplyEffect(buffEffect);

		// Cross expiration (10s total)
		target.EffectsManager.UpdateEffects(10f - 9.9f);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);

		// Before expiration
		target.EffectsManager.UpdateEffects(10f);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
	}

	[Fact]
	[Trait("Duration", null)]
	public void Attribute_based_duration_finishes_with_negative_attribute_changes()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		// Owner TestAttributeSet.Attribute5 base is 5. Duration = 5 * 2 = 10 seconds.
		var durationMagnitude = new ModifierMagnitude(
			MagnitudeCalculationType.AttributeBased,
			attributeBasedFloat: new AttributeBasedFloat(
				new AttributeCaptureDefinition("TestAttributeSet.Attribute5", AttributeCaptureSource.Source, false),
				AttributeCalculationType.BaseValue,
				new ScalableFloat(2f), // coefficient
				new ScalableFloat(0f), // pre-add
				new ScalableFloat(0f))); // post-add

		var effectData = new EffectData(
			"AB Duration",
			new DurationData(DurationType.HasDuration, durationMagnitude),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10)))
			]);

		var effect = new Effect(effectData, new EffectOwnership(owner, owner));

		var buffEffectData = new EffectData(
				"Buff Attribute5",
				new DurationData(DurationType.Instant),
				[
					new Modifier(
						"TestAttributeSet.Attribute5",
						ModifierOperation.FlatBonus,
						new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-3)))
				]);

		var debuffEffect = new Effect(buffEffectData, new EffectOwnership(owner, owner));

		// Apply and verify modifier is applied
		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);

		// Before expiration
		target.EffectsManager.UpdateEffects(9.0f);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);

		owner.EffectsManager.ApplyEffect(debuffEffect);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
	}

	[Fact]
	[Trait("Periodic", null)]
	public void Set_by_caller_magnitude_updates_periodic_application_value()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var setByCallerTag = Tag.RequestTag(_tagsManager, "tag");

		var effectData = new EffectData(
			"Level Up",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.SetByCaller,
						setByCallerFloat: new SetByCallerFloat(setByCallerTag, false)))
			],
			periodicData: new PeriodicData(new ScalableFloat(1f), true, PeriodInhibitionRemovedPolicy.NeverReset),
			snapshotLevel: false);

		var effect = new Effect(
			effectData,
			new EffectOwnership(
				new TestEntity(_tagsManager, _cuesManager),
				owner));

		effect.SetSetByCallerMagnitude(setByCallerTag, 1);

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [2, 2, 0, 0]);

		effect.SetSetByCallerMagnitude(setByCallerTag, 2);

		target.EffectsManager.UpdateEffects(1f);

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [4, 4, 0, 0]);
	}

	private sealed class DurationFromSourceAttributeCalculator : CustomModifierMagnitudeCalculator
	{
		private readonly AttributeCaptureDefinition _sourceAttr;

		public DurationFromSourceAttributeCalculator()
		{
			// Use owner's Attribute2 (base 2), duration = captured * 0.5 => 1.0 second
			_sourceAttr = new AttributeCaptureDefinition(
				"TestAttributeSet.Attribute2",
				AttributeCaptureSource.Source,
				Snapshot: false);
			AttributesToCapture.Add(_sourceAttr);
		}

		public override float CalculateBaseMagnitude(Effect effect, IForgeEntity target)
		{
			var value = CaptureAttributeMagnitude(_sourceAttr, effect, target);
			return value * 0.5f; // 2 * 0.5 = 1.0
		}
	}
}
