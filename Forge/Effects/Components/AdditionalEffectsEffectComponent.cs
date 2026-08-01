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
/// Every entry, on either side, is a <see cref="ConditionalEffect"/>: it can be gated on the source's tags and pointed
/// at an entity other than the target. Pointing one at <see cref="EffectApplicationTarget.Source"/> is what expresses
/// lifesteal, recoil, thorns, and "the curse pays its caster back when it ends" without writing a
/// <see cref="Calculator.CustomExecution"/>. Only <see cref="ConditionalEffectRemovalPolicy.RemoveOnEnd"/> is specific
/// to the application side, because only application effects have a later end at which they can be taken back.
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
	ConditionalEffect[]? onCompleteAlways = null,
	ConditionalEffect[]? onCompleteNormal = null,
	ConditionalEffect[]? onCompletePrematurely = null,
	bool copyDataFromOriginalEffect = false) : IEffectComponent
{
	private readonly List<RemoveOnEndEffect> _removeOnEndEffects = [];

	internal ConditionalEffect[] OnApplication { get; } = onApplication ?? [];

	internal ConditionalEffect[] OnCompleteAlways { get; } = onCompleteAlways ?? [];

	internal ConditionalEffect[] OnCompleteNormal { get; } = onCompleteNormal ?? [];

	internal ConditionalEffect[] OnCompletePrematurely { get; } = onCompletePrematurely ?? [];

	internal bool CopyDataFromOriginalEffect { get; } = copyDataFromOriginalEffect;

	// Completion effects hang off OnActiveEffectUnapplied, which an instant effect never reaches. EffectData rejects
	// the combination.
	internal bool HasCompletionEffects => OnCompleteAlways.Length > 0
		|| OnCompleteNormal.Length > 0
		|| OnCompletePrematurely.Length > 0;

	// RemoveOnEnd needs an owner with an end to hook, which an instant effect has not. EffectData rejects it.
	internal bool HasRemoveOnEndEffect => HasRemoveOnEnd(OnApplication);

	// ...and an applied effect still around when that end arrives, which an instant one is not. EffectData rejects it.
	internal bool HasInstantRemoveOnEndEffect => Array.Exists(
		OnApplication,
		x => x.RemovalPolicy == ConditionalEffectRemovalPolicy.RemoveOnEnd
			&& x.EffectData.DurationData.DurationType == DurationType.Instant);

	// A completion effect is applied as its applier ends, so there is no later end at which to take it back. The
	// removal policy is meaningless on one and EffectData rejects it rather than letting it read as configured.
	internal bool HasRemoveOnEndCompletionEffect => HasRemoveOnEnd(OnCompleteAlways)
		|| HasRemoveOnEnd(OnCompleteNormal)
		|| HasRemoveOnEnd(OnCompletePrematurely);

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
			ActiveEffectHandle? handle = ApplyConditionalEffect(
				conditionalEffect,
				target,
				parentEffect,
				effectEvaluatedData.Level);

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

	private static bool HasRemoveOnEnd(ConditionalEffect[] conditionalEffects)
	{
		return Array.Exists(
			conditionalEffects,
			x => x.RemovalPolicy == ConditionalEffectRemovalPolicy.RemoveOnEnd);
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

	private ActiveEffectHandle? ApplyConditionalEffect(
		in ConditionalEffect conditionalEffect,
		IForgeEntity target,
		Effect parentEffect,
		int level)
	{
		if (!SourceRequirementsMet(conditionalEffect, parentEffect))
		{
			return null;
		}

		IForgeEntity? appliedTo = conditionalEffect.Target.Resolve(target, parentEffect.Ownership);

		// A conditional pointed at an ownership entity the effect doesn't have — thorns on an effect with no source —
		// has nowhere to land and is skipped rather than redirected back at the target.
		if (appliedTo is null)
		{
			return null;
		}

		return appliedTo.EffectsManager.ApplyEffect(
			BuildEffect(conditionalEffect.EffectData, parentEffect, level));
	}

	private void ApplyCompletionEffects(
		ConditionalEffect[] completionEffects,
		IForgeEntity target,
		Effect parentEffect,
		int level)
	{
		foreach (ConditionalEffect completionEffect in completionEffects)
		{
			// The removal policy is meaningless here — the effect that would take these back is the one ending — so a
			// completion effect is never tracked. EffectData rejects one that asks for it.
			ApplyConditionalEffect(completionEffect, target, parentEffect, level);
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
