// Copyright © 2024 Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core;
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

		// Modifiers should be evaluated last because their evaluation requires already evaluated values.
		ModifiersEvaluatedData = EvaluateModifiers(gameplayEffect.EffectData.Modifiers);
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

	private ModifierEvaluatedData[] EvaluateModifiers(Modifier[] modifiers)
	{
		var modifiersEvaluatedData = new ModifierEvaluatedData[modifiers.Length];

		for (var i = 0; i < modifiers.Length; i++)
		{
			Modifier modifier = modifiers[i];
			modifiersEvaluatedData[i] = new ModifierEvaluatedData(
				Target.Attributes[modifier.Attribute],
				modifier.Operation,
				EvaluateModifierMagnitude(modifier),
				modifier.Channel,
				EvaluateModifierSnapshop(modifier),
				EvaluateModifierBackingAttribute(modifier));
		}

		return modifiersEvaluatedData;
	}

	private float EvaluateModifierMagnitude(Modifier modifier)
	{
		float stackMultiplier = Stack;
		if (GameplayEffect.EffectData.StackingData.HasValue &&
			GameplayEffect.EffectData.StackingData.Value.MagnitudePolicy == StackMagnitudePolicy.DontStack)
		{
			stackMultiplier = 1;
		}

		return modifier.Magnitude.GetMagnitude(GameplayEffect, Target, Level) * stackMultiplier;
	}

	private bool EvaluateModifierSnapshop(Modifier modifier)
	{
		if (GameplayEffect.EffectData.DurationData.Type == DurationType.Instant)
		{
			return true;
		}

		if (modifier.Magnitude.MagnitudeCalculationType != MagnitudeCalculationType.AttributeBased)
		{
			return true;
		}

		Debug.Assert(
			modifier.Magnitude.AttributeBasedFloat.HasValue,
			"AttributeBasedFloat should always have a value at this point.");

		return modifier.Magnitude.AttributeBasedFloat.Value.BackingAttribute.Snapshot;
	}

	private Attribute? EvaluateModifierBackingAttribute(Modifier modifier)
	{
		if (GameplayEffect.EffectData.DurationData.Type == DurationType.Instant
			|| modifier.Magnitude.MagnitudeCalculationType != MagnitudeCalculationType.AttributeBased)
		{
			return null;
		}

		Debug.Assert(
			modifier.Magnitude.AttributeBasedFloat.HasValue,
			"AttributeBasedFloat should always have a value at this point.");

		if (modifier.Magnitude.AttributeBasedFloat.Value.BackingAttribute.Snapshot)
		{
			return null;
		}

		IForgeEntity attributeSourceOwner = Target;

		if (modifier.Magnitude.AttributeBasedFloat.Value.BackingAttribute.Source ==
				AttributeCaptureSource.Source)
		{
			attributeSourceOwner = GameplayEffect.Ownership.Owner;
		}

		return modifier.Magnitude.AttributeBasedFloat.Value.BackingAttribute.GetAttribute(attributeSourceOwner);
	}
}
