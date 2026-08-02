// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// One effect a component applies off the back of its own, together with the condition gating it, where it lands, and
/// what becomes of it afterwards.
/// </summary>
/// <remarks>
/// <para>
/// This is the shared vocabulary for "an effect some other effect causes".
/// <see cref="AdditionalEffectsEffectComponent"/> applies these when its effect lands and when it ends;
/// <see cref="StackThresholdEffectComponent"/> applies one when its stack count reaches a threshold. Learning it once
/// covers both.
/// </para>
/// <para>
/// The child always inherits its applier's ownership and evaluated level. Whether it also inherits the applier's
/// <see cref="Magnitudes.SetByCallerFloat"/> magnitudes is decided once, for the whole component, by
/// <c>copyDataFromOriginalEffect</c>.
/// </para>
/// <para>
/// <paramref name="RemovalPolicy"/> asks whether this effect outlives the thing that caused it, and each component
/// decides what "the end" means for it: for <see cref="AdditionalEffectsEffectComponent"/> it is the applier being
/// removed, and for <see cref="StackThresholdEffectComponent"/> it is the stack count falling back below the threshold
/// — the end of the condition the effect hangs off, in both cases. <paramref name="StacksToRemove"/> only means
/// anything under <see cref="ConditionalEffectRemovalPolicy.RemoveOnEnd"/>.
/// </para>
/// <para>
/// A completion effect of <see cref="AdditionalEffectsEffectComponent"/> is the one place the policy has no meaning at
/// all, since such an effect is applied as its applier ends and there is no later end at which to take it back;
/// <see cref="EffectData"/> rejects one asking for <see cref="ConditionalEffectRemovalPolicy.RemoveOnEnd"/> rather than
/// letting it read as configured.
/// </para>
/// </remarks>
/// <param name="EffectData">The effect to apply.</param>
/// <param name="SourceTagRequirements">Requirements the effect's source must meet for this one to be applied. When
/// <see langword="null"/> or empty, the effect is always applied.</param>
/// <param name="RemovalPolicy">Whether this effect outlives the one that applied it.</param>
/// <param name="StacksToRemove">How many stacks to take when the applier ends under
/// <see cref="ConditionalEffectRemovalPolicy.RemoveOnEnd"/>. Any negative value, the default, removes the effect
/// entirely regardless of its stack count.</param>
/// <param name="Target">Which entity receives the effect.</param>
public readonly record struct ConditionalEffect(
	EffectData EffectData,
	TagRequirements? SourceTagRequirements = null,
	ConditionalEffectRemovalPolicy RemovalPolicy = ConditionalEffectRemovalPolicy.Ignore,
	int StacksToRemove = -1,
	EffectApplicationTarget Target = EffectApplicationTarget.Target)
{
	/// <summary>
	/// Gets a value indicating whether this effect is taken back when the condition that applied it ends.
	/// </summary>
	internal bool IsTakenBack => RemovalPolicy == ConditionalEffectRemovalPolicy.RemoveOnEnd;

	/// <summary>
	/// Evaluates this entry's condition and, if it passes, builds and applies the effect.
	/// </summary>
	/// <remarks>
	/// Returns <see langword="null"/> when the condition failed, when the effect names an ownership entity the applier
	/// does not have, or when the application itself was refused. An <see cref="Duration.DurationType.Instant"/> effect
	/// also returns nothing, since it never becomes active. None of those leave anything to take back later.
	/// </remarks>
	/// <param name="target">The entity carrying the applying effect.</param>
	/// <param name="parentEffect">The effect applying this one.</param>
	/// <param name="level">The level to apply at, usually the parent's evaluated level.</param>
	/// <param name="copyDataFromOriginalEffect">Whether the applied effect inherits the parent's
	/// <see cref="Magnitudes.SetByCallerFloat"/> magnitudes.</param>
	/// <returns>The handle of the applied effect, or <see langword="null"/> when nothing was applied.</returns>
	internal ActiveEffectHandle? TryApply(
		IForgeEntity target,
		Effect parentEffect,
		int level,
		bool copyDataFromOriginalEffect)
	{
		if (!SourceRequirementsMet(parentEffect))
		{
			return null;
		}

		IForgeEntity? appliedTo = Target.Resolve(target, parentEffect.Ownership);

		// An entry pointed at an ownership entity the effect doesn't have — thorns on an effect with no source — has
		// nowhere to land and is skipped rather than redirected back at the target.
		if (appliedTo is null)
		{
			return null;
		}

		// Ownership and level always carry over — the applied effect is the parent's doing, at the parent's power. Only
		// the SetByCaller magnitudes are opt-in, since most applied effects have no use for the caller's data.
		Effect effect = copyDataFromOriginalEffect
			? Effect.CreateLinkedEffect(EffectData, parentEffect, level)
			: new Effect(EffectData, parentEffect.Ownership, level);

		return appliedTo.EffectsManager.ApplyEffect(effect);
	}

	private bool SourceRequirementsMet(Effect parentEffect)
	{
		if (SourceTagRequirements?.IsEmpty != false)
		{
			return true;
		}

		TagContainer? sourceTags = parentEffect.ResolveSourceTags();

		// An effect with no tag context anywhere cannot satisfy requirements it has no way to evaluate.
		return sourceTags is not null && SourceTagRequirements.Value.RequirementsMet(sourceTags);
	}
}
