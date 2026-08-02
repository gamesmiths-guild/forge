// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Duration;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Effect component that applies further effects once its own stack count reaches <paramref name="threshold"/> — five
/// stacks of Bleed causing a Hemorrhage — and, for each entry that asks to be taken back, removes it again as the count
/// drops below.
/// </summary>
/// <remarks>
/// <para>
/// This leans on Forge's stacking model, which is substantially richer than a plain counter: the threshold is read
/// from the effect's evaluated stack count, so every stacking policy — aggregation by source or target, level
/// segregation, overflow, expiration — decides what the count is, and this component only decides what happens once it
/// gets there.
/// </para>
/// <para>
/// Every entry is a <see cref="ConditionalEffect"/>, the same shape <see cref="AdditionalEffectsEffectComponent"/>
/// uses, so a threshold effect can be gated on the source's tags and pointed at an entity other than the target without
/// this component inventing its own vocabulary for it. Entries are evaluated in order, each independently, and each
/// keeps its own <see cref="ConditionalEffect.RemovalPolicy"/>, which is what decides the two useful shapes:
/// </para>
/// <list type="bullet">
/// <item>
/// <see cref="ConditionalEffectRemovalPolicy.RemoveOnEnd"/> ties the effect to the condition — applied while the count
/// is at or above the threshold, taken back as soon as it drops below or this effect ends, and applied again if the
/// count climbs back. A Hemorrhage that subsides as the stacks do.
/// </item>
/// <item>
/// <see cref="ConditionalEffectRemovalPolicy.Ignore"/> makes the crossing a trigger rather than a sustaining
/// condition — applied the first time the count reaches the threshold and then left alone, never removed here and
/// never applied a second time. It lives by its own duration.
/// </item>
/// </list>
/// <para>
/// <b>The threshold watches the stack count alone.</b> Inhibiting the effect does not take the threshold effects back,
/// which keeps the behavior identical for periodic and non-periodic owners — a periodic effect does not report its
/// inhibition changes through <see cref="OnActiveEffectChanged"/>, so an inhibition-aware threshold would work on some
/// owners and silently not on others. Gate on inhibition through the threshold effects' own requirements when it
/// matters.
/// </para>
/// <para>
/// Use <see cref="AdditionalEffectsEffectComponent"/> instead when the further effects should fire on <i>every</i>
/// application rather than at a count: its application effects fire once per successfully applied stack.
/// </para>
/// <para>
/// This component maintains per-effect-instance state (the handles of the effects it applied). When used in
/// <see cref="EffectData"/>, each effect application will create its own instance via <see cref="CreateInstance"/> to
/// isolate state between different effect applications.
/// </para>
/// </remarks>
/// <param name="threshold">The stack count at which the threshold effects are applied.</param>
/// <param name="thresholdEffects">The effects to apply once the threshold is reached, each with its own condition,
/// target, and removal policy.</param>
/// <param name="copyDataFromOriginalEffect">Whether the threshold effects inherit this effect's
/// <see cref="Magnitudes.SetByCallerFloat"/> magnitudes.</param>
public class StackThresholdEffectComponent(
	int threshold,
	ConditionalEffect[] thresholdEffects,
	bool copyDataFromOriginalEffect = false) : IEffectComponent
{
	private readonly ActiveEffectHandle?[] _appliedHandles = new ActiveEffectHandle?[thresholdEffects.Length];

	private readonly bool[] _hasFired = new bool[thresholdEffects.Length];

	internal int Threshold { get; } = threshold;

	internal ConditionalEffect[] ThresholdEffects { get; } = thresholdEffects;

	internal bool CopyDataFromOriginalEffect { get; } = copyDataFromOriginalEffect;

	internal bool HasUnreachableLowThreshold => Threshold <= 1;

	internal bool HasInstantSustainedEffect => Array.Exists(
		ThresholdEffects,
		x => x.IsTakenBack && x.EffectData.DurationData.DurationType == DurationType.Instant);

	/// <inheritdoc/>
	public IEffectComponent CreateInstance()
	{
		// Create a new instance for each effect application to isolate the handles of what it applied.
		return new StackThresholdEffectComponent(Threshold, ThresholdEffects, CopyDataFromOriginalEffect);
	}

	/// <inheritdoc/>
	public void OnPostActiveEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		// An effect whose initial stack count already meets the threshold has crossed it on arrival. Evaluated here
		// rather than in OnActiveEffectAdded so the effect is fully applied before anything is hung off it.
		Evaluate(target, in activeEffectEvaluatedData);
	}

	/// <inheritdoc/>
	public void OnActiveEffectChanged(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		Evaluate(target, in activeEffectEvaluatedData);
	}

	/// <inheritdoc/>
	public void OnActiveEffectUnapplied(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData,
		bool removed,
		EffectRemovalReason reason)
	{
		// Losing a stack is handled by OnActiveEffectChanged, which sees the new count. Only a full removal ends the
		// condition outright.
		if (removed)
		{
			RemoveTrackedEffects();
		}
	}

	private void Evaluate(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		EffectEvaluatedData effectEvaluatedData = activeEffectEvaluatedData.EffectEvaluatedData;

		if (effectEvaluatedData.Stack < Threshold)
		{
			RemoveTrackedEffects();
			return;
		}

		for (int i = 0; i < ThresholdEffects.Length; i++)
		{
			ConditionalEffect thresholdEffect = ThresholdEffects[i];

			if (thresholdEffect.IsTakenBack)
			{
				// Already standing. An entry whose effect ran out of its own duration while the count stayed high is
				// re-applied here, since the condition it hangs off never stopped holding.
				if (_appliedHandles[i]?.IsValid == true)
				{
					continue;
				}
			}
			else if (_hasFired[i])
			{
				// Fires on the first crossing and never again, even if the count dips below and climbs back, since
				// re-applying would pile up duplicates nothing would clean away.
				continue;
			}

			// Marked before the application rather than after, so an entry the target refused is not retried on every
			// subsequent change.
			_hasFired[i] = true;
			_appliedHandles[i] = thresholdEffect.TryApply(
				target,
				effectEvaluatedData.Effect,
				effectEvaluatedData.Level,
				CopyDataFromOriginalEffect);
		}
	}

	private void RemoveTrackedEffects()
	{
		for (int i = 0; i < _appliedHandles.Length; i++)
		{
			if (!ThresholdEffects[i].IsTakenBack)
			{
				continue;
			}

			ActiveEffectHandle? handle = _appliedHandles[i];

			// Cleared before the removal rather than after: removal callbacks can apply or remove further effects, and
			// one that comes back through here must not find the same handle waiting a second time.
			_appliedHandles[i] = null;

			// The applied effect can have expired or been dispelled since, and a refused or instant one left no handle.
			if (handle?.IsValid != true)
			{
				continue;
			}

			handle.Target?.EffectsManager.RemoveEffect(handle, ThresholdEffects[i].StacksToRemove);
		}
	}
}
