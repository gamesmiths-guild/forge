// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Cues;

/// <summary>
/// A singleton class that manages the registration and execution of cues for an entity.
/// </summary>
public sealed class CuesManager
{
	/// <summary>
	/// The most cues one effect can have before the magnitudes held across its hooks move from the stack to the heap.
	/// </summary>
	internal const int MaxStackCueMagnitudes = 8;

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

	/// <summary>
	/// Decides whether an effect's cues fire for a trigger and, when they do, reads the magnitude of each one into
	/// <paramref name="magnitudes"/>, one slot per entry of the effect's cues.
	/// </summary>
	/// <remarks>
	/// The read and the dispatch are separate on purpose. The magnitudes come from the attribute deltas still pending
	/// from the operation that fires the cues, and any hook that runs before the handlers — a component, a manager
	/// event — can land another effect on the target whose own application flushes them. Reading here and dispatching
	/// through <see cref="ExecuteCues"/> or <see cref="UpdateCues(in EffectEvaluatedData, ReadOnlySpan{int})"/>
	/// once the hooks are done keeps the cues describing that operation alone, without moving the handlers ahead of
	/// the components that expect to run first.
	/// </remarks>
	/// <param name="effectEvaluatedData">The evaluated data of the effect whose cues are being read.</param>
	/// <param name="triggerRequirement">The trigger the cues are being read for.</param>
	/// <param name="magnitudes">Receives one magnitude per cue; at least as long as the effect's cues.</param>
	/// <returns><see langword="true"/> if the cues fire and <paramref name="magnitudes"/> was filled; otherwise,
	/// <see langword="false"/>.</returns>
	internal static bool TryCaptureCueMagnitudes(
		in EffectEvaluatedData effectEvaluatedData,
		CueTriggerRequirement triggerRequirement,
		Span<int> magnitudes)
	{
		EffectData effectData = effectEvaluatedData.Effect.EffectData;

		EntityAttributes targetAttributes = effectEvaluatedData.Target.Attributes;
		if (!ShouldTriggerCue(in effectData, in targetAttributes, triggerRequirement))
		{
			return false;
		}

		for (int i = 0; i < effectData.Cues.Length; i++)
		{
			magnitudes[i] = CalculateMagnitude(in effectData.Cues[i], in effectEvaluatedData);
		}

		return true;
	}

