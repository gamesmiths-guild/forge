// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects;

namespace Gamesmiths.Forge.GameplayCues;

/// <summary>
/// A singleton class that manages the registration and execution of gameplay cues for an entity.
/// </summary>
public sealed class GameplayCuesManager
{
	private readonly Dictionary<StringKey, HashSet<IGameplayCue>> _registeredCues = [];

	/// <summary>
	/// Registers a gameplay cue that can be triggered later.
	/// </summary>
	/// <param name="cueKey">The key for registering the cue.</param>
	/// <param name="cue">The cue to be registered to listen for the given key.</param>
	public void RegisterCue(StringKey cueKey, IGameplayCue cue)
	{
		if (_registeredCues.TryGetValue(cueKey, out HashSet<IGameplayCue>? value))
		{
			value.Add(cue);
			return;
		}

		_registeredCues[cueKey] = [cue];
	}

	/// <summary>
	/// Unregisters a gameplay cue.
	/// </summary>
	/// <param name="cueKey">The key for the unregistered cue.</param>
	/// <param name="cue">The cue to be unregistered.</param>
	public void UnregisterCue(StringKey cueKey, IGameplayCue cue)
	{
		if (!_registeredCues.TryGetValue(cueKey, out HashSet<IGameplayCue>? value))
		{
			return;
		}

		value.Remove(cue);

		if (value.Count == 0)
		{
			_registeredCues.Remove(cueKey);
		}
	}

	/// <summary>
	/// Executes a one-shot gameplay cue.
	/// </summary>
	/// <param name="cueKey">The key for the cue to be triggered.</param>
	/// <param name="target">An optional target for the cue.</param>
	/// <param name="parameters">The optional parameters for the cue.</param>>
	public void ExecuteCue(StringKey cueKey, IForgeEntity? target, GameplayCueParameters? parameters)
	{
		if (!_registeredCues.TryGetValue(cueKey, out HashSet<IGameplayCue>? cues))
		{
			return;
		}

		foreach (IGameplayCue cue in cues)
		{
			cue.OnExecute(target, parameters);
		}
	}

	/// <summary>
	/// Adds a persistent gameplay cue.
	/// </summary>
	/// <param name="cueKey">The key for the cue to be triggered.</param>
	/// <param name="target">An optional target for the cue.</param>
	/// <param name="parameters">The optional parameters for the cue.</param>>
	public void ApplyCue(StringKey cueKey, IForgeEntity? target, GameplayCueParameters? parameters)
	{
		if (!_registeredCues.TryGetValue(cueKey, out HashSet<IGameplayCue>? cues))
		{
			return;
		}

		foreach (IGameplayCue cue in cues)
		{
			cue.OnApply(target, parameters);
		}
	}

	/// <summary>
	/// Removes a persistent gameplay cue.
	/// </summary>
	/// <param name="cueKey">The key for the cue to be triggered.</param>
	/// <param name="target">An optional target for the cue.</param>
	/// <param name="interrupted">Whether this removal is the result of an interruption.</param>
	public void RemoveCue(StringKey cueKey, IForgeEntity? target, bool interrupted)
	{
		if (!_registeredCues.TryGetValue(cueKey, out HashSet<IGameplayCue>? cues))
		{
			return;
		}

		foreach (IGameplayCue cue in cues)
		{
			cue.OnRemove(target, interrupted);
		}
	}

	/// <summary>
	/// Updates an active cue with new values.
	/// </summary>
	/// <param name="cueKey">The key for the cue to be triggered.</param>
	/// <param name="target">An optional target for the cue.</param>
	/// <param name="parameters">The optional parameters for the cue.</param>>
	public void UpdateCue(StringKey cueKey, IForgeEntity? target, GameplayCueParameters? parameters)
	{
		if (!_registeredCues.TryGetValue(cueKey, out HashSet<IGameplayCue>? cues))
		{
			return;
		}

		foreach (IGameplayCue cue in cues)
		{
			cue.OnUpdate(target, parameters);
		}
	}

	internal void ApplyCues(in GameplayEffectEvaluatedData effectEvaluatedData)
	{
		GameplayEffectData effectData = effectEvaluatedData.GameplayEffect.EffectData;

		Attributes targetAttributes = effectEvaluatedData.Target.Attributes;
		if (!ShouldTriggerCue(in effectData, in targetAttributes))
		{
			return;
		}

		foreach (GameplayCueData cueData in effectData.GameplayCues)
		{
			var magnitude = CalculateMagnitude(in cueData, in effectEvaluatedData);

			ApplyCue(
				cueData.CueKey,
				effectEvaluatedData.Target,
				new GameplayCueParameters(
					magnitude,
					cueData.NormalizedMagnitude(magnitude),
					effectEvaluatedData.GameplayEffect.Ownership.Source,
					effectEvaluatedData.CustomCueParameters));
		}
	}

