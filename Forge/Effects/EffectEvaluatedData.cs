// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Calculator;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// Represents the precomputed data for a effect that has been applied.
/// </summary>
/// <remarks>
/// Optimizes performance by avoiding repeated complex calculations and serves as data for event arguments.
/// </remarks>
public sealed class EffectEvaluatedData
{
	private const string InvalidPeriodicDataException = "Evaluated period must be greater than zero. A non-positive" +
		" value would cause the effect to loop indefinitely.";

	private readonly int _snapshotLevel;

	/// <summary>
	/// Gets the effect for this evaluated data.
	/// </summary>
	public Effect Effect { get; private set; }

	/// <summary>
	/// Gets the target used for the evaluation of this effect.
	/// </summary>
	public IForgeEntity Target { get; }

	/// <summary>
	/// Gets the stack count of the effect at the moment of the evaluation.
	/// </summary>
	public int Stack { get; private set; }

	/// <summary>
	/// Gets the level of the effect at the moment of the evaluation.
	/// </summary>
	public int Level { get; private set; }

	/// <summary>
	/// Gets the duration of the effect at the moment of the evaluation.
	/// </summary>
	public float Duration { get; private set; }

	/// <summary>
	/// Gets the period of the effect at the moment of the evaluation.
	/// </summary>
	public float Period { get; private set; }

	/// <summary>
	/// Gets the evaluated data for the modifiers of the effect.
	/// </summary>
	public ModifierEvaluatedData[] ModifiersEvaluatedData { get; private set; }

	/// <summary>
	/// Gets an array of the attributes to be captured by an active effect.
	/// </summary>
	public EntityAttribute[] AttributesToCapture { get; }

	/// <summary>
	/// Gets an array of custom cue parameters.
	/// </summary>
	public Dictionary<StringKey, object>? CustomCueParameters { get; private set; }

	internal Dictionary<AttributeSnapshotKey, float> SnapshotAttributes { get; } = [];

	internal Dictionary<Tag, float> SnapshotSetByCallers { get; } = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="EffectEvaluatedData"/> class.
	/// </summary>
	/// <param name="effect">The target effect of this evaluated data.</param>
	/// <param name="target">The target of this evaluated data.</param>
	/// <param name="stack">The stack for this evaluated data.</param>
	/// <param name="level">The level for this evaluated data.</param>
	public EffectEvaluatedData(
		Effect effect,
		IForgeEntity target,
		int stack = 1,
		int? level = null)
	{
		Effect = effect;
		Target = target;
		Stack = stack;
		Level = level ?? effect.Level;

		if (effect.EffectData.SnapshotLevel)
		{
			_snapshotLevel = Level;
		}

		Duration = EvaluateDuration(effect.EffectData.DurationData);
		Period = EvaluatePeriod(effect.EffectData.PeriodicData);
		ModifiersEvaluatedData = EvaluateModifiers();

		CustomCueParameters = EvaluateCustomCueParameters();

		if (effect.EffectData.DurationData.DurationType == DurationType.Instant)
		{
			AttributesToCapture = [];
			return;
		}

		AttributesToCapture = EvaluateAttributesToCapture();
	}

	internal void ReEvaluate(Effect effect, int stack = 1, int? level = null)
	{
		Effect = effect;
		Stack = stack;
		Level = level ?? effect.Level;

		if (level is null && effect.EffectData.SnapshotLevel)
		{
			Level = _snapshotLevel;
		}

		Duration = EvaluateDuration(effect.EffectData.DurationData);
		Period = EvaluatePeriod(effect.EffectData.PeriodicData);
		ModifiersEvaluatedData = EvaluateModifiers();

		CustomCueParameters = EvaluateCustomCueParameters();
	}

	internal float EvaluateDuration(DurationData durationData)
	{
		if (!durationData.DurationMagnitude.HasValue)
		{
			return 0;
		}

		return durationData.DurationMagnitude.Value.GetMagnitude(Effect, Target, Level, this);
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

		return evaluatedDuration;
	}

	private ModifierEvaluatedData[] EvaluateModifiers()
	{
		var modifiersEvaluatedData = new List<ModifierEvaluatedData>(Effect.EffectData.Modifiers.Length);

		foreach (Modifier modifier in Effect.EffectData.Modifiers)
		{
			// Ignore modifiers for attributes not present in the target.
			if (!Target.Attributes.ContainsAttribute(modifier.Attribute))
			{
				continue;
			}

			var baseMagnitude = modifier.Magnitude.GetMagnitude(Effect, Target, Level, this);
			var finalMagnitude = ApplyStackPolicy(baseMagnitude);

			modifiersEvaluatedData.Add(
				new ModifierEvaluatedData(
					Target.Attributes[modifier.Attribute],
					modifier.Operation,
					finalMagnitude,
					modifier.Channel));
		}

		foreach (CustomExecution execution in Effect.EffectData.CustomExecutions)
		{
			if (CustomExecution.ExecutionHasInvalidAttributeCaptures(execution, Effect, Target))
			{
				continue;
			}

			modifiersEvaluatedData.AddRange(execution.EvaluateExecution(Effect, Target, this));
		}

		return [.. modifiersEvaluatedData];
	}

