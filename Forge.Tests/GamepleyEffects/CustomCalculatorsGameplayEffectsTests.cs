// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayCues;
using Gamesmiths.Forge.GameplayEffects;
using Gamesmiths.Forge.GameplayEffects.Calculator;
using Gamesmiths.Forge.GameplayEffects.Duration;
using Gamesmiths.Forge.GameplayEffects.Magnitudes;
using Gamesmiths.Forge.GameplayEffects.Modifiers;
using Gamesmiths.Forge.GameplayTags;
using Gamesmiths.Forge.Tests.Core;
using Gamesmiths.Forge.Tests.GameplayCues;
using Gamesmiths.Forge.Tests.GameplayTags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.GamepleyEffects;

public class CustomCalculatorsGameplayEffectsTests(
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
	[InlineData("Invalid.Attribute", "Invalid.Attribute", 1, 1, 0, 0, 0)]
	[InlineData("TestAttributeSet.Attribute1", "Invalid.Attribute", 1, 1, 0, 0, 1)]
	[InlineData("Invalid.Attribute", "TestAttributeSet.Attribute1", 1, 1, 0, 0, 0)]
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
	[InlineData("Invalid.Attribute", "Invalid.Attribute", 1, 1, 0, 0, 0)]
	[InlineData("TestAttributeSet.Attribute1", "Invalid.Attribute", 1, 1, 0, 0, 5)]
	[InlineData("Invalid.Attribute", "TestAttributeSet.Attribute1", 1, 1, 0, 0, 0)]
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
	[InlineData(
		"Invalid.Attribute",
		"Invalid.Attribute",
		AttributeCaptureSource.Source,
		1,
		1,
		0,
		0,
		1,
		AttributeCaptureSource.Source,
		new int[] { },
		new int[] { })]
	[InlineData(
		"TestAttributeSet.Attribute1",
		"Invalid.Attribute",
		AttributeCaptureSource.Source,
		1,
		1,
		0,
		0,
		1,
		AttributeCaptureSource.Source,
		new int[] { 1, 1, 0, 0 },
		new int[] { 1, 1, 0, 0 })]
	[InlineData(
		"Invalid.Attribute",
		"TestAttributeSet.Attribute1",
		AttributeCaptureSource.Source,
		1,
		1,
		0,
		0,
		1,
		AttributeCaptureSource.Source,
		new int[] { },
		new int[] { })]
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

		ActiveGameplayEffectHandle? effectHandler = effect2Target.EffectsManager.ApplyEffect(effect2);

		TestUtils.TestAttribute(target, targetAttribute, expectedResults2);

		Debug.Assert(effectHandler is not null, "effectHandler should never be null here");
		effect2Target.EffectsManager.UnapplyEffect(effectHandler);

		TestUtils.TestAttribute(target, targetAttribute, expectedResults1);
	}

	[Fact]
	[Trait("Execution", null)]
	public void Custom_executions_modifies_attribute_accordingly()
	{
		var owner = new TestEntity(_gameplayTagsManager, _gameplayCuesManager);
		var target = new TestEntity(_gameplayTagsManager, _gameplayCuesManager);

		var customCalculatorClass = new CustomTestExecutionClass();

		var effectData = new GameplayEffectData(
			"Test Effect",
			[],
			new DurationData(DurationType.Instant),
			null,
			null,
			executions:
			[
				customCalculatorClass
			]);

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(
				owner,
				owner));

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(owner, "TestAttributeSet.Attribute90", [89, 89, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [16, 16, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [10, 10, 0, 0]);

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(owner, "TestAttributeSet.Attribute90", [88, 88, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [31, 31, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [18, 18, 0, 0]);

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(owner, "TestAttributeSet.Attribute90", [87, 87, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [46, 46, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [26, 26, 0, 0]);
	}

	[Fact]
	[Trait("Execution", null)]
	public void Custom_executions_modifies_update_with_non_snapshot_attributes()
	{
		var owner = new TestEntity(_gameplayTagsManager, _gameplayCuesManager);
		var target = new TestEntity(_gameplayTagsManager, _gameplayCuesManager);

		var customCalculatorClass = new CustomTestExecutionClass();

		var effectData = new GameplayEffectData(
			"Test Effect",
			[],
			new DurationData(DurationType.Infinite),
			null,
			null,
			executions:
			[
				customCalculatorClass
			]);

		var effectData2 = new GameplayEffectData(
			"Backing Attribute Effect",
			[
				new Modifier(
					"TestAttributeSet.Attribute3",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(1)))
			],
			new DurationData(DurationType.Infinite),
			null,
			null);

		var effectData3 = new GameplayEffectData(
			"Backing Attribute Effect",
			[
				new Modifier(
						"TestAttributeSet.Attribute5",
						ModifierOperation.FlatBonus,
						new ModifierMagnitude(
							MagnitudeCalculationType.ScalableFloat,
							new ScalableFloat(2)))
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

		var effect3 = new GameplayEffect(
			effectData3,
			new GameplayEffectOwnership(
				owner,
				owner));

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(owner, "TestAttributeSet.Attribute90", [89, 90, -1, 0]);

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [16, 1, 15, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [10, 2, 8, 0]);

		ActiveGameplayEffectHandle? effectHandler1 = owner.EffectsManager.ApplyEffect(effect2);

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [21, 1, 20, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [11, 2, 9, 0]);

		ActiveGameplayEffectHandle? effectHandler2 = owner.EffectsManager.ApplyEffect(effect3);

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [29, 1, 28, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [13, 2, 11, 0]);

		Debug.Assert(effectHandler2 is not null, "effectHandler2 should never be null here");
		owner.EffectsManager.UnapplyEffect(effectHandler2);

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [21, 1, 20, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [11, 2, 9, 0]);

		Debug.Assert(effectHandler1 is not null, "effectHandler1 should never be null here");
		owner.EffectsManager.UnapplyEffect(effectHandler1);

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [16, 1, 15, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [10, 2, 8, 0]);
	}

	[Fact]
	[Trait("Execution", "Invalid attributes")]
	public void Custom_execution_without_valid_attributes_applies_with_no_attribute_changes()
	{
		var owner = new NoAttributesEntity(_gameplayTagsManager, _gameplayCuesManager);
		var target = new NoAttributesEntity(_gameplayTagsManager, _gameplayCuesManager);

		var customCalculatorClass = new CustomTestExecutionClass();

		var effectData = new GameplayEffectData(
			"Test Effect",
			[],
			new DurationData(DurationType.Instant),
			null,
			null,
			executions:
			[
				customCalculatorClass
			]);

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(
				owner,
				owner));

		target.EffectsManager.ApplyEffect(effect);
		target.EffectsManager.ApplyEffect(effect);
		target.EffectsManager.ApplyEffect(effect);

		target.Attributes.Should().BeEmpty();
	}

	[Fact]
	[Trait("Execution", "Invalid attributes")]
	public void Custom_execution_without_valid_owner_attributes_applies_with_no_attribute_changes()
	{
		var owner = new NoAttributesEntity(_gameplayTagsManager, _gameplayCuesManager);
		var target = new TestEntity(_gameplayTagsManager, _gameplayCuesManager);

		var customCalculatorClass = new CustomTestExecutionClass();

		var effectData = new GameplayEffectData(
			"Test Effect",
			[],
			new DurationData(DurationType.Instant),
			null,
			null,
			executions:
			[
				customCalculatorClass
			]);

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(
				owner,
				owner));

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [2, 2, 0, 0]);

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [2, 2, 0, 0]);

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [2, 2, 0, 0]);
	}

	[Fact]
	[Trait("Execution", "Invalid attributes")]
	public void Custom_execution_without_valid_target_attributes_applies_with_no_attribute_changes()
	{
		var owner = new TestEntity(_gameplayTagsManager, _gameplayCuesManager);
		var target = new NoAttributesEntity(_gameplayTagsManager, _gameplayCuesManager);

		var customCalculatorClass = new CustomTestExecutionClass();

		var effectData = new GameplayEffectData(
			"Test Effect",
			[],
			new DurationData(DurationType.Instant),
			null,
			null,
			executions:
			[
				customCalculatorClass
			]);

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(
				owner,
				owner));

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(owner, "TestAttributeSet.Attribute90", [90, 90, 0, 0]);

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(owner, "TestAttributeSet.Attribute90", [90, 90, 0, 0]);

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(owner, "TestAttributeSet.Attribute90", [90, 90, 0, 0]);
	}

	private sealed class CustomMagnitudeCalculator : CustomModifierMagnitudeCalculator
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

	private class CustomTestExecutionClass : Execution
	{
		public AttributeCaptureDefinition SourceAttribute1 { get; }

		public AttributeCaptureDefinition SourceAttribute2 { get; }

		public AttributeCaptureDefinition SourceAttribute3 { get; }

		public AttributeCaptureDefinition TargetAttribute1 { get; }

		public AttributeCaptureDefinition TargetAttribute2 { get; }

		public CustomTestExecutionClass()
		{
			SourceAttribute1 = new AttributeCaptureDefinition(
				"TestAttributeSet.Attribute3",
				AttributeCaptureSource.Source,
				false);
			SourceAttribute2 = new AttributeCaptureDefinition(
				"TestAttributeSet.Attribute5",
				AttributeCaptureSource.Source,
				false);
			SourceAttribute3 = new AttributeCaptureDefinition(
				"TestAttributeSet.Attribute90",
				AttributeCaptureSource.Source,
				true);
			TargetAttribute1 = new AttributeCaptureDefinition(
				"TestAttributeSet.Attribute1",
				AttributeCaptureSource.Target,
				false);
			TargetAttribute2 = new AttributeCaptureDefinition(
				"TestAttributeSet.Attribute2",
				AttributeCaptureSource.Target,
				false);

			AttributesToCapture.Add(SourceAttribute1);
			AttributesToCapture.Add(SourceAttribute2);
			AttributesToCapture.Add(SourceAttribute3);
			AttributesToCapture.Add(TargetAttribute1);
			AttributesToCapture.Add(TargetAttribute2);
		}

		public override ModifierEvaluatedData[] CalculateExecution(GameplayEffect effect, IForgeEntity target)
		{
			var result = new ModifierEvaluatedData[3];

			var sourceAttribute1value = CaptureAttributeMagnitude(SourceAttribute1, effect, effect.Ownership.Source);
			var sourceAttribute2value = CaptureAttributeMagnitude(SourceAttribute2, effect, effect.Ownership.Source);

			result[0] = CreateCustomModifierEvaluatedData(
				TargetAttribute1.GetAttribute(target),
				ModifierOperation.FlatBonus,
				sourceAttribute1value * sourceAttribute2value);

			result[1] = CreateCustomModifierEvaluatedData(
				TargetAttribute2.GetAttribute(target),
				ModifierOperation.FlatBonus,
				sourceAttribute1value + sourceAttribute2value);

			result[2] = CreateCustomModifierEvaluatedData(
				SourceAttribute3.GetAttribute(effect.Ownership.Source),
				ModifierOperation.FlatBonus,
				-1);

			return result;
		}
	}

	private sealed class NoAttributesEntity : IForgeEntity
	{
		public Attributes Attributes { get; }

		public Forge.Core.GameplayTags GameplayTags { get; }

		public GameplayEffectsManager EffectsManager { get; }

		public NoAttributesEntity(GameplayTagsManager tagsManager, GameplayCuesManager cuesManager)
		{
			EffectsManager = new(this, cuesManager);
			Attributes = new();
			GameplayTags = new(new GameplayTagContainer(tagsManager));
		}
	}
}
