// Copyright © Gamesmiths Guild.

using System.Runtime.ExceptionServices;
using Gamesmiths.Forge.Attributes;
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

	private enum PendingCueKind : byte
	{
		Execute = 0,
		Apply = 1,
		Remove = 2,
		Update = 3,
	}

	private readonly Dictionary<Tag, HashSet<ICueHandler>> _registeredCues = [];

	private readonly List<PendingCue> _pendingCues = [];

	private readonly List<PendingCue> _dispatchBuffer = [];

	/// <summary>
	/// Gets or sets a value indicating whether cue handlers wait for the presentation pass, <see cref="PresentCues"/>,
	/// instead of being called the moment a cue fires. Off by default.
	/// </summary>
	/// <remarks>
	/// Deferred handlers see only net change: a cue applied and removed again before the pass is never shown, along
	/// with any update sent to it in between. Everything else reaches them in the order it happened. A host that
	/// presents once per frame turns this on, so a cue that comes and goes within one frame never flickers.
	/// </remarks>
	public bool DeferDispatch { get; set; }

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

		if (DeferDispatch)
		{
			_pendingCues.Add(new PendingCue(PendingCueKind.Execute, cueTag, target, parameters, false));
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

		if (DeferDispatch)
		{
			_pendingCues.Add(new PendingCue(PendingCueKind.Apply, cueTag, target, parameters, false));
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

		if (DeferDispatch)
		{
			if (!CancelPendingApply(cueTag, target))
			{
				_pendingCues.Add(new PendingCue(PendingCueKind.Remove, cueTag, target, null, interrupted));
			}

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

		if (DeferDispatch)
		{
			_pendingCues.Add(new PendingCue(PendingCueKind.Update, cueTag, target, parameters, false));
			return;
		}

		foreach (ICueHandler cue in cues)
		{
			cue.OnUpdate(target, parameters);
		}
	}

	/// <summary>
	/// Runs the presentation pass for deferred cues (<see cref="DeferDispatch"/>): calls the handlers of every cue
	/// fired since the last pass, in the order they fired. Call this once at the end of each frame, after the frame's
	/// updates.
	/// </summary>
	/// <remarks>
	/// Cues fired by the handlers it calls wait for the next pass. A handler that throws keeps neither the other
	/// handlers from running nor the rest of the cues from being dispatched, and the first exception is rethrown after.
	/// </remarks>
	public void PresentCues()
	{
		_dispatchBuffer.Clear();
		_dispatchBuffer.AddRange(_pendingCues);
		_pendingCues.Clear();

		ExceptionDispatchInfo? failure = null;

		foreach (PendingCue pending in _dispatchBuffer)
		{
			if (!_registeredCues.TryGetValue(pending.CueTag, out HashSet<ICueHandler>? cues))
			{
				continue;
			}

			foreach (ICueHandler cue in cues)
			{
				try
				{
					Dispatch(cue, pending);
				}
				catch (Exception exception)
				{
					failure ??= ExceptionDispatchInfo.Capture(exception);
				}
			}
		}

		failure?.Throw();
	}

	/// <summary>
	/// Decides whether an effect's cues fire for a trigger and, when they do, reads the magnitude of each one into
	/// <paramref name="magnitudes"/>, one slot per entry of the effect's cues.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The read and the dispatch are separate on purpose: every cue of an operation is read at once, right after the
	/// operation's own writes, and the handlers run later with those values. Reading at dispatch instead would let a
	/// hook, or an earlier handler of the same operation, move a value before a later cue looks at it, so the cues of
	/// one operation would not describe one state.
	/// </para>
	/// <para>
	/// An attribute's change comes from the <see cref="AttributeChangeSet"/> the operation tallied its writes into,
	/// never from the entity-wide pending values: those accumulate across every operation until the next flush, so a
	/// nested one — a raised event activating an ability that applies its own effects while a hit is still landing —
	/// would read the hit's deltas as its own.
	/// </para>
	/// </remarks>
	/// <param name="effectEvaluatedData">The evaluated data of the effect whose cues are being read.</param>
	/// <param name="triggerRequirement">The trigger the cues are being read for.</param>
	/// <param name="changes">The changes the operation made to the target's attributes, or <see langword="null"/>
	/// when it made none.</param>
	/// <param name="magnitudes">Receives one magnitude per cue; at least as long as the effect's cues.</param>
	/// <returns><see langword="true"/> if the cues fire and <paramref name="magnitudes"/> was filled; otherwise,
	/// <see langword="false"/>.</returns>
	internal static bool TryCaptureCueMagnitudes(
		in EffectEvaluatedData effectEvaluatedData,
		CueTriggerRequirement triggerRequirement,
		AttributeChangeSet? changes,
		Span<int> magnitudes)
	{
		EffectData effectData = effectEvaluatedData.Effect.EffectData;

		if (!ShouldTriggerCue(in effectData, changes, triggerRequirement))
		{
			return false;
		}

		for (int i = 0; i < effectData.Cues.Length; i++)
		{
			magnitudes[i] = CalculateMagnitude(in effectData.Cues[i], in effectEvaluatedData, changes);
		}

		return true;
	}

	/// <summary>
	/// Applies the cues of an effect that just landed on its target. Nothing runs between an application and its
	/// cues, so they are read and dispatched here in one go.
	/// </summary>
	/// <param name="effectEvaluatedData">The evaluated data of the applied effect.</param>
	/// <param name="changes">The changes the application made to the target's attributes.</param>
	internal void ApplyCues(in EffectEvaluatedData effectEvaluatedData, AttributeChangeSet changes)
	{
		CueData[] cues = effectEvaluatedData.Effect.EffectData.Cues;
		Span<int> magnitudes = cues.Length <= MaxStackCueMagnitudes
			? stackalloc int[cues.Length]
			: new int[cues.Length];

		if (!TryCaptureCueMagnitudes(in effectEvaluatedData, CueTriggerRequirement.OnApply, changes, magnitudes))
		{
			return;
		}

		for (int i = 0; i < cues.Length; i++)
		{
			CueData cueData = cues[i];

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
					magnitudes[i],
					cueData.NormalizedMagnitude(magnitudes[i]),
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

	/// <summary>
	/// Executes the cues of an effect that just executed on its target, with the magnitudes
	/// <see cref="TryCaptureCueMagnitudes"/> read before the execution's hooks ran.
	/// </summary>
	/// <param name="effectEvaluatedData">The evaluated data of the executed effect.</param>
	/// <param name="magnitudes">The captured magnitude of each cue.</param>
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

	/// <summary>
	/// Updates the cues of an active effect whose change ran no hooks, reading and dispatching them in one go.
	/// </summary>
	/// <param name="effectEvaluatedData">The evaluated data of the changed effect.</param>
	/// <param name="changes">The changes the operation made to the target's attributes, or <see langword="null"/>
	/// when it made none.</param>
	internal void UpdateCues(in EffectEvaluatedData effectEvaluatedData, AttributeChangeSet? changes)
	{
		CueData[] cues = effectEvaluatedData.Effect.EffectData.Cues;
		Span<int> magnitudes = cues.Length <= MaxStackCueMagnitudes
			? stackalloc int[cues.Length]
			: new int[cues.Length];

		if (TryCaptureCueMagnitudes(in effectEvaluatedData, CueTriggerRequirement.OnUpdate, changes, magnitudes))
		{
			UpdateCues(in effectEvaluatedData, magnitudes);
		}
	}

	/// <summary>
	/// Updates the cues of an active effect that just changed on its target, with the magnitudes
	/// <see cref="TryCaptureCueMagnitudes"/> read before the change's hooks ran.
	/// </summary>
	/// <param name="effectEvaluatedData">The evaluated data of the changed effect.</param>
	/// <param name="magnitudes">The captured magnitude of each cue.</param>
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

	private static void Dispatch(ICueHandler cue, in PendingCue pending)
	{
		switch (pending.Kind)
		{
			case PendingCueKind.Execute:
				cue.OnExecute(pending.Target, pending.Parameters);
				break;

			case PendingCueKind.Apply:
				cue.OnApply(pending.Target, pending.Parameters);
				break;

			case PendingCueKind.Remove:
				cue.OnRemove(pending.Target, pending.Interrupted);
				break;

			case PendingCueKind.Update:
				cue.OnUpdate(pending.Target, pending.Parameters);
				break;
		}
	}

	private static bool ShouldTriggerCue(
		in EffectData effectData,
		AttributeChangeSet? changes,
		CueTriggerRequirement triggerRequirements)
	{
		if (!effectData.RequireModifierSuccessToTriggerCue.HasFlag(triggerRequirements))
		{
			return true;
		}

		return changes?.HasChanges == true;
	}

	private static int CalculateMagnitude(
		in CueData cueData,
		in EffectEvaluatedData effectEvaluatedData,
		AttributeChangeSet? changes)
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

				return changes?.DeltaOf(cueData.MagnitudeAttribute) ?? 0;

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
					"Cues with CueMagnitudeType.AttributeMagnitudeEvaluatedUpToChannel must contains a configured " +
					"MagnitudeAttribute.");

				if (!effectEvaluatedData.Target.Attributes.ContainsAttribute(cueData.MagnitudeAttribute))
				{
					return 0;
				}

				return (int)effectEvaluatedData.Target.Attributes[cueData.MagnitudeAttribute]
					.CalculateMagnitudeUpToChannel(cueData.FinalChannel);
		}
	}

	// The latest persistent event pending for the cue on the target decides. An apply was never shown, so it goes, with
	// the updates sent to it since, and the removal with it; after a removal, this removal stands.
	private bool CancelPendingApply(Tag cueTag, IForgeEntity? target)
	{
		for (int i = _pendingCues.Count - 1; i >= 0; i--)
		{
			PendingCue pending = _pendingCues[i];

			if (pending.CueTag != cueTag || pending.Target != target || pending.Kind == PendingCueKind.Execute)
			{
				continue;
			}

			if (pending.Kind == PendingCueKind.Remove)
			{
				return false;
			}

			if (pending.Kind == PendingCueKind.Apply)
			{
				for (int j = _pendingCues.Count - 1; j > i; j--)
				{
					PendingCue later = _pendingCues[j];

					if (later.Kind == PendingCueKind.Update && later.CueTag == cueTag && later.Target == target)
					{
						_pendingCues.RemoveAt(j);
					}
				}

				_pendingCues.RemoveAt(i);
				return true;
			}
		}

		return false;
	}

	private readonly record struct PendingCue(
		PendingCueKind Kind,
		Tag CueTag,
		IForgeEntity? Target,
		CueParameters? Parameters,
		bool Interrupted);
}
