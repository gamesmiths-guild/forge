// Copyright © 2025 Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayCues;
using Gamesmiths.Forge.GameplayEffects;
using Gamesmiths.Forge.GameplayEffects.Calculator;
using Gamesmiths.Forge.GameplayEffects.Duration;
using Gamesmiths.Forge.GameplayEffects.Magnitudes;
using Gamesmiths.Forge.GameplayEffects.Modifiers;
using Gamesmiths.Forge.GameplayTags;
using Gamesmiths.Forge.Tests.GameplayCues;
using Gamesmiths.Forge.Tests.GameplayTags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.GamepleyEffects;

public class CustomCalculatorGameplayEffectsTests(
	GameplayTagsManagerFixture tagsManagerFixture,
	GameplayCuesManagerFixture cuesManagerFixture)
		: IClassFixture<GameplayTagsManagerFixture>, IClassFixture<GameplayCuesManagerFixture>
{
	private readonly GameplayTagsManager _gameplayTagsManager = tagsManagerFixture.GameplayTagsManager;
	private readonly GameplayCuesManager _gameplayCuesManager = cuesManagerFixture.GameplayCuesManager;

	[Theory]
	[Trait("Instant", null)]
	[InlineData("TestAttributeSet.Attribute1", "TestAttributeSet.Attribute1", 1, 1, 0, 0, 2)]
	[InlineData("TestAttributeSet.Attribute2", "TestAttributeSet.Attribute2", 3, 1, 0, 0, 10)]
	[InlineData("TestAttributeSet.Attribute3", "TestAttributeSet.Attribute2", 2, 2, 1, 1, 14)]
	[InlineData("TestAttributeSet.Attribute5", "TestAttributeSet.Attribute3", 2, 1, 0, -3, 11)]
	[InlineData("TestAttributeSet.Attribute90", "TestAttributeSet.Attribute90", 0.5f, -1, 0, 0, 81)]
	public void Custom_calculator_class_magnitude_modifies_attribute_accordingly(
		string targetAttribute,
		string customMagnitudeCalculatorAttribute,
		float customMagnitudeCalculatorExpoent,
		float coefficient,
		float preMultiplyAdditiveValue,
		float postMultiplyAdditiveValue,
		int expectedResult)
	{
		var owner = new TestEntity(_gameplayTagsManager, _gameplayCuesManager);
		var target = new TestEntity(_gameplayTagsManager, _gameplayCuesManager);

		var customCalculatorClass = new CustomMagnitudeCalculator(
			customMagnitudeCalculatorAttribute,
			AttributeCaptureSource.Source,
			customMagnitudeCalculatorExpoent);

		var effectData = new GameplayEffectData(
			"Level Up",
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.CustomCalculatorClass,
						customCalculationBasedFloat: new CustomCalculationBasedFloat(
							customCalculatorClass,
							new ScalableFloat(coefficient),
							new ScalableFloat(preMultiplyAdditiveValue),
							new ScalableFloat(postMultiplyAdditiveValue))))
			],
			new DurationData(DurationType.Instant),
			null,
			null);

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(
				new TestEntity(_gameplayTagsManager, _gameplayCuesManager),
				owner));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, [expectedResult, expectedResult, 0, 0]);
	}

	[Theory]
	[Trait("Instant", null)]
	[InlineData("TestAttributeSet.Attribute1", "TestAttributeSet.Attribute1", 1, 1, 0, 0, 5)]
	[InlineData("TestAttributeSet.Attribute2", "TestAttributeSet.Attribute2", 3, 1, 0, 0, 4)]
	[InlineData("TestAttributeSet.Attribute3", "TestAttributeSet.Attribute2", 2, 2, 1, 1, 4)]
	[InlineData("TestAttributeSet.Attribute5", "TestAttributeSet.Attribute3", 2, 1, 0, -3, 8)]
	[InlineData("TestAttributeSet.Attribute90", "TestAttributeSet.Attribute90", 0.5f, -1, 0, 0, 94)]
	public void Custom_calculator_class_magnitude_with_curve_modifies_attribute_accordingly(
		string targetAttribute,
		string customMagnitudeCalculatorAttribute,
		float customMagnitudeCalculatorExpoent,
		float coefficient,
		float preMultiplyAdditiveValue,
		float postMultiplyAdditiveValue,
		int expectedResult)
	{
		var owner = new TestEntity(_gameplayTagsManager, _gameplayCuesManager);
		var target = new TestEntity(_gameplayTagsManager, _gameplayCuesManager);

		var customCalculatorClass = new CustomMagnitudeCalculator(
			customMagnitudeCalculatorAttribute,
			AttributeCaptureSource.Source,
			customMagnitudeCalculatorExpoent);

		var effectData = new GameplayEffectData(
			"Level Up",
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.CustomCalculatorClass,
						customCalculationBasedFloat: new CustomCalculationBasedFloat(
							customCalculatorClass,
							new ScalableFloat(coefficient),
							new ScalableFloat(preMultiplyAdditiveValue),
							new ScalableFloat(postMultiplyAdditiveValue),
							new Curve(
								[
									new CurveKey(1, 4),
									new CurveKey(6, 3),
									new CurveKey(8, 2),
									new CurveKey(11, 1),
								]))))
			],
			new DurationData(DurationType.Instant),
			null,
			null);

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(
				new TestEntity(_gameplayTagsManager, _gameplayCuesManager),
				owner));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, [expectedResult, expectedResult, 0, 0]);
	}

	[Theory]
	[Trait("Non snapshot", null)]
	[InlineData(
		"TestAttributeSet.Attribute1",
		"TestAttributeSet.Attribute1",
		AttributeCaptureSource.Source,
		1,
		1,
		0,
		0,
		1,
		AttributeCaptureSource.Source,
		new int[] { 2, 1, 1, 0 },
		new int[] { 3, 1, 2, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute1",
		"TestAttributeSet.Attribute90",
		AttributeCaptureSource.Source,
		1,
		1,
		0,
		0,
		-10,
		AttributeCaptureSource.Source,
		new int[] { 91, 1, 90, 0 },
		new int[] { 81, 1, 80, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute5",
		"TestAttributeSet.Attribute1",
		AttributeCaptureSource.Source,
		2,
		0.5f,
		0,
		0,
		3,
		AttributeCaptureSource.Source,
		new int[] { 5, 5, 0, 0 },
		new int[] { 13, 5, 8, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute1",
		"TestAttributeSet.Attribute1",
		AttributeCaptureSource.Target,
		1,
		1,
		0,
		0,
		1,
		AttributeCaptureSource.Target,
		new int[] { 2, 1, 1, 0 },
		new int[] { 4, 1, 3, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute1",
		"TestAttributeSet.Attribute1",
		AttributeCaptureSource.Target,
		1,
		1,
		0,
		0,
		1,
		AttributeCaptureSource.Source,
		new int[] { 2, 1, 1, 0 },
		new int[] { 2, 1, 1, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute1",
		"TestAttributeSet.Attribute1",
		AttributeCaptureSource.Source,
		1,
		1,
		0,
		0,
		1,
		AttributeCaptureSource.Target,
		new int[] { 2, 1, 1, 0 },
		new int[] { 3, 1, 2, 0 })]
	public void Custom_calculator_class_non_snapshot_modifies_attribute_accordingly(
		string targetAttribute,
		string customMagnitudeCalculatorAttribute,
		AttributeCaptureSource captureSource,
		float customMagnitudeCalculatorExpoent,
		float coefficient,
		float preMultiplyAdditiveValue,
		float postMultiplyAdditiveValue,
		float effect2Magnitude,
		AttributeCaptureSource applyEffect2To,
		int[] expectedResults1,
		int[] expectedResults2)
	{
		var owner = new TestEntity(_gameplayTagsManager, _gameplayCuesManager);
		var target = new TestEntity(_gameplayTagsManager, _gameplayCuesManager);

		var customCalculatorClass = new CustomMagnitudeCalculator(
			customMagnitudeCalculatorAttribute,
			captureSource,
			customMagnitudeCalculatorExpoent);

		var effectData = new GameplayEffectData(
			"Test Effect",
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.CustomCalculatorClass,
						customCalculationBasedFloat: new CustomCalculationBasedFloat(
							customCalculatorClass,
							new ScalableFloat(coefficient),
							new ScalableFloat(preMultiplyAdditiveValue),
							new ScalableFloat(postMultiplyAdditiveValue))))
			],
			new DurationData(DurationType.Infinite),
			null,
			null);

		var effectData2 = new GameplayEffectData(
			"Backing Attribute Effect",
			[
				new Modifier(
					customMagnitudeCalculatorAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(effect2Magnitude)))
			],
			new DurationData(DurationType.Infinite),
			null,
			null);

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(
				owner,
				owner));

		var effect2 = new GameplayEffect(
			effectData2,
			new GameplayEffectOwnership(
				owner,
				owner));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, expectedResults1);

		TestEntity effect2Target = applyEffect2To == AttributeCaptureSource.Target ? target : owner;

		ActiveGameplayEffectHandle? handler = effect2Target.EffectsManager.ApplyEffect(effect2);

		TestUtils.TestAttribute(target, targetAttribute, expectedResults2);

		Debug.Assert(handler is not null, "effectHandler should never be null here.");
		effect2Target.EffectsManager.UnapplyEffect(handler);

		TestUtils.TestAttribute(target, targetAttribute, expectedResults1);
	}

	private class CustomMagnitudeCalculator : CustomModifierMagnitudeCalculator
	{
		private readonly float _expoent;

		public AttributeCaptureDefinition Attribute1 { get; }

		public CustomMagnitudeCalculator(StringKey attribute, AttributeCaptureSource captureSource, float expoent)
		{
			Attribute1 = new AttributeCaptureDefinition(attribute, captureSource, false);

			AttributesToCapture.Add(Attribute1);

			_expoent = expoent;
		}

		public override float CalculateBaseMagnitude(GameplayEffect effect, IForgeEntity target)
		{
			return (float)Math.Pow(CaptureAttributeMagnitude(Attribute1, effect, target), _expoent);
		}
	}
}
