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
/// <remarks>
/// This component maintains per-effect-instance state (event subscriptions).
/// When used in <see cref="EffectData"/>, each effect application will create its own instance
/// via <see cref="CreateInstance"/> to isolate state between different effect applications.
/// </remarks>
/// <param name="applicationTagRequirements">Tags required for the effect to be applied.</param>
/// <param name="removalTagRequirements">Tags that, if met, trigger effect removal.</param>
/// <param name="ongoingTagRequirements">Tags that, if met, toggle the inhibition state of the effect.</param>
public class TargetTagRequirementsEffectComponent(
	TagRequirements? applicationTagRequirements = null,
	TagRequirements? removalTagRequirements = null,
	TagRequirements? ongoingTagRequirements = null) : IEffectComponent
{
	private Action<TagContainer>? _handler;

	private TagRequirements? ApplicationTagRequirements { get; } = applicationTagRequirements;

	private TagRequirements? RemovalTagRequirements { get; } = removalTagRequirements;

	private TagRequirements? OngoingTagRequirements { get; } = ongoingTagRequirements;

	/// <inheritdoc/>
	public IEffectComponent CreateInstance()
	{
		// Create a new instance for each effect application to isolate event subscription state
		return new TargetTagRequirementsEffectComponent(
			ApplicationTagRequirements,
			RemovalTagRequirements,
			OngoingTagRequirements);
	}

	/// <inheritdoc/>
	public bool CanApplyEffect(in IForgeEntity target, in Effect effect)
	{
		TagContainer tags = target.Tags.CombinedTags;

		if (ApplicationTagRequirements.HasValue
			&& !ApplicationTagRequirements.Value.IsEmpty
			&& !ApplicationTagRequirements.Value.RequirementsMet(tags))
		{
			return false;
		}

		if (RemovalTagRequirements.HasValue
			&& !RemovalTagRequirements.Value.IsEmpty
			&& RemovalTagRequirements.Value.RequirementsMet(tags))
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

		// Create a handler that captures this 'target' and 'handle'
		void Handler(TagContainer tags)
		{
			if (RemovalTagRequirements.HasValue
				&& !RemovalTagRequirements.Value.IsEmpty
				&& RemovalTagRequirements.Value.RequirementsMet(tags))
			{
				target.EffectsManager.RemoveEffect(handle, true);
				return;
			}

			if (OngoingTagRequirements.HasValue && !OngoingTagRequirements.Value.IsEmpty)
			{
				handle.SetInhibit(!OngoingTagRequirements.Value.RequirementsMet(tags));
			}
		}

		// Store it so we can unsubscribe later
		_handler = Handler;
		target.Tags.OnTagsChanged += _handler;

		return OngoingTagRequirements?.IsEmpty != false
			|| OngoingTagRequirements.Value.RequirementsMet(target.Tags.CombinedTags);
	}

	/// <inheritdoc/>
	public void OnActiveEffectUnapplied(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData,
		bool removed)
	{
		if (removed && _handler is not null)
		{
			target.Tags.OnTagsChanged -= _handler;
			_handler = null;
		}
	}
}