	internal void ApplyCues(in EffectEvaluatedData effectEvaluatedData)
	{
		EffectData effectData = effectEvaluatedData.Effect.EffectData;

		EntityAttributes targetAttributes = effectEvaluatedData.Target.Attributes;
		if (!ShouldTriggerCue(in effectData, in targetAttributes, CueTriggerRequirement.OnApply))
		{
			return;
		}

		foreach (CueData cueData in effectData.Cues)
		{
			int magnitude = CalculateMagnitude(in cueData, in effectEvaluatedData);

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

		foreach (TagContainer? cueTags in effectData.Cues.Select(x => x.CueTags))
		{
			if (cueTags is null)
			{
				continue;
			}

			foreach (Tag cueTag in cueTags)
			{
				RemoveCue(cueTag, effectEvaluatedData.Target, interrupted);
			}
		}
	}

	internal void ExecuteCues(in EffectEvaluatedData effectEvaluatedData, ReadOnlySpan<int> magnitudes)
	{
		CueData[] cues = effectEvaluatedData.Effect.EffectData.Cues;

		for (int i = 0; i < cues.Length; i++)
		{
			CueData cueData = cues[i];

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
					magnitudes[i],
					cueData.NormalizedMagnitude(magnitudes[i]),
					effectEvaluatedData.Effect.Ownership.Source,
					effectEvaluatedData.CustomCueParameters));
			}
		}
	}

	internal void UpdateCues(in EffectEvaluatedData effectEvaluatedData)
	{
		CueData[] cues = effectEvaluatedData.Effect.EffectData.Cues;
		Span<int> magnitudes = cues.Length <= MaxStackCueMagnitudes ? stackalloc int[cues.Length] : new int[cues.Length];

		if (TryCaptureCueMagnitudes(in effectEvaluatedData, CueTriggerRequirement.OnUpdate, magnitudes))
		{
			UpdateCues(in effectEvaluatedData, magnitudes);
		}
	}

	internal void UpdateCues(in EffectEvaluatedData effectEvaluatedData, ReadOnlySpan<int> magnitudes)
	{
		CueData[] cues = effectEvaluatedData.Effect.EffectData.Cues;

		for (int i = 0; i < cues.Length; i++)
		{
			CueData cueData = cues[i];

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
					magnitudes[i],
					cueData.NormalizedMagnitude(magnitudes[i]),
					effectEvaluatedData.Effect.Ownership.Source,
					effectEvaluatedData.CustomCueParameters));
			}
		}
	}

	private static bool ShouldTriggerCue(
		in EffectData effectData,
		in EntityAttributes attributes,
		CueTriggerRequirement triggerRequirements)
	{
		if (!effectData.RequireModifierSuccessToTriggerCue.HasFlag(triggerRequirements))
		{
			return true;
		}

		return attributes.Any(x => x.PendingValueChange != 0);
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
				Validation.Assert(
					cueData.MagnitudeAttribute is not null,
					"Cues with CueMagnitudeType.AttributeMagnitude must contains a configured MagnitudeAttribute.");

				if (!effectEvaluatedData.Target.Attributes.ContainsAttribute(cueData.MagnitudeAttribute))
				{
					return 0;
				}

				return effectEvaluatedData.Target.Attributes[cueData.MagnitudeAttribute].PendingValueChange;

			case CueMagnitudeType.AttributeBaseValue:
				Validation.Assert(
					cueData.MagnitudeAttribute is not null,
					"Cues with CueMagnitudeType.AttributeBaseValue must contains a configured MagnitudeAttribute.");

				if (!effectEvaluatedData.Target.Attributes.ContainsAttribute(cueData.MagnitudeAttribute))
				{
					return 0;
				}

				return effectEvaluatedData.Target.Attributes[cueData.MagnitudeAttribute].BaseValue;

			case CueMagnitudeType.AttributeCurrentValue:
				Validation.Assert(
					cueData.MagnitudeAttribute is not null,
					"Cues with CueMagnitudeType.AttributeMagnitude must contains a configured MagnitudeAttribute.");

				if (!effectEvaluatedData.Target.Attributes.ContainsAttribute(cueData.MagnitudeAttribute))
				{
					return 0;
				}

				return effectEvaluatedData.Target.Attributes[cueData.MagnitudeAttribute].CurrentValue;

			case CueMagnitudeType.AttributeModifier:
				Validation.Assert(
					cueData.MagnitudeAttribute is not null,
					"Cues with CueMagnitudeType.AttributeMagnitude must contains a configured MagnitudeAttribute.");

				if (!effectEvaluatedData.Target.Attributes.ContainsAttribute(cueData.MagnitudeAttribute))
				{
					return 0;
				}

				return effectEvaluatedData.Target.Attributes[cueData.MagnitudeAttribute].Modifier;

			case CueMagnitudeType.AttributeOverflow:
				Validation.Assert(
					cueData.MagnitudeAttribute is not null,
					"Cues with CueMagnitudeType.AttributeOverflow must contains a configured MagnitudeAttribute.");

				if (!effectEvaluatedData.Target.Attributes.ContainsAttribute(cueData.MagnitudeAttribute))
				{
					return 0;
				}

				return effectEvaluatedData.Target.Attributes[cueData.MagnitudeAttribute].Overflow;

			case CueMagnitudeType.AttributeValidModifier:
				Validation.Assert(
					cueData.MagnitudeAttribute is not null,
					"Cues with CueMagnitudeType.AttributeValidModifier must contains a configured MagnitudeAttribute.");

				if (!effectEvaluatedData.Target.Attributes.ContainsAttribute(cueData.MagnitudeAttribute))
				{
					return 0;
				}

				return effectEvaluatedData.Target.Attributes[cueData.MagnitudeAttribute].ValidModifier;

			case CueMagnitudeType.AttributeMin:
				Validation.Assert(
					cueData.MagnitudeAttribute is not null,
					"Cues with CueMagnitudeType.AttributeMin must contains a configured MagnitudeAttribute.");

				if (!effectEvaluatedData.Target.Attributes.ContainsAttribute(cueData.MagnitudeAttribute))
				{
					return 0;
				}

				return effectEvaluatedData.Target.Attributes[cueData.MagnitudeAttribute].Min;

			case CueMagnitudeType.AttributeMax:
				Validation.Assert(
					cueData.MagnitudeAttribute is not null,
					"Cues with CueMagnitudeType.AttributeMax must contains a configured MagnitudeAttribute.");

				if (!effectEvaluatedData.Target.Attributes.ContainsAttribute(cueData.MagnitudeAttribute))
				{
					return 0;
				}

				return effectEvaluatedData.Target.Attributes[cueData.MagnitudeAttribute].Max;

			case CueMagnitudeType.AttributeMagnitudeEvaluatedUpToChannel:
				Validation.Assert(
					cueData.MagnitudeAttribute is not null,
					"Cues with CueMagnitudeType.AttributeMagnitudeEvaluatedUpToChannel must contains a configured MagnitudeAttribute.");

				if (!effectEvaluatedData.Target.Attributes.ContainsAttribute(cueData.MagnitudeAttribute))
				{
					return 0;
				}

				return (int)effectEvaluatedData.Target.Attributes[cueData.MagnitudeAttribute]
					.CalculateMagnitudeUpToChannel(cueData.FinalChannel);
		}
	}
}
