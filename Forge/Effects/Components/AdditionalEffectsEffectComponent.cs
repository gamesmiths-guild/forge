// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Effect component that applies further effects off the back of its own: <paramref name="onApplication"/> when the
/// effect lands, and the <c>onComplete</c> sets when it ends.
/// </summary>
/// <remarks>
/// <para>
/// Each entry in <paramref name="onApplication"/> is a <see cref="ConditionalEffect"/>, so it can be gated on the
/// source's tags, pointed at an entity other than the target, and taken back when its applier ends. Pointing one at
/// <see cref="EffectApplicationTarget.Source"/> is what expresses lifesteal, recoil, and thorns without writing a
/// <see cref="Calculator.CustomExecution"/>.
/// </para>
/// <para>
/// Applied effects inherit their applier's ownership and evaluated level, so they credit the same source and land at
/// the same power. Set <paramref name="copyDataFromOriginalEffect"/> to also carry over the applier's
/// <see cref="Magnitudes.SetByCallerFloat"/> magnitudes, for a child keyed on the same values the caller set on the
/// parent. Unlike Unreal's equivalent, the flag governs the <c>onComplete</c> sets too rather than only the
/// application ones.
/// </para>
/// <para>
/// Application effects fire from <see cref="OnEffectApplied"/>, which runs <i>before</i> the applier's own modifiers
/// execute and fires again for each successfully applied stack. A child reading an attribute the parent modifies
/// therefore sees the value from before the parent touched it.
/// </para>
/// <para>
/// Nothing stops two effects from applying each other. <see cref="EffectsManager"/> cuts a cascade off once it nests
/// too deeply and asserts, but a cycle is a configuration bug rather than a supported pattern — gate one of the
/// applications on a tag the other grants.
/// </para>
/// <para>
/// This component maintains per-effect-instance state (the handles of the effects it has to take back on removal).
/// When used in <see cref="EffectData"/>, each effect application will create its own instance via
/// <see cref="CreateInstance"/> to isolate state between different effect applications.
/// </para>
/// </remarks>
/// <param name="onApplication">The effects to apply when this effect is applied, each with its own condition, target,
/// and removal policy.</param>
/// <param name="onCompleteAlways">The effects to apply when this effect is removed, however it ended.</param>
/// <param name="onCompleteNormal">The effects to apply when this effect ends by running out of duration.</param>
/// <param name="onCompletePrematurely">The effects to apply when this effect is taken away before it could expire.
/// </param>
/// <param name="copyDataFromOriginalEffect">Whether the applied effects inherit this effect's
/// <see cref="Magnitudes.SetByCallerFloat"/> magnitudes.</param>
public class AdditionalEffectsEffectComponent(
	ConditionalEffect[]? onApplication = null,
	EffectData[]? onCompleteAlways = null,
	EffectData[]? onCompleteNormal = null,
	EffectData[]? onCompletePrematurely = null,
	bool copyDataFromOriginalEffect = false) : IEffectComponent
{
	private readonly List<RemoveOnEndEffect> _removeOnEndEffects = [];

	internal ConditionalEffect[] OnApplication { get; } = onApplication ?? [];

	internal EffectData[] OnCompleteAlways { get; } = onCompleteAlways ?? [];

	internal EffectData[] OnCompleteNormal { get; } = onCompleteNormal ?? [];

	internal EffectData[] OnCompletePrematurely { get; } = onCompletePrematurely ?? [];

	internal bool CopyDataFromOriginalEffect { get; } = copyDataFromOriginalEffect;

	// Completion effects hang off OnActiveEffectUnapplied, which an instant effect never reaches. EffectData rejects
	// the combination.
	internal bool HasCompletionEffects => OnCompleteAlways.Length > 0
		|| OnCompleteNormal.Length > 0
		|| OnCompletePrematurely.Length > 0;

	// RemoveOnEnd needs an owner with an end to hook, which an instant effect has not. EffectData rejects it.
	internal bool HasRemoveOnEndEffect => Array.Exists(
		OnApplication,
		x => x.RemovalPolicy == ConditionalEffectRemovalPolicy.RemoveOnEnd);

	// ...and an applied effect still around when that end arrives, which an instant one is not. EffectData rejects it.
	internal bool HasInstantRemoveOnEndEffect => Array.Exists(
		OnApplication,
		x => x.RemovalPolicy == ConditionalEffectRemovalPolicy.RemoveOnEnd
			&& x.EffectData.DurationData.DurationType == DurationType.Instant);

	/// <inheritdoc/>
	public IEffectComponent CreateInstance()
	{
		// Create a new instance for each effect application to isolate the handles it has to take back on removal.
		return new AdditionalEffectsEffectComponent(
			OnApplication,
			OnCompleteAlways,
			OnCompleteNormal,
			OnCompletePrematurely,
			CopyDataFromOriginalEffect);
	}

	/// <inheritdoc/>
	public void OnEffectApplied(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
	{
		Effect parentEffect = effectEvaluatedData.Effect;

		foreach (ConditionalEffect conditionalEffect in OnApplication)
		{
			if (!SourceRequirementsMet(conditionalEffect, parentEffect))
			{
				continue;
			}

			IForgeEntity? appliedTo = ResolveTarget(conditionalEffect.Target, target, parentEffect.Ownership);

			// A conditional pointed at an ownership entity the effect doesn't have — thorns on an effect with no
			// source — has nowhere to land and is skipped rather than redirected back at the target.
			if (appliedTo is null)
			{
				continue;
			}

			ActiveEffectHandle? handle = appliedTo.EffectsManager.ApplyEffect(
				BuildEffect(conditionalEffect.EffectData, parentEffect, effectEvaluatedData.Level));

			// Instant effects never become active and denied applications return nothing; neither leaves anything to
			// take back later.
			if (handle is not null && conditionalEffect.RemovalPolicy == ConditionalEffectRemovalPolicy.RemoveOnEnd)
			{
				_removeOnEndEffects.Add(new RemoveOnEndEffect(handle, conditionalEffect.StacksToRemove));
			}
		}
	}

	/// <inheritdoc/>
	public void OnActiveEffectUnapplied(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData,
		bool removed,
		EffectRemovalReason reason)
	{
		// Losing a stack is not an ending: the effect is still there, and so is everything it applied.
		if (!removed)
		{
			return;
		}

		EffectEvaluatedData effectEvaluatedData = activeEffectEvaluatedData.EffectEvaluatedData;
		Effect parentEffect = effectEvaluatedData.Effect;

		ApplyCompletionEffects(OnCompleteAlways, target, parentEffect, effectEvaluatedData.Level);

		ApplyCompletionEffects(
			reason == EffectRemovalReason.Expired ? OnCompleteNormal : OnCompletePrematurely,
			target,
			parentEffect,
			effectEvaluatedData.Level);

		// The aftermath lands before the clean-up, so a completion effect can replace something the effect was keeping
		// alive without the removal pass taking its replacement away.
		RemoveTrackedEffects();
	}

	private static IForgeEntity? ResolveTarget(
		EffectApplicationTarget applicationTarget,
		IForgeEntity target,
		in EffectOwnership ownership)
	{
		return applicationTarget switch
		{
			EffectApplicationTarget.Source => ownership.Source,
			EffectApplicationTarget.Owner => ownership.Owner,
			_ => target,
		};
	}

	private static bool SourceRequirementsMet(in ConditionalEffect conditionalEffect, Effect parentEffect)
	{
		if (conditionalEffect.SourceTagRequirements?.IsEmpty != false)
		{
			return true;
		}

		TagContainer? sourceTags = parentEffect.ResolveSourceTags();

		// An effect with no tag context anywhere cannot satisfy requirements it has no way to evaluate.
		return sourceTags is not null && conditionalEffect.SourceTagRequirements.Value.RequirementsMet(sourceTags);
	}

	private Effect BuildEffect(EffectData effectData, Effect parentEffect, int level)
	{
		// Ownership and level always carry over — the applied effect is this one's doing, at this one's power. Only the
		// SetByCaller magnitudes are opt-in, since most applied effects have no use for the caller's data.
		return CopyDataFromOriginalEffect
			? Effect.CreateLinkedEffect(effectData, parentEffect, level)
			: new Effect(effectData, parentEffect.Ownership, level);
	}

	private void ApplyCompletionEffects(
		EffectData[] completionEffects,
		IForgeEntity target,
		Effect parentEffect,
		int level)
	{
		foreach (EffectData completionEffect in completionEffects)
		{
			target.EffectsManager.ApplyEffect(BuildEffect(completionEffect, parentEffect, level));
		}
	}

	private void RemoveTrackedEffects()
	{
		// Snapshotting is required: removal callbacks can apply or remove further effects.
		RemoveOnEndEffect[] trackedEffects = [.. _removeOnEndEffects];
		_removeOnEndEffects.Clear();

		foreach (RemoveOnEndEffect trackedEffect in trackedEffects)
		{
			// The applied effect can have expired, been dispelled, or been taken by an earlier entry of this same pass
			// pointing at the same application.
			if (!trackedEffect.Handle.IsValid)
			{
				continue;
			}

			trackedEffect.Handle.Target?.EffectsManager.RemoveEffect(
				trackedEffect.Handle,
				trackedEffect.StacksToRemove);
		}
	}

	/// <summary>
	/// One effect this component applied and has to take back when its own effect ends.
	/// </summary>
	/// <param name="Handle">The handle of the applied effect.</param>
	/// <param name="StacksToRemove">How many of its stacks to take, from the
	/// <see cref="ConditionalEffect"/> that applied it.</param>
	private readonly record struct RemoveOnEndEffect(ActiveEffectHandle Handle, int StacksToRemove);
}