	private EntityAttribute[] EvaluateAttributesToCapture()
	{
		var attributesToCapture = new List<EntityAttribute>();

		foreach (ModifierMagnitude modifierMagnitude in Effect.EffectData.Modifiers.Select(x => x.Magnitude))
		{
			if (!IsModifierSnapshot(modifierMagnitude))
			{
				attributesToCapture.AddRange(CaptureModifierBackingAttribute(modifierMagnitude));
			}
		}

		if (Effect.EffectData.DurationData.DurationType == DurationType.HasDuration
			&& Effect.EffectData.DurationData.DurationMagnitude.HasValue)
		{
			attributesToCapture.AddRange(
				CaptureModifierBackingAttribute(Effect.EffectData.DurationData.DurationMagnitude.Value));
		}

		foreach (CustomExecution execution in Effect.EffectData.CustomExecutions)
		{
			foreach (AttributeCaptureDefinition attributeCaptureDefinition in execution.AttributesToCapture)
			{
				if (!attributeCaptureDefinition.Snapshot)
				{
					IForgeEntity? attributeSource = attributeCaptureDefinition.Source
						== AttributeCaptureSource.Source ? Effect.Ownership.Source : Target;

					if (!attributeCaptureDefinition.TryGetAttribute(
							attributeSource,
							out EntityAttribute? attributeToCapture))
					{
						continue;
					}

					attributesToCapture.Add(attributeToCapture);
				}
			}
		}

		return [.. attributesToCapture];
	}

	private float ApplyStackPolicy(float baseMagnitude)
	{
		var stackMultiplier = Stack;
		if (Effect.EffectData.StackingData.HasValue &&
			Effect.EffectData.StackingData.Value.MagnitudePolicy == StackMagnitudePolicy.DontStack)
		{
			stackMultiplier = 1;
		}

		return baseMagnitude * stackMultiplier;
	}

	private bool IsModifierSnapshot(ModifierMagnitude modifierMagnitude)
	{
		if (Effect.EffectData.DurationData.DurationType == DurationType.Instant)
		{
			return true;
		}

		if (modifierMagnitude.MagnitudeCalculationType == MagnitudeCalculationType.CustomCalculatorClass)
		{
			Validation.Assert(
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

		Validation.Assert(
			modifierMagnitude.AttributeBasedFloat.HasValue,
			"AttributeBasedFloat should always have a value at this point.");

		return modifierMagnitude.AttributeBasedFloat.Value.BackingAttribute.Snapshot;
	}

	private EntityAttribute[] CaptureModifierBackingAttribute(ModifierMagnitude modifierMagnitude)
	{
		if (Effect.EffectData.DurationData.DurationType == DurationType.Instant)
		{
			return [];
		}

		if (modifierMagnitude.MagnitudeCalculationType == MagnitudeCalculationType.AttributeBased)
		{
			Validation.Assert(
				modifierMagnitude.AttributeBasedFloat.HasValue,
				"AttributeBasedFloat should always have a value at this point.");

			if (TryGetBackingAttribute(
				modifierMagnitude.AttributeBasedFloat.Value.BackingAttribute,
				out EntityAttribute? backingAttribute))
			{
				return [backingAttribute];
			}

			return [];
		}

		if (modifierMagnitude.MagnitudeCalculationType == MagnitudeCalculationType.CustomCalculatorClass)
		{
			Validation.Assert(
				modifierMagnitude.CustomCalculationBasedFloat.HasValue,
				"CustomCalculationBasedFloat should always have a value at this point.");

			var attributeList = new List<EntityAttribute>();

			foreach (AttributeCaptureDefinition attributeSource in
				modifierMagnitude.CustomCalculationBasedFloat.Value.MagnitudeCalculatorClass.AttributesToCapture)
			{
				if (TryGetBackingAttribute(attributeSource, out EntityAttribute? backingAttribute))
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
		[NotNullWhen(true)] out EntityAttribute? backingAttribute)
	{
		backingAttribute = null;

		if (attributeSource.Snapshot)
		{
			return false;
		}

		IForgeEntity? attributeSourceOwner = Target;

		if (attributeSource.Source == AttributeCaptureSource.Source)
		{
			attributeSourceOwner = Effect.Ownership.Owner;
		}

		return attributeSource.TryGetAttribute(attributeSourceOwner, out backingAttribute);
	}

	private Dictionary<StringKey, object>? EvaluateCustomCueParameters()
	{
		var customParameters = new Dictionary<StringKey, object>();

		foreach (Modifier modifier in Effect.EffectData.Modifiers)
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

			Validation.Assert(
				modifier.Magnitude.CustomCalculationBasedFloat.HasValue,
				"If modifier is set to CustomCalculatorClass, this should never be null.");

			customParameters = customParameters.Union(
				modifier.Magnitude.CustomCalculationBasedFloat.Value.MagnitudeCalculatorClass.CustomCueParameters)
				.ToDictionary(x => x.Key, x => x.Value);
		}

		foreach (CustomExecution execution in Effect.EffectData.CustomExecutions)
		{
			if (CustomExecution.ExecutionHasInvalidAttributeCaptures(execution, Effect, Target))
			{
				continue;
			}

			customParameters = customParameters
				.Union(execution.CustomCueParameters)
				.ToDictionary(x => x.Key, x => x.Value);
		}

		return customParameters.Count == 0 ? null : customParameters;
	}
}