	internal void RemoveCues(in GameplayEffectEvaluatedData effectEvaluatedData, bool interrupted)
	{
		GameplayEffectData effectData = effectEvaluatedData.GameplayEffect.EffectData;

		foreach (GameplayCueData cueData in effectData.GameplayCues)
		{
			RemoveCue(cueData.CueKey, effectEvaluatedData.Target, interrupted);
		}
	}

	internal void ExecuteCues(in GameplayEffectEvaluatedData effectEvaluatedData)
	{
		GameplayEffectData effectData = effectEvaluatedData.GameplayEffect.EffectData;

		Attributes targetAttributes = effectEvaluatedData.Target.Attributes;
		if (!ShouldTriggerCue(in effectData, in targetAttributes))
		{
			return;
		}

		foreach (GameplayCueData cueData in effectData.GameplayCues)
		{
			var magnitude = CalculateMagnitude(in cueData, in effectEvaluatedData);

			ExecuteCue(
				cueData.CueKey,
				effectEvaluatedData.Target,
				new GameplayCueParameters(
					magnitude,
					cueData.NormalizedMagnitude(magnitude),
					effectEvaluatedData.GameplayEffect.Ownership.Source,
					effectEvaluatedData.CustomCueParameters));
		}
	}

	internal void UpdateCues(in GameplayEffectEvaluatedData effectEvaluatedData)
	{
		GameplayEffectData effectData = effectEvaluatedData.GameplayEffect.EffectData;

		Attributes targetAttributes = effectEvaluatedData.Target.Attributes;
		if (!ShouldTriggerCue(in effectData, in targetAttributes))
		{
			return;
		}

		foreach (GameplayCueData cueData in effectData.GameplayCues)
		{
			var magnitude = CalculateMagnitude(in cueData, in effectEvaluatedData);

			UpdateCue(
				cueData.CueKey,
				effectEvaluatedData.Target,
				new GameplayCueParameters(
					magnitude,
					cueData.NormalizedMagnitude(magnitude),
					effectEvaluatedData.GameplayEffect.Ownership.Source,
					effectEvaluatedData.CustomCueParameters));
		}
	}

	private static bool ShouldTriggerCue(
		in GameplayEffectData effectData,
		in Attributes attributes)
	{
		return !effectData.RequireModifierSuccessToTriggerCue || attributes.Any(x => x.PendingValueChange != 0);
	}

	private static int CalculateMagnitude(
		in GameplayCueData cueData,
		in GameplayEffectEvaluatedData effectEvaluatedData)
	{
		switch (cueData.MagnitudeType)
		{
			default:
				return effectEvaluatedData.Level;

			case CueMagnitudeType.StackCount:
				return effectEvaluatedData.Stack;

			case CueMagnitudeType.AttributeValueChange:
				Debug.Assert(
					cueData.MagnitudeAttribute is not null,
					"Cues with CueMagnitudeType.AttributeMagnitude must contains a configured MagnitudeAttribute.");

				if (!effectEvaluatedData.Target.Attributes.ContainsAttribute(cueData.MagnitudeAttribute))
				{
					return 0;
				}

				return effectEvaluatedData.Target.Attributes[cueData.MagnitudeAttribute].PendingValueChange;

			case CueMagnitudeType.AttributeCurrentValue:
				Debug.Assert(
					cueData.MagnitudeAttribute is not null,
					"Cues with CueMagnitudeType.AttributeMagnitude must contains a configured MagnitudeAttribute.");

				if (!effectEvaluatedData.Target.Attributes.ContainsAttribute(cueData.MagnitudeAttribute))
				{
					return 0;
				}

				return effectEvaluatedData.Target.Attributes[cueData.MagnitudeAttribute].CurrentValue;

			case CueMagnitudeType.AttributeModifier:
				Debug.Assert(
					cueData.MagnitudeAttribute is not null,
					"Cues with CueMagnitudeType.AttributeMagnitude must contains a configured MagnitudeAttribute.");

				if (!effectEvaluatedData.Target.Attributes.ContainsAttribute(cueData.MagnitudeAttribute))
				{
					return 0;
				}

				return effectEvaluatedData.Target.Attributes[cueData.MagnitudeAttribute].Modifier;
		}
	}
}
