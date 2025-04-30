// Copyright © 2025 Gamesmiths Guild.

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
	private readonly Dictionary<ActiveGameplayEffectHandle, Action<GameplayTagContainer>> _subscriptionMap = [];

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
		ActiveGameplayEffectHandle handle = activeEffectEvaluatedData.ActiveGameplayEffectHandle;

		// Create a distinct handler that captures this 'target' and 'handle'
		void Handler(GameplayTagContainer tags)
		{
			if (!RemovalTagRequirements.IsEmpty
				&& RemovalTagRequirements.RequirementsMet(tags))
			{
				target.EffectsManager.UnapplyEffect(handle, true);
				return;
			}

			if (!OngoingTagRequirements.IsEmpty)
			{
				handle.SetInhibit(!OngoingTagRequirements.RequirementsMet(tags));
			}
		}

		// Store it so we can unsubscribe later
		_subscriptionMap[handle] = Handler;
		target.GameplayTags.OnTagsChanged += Handler;

		return OngoingTagRequirements.IsEmpty || OngoingTagRequirements.RequirementsMet(target.GameplayTags.CombinedTags);
	}

	/// <inheritdoc/>
	public void OnActiveGameplayEffectUnapplied(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData,
		bool removed)
	{
		ActiveGameplayEffectHandle handle = activeEffectEvaluatedData.ActiveGameplayEffectHandle;

		if (removed && _subscriptionMap.TryGetValue(handle, out Action<GameplayTagContainer>? handler))
		{
			target.GameplayTags.OnTagsChanged -= handler;
			_subscriptionMap.Remove(handle);
		}
	}
}
