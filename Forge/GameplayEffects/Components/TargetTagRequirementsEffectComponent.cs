// Copyright © 2024 Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayTags;

namespace Gamesmiths.Forge.GameplayEffects.Components;

/// <summary>
/// Component that validates if tag requirements are met on the target for a given gameplay effect. The target must
/// comply with the <paramref name="applicationTagRequirements"/> at the moment of application. The
/// <paramref name="removalTagRequirements"/> define the conditions under which the effect should be removed, while the
/// <paramref name="ongoingTagRequirements"/> specify conditions for toggling effect inhibition.
/// </summary>
/// <param name="applicationTagRequirements">Tags required for the effect to be applied.</param>
/// <param name="removalTagRequirements">Tags that, if met, trigger effect removal.</param>
/// <param name="ongoingTagRequirements">Tags that, if met, toggle the inhibition state of the effect.</param>
public class TargetTagRequirementsEffectComponent(
	GameplayTagRequirements applicationTagRequirements,
	GameplayTagRequirements removalTagRequirements,
	GameplayTagRequirements ongoingTagRequirements) : IGameplayEffectComponent
{
	private IForgeEntity? _target;

	private ActiveGameplayEffectHandle? _effectHandle;

	private GameplayTagRequirements ApplicationTagRequirements { get; } = applicationTagRequirements;

	private GameplayTagRequirements RemovalTagRequirements { get; } = removalTagRequirements;

	private GameplayTagRequirements OngoingTagRequirements { get; } = ongoingTagRequirements;

	/// <inheritdoc/>
	public bool CanApplyGameplayEffect(in IForgeEntity target, in GameplayEffect effect)
	{
		GameplayTagContainer tags = target.GameplayTags.CombinedTags;

		if (!ApplicationTagRequirements.IsEmpty && !ApplicationTagRequirements.RequirementsMet(tags))
		{
			return false;
		}

		if (!RemovalTagRequirements.IsEmpty && RemovalTagRequirements.RequirementsMet(tags))
		{
			return false;
		}

		return true;
	}

	/// <inheritdoc/>
	public bool OnActiveGameplayEffectAdded(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		_target = target;
		_effectHandle = activeEffectEvaluatedData.ActiveGameplayEffectHandle;

		target.GameplayTags.OnTagsChanged += GameplayTags_OnTagsChanged;

		return OngoingTagRequirements.IsEmpty || OngoingTagRequirements.RequirementsMet(_target.GameplayTags.CombinedTags);
	}

	/// <inheritdoc/>
	public void OnActiveGameplayEffectUnapplied(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData,
		bool removed)
	{
		if (removed)
		{
			_target = null;
			_effectHandle = null;
			target.GameplayTags.OnTagsChanged -= GameplayTags_OnTagsChanged;
		}
	}

	private void GameplayTags_OnTagsChanged(GameplayTagContainer tags)
	{
		Debug.Assert(_target is not null, "Target should never be null at this point.");
		Debug.Assert(_effectHandle is not null, "Effect handle should never be null at this point.");

		if (!RemovalTagRequirements.IsEmpty && RemovalTagRequirements.RequirementsMet(tags))
		{
			_target.EffectsManager.UnapplyEffect(_effectHandle, true);
			return;
		}

		if (OngoingTagRequirements.IsEmpty)
		{
			return;
		}

		_effectHandle.SetInhibit(!OngoingTagRequirements.RequirementsMet(tags));
	}
}
