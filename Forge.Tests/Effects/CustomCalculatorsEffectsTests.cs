// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Calculator;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Core;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

public class CustomCalculatorsEffectsTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

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
		float customMagnitudeCalculatorExponent,
		float coefficient,
		float preMultiplyAdditiveValue,
		float postMultiplyAdditiveValue,
		int expectedResult)
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var customCalculatorClass = new CustomMagnitudeCalculator(
			customMagnitudeCalculatorAttribute,
			AttributeCaptureSource.Source,
			false,
			customMagnitudeCalculatorExponent);

		var effectData = new EffectData(
			"Level Up",
			new DurationData(DurationType.Instant),
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
		float customMagnitudeCalculatorExponent,
		float coefficient,
		float preMultiplyAdditiveValue,
		float postMultiplyAdditiveValue,
		int expectedResult)
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var customCalculatorClass = new CustomMagnitudeCalculator(
			customMagnitudeCalculatorAttribute,
			AttributeCaptureSource.Source,
			false,
			customMagnitudeCalculatorExponent);

		var effectData = new EffectData(
			"Level Up",
			new DurationData(DurationType.Instant),
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
		float customMagnitudeCalculatorExponent,
		float coefficient,
		float preMultiplyAdditiveValue,
		float postMultiplyAdditiveValue,
		float effect2Magnitude,
		AttributeCaptureSource applyEffect2To,
		int[] expectedResults1,
		int[] expectedResults2)
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var customCalculatorClass = new CustomMagnitudeCalculator(
			customMagnitudeCalculatorAttribute,
			captureSource,
			false,
			customMagnitudeCalculatorExponent);

		var effectData = new EffectData(
			"Test Effect",
			new DurationData(DurationType.Infinite),
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
			]);

		var effectData2 = new EffectData(
			"Backing Attribute Effect",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					customMagnitudeCalculatorAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(effect2Magnitude)))
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(
				owner,
				owner));

		var effect2 = new Effect(
			effectData2,
			new EffectOwnership(
				owner,
				owner));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, expectedResults1);

		TestEntity effect2Target = applyEffect2To == AttributeCaptureSource.Target ? target : owner;

		ActiveEffectHandle? effectHandler = effect2Target.EffectsManager.ApplyEffect(effect2);

		TestUtils.TestAttribute(target, targetAttribute, expectedResults2);

		effect2Target.EffectsManager.UnapplyEffect(effectHandler!);

		TestUtils.TestAttribute(target, targetAttribute, expectedResults1);
	}

	[Fact]
	[Trait("Execution", null)]
	public void Custom_executions_modifies_attribute_accordingly()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var customCalculatorClass = new CustomTestExecutionClass(false);

		var effectData = new EffectData(
			"Test Effect",
			new DurationData(DurationType.Instant),
			customExecutions:
			[
				customCalculatorClass
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(
				owner,
				owner));

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(owner, "TestAttributeSet.Attribute90", [89, 89, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [16, 16, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [11, 11, 0, 0]);

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(owner, "TestAttributeSet.Attribute90", [88, 88, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [31, 31, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [35, 35, 0, 0]);

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(owner, "TestAttributeSet.Attribute90", [87, 87, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [46, 46, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [74, 74, 0, 0]);
	}

	[Fact]
	[Trait("Execution", null)]
	public void Custom_executions_modifies_update_with_non_snapshot_attributes()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var customCalculatorClass = new CustomTestExecutionClass(false);

		var effectData = new EffectData(
			"Test Effect",
			new DurationData(DurationType.Infinite),
			customExecutions:
			[
				customCalculatorClass
			]);

		var effectData2 = new EffectData(
			"Backing Attribute Effect",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					"TestAttributeSet.Attribute3",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(1)))
			]);

		var effectData3 = new EffectData(
			"Backing Attribute Effect",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
						"TestAttributeSet.Attribute5",
						ModifierOperation.FlatBonus,
						new ModifierMagnitude(
							MagnitudeCalculationType.ScalableFloat,
							new ScalableFloat(2)))
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(
				owner,
				owner));

		var effect2 = new Effect(
			effectData2,
			new EffectOwnership(
				owner,
				owner));

		var effect3 = new Effect(
			effectData3,
			new EffectOwnership(
				owner,
				owner));

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(owner, "TestAttributeSet.Attribute90", [89, 90, -1, 0]);

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [16, 1, 15, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [11, 2, 9, 0]);

		ActiveEffectHandle? effectHandler1 = owner.EffectsManager.ApplyEffect(effect2);

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [21, 1, 20, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [12, 2, 10, 0]);

		ActiveEffectHandle? effectHandler2 = owner.EffectsManager.ApplyEffect(effect3);

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [29, 1, 28, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [14, 2, 12, 0]);

		owner.EffectsManager.UnapplyEffect(effectHandler2!);

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [21, 1, 20, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [12, 2, 10, 0]);

		owner.EffectsManager.UnapplyEffect(effectHandler1!);

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [16, 1, 15, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [11, 2, 9, 0]);
	}

	[Fact]
	[Trait("Execution", "Invalid attributes")]
	public void Custom_execution_without_valid_attributes_applies_with_no_attribute_changes()
	{
		var owner = new NoAttributesEntity(_tagsManager, _cuesManager);
		var target = new NoAttributesEntity(_tagsManager, _cuesManager);

		var customCalculatorClass = new CustomTestExecutionClass(false);

		var effectData = new EffectData(
			"Test Effect",
			new DurationData(DurationType.Instant),
			customExecutions:
			[
				customCalculatorClass
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(
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
		var owner = new NoAttributesEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var customCalculatorClass = new CustomTestExecutionClass(false);

		var effectData = new EffectData(
			"Test Effect",
			new DurationData(DurationType.Instant),
			customExecutions:
			[
				customCalculatorClass
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(
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
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new NoAttributesEntity(_tagsManager, _cuesManager);

		var customCalculatorClass = new CustomTestExecutionClass(false);

		var effectData = new EffectData(
			"Test Effect",
			new DurationData(DurationType.Instant),
			customExecutions:
			[
				customCalculatorClass
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(
				owner,
				owner));

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(owner, "TestAttributeSet.Attribute90", [90, 90, 0, 0]);

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(owner, "TestAttributeSet.Attribute90", [90, 90, 0, 0]);

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(owner, "TestAttributeSet.Attribute90", [90, 90, 0, 0]);
	}

	[Fact]
	[Trait("Execution", null)]
	public void Custom_execution_captures_pending_modifiers_from_same_effect()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var customCalculatorClass = new CustomTestExecutionClass(false);

		var effectData = new EffectData(
			"Test Effect",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(5))),
			],
			customExecutions:
			[
				customCalculatorClass
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(
				owner,
				owner));

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(owner, "TestAttributeSet.Attribute90", [89, 89, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [21, 21, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [16, 16, 0, 0]);

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(owner, "TestAttributeSet.Attribute90", [88, 88, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [41, 41, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [50, 50, 0, 0]);

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(owner, "TestAttributeSet.Attribute90", [87, 87, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [61, 61, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [99, 99, 0, 0]);
	}

	[Fact]
	[Trait("Execution", null)]
	public void Custom_execution_considers_previously_applied_modifiers_on_different_channels()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var buffEffectData = new EffectData(
			"Test Buff Effect",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.PercentBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(0.5f)),
					1),
			]);

		var buffEffect = new Effect(
			buffEffectData,
			new EffectOwnership(
				owner,
				owner));

		var customCalculatorClass = new CustomTestExecutionClass(false);

		var effectData = new EffectData(
			"Test Effect",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.Override,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(20))),
			],
			customExecutions:
			[
				customCalculatorClass
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(
				owner,
				owner));

		target.EffectsManager.ApplyEffect(buffEffect);
		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(owner, "TestAttributeSet.Attribute90", [89, 89, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [52, 35, 17, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [40, 40, 0, 0]);
	}

	[Fact]
	[Trait("Instant", null)]
	public void Custom_calculator_class_with_invalid_ownership_applies_with_no_attribute_changes()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		var customCalculatorClass = new CustomMagnitudeCalculator(
			"TestAttributeSet.Attribute1",
			AttributeCaptureSource.Source,
			false,
			1);

		var effectData = new EffectData(
			"Level Up",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.CustomCalculatorClass,
						customCalculationBasedFloat: new CustomCalculationBasedFloat(
							customCalculatorClass,
							new ScalableFloat(1),
							new ScalableFloat(0),
							new ScalableFloat(0))))
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(null, null));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
	}

	[Fact]
	[Trait("Execution", null)]
	public void Custom_executions_with_invalid_ownership_applies_with_no_attribute_changes()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		var customCalculatorClass = new CustomTestExecutionClass(false);

		var effectData = new EffectData(
			"Test Effect",
			new DurationData(DurationType.Instant),
			customExecutions:
			[
				customCalculatorClass
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(
				null,
				null));

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

	[Theory]
	[Trait("Attribute calculation", null)]
	[InlineData(
		"TestAttributeSet.Attribute1",
		"TestAttributeSet.Attribute1",
		AttributeCalculationType.Max,
		new int[] { 99, 1, 99, 1 })]
	[InlineData(
		"TestAttributeSet.Attribute2",
		"TestAttributeSet.Attribute2",
		AttributeCalculationType.Min,
		new int[] { 2, 2, 0, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute5",
		"TestAttributeSet.Attribute5",
		AttributeCalculationType.Modifier,
		new int[] { 20, 5, 15, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute5",
		"TestAttributeSet.Attribute90",
		AttributeCalculationType.Overflow,
		new int[] { 11, 5, 6, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute5",
		"TestAttributeSet.Attribute90",
		AttributeCalculationType.ValidModifier,
		new int[] { 14, 5, 9, 0 })]
	[InlineData(
		"TestAttributeSet.Attribute5",
		"TestAttributeSet.Attribute5",
		AttributeCalculationType.MagnitudeEvaluatedUpToChannel,
		new int[] { 10, 5, 5, 0 })]
	public void Custom_calculator_class_magnitude_captures_magnitude_correctly(
		string targetAttribute,
		string customMagnitudeCalculatorAttribute,
		AttributeCalculationType attributeCalculationType,
		int[] expectedResults)
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var customCalculatorClass = new CustomMagnitudeCalculator(
			customMagnitudeCalculatorAttribute,
			AttributeCaptureSource.Source,
			false,
			1,
			attributeCalculationType);

		var buffEffectData = new EffectData(
			"Buff",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					customMagnitudeCalculatorAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(15)),
					1)
			]);

		var buffEffect = new Effect(buffEffectData, new EffectOwnership(owner, owner));

		var effectData = new EffectData(
			"Level Up",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.CustomCalculatorClass,
						customCalculationBasedFloat: new CustomCalculationBasedFloat(
							customCalculatorClass,
							new ScalableFloat(1),
							new ScalableFloat(0),
							new ScalableFloat(0))))
			]);

		var effect = new Effect(effectData, new EffectOwnership(owner, owner));

		owner.EffectsManager.ApplyEffect(buffEffect);
		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, expectedResults);
	}

	[Fact]
	[Trait("Snapshot", null)]
	public void Custom_calculator_class_snapshot_does_not_update_value_with_effect_level_up()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var customCalculatorClass = new CustomMagnitudeCalculator(
			"TestAttributeSet.Attribute1",
			AttributeCaptureSource.Source,
			true,
			1);

		var effectData = new EffectData(
			"Test Effect",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.CustomCalculatorClass,
						customCalculationBasedFloat: new CustomCalculationBasedFloat(
							customCalculatorClass,
							new ScalableFloat(1),
							new ScalableFloat(0),
							new ScalableFloat(0))))
			],
			snapshotLevel: false);

		var effectData2 = new EffectData(
			"Backing Attribute Effect",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(1)))
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(
				owner,
				owner));

		var effect2 = new Effect(
			effectData2,
			new EffectOwnership(
				owner,
				owner));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [2, 1, 1, 0]);

		owner.EffectsManager.ApplyEffect(effect2);
		effect.LevelUp();

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [2, 1, 1, 0]);
	}

	[Fact]
	[Trait("Snapshot", null)]
	public void Custom_executions_does_not_update_with_snapshot_attributes_when_effect_level_up()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var customCalculatorClass = new CustomTestExecutionClass(true);

		var effectData = new EffectData(
			"Test Effect",
			new DurationData(DurationType.Infinite),
			snapshotLevel: false,
			customExecutions:
			[
				customCalculatorClass
			]);

		var effectData2 = new EffectData(
			"Backing Attribute Effect",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					"TestAttributeSet.Attribute3",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(1)))
			]);

		var effectData3 = new EffectData(
			"Backing Attribute Effect",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
						"TestAttributeSet.Attribute5",
						ModifierOperation.FlatBonus,
						new ModifierMagnitude(
							MagnitudeCalculationType.ScalableFloat,
							new ScalableFloat(2)))
			]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(
				owner,
				owner));

		var effect2 = new Effect(
			effectData2,
			new EffectOwnership(
				owner,
				owner));

		var effect3 = new Effect(
			effectData3,
			new EffectOwnership(
				owner,
				owner));

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(owner, "TestAttributeSet.Attribute90", [89, 90, -1, 0]);

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [16, 1, 15, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [11, 2, 9, 0]);

		ActiveEffectHandle? effectHandler1 = owner.EffectsManager.ApplyEffect(effect2);
		effect.LevelUp();

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [16, 1, 15, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [11, 2, 9, 0]);

		ActiveEffectHandle? effectHandler2 = owner.EffectsManager.ApplyEffect(effect3);
		effect.LevelUp();

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [16, 1, 15, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [11, 2, 9, 0]);

		owner.EffectsManager.UnapplyEffect(effectHandler2!);
		effect.LevelUp();

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [16, 1, 15, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [11, 2, 9, 0]);

		owner.EffectsManager.UnapplyEffect(effectHandler1!);
		effect.LevelUp();

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [16, 1, 15, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [11, 2, 9, 0]);
	}

	private sealed class CustomMagnitudeCalculator : CustomModifierMagnitudeCalculator
	{
		private readonly float _exponent;
		private readonly AttributeCalculationType _attributeCalculationType;

		public AttributeCaptureDefinition Attribute1 { get; }

		public CustomMagnitudeCalculator(
			StringKey attribute,
			AttributeCaptureSource captureSource,
			bool snapshot,
			float exponent,
			AttributeCalculationType attributeCalculationType = AttributeCalculationType.CurrentValue)
		{
			Attribute1 = new AttributeCaptureDefinition(attribute, captureSource, snapshot);

			AttributesToCapture.Add(Attribute1);

			_exponent = exponent;
			_attributeCalculationType = attributeCalculationType;
		}

		public override float CalculateBaseMagnitude(
			Effect effect,
			IForgeEntity target,
			EffectEvaluatedData? effectEvaluatedData)
		{
			var capturedMagnitude = CaptureAttributeMagnitude(
				Attribute1,
				effect,
				target,
				effectEvaluatedData,
				_attributeCalculationType);

			return (float)Math.Pow(capturedMagnitude, _exponent);
		}
	}

	private sealed class NoAttributesEntity : IForgeEntity
	{
		public EntityAttributes Attributes { get; }

		public EntityTags Tags { get; }

		public EffectsManager EffectsManager { get; }

		public EntityAbilities Abilities { get; }

		public EventManager Events { get; }

		public NoAttributesEntity(TagsManager tagsManager, CuesManager cuesManager)
		{
			EffectsManager = new(this, cuesManager);
			Attributes = new();
			Tags = new(new TagContainer(tagsManager));
			Abilities = new(this);
			Events = new();
		}
	}
}
