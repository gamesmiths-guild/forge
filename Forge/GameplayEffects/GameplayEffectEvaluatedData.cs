// Copyright © 2025 Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects.Calculator;
using Gamesmiths.Forge.GameplayEffects.Duration;
using Gamesmiths.Forge.GameplayEffects.Magnitudes;
using Gamesmiths.Forge.GameplayEffects.Modifiers;
using Gamesmiths.Forge.GameplayEffects.Periodic;
using Gamesmiths.Forge.GameplayEffects.Stacking;
using Attribute = Gamesmiths.Forge.Core.Attribute;

namespace Gamesmiths.Forge.GameplayEffects;

/// <summary>
/// Represents the precomputed static data for a gameplay effect that has been applied.
/// </summary>
/// <remarks>
/// Optimizes performance by avoiding repeated complex calculations and serves as data for event arguments.
/// </remarks>
public readonly struct GameplayEffectEvaluatedData
{
	/// <summary>
	/// Gets the gameplay effect for this evaluated data.
	/// </summary>
	public GameplayEffect GameplayEffect { get; }

	/// <summary>
	/// Gets the target used for the evaluation of this gameplay effect.
	/// </summary>
	public IForgeEntity Target { get; }

	/// <summary>
	/// Gets the stack count of the effect at the moment of the evaluation.
	/// </summary>
	public int Stack { get; }

	/// <summary>
	/// Gets the level of the effect at the moment of the evaluation.
	/// </summary>
	public int Level { get; }

	/// <summary>
	/// Gets the duration of the effect at the moment of the evaluation.
	/// </summary>
	public float Duration { get; }

	/// <summary>
	/// Gets the period of the effect at the moment of the evaluation.
	/// </summary>
	public float Period { get; }

	/// <summary>
	/// Gets the evaluated data for the modifiers of the gameplay effect.
	/// </summary>
	public ModifierEvaluatedData[] ModifiersEvaluatedData { get; }

	/// <summary>
	/// Gets an array of the attributes to be captured by an active effect.
	/// </summary>
	public Attribute[] AttributesToCapture { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="GameplayEffectEvaluatedData"/> struct.
	/// </summary>
	/// <param name="gameplayEffect">The taget gameplay effect of this evaluated data.</param>
	/// <param name="target">The target of this evaluated data.</param>
	/// <param name="stack">The stack for this evaluated data.</param>
	/// <param name="level">The level for this evaluated data.</param>
	public GameplayEffectEvaluatedData(
		GameplayEffect gameplayEffect,
		IForgeEntity target,
		int stack = 1,
		int? level = null)
	{
		GameplayEffect = gameplayEffect;
		Target = target;
		Stack = stack;
		Level = level ?? gameplayEffect.Level;

		Duration = EvaluateDuration(gameplayEffect.EffectData.DurationData);
		Period = EvaluatePeriod(gameplayEffect.EffectData.PeriodicData);

		// Modifiers should be evaluated after dauration and period because it requires those already evaluated.
		ModifiersEvaluatedData = EvaluateModifiers();

		if (gameplayEffect.EffectData.DurationData.Type == DurationType.Instant)
		{
			AttributesToCapture = [];
			return;
		}

		AttributesToCapture = EvaluateAttributesToCapture();
	}

	private float EvaluateDuration(DurationData durationData)
	{
		if (!durationData.Duration.HasValue)
		{
			return 0;
		}

		return durationData.Duration.Value.GetValue(Level);
	}

	private float EvaluatePeriod(PeriodicData? periodicData)
	{
		if (!periodicData.HasValue)
		{
			return 0;
		}

		return periodicData.Value.Period.GetValue(Level);
	}

	private ModifierEvaluatedData[] EvaluateModifiers()
	{
		var modifiersEvaluatedData = new List<ModifierEvaluatedData>(GameplayEffect.EffectData.Modifiers.Length);

		foreach (Modifier modifier in GameplayEffect.EffectData.Modifiers)
		{
			// Ignore modifiers for attributes not present in the target.
			if (!Target.Attributes.ContainsAttribute(modifier.Attribute))
			{
				continue;
			}

			modifiersEvaluatedData.Add(
				new ModifierEvaluatedData(
					Target.Attributes[modifier.Attribute],
					modifier.Operation,
					EvaluateModifierMagnitude(modifier.Magnitude),
					modifier.Channel));
		}

		foreach (Execution execution in GameplayEffect.EffectData.Executions)
		{
			// Filter attributes not contained
			modifiersEvaluatedData.AddRange(execution.CalculateExecution(GameplayEffect, Target));
		}

		return [.. modifiersEvaluatedData];
	}

	private Attribute[] EvaluateAttributesToCapture()
	{
		var attributesToCapture = new List<Attribute>();

		foreach (ModifierMagnitude modifierMagnitude in GameplayEffect.EffectData.Modifiers.Select(x => x.Magnitude))
		{
			if (!IsModifierSnapshop(modifierMagnitude))
			{
				attributesToCapture.AddRange(CaptureModifierBackingAttribute(modifierMagnitude));
			}
		}

		foreach (Execution execution in GameplayEffect.EffectData.Executions)
		{
			// Filter attributes not contained
			foreach (AttributeCaptureDefinition attributeCaptureDefinition in execution.AttributesToCapture)
			{
				if (!attributeCaptureDefinition.Snapshot)
				{
					IForgeEntity attributeSource = attributeCaptureDefinition.Source
						== AttributeCaptureSource.Source ? GameplayEffect.Ownership.Source : Target;

					attributesToCapture.Add(attributeCaptureDefinition.GetAttribute(attributeSource));
				}
			}
		}

		return [.. attributesToCapture];
	}

	private float EvaluateModifierMagnitude(ModifierMagnitude modifierMagnitude)
	{
		float stackMultiplier = Stack;
		if (GameplayEffect.EffectData.StackingData.HasValue &&
			GameplayEffect.EffectData.StackingData.Value.MagnitudePolicy == StackMagnitudePolicy.DontStack)
		{
			stackMultiplier = 1;
		}

		return modifierMagnitude.GetMagnitude(GameplayEffect, Target, Level) * stackMultiplier;
	}

	private bool IsModifierSnapshop(ModifierMagnitude modifierMagnitude)
	{
		if (GameplayEffect.EffectData.DurationData.Type == DurationType.Instant)
		{
			return true;
		}

		if (modifierMagnitude.MagnitudeCalculationType == MagnitudeCalculationType.CustomCalculatorClass)
		{
			Debug.Assert(
				modifierMagnitude.CustomCalculationBasedFloat.HasValue,
				"CustomCalculationBasedFloat should always have a value at this point.");

			List<AttributeCaptureDefinition> attributesToCapture =
				modifierMagnitude.CustomCalculationBasedFloat.Value.MagnitudeCalculatorClass.AttributesToCapture;
			return attributesToCapture.TrueForAll(x => x.Snapshot);
		}

		if (modifierMagnitude.MagnitudeCalculationType != MagnitudeCalculationType.AttributeBased)
		{
			return true;
		}

		Debug.Assert(
			modifierMagnitude.AttributeBasedFloat.HasValue,
			"AttributeBasedFloat should always have a value at this point.");

		return modifierMagnitude.AttributeBasedFloat.Value.BackingAttribute.Snapshot;
	}

	private Attribute[] CaptureModifierBackingAttribute(ModifierMagnitude modifierMagnitude)
	{
		if (GameplayEffect.EffectData.DurationData.Type == DurationType.Instant)
		{
			return [];
		}

		if (modifierMagnitude.MagnitudeCalculationType == MagnitudeCalculationType.AttributeBased)
		{
			Debug.Assert(
				modifierMagnitude.AttributeBasedFloat.HasValue,
				"AttributeBasedFloat should always have a value at this point.");

			if (TryGetBackingAttribute(
				modifierMagnitude.AttributeBasedFloat.Value.BackingAttribute,
				out Attribute? backingAttribute))
			{
				Debug.Assert(
						backingAttribute is not null,
						"backingAttribute should never be null at this point.");

				return [backingAttribute];
			}

			return [];
		}

		if (modifierMagnitude.MagnitudeCalculationType == MagnitudeCalculationType.CustomCalculatorClass)
		{
			Debug.Assert(
				modifierMagnitude.CustomCalculationBasedFloat.HasValue,
				"CustomCalculationBasedFloat should always have a value at this point.");

			var attributeList = new List<Attribute>();

			foreach (AttributeCaptureDefinition attributeSource in
				modifierMagnitude.CustomCalculationBasedFloat.Value.MagnitudeCalculatorClass.AttributesToCapture)
			{
				if (TryGetBackingAttribute(attributeSource, out Attribute? backingAttribute))
				{
					Debug.Assert(
						backingAttribute is not null,
						"backingAttribute should never be null at this point.");

					attributeList.Add(backingAttribute);
				}
			}

			return [.. attributeList];
		}

		return [];
	}

	private bool TryGetBackingAttribute(AttributeCaptureDefinition attributeSource, out Attribute? backingAttribute)
	{
		backingAttribute = null;

		if (attributeSource.Snapshot)
		{
			return false;
		}

		IForgeEntity attributeSourceOwner = Target;

		if (attributeSource.Source == AttributeCaptureSource.Source)
		{
			attributeSourceOwner = GameplayEffect.Ownership.Owner;
		}

		if (!attributeSourceOwner.Attributes.ContainsAttribute(attributeSource.Attribute))
		{
			return false;
		}

		backingAttribute = attributeSource.GetAttribute(attributeSourceOwner);

		return true;
	}
}
