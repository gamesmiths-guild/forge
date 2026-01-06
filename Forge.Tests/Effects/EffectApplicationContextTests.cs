// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Calculator;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

public class EffectApplicationContextTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private enum HealType
	{
		Instant = 0,
		OverTime = 1,
	}

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Context", "Instant")]
	public void Custom_execution_can_access_context_data_from_instant_effect()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var customExecution = new ContextAwareExecution();

		var effectData = new EffectData(
			"Context Effect",
			new DurationData(DurationType.Instant),
			customExecutions: [customExecution]);

		var effect = new Effect(effectData, new EffectOwnership(owner, owner));

		var contextData = new DamageContext(25.5f, true, ["Head", "Torso"]);

		target.EffectsManager.ApplyEffect(effect, contextData);

		customExecution.ReceivedContext.Should().NotBeNull();
		customExecution.ReceivedDamage.Should().Be(25.5f);
		customExecution.ReceivedIsCritical.Should().BeTrue();
		customExecution.ReceivedHitLocations.Should().BeEquivalentTo("Head", "Torso");
	}

	[Fact]
	[Trait("Context", "Instant")]
	public void Custom_execution_receives_null_context_when_applied_without_context()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var customExecution = new ContextAwareExecution();

		var effectData = new EffectData(
			"Context Effect",
			new DurationData(DurationType.Instant),
			customExecutions: [customExecution]);

		var effect = new Effect(effectData, new EffectOwnership(owner, owner));

		target.EffectsManager.ApplyEffect(effect);

		customExecution.ReceivedContext.Should().BeNull();
		customExecution.WasExecuted.Should().BeTrue();
	}

	[Fact]
	[Trait("Context", "Infinite")]
	public void Custom_execution_can_access_context_data_from_infinite_effect()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var customExecution = new ContextAwareExecution();

		var effectData = new EffectData(
			"Context Effect",
			new DurationData(DurationType.Infinite),
			customExecutions: [customExecution]);

		var effect = new Effect(effectData, new EffectOwnership(owner, owner));

		var contextData = new DamageContext(100f, false, ["Arm"]);

		target.EffectsManager.ApplyEffect(effect, contextData);

		customExecution.ReceivedContext.Should().NotBeNull();
		customExecution.ReceivedDamage.Should().Be(100f);
		customExecution.ReceivedIsCritical.Should().BeFalse();
		customExecution.ReceivedHitLocations.Should().BeEquivalentTo("Arm");
	}

	[Fact]
	[Trait("Context", "Calculator")]
	public void Custom_magnitude_calculator_can_access_context_data()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var customCalculator = new ContextAwareMagnitudeCalculator();

		var effectData = new EffectData(
			"Context Effect",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.CustomCalculatorClass,
						customCalculationBasedFloat: new CustomCalculationBasedFloat(
							customCalculator,
							new ScalableFloat(1),
							new ScalableFloat(0),
							new ScalableFloat(0))))
			]);

		var effect = new Effect(effectData, new EffectOwnership(owner, owner));

		var contextData = new DamageContext(10f, true, []);

		target.EffectsManager.ApplyEffect(effect, contextData);

		// Base magnitude is contextData.Damage * 2 (if critical) = 10 * 2 = 20
		// Attribute1 base is 1, so result should be 1 + 20 = 21
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [21, 21, 0, 0]);
	}

	[Fact]
	[Trait("Context", "Calculator")]
	public void Custom_magnitude_calculator_uses_base_damage_when_not_critical()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var customCalculator = new ContextAwareMagnitudeCalculator();

		var effectData = new EffectData(
			"Context Effect",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.CustomCalculatorClass,
						customCalculationBasedFloat: new CustomCalculationBasedFloat(
							customCalculator,
							new ScalableFloat(1),
							new ScalableFloat(0),
							new ScalableFloat(0))))
			]);

		var effect = new Effect(effectData, new EffectOwnership(owner, owner));

		var contextData = new DamageContext(15f, false, []);

		target.EffectsManager.ApplyEffect(effect, contextData);

		// Base magnitude is contextData.Damage = 15 (not critical, no multiplier)
		// Attribute1 base is 1, so result should be 1 + 15 = 16
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [16, 16, 0, 0]);
	}

	[Fact]
	[Trait("Context", "Calculator")]
	public void Custom_magnitude_calculator_returns_zero_when_no_context()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var customCalculator = new ContextAwareMagnitudeCalculator();

		var effectData = new EffectData(
			"Context Effect",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.CustomCalculatorClass,
						customCalculationBasedFloat: new CustomCalculationBasedFloat(
							customCalculator,
							new ScalableFloat(1),
							new ScalableFloat(0),
							new ScalableFloat(0))))
			]);

		var effect = new Effect(effectData, new EffectOwnership(owner, owner));

		target.EffectsManager.ApplyEffect(effect);

		// No context, so magnitude should be 0
		// Attribute1 base is 1, so result should be 1 + 0 = 1
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
	}

	[Fact]
	[Trait("Context", "TryGetData")]
	public void EffectApplicationContext_TryGetData_returns_true_for_matching_type()
	{
		var context = new EffectApplicationContext<DamageContext>(new DamageContext(50f, true, ["Leg"]));

		var result = context.TryGetData(out DamageContext? data);

		result.Should().BeTrue();
		data.Should().NotBeNull();
		data!.Damage.Should().Be(50f);
		data.IsCritical.Should().BeTrue();
		data.HitLocations.Should().BeEquivalentTo("Leg");
	}

	[Fact]
	[Trait("Context", "TryGetData")]
	public void EffectApplicationContext_TryGetData_returns_false_for_mismatched_type()
	{
		var context = new EffectApplicationContext<DamageContext>(new DamageContext(50f, true, ["Leg"]));

		var result = context.TryGetData(out HealingContext? data);

		result.Should().BeFalse();
		data.Should().BeNull();
	}

	[Fact]
	[Trait("Context", "MultipleHits")]
	public void Context_data_can_carry_multiple_hit_locations_for_shotgun_style_effects()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var customExecution = new MultiHitExecution();

		var effectData = new EffectData(
			"Shotgun Blast",
			new DurationData(DurationType.Instant),
			customExecutions: [customExecution]);

		var effect = new Effect(effectData, new EffectOwnership(owner, owner));

		// Simulate a shotgun hitting 5 different locations
		var contextData = new DamageContext(
			Damage: 10f,
			IsCritical: false,
			HitLocations: ["Head", "Torso", "LeftArm", "RightArm", "Leg"]);

		target.EffectsManager.ApplyEffect(effect, contextData);

		// Each hit location adds 10 damage = 5 * 10 = 50 total
		// Attribute1 base is 1, so result should be 1 + 50 = 51
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [51, 51, 0, 0]);
	}

	[Fact]
	[Trait("Context", "DifferentDataTypes")]
	public void Different_context_types_can_be_used_for_different_effects()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var damageExecution = new ContextAwareExecution();
		var healingExecution = new HealingExecution();

		var damageEffectData = new EffectData(
			"Damage Effect",
			new DurationData(DurationType.Instant),
			customExecutions: [damageExecution]);

		var healingEffectData = new EffectData(
			"Healing Effect",
			new DurationData(DurationType.Instant),
			customExecutions: [healingExecution]);

		var damageEffect = new Effect(damageEffectData, new EffectOwnership(owner, owner));
		var healingEffect = new Effect(healingEffectData, new EffectOwnership(owner, owner));

		target.EffectsManager.ApplyEffect(damageEffect, new DamageContext(25f, true, ["Head"]));
		target.EffectsManager.ApplyEffect(healingEffect, new HealingContext(50f, HealType.OverTime));

		damageExecution.ReceivedDamage.Should().Be(25f);
		healingExecution.ReceivedHealAmount.Should().Be(50f);
		healingExecution.ReceivedHealType.Should().Be(HealType.OverTime);
	}

	private sealed record DamageContext(float Damage, bool IsCritical, string[] HitLocations);

	private sealed record HealingContext(float HealAmount, HealType Type);

	/// <summary>
	/// Test custom execution that accesses context data.
	/// </summary>
	private sealed class ContextAwareExecution : CustomExecution
	{
		public EffectApplicationContext? ReceivedContext { get; private set; }

		public float ReceivedDamage { get; private set; }

		public bool ReceivedIsCritical { get; private set; }

		public string[] ReceivedHitLocations { get; private set; } = [];

		public bool WasExecuted { get; private set; }

		public override ModifierEvaluatedData[] EvaluateExecution(
			Effect effect,
			IForgeEntity target,
			EffectEvaluatedData? effectEvaluatedData)
		{
			WasExecuted = true;
			ReceivedContext = effectEvaluatedData?.ApplicationContext;

			if (effectEvaluatedData?.TryGetContextData(out DamageContext? damageContext) == true)
			{
				ReceivedDamage = damageContext.Damage;
				ReceivedIsCritical = damageContext.IsCritical;
				ReceivedHitLocations = damageContext.HitLocations;
			}

			return [];
		}
	}

	/// <summary>
	/// Test custom magnitude calculator that uses context data.
	/// </summary>
	private sealed class ContextAwareMagnitudeCalculator : CustomModifierMagnitudeCalculator
	{
		public override float CalculateBaseMagnitude(
			Effect effect,
			IForgeEntity target,
			EffectEvaluatedData? effectEvaluatedData)
		{
			if (effectEvaluatedData?.TryGetContextData(out DamageContext? damageContext) != true)
			{
				return 0f;
			}

			// Double damage if critical
			return damageContext!.IsCritical ? damageContext.Damage * 2 : damageContext.Damage;
		}
	}

	/// <summary>
	/// Test execution that applies damage per hit location.
	/// </summary>
	private sealed class MultiHitExecution : CustomExecution
	{
		private readonly AttributeCaptureDefinition _targetAttribute;

		public MultiHitExecution()
		{
			_targetAttribute = new AttributeCaptureDefinition(
				"TestAttributeSet.Attribute1",
				AttributeCaptureSource.Target,
				true);

			AttributesToCapture.Add(_targetAttribute);
		}

		public override ModifierEvaluatedData[] EvaluateExecution(
			Effect effect,
			IForgeEntity target,
			EffectEvaluatedData? effectEvaluatedData)
		{
			if (effectEvaluatedData?.TryGetContextData(out DamageContext? damageContext) != true)
			{
				return [];
			}

			if (!_targetAttribute.TryGetAttribute(target, out EntityAttribute? attribute))
			{
				return [];
			}

			// Apply damage for each hit location
			var totalDamage = damageContext!.Damage * damageContext.HitLocations.Length;

			return
			[
				new ModifierEvaluatedData(
					attribute,
					ModifierOperation.FlatBonus,
					totalDamage)
			];
		}
	}

	/// <summary>
	/// Test execution for healing context.
	/// </summary>
	private sealed class HealingExecution : CustomExecution
	{
		public float ReceivedHealAmount { get; private set; }

		public HealType ReceivedHealType { get; private set; }

		public override ModifierEvaluatedData[] EvaluateExecution(
			Effect effect,
			IForgeEntity target,
			EffectEvaluatedData? effectEvaluatedData)
		{
			if (effectEvaluatedData?.TryGetContextData(out HealingContext? healingContext) == true)
			{
				ReceivedHealAmount = healingContext.HealAmount;
				ReceivedHealType = healingContext.Type;
			}

			return [];
		}
	}
}
