// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Component that validates if tag requirements are met on the target for a given effect. The target must
/// comply with the <paramref name="applicationTagRequirements"/> at the moment of application. The
/// <paramref name="removalTagRequirements"/> define the conditions under which the effect should be removed, while the
/// <paramref name="ongoingTagRequirements"/> specify conditions for toggling effect inhibition.
/// </summary>
/// <param name="applicationTagRequirements">Tags required for the effect to be applied.</param>
/// <param name="removalTagRequirements">Tags that, if met, trigger effect removal.</param>
/// <param name="ongoingTagRequirements">Tags that, if met, toggle the inhibition state of the effect.</param>
public class TargetTagRequirementsEffectComponent(
	TagRequirements applicationTagRequirements,
	TagRequirements removalTagRequirements,
	TagRequirements ongoingTagRequirements) : IEffectComponent
{
	private readonly Dictionary<ActiveEffectHandle, Action<TagContainer>> _subscriptionMap = [];

	private TagRequirements ApplicationTagRequirements { get; } = applicationTagRequirements;

	private TagRequirements RemovalTagRequirements { get; } = removalTagRequirements;

	private TagRequirements OngoingTagRequirements { get; } = ongoingTagRequirements;

	/// <inheritdoc/>
	public bool CanApplyEffect(in IForgeEntity target, in Effect effect)
	{
		TagContainer tags = target.Tags.CombinedTags;

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
	public bool OnActiveEffectAdded(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		ActiveEffectHandle handle = activeEffectEvaluatedData.ActiveEffectHandle;

		// Create a distinct handler that captures this 'target' and 'handle'
		void Handler(TagContainer tags)
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
		target.Tags.OnTagsChanged += Handler;

		return OngoingTagRequirements.IsEmpty || OngoingTagRequirements.RequirementsMet(target.Tags.CombinedTags);
	}

	/// <inheritdoc/>
	public void OnActiveEffectUnapplied(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData,
		bool removed)
	{
		ActiveEffectHandle handle = activeEffectEvaluatedData.ActiveEffectHandle;

		if (removed && _subscriptionMap.TryGetValue(handle, out Action<TagContainer>? handler))
		{
			target.Tags.OnTagsChanged -= handler;
			_subscriptionMap.Remove(handle);
		}
	}
}
