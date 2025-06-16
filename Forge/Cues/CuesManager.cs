// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Cues;

/// <summary>
/// A singleton class that manages the registration and execution of cues for an entity.
/// </summary>
public sealed class CuesManager
{
	private readonly Dictionary<Tag, HashSet<ICueHandler>> _registeredCues = [];

	/// <summary>
	/// Registers a cue that can be triggered later.
	/// </summary>
	/// <param name="cueTag">The tag for registering the cue.</param>
	/// <param name="cue">The cue to be registered to listen for the given tag.</param>
	public void RegisterCue(Tag cueTag, ICueHandler cue)
	{
		if (_registeredCues.TryGetValue(cueTag, out HashSet<ICueHandler>? value))
		{
			value.Add(cue);
			return;
		}

		_registeredCues[cueTag] = [cue];
	}

	/// <summary>
	/// Unregisters a cue.
	/// </summary>
	/// <param name="cueTag">The tag for the unregistered cue.</param>
	/// <param name="cue">The cue to be unregistered.</param>
	public void UnregisterCue(Tag cueTag, ICueHandler cue)
	{
		if (!_registeredCues.TryGetValue(cueTag, out HashSet<ICueHandler>? value))
		{
			return;
		}

		value.Remove(cue);

		if (value.Count == 0)
		{
			_registeredCues.Remove(cueTag);
		}
	}

	/// <summary>
	/// Executes a one-shot cue.
	/// </summary>
	/// <param name="cueTag">The tag for the cue to be triggered.</param>
	/// <param name="target">An optional target for the cue.</param>
	/// <param name="parameters">The optional parameters for the cue.</param>>
	public void ExecuteCue(Tag cueTag, IForgeEntity? target, CueParameters? parameters)
	{
		if (!_registeredCues.TryGetValue(cueTag, out HashSet<ICueHandler>? cues))
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
	/// <param name="cueTag">The tag for the cue to be triggered.</param>
	/// <param name="target">An optional target for the cue.</param>
	/// <param name="parameters">The optional parameters for the cue.</param>>
	public void ApplyCue(Tag cueTag, IForgeEntity? target, CueParameters? parameters)
	{
		if (!_registeredCues.TryGetValue(cueTag, out HashSet<ICueHandler>? cues))
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
	/// <param name="cueTag">The tag for the cue to be triggered.</param>
	/// <param name="target">An optional target for the cue.</param>
	/// <param name="interrupted">Whether this removal is the result of an interruption.</param>
	public void RemoveCue(Tag cueTag, IForgeEntity? target, bool interrupted)
	{
		if (!_registeredCues.TryGetValue(cueTag, out HashSet<ICueHandler>? cues))
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
	/// <param name="cueTag">The tag for the cue to be triggered.</param>
	/// <param name="target">An optional target for the cue.</param>
	/// <param name="parameters">The optional parameters for the cue.</param>>
	public void UpdateCue(Tag cueTag, IForgeEntity? target, CueParameters? parameters)
	{
		if (!_registeredCues.TryGetValue(cueTag, out HashSet<ICueHandler>? cues))
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

			if (cueData.CueTags is null)
			{
				continue;
			}

			foreach (Tag cueTag in cueData.CueTags)
			{
				ApplyCue(
				cueTag,
				effectEvaluatedData.Target,
				new CueParameters(
					magnitude,
					cueData.NormalizedMagnitude(magnitude),
					effectEvaluatedData.Effect.Ownership.Source,
					effectEvaluatedData.CustomCueParameters));
			}
		}
	}

	internal void RemoveCues(in EffectEvaluatedData effectEvaluatedData, bool interrupted)
	{
		EffectData effectData = effectEvaluatedData.Effect.EffectData;

		foreach (CueData cueData in effectData.Cues)
		{
			if (cueData.CueTags is null)
			{
				continue;
			}

			foreach (Tag cueTag in cueData.CueTags)
			{
				RemoveCue(cueTag, effectEvaluatedData.Target, interrupted);
			}
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

			if (cueData.CueTags is null)
			{
				continue;
			}

			foreach (Tag cueTag in cueData.CueTags)
			{
				ExecuteCue(
				cueTag,
				effectEvaluatedData.Target,
				new CueParameters(
					magnitude,
					cueData.NormalizedMagnitude(magnitude),
					effectEvaluatedData.Effect.Ownership.Source,
					effectEvaluatedData.CustomCueParameters));
			}
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

			if (cueData.CueTags is null)
			{
				continue;
			}

			foreach (Tag cueTag in cueData.CueTags)
			{
				UpdateCue(
				cueTag,
				effectEvaluatedData.Target,
				new CueParameters(
					magnitude,
					cueData.NormalizedMagnitude(magnitude),
					effectEvaluatedData.Effect.Ownership.Source,
					effectEvaluatedData.CustomCueParameters));
			}
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
