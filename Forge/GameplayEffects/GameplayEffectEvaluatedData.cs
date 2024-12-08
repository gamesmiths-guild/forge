// Copyright © 2024 Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects.Duration;
using Gamesmiths.Forge.GameplayEffects.Magnitudes;
using Gamesmiths.Forge.GameplayEffects.Modifiers;
using Gamesmiths.Forge.GameplayEffects.Periodic;
using Gamesmiths.Forge.GameplayEffects.Stacking;
using Attribute = Gamesmiths.Forge.Attributes.Attribute;

namespace Gamesmiths.Forge.GameplayEffects;

internal readonly struct GameplayEffectEvaluatedData
{
	public GameplayEffect GameplayEffect { get; }

	public IForgeEntity Target { get; }

	public int Stack { get; }

	public int Level { get; }

	public float Duration { get; }

	public float Period { get; }

	public ModifierEvaluatedData[] ModifiersEvaluatedData { get; }

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
			attributeSourceOwner = GameplayEffect.Context.Instigator;
		}

		return modifier.Magnitude.AttributeBasedFloat.Value.BackingAttribute.GetAttribute(attributeSourceOwner);
	}
}
