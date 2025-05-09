// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
	private const string InvalidPeriodicDataException = "Evaluated period must be greater than zero. A non-positive" +
		" value would cause the effect to loop indefinitely.";

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
	/// Getsan array of custom cue parameters.
	/// </summary>
	public Dictionary<StringKey, object>? CustomCueParameters { get; }

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

		CustomCueParameters = EvaluateCustomCueParameters();

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
			return 1;
		}

		var evaluatedDuration = periodicData.Value.Period.GetValue(Level);

		if (evaluatedDuration <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(periodicData), InvalidPeriodicDataException);
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
			if (ExecutionHasInvalidAttributeCaptures(execution))
			{
				continue;
			}

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
			foreach (AttributeCaptureDefinition attributeCaptureDefinition in execution.AttributesToCapture)
			{
				if (!attributeCaptureDefinition.Snapshot)
				{
					IForgeEntity? attributeSource = attributeCaptureDefinition.Source
						== AttributeCaptureSource.Source ? GameplayEffect.Ownership.Source : Target;

					if (!attributeCaptureDefinition.TryGetAttribute(attributeSource, out Attribute? attributeToCapture))
					{
						continue;
					}

					attributesToCapture.Add(attributeToCapture);
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
					attributeList.Add(backingAttribute);
				}
			}

			return [.. attributeList];
		}

		return [];
	}

	private bool TryGetBackingAttribute(
		AttributeCaptureDefinition attributeSource,
		[NotNullWhen(true)] out Attribute? backingAttribute)
	{
		backingAttribute = null;

		if (attributeSource.Snapshot)
		{
			return false;
		}

		IForgeEntity? attributeSourceOwner = Target;

		if (attributeSource.Source == AttributeCaptureSource.Source)
		{
			attributeSourceOwner = GameplayEffect.Ownership.Owner;
		}

		return attributeSource.TryGetAttribute(attributeSourceOwner, out backingAttribute);
	}

	private bool ExecutionHasInvalidAttributeCaptures(Execution execution)
	{
		foreach (AttributeCaptureDefinition capturedAttribute in execution.AttributesToCapture)
		{
			switch (capturedAttribute.Source)
			{
				case AttributeCaptureSource.Target:

					if (!Target.Attributes.ContainsAttribute(capturedAttribute.Attribute))
					{
						return true;
					}

					break;

				case AttributeCaptureSource.Source:

					IForgeEntity? sourceEntity = GameplayEffect.Ownership.Source;

					if (sourceEntity?.Attributes.ContainsAttribute(capturedAttribute.Attribute) != true)
					{
						return true;
					}

					break;
			}
		}

		return false;
	}

	private Dictionary<StringKey, object>? EvaluateCustomCueParameters()
	{
		var customParameters = new Dictionary<StringKey, object>();

		foreach (Modifier modifier in GameplayEffect.EffectData.Modifiers)
		{
			// Ignore modifiers for attributes not present in the target.
			if (!Target.Attributes.ContainsAttribute(modifier.Attribute))
			{
				continue;
			}

			if (modifier.Magnitude.MagnitudeCalculationType != MagnitudeCalculationType.CustomCalculatorClass)
			{
				continue;
			}

			Debug.Assert(
				modifier.Magnitude.CustomCalculationBasedFloat.HasValue,
				"If modifier is set to CustomCalculatorClass, this should never be null.");

			customParameters = customParameters.Union(
				modifier.Magnitude.CustomCalculationBasedFloat.Value.MagnitudeCalculatorClass.CustomCueParameters)
				.ToDictionary();
		}

		foreach (Execution execution in GameplayEffect.EffectData.Executions)
		{
			if (ExecutionHasInvalidAttributeCaptures(execution))
			{
				continue;
			}

			customParameters = customParameters.Union(execution.CustomCueParameters).ToDictionary();
		}

		return customParameters.Count == 0 ? null : customParameters;
	}
}
