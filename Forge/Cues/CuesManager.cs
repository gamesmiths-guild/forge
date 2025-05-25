// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Cues;

/// <summary>
/// A singleton class that manages the registration and execution of cues for an entity.
/// </summary>
public sealed class CuesManager
{
	private readonly Dictionary<StringKey, HashSet<ICueHandler>> _registeredCues = [];

	/// <summary>
	/// Registers a cue that can be triggered later.
	/// </summary>
	/// <param name="cueKey">The key for registering the cue.</param>
	/// <param name="cue">The cue to be registered to listen for the given key.</param>
	public void RegisterCue(StringKey cueKey, ICueHandler cue)
	{
		if (_registeredCues.TryGetValue(cueKey, out HashSet<ICueHandler>? value))
		{
			value.Add(cue);
			return;
		}

		_registeredCues[cueKey] = [cue];
	}

	/// <summary>
	/// Unregisters a cue.
	/// </summary>
	/// <param name="cueKey">The key for the unregistered cue.</param>
	/// <param name="cue">The cue to be unregistered.</param>
	public void UnregisterCue(StringKey cueKey, ICueHandler cue)
	{
		if (!_registeredCues.TryGetValue(cueKey, out HashSet<ICueHandler>? value))
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
	/// Executes a one-shot cue.
	/// </summary>
	/// <param name="cueKey">The key for the cue to be triggered.</param>
	/// <param name="target">An optional target for the cue.</param>
	/// <param name="parameters">The optional parameters for the cue.</param>>
	public void ExecuteCue(StringKey cueKey, IForgeEntity? target, CueParameters? parameters)
	{
		if (!_registeredCues.TryGetValue(cueKey, out HashSet<ICueHandler>? cues))
		{
			return;
		}

		foreach (ICueHandler cue in cues)
		{
			cue.OnExecute(target, parameters);
		}
	}

	/// <summary>
	/// Adds a persistent cue.
	/// </summary>
	/// <param name="cueKey">The key for the cue to be triggered.</param>
	/// <param name="target">An optional target for the cue.</param>
	/// <param name="parameters">The optional parameters for the cue.</param>>
	public void ApplyCue(StringKey cueKey, IForgeEntity? target, CueParameters? parameters)
	{
		if (!_registeredCues.TryGetValue(cueKey, out HashSet<ICueHandler>? cues))
		{
			return;
		}

		foreach (ICueHandler cue in cues)
		{
			cue.OnApply(target, parameters);
		}
	}

	/// <summary>
	/// Removes a persistent cue.
	/// </summary>
	/// <param name="cueKey">The key for the cue to be triggered.</param>
	/// <param name="target">An optional target for the cue.</param>
	/// <param name="interrupted">Whether this removal is the result of an interruption.</param>
	public void RemoveCue(StringKey cueKey, IForgeEntity? target, bool interrupted)
	{
		if (!_registeredCues.TryGetValue(cueKey, out HashSet<ICueHandler>? cues))
		{
			return;
		}

		foreach (ICueHandler cue in cues)
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
	public void UpdateCue(StringKey cueKey, IForgeEntity? target, CueParameters? parameters)
	{
		if (!_registeredCues.TryGetValue(cueKey, out HashSet<ICueHandler>? cues))
		{
			return;
		}

		foreach (ICueHandler cue in cues)
		{
			cue.OnUpdate(target, parameters);
		}
	}

	internal void ApplyCues(in EffectEvaluatedData effectEvaluatedData)
	{
		EffectData effectData = effectEvaluatedData.Effect.EffectData;

		EntityAttributes targetAttributes = effectEvaluatedData.Target.Attributes;
		if (!ShouldTriggerCue(in effectData, in targetAttributes))
		{
			return;
		}

		foreach (CueData cueData in effectData.Cues)
		{
			var magnitude = CalculateMagnitude(in cueData, in effectEvaluatedData);

			ApplyCue(
				cueData.CueKey,
				effectEvaluatedData.Target,
				new CueParameters(
					magnitude,
					cueData.NormalizedMagnitude(magnitude),
					effectEvaluatedData.Effect.Ownership.Source,
					effectEvaluatedData.CustomCueParameters));
		}
	}

	internal void RemoveCues(in EffectEvaluatedData effectEvaluatedData, bool interrupted)
	{
		EffectData effectData = effectEvaluatedData.Effect.EffectData;

		foreach (CueData cueData in effectData.Cues)
		{
			RemoveCue(cueData.CueKey, effectEvaluatedData.Target, interrupted);
		}
	}

	internal void ExecuteCues(in EffectEvaluatedData effectEvaluatedData)
	{
		EffectData effectData = effectEvaluatedData.Effect.EffectData;

		EntityAttributes targetAttributes = effectEvaluatedData.Target.Attributes;
		if (!ShouldTriggerCue(in effectData, in targetAttributes))
		{
			return;
		}

		foreach (CueData cueData in effectData.Cues)
		{
			var magnitude = CalculateMagnitude(in cueData, in effectEvaluatedData);

			ExecuteCue(
				cueData.CueKey,
				effectEvaluatedData.Target,
				new CueParameters(
					magnitude,
					cueData.NormalizedMagnitude(magnitude),
					effectEvaluatedData.Effect.Ownership.Source,
					effectEvaluatedData.CustomCueParameters));
		}
	}

	internal void UpdateCues(in EffectEvaluatedData effectEvaluatedData)
	{
		EffectData effectData = effectEvaluatedData.Effect.EffectData;

		EntityAttributes targetAttributes = effectEvaluatedData.Target.Attributes;
		if (!ShouldTriggerCue(in effectData, in targetAttributes))
		{
			return;
		}

		foreach (CueData cueData in effectData.Cues)
		{
			var magnitude = CalculateMagnitude(in cueData, in effectEvaluatedData);

			UpdateCue(
				cueData.CueKey,
				effectEvaluatedData.Target,
				new CueParameters(
					magnitude,
					cueData.NormalizedMagnitude(magnitude),
					effectEvaluatedData.Effect.Ownership.Source,
					effectEvaluatedData.CustomCueParameters));
		}
	}

	private static bool ShouldTriggerCue(
		in EffectData effectData,
		in EntityAttributes attributes)
	{
		return !effectData.RequireModifierSuccessToTriggerCue || attributes.Any(x => x.PendingValueChange != 0);
	}

	private static int CalculateMagnitude(
		in CueData cueData,
		in EffectEvaluatedData effectEvaluatedData)
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
