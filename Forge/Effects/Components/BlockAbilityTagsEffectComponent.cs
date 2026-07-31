// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Effect component for blocking the target's abilities while the effect is active. Abilities carrying any of the
/// configured tags fail activation with
/// <see cref="Abilities.AbilityActivationFailures.BlockedByTags"/>, and are unblocked once the effect is removed.
/// </summary>
/// <remarks>
/// <para>
/// Only effects with duration can block abilities.
/// </para>
/// <para>
/// Unlike <see cref="ModifierTagsEffectComponent"/>, blocking respects inhibition: an effect that lands inhibited
/// blocks nothing, and blocks are dropped and restored as the effect's inhibition state flips. An inhibited stun
/// should not still lock abilities.
/// </para>
/// <para>
/// This component maintains per-effect-instance state (whether its blocks are currently applied).
/// When used in <see cref="EffectData"/>, each effect application will create its own instance via
/// <see cref="CreateInstance"/> to isolate state between different effect applications.
/// </para>
/// </remarks>
/// <param name="tagsToBlock">Which tags to block from being activated.</param>
public class BlockAbilityTagsEffectComponent(TagContainer tagsToBlock) : IEffectComponent
{
	private bool _isBlocking;

	internal TagContainer TagsToBlock { get; } = tagsToBlock;

	/// <inheritdoc/>
	public IEffectComponent CreateInstance()
	{
		// Create a new instance for each effect application to isolate blocking state.
		return new BlockAbilityTagsEffectComponent(TagsToBlock);
	}

	/// <inheritdoc/>
	public void OnPostActiveEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		// Inhibition is only settled after every component has processed OnActiveEffectAdded, so the initial block is
		// decided here instead of there.
		UpdateBlocking(target, !activeEffectEvaluatedData.ActiveEffectHandle.IsInhibited);
	}

	/// <inheritdoc/>
	public void OnActiveEffectChanged(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		UpdateBlocking(target, !activeEffectEvaluatedData.ActiveEffectHandle.IsInhibited);
	}

	/// <inheritdoc/>
	public void OnActiveEffectUnapplied(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData,
		bool removed,
		EffectRemovalReason reason)
	{
		if (!removed)
		{
			return;
		}

		UpdateBlocking(target, false);
	}

	private void UpdateBlocking(IForgeEntity target, bool shouldBlock)
	{
		// Guarded so that OnActiveEffectChanged, which also fires for level, modifier and stack changes, never double
		// adds or double removes the blocked tags.
		if (_isBlocking == shouldBlock)
		{
			return;
		}

		_isBlocking = shouldBlock;

		if (shouldBlock)
		{
			target.Abilities.BlockedAbilityTags.AddModifierTags(TagsToBlock);
			return;
		}

		target.Abilities.BlockedAbilityTags.RemoveModifierTags(TagsToBlock);
	}
}
