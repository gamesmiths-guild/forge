// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Component that validates tag requirements against the effect's *source* rather than its target. The source must
/// comply with the <paramref name="applicationTagRequirements"/> at the moment of application. The
/// <paramref name="removalTagRequirements"/> define the conditions under which the effect should be removed, while
/// the <paramref name="ongoingTagRequirements"/> specify conditions for toggling effect inhibition.
/// </summary>
/// <remarks>
/// <para>
/// This is <see cref="TargetTagRequirementsEffectComponent"/> pointed at the other end of the effect. It expresses
/// conditions the target's own tags cannot reach — "this poison only applies if the attacker is Venomous", or "the
/// aura suppresses itself while its caster is silenced".
/// </para>
/// <para>
/// Which entity counts as the source is chosen with <paramref name="ownershipEntity"/>. When that entity is
/// <see langword="null"/> the requirements are evaluated against an empty tag container, so required tags fail and
/// ignored tags pass.
/// </para>
/// <para>
/// This component maintains per-effect-instance state (event subscriptions). When used in <see cref="EffectData"/>,
/// each effect application will create its own instance via <see cref="CreateInstance"/> to isolate state between
/// different effect applications.
/// </para>
/// </remarks>
/// <param name="applicationTagRequirements">Tags required on the source for the effect to be applied.</param>
/// <param name="removalTagRequirements">Tags that, if met on the source, trigger effect removal.</param>
/// <param name="ongoingTagRequirements">Tags that, if unmet on the source, inhibit the effect.</param>
/// <param name="ownershipEntity">Which ownership entity supplies the tags.</param>
public class SourceTagRequirementsEffectComponent(
	TagRequirements? applicationTagRequirements = null,
	TagRequirements? removalTagRequirements = null,
	TagRequirements? ongoingTagRequirements = null,
	OwnershipEntity ownershipEntity = OwnershipEntity.Source) : IEffectComponent
{
	private Action<TagContainer>? _handler;
	private EntityTags? _subscribedTags;

	internal TagRequirements? ApplicationTagRequirements { get; } = applicationTagRequirements;

	internal TagRequirements? RemovalTagRequirements { get; } = removalTagRequirements;

	internal TagRequirements? OngoingTagRequirements { get; } = ongoingTagRequirements;

	internal OwnershipEntity OwnershipEntity { get; } = ownershipEntity;

	// Ongoing requirements inhibit an active effect, which an instant effect never becomes. EffectData rejects it.
	internal bool HasOngoingRequirements => OngoingTagRequirements?.IsEmpty == false;

	/// <inheritdoc/>
	public IEffectComponent CreateInstance()
	{
		// Create a new instance for each effect application to isolate event subscription state.
		return new SourceTagRequirementsEffectComponent(
			ApplicationTagRequirements,
			RemovalTagRequirements,
			OngoingTagRequirements,
			OwnershipEntity);
	}

	/// <inheritdoc/>
	public bool CanApplyEffect(in IForgeEntity target, in Effect effect)
	{
		TagContainer tags = ResolveTags(effect.Ownership, target);

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
	public bool OnActiveEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		ActiveEffectHandle handle = activeEffectEvaluatedData.ActiveEffectHandle;
		IForgeEntity? sourceEntity = ResolveEntity(
			activeEffectEvaluatedData.EffectEvaluatedData.Effect.Ownership);

		// Captures this 'target' and 'handle', but reacts to the *source's* tag changes.
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

		// A null source has no tag container to watch, so the requirements stay frozen at their initial evaluation.
		if (sourceEntity is not null)
		{
			_handler = Handler;
			_subscribedTags = sourceEntity.Tags;
			_subscribedTags.OnTagsChanged += _handler;
		}

		return OngoingTagRequirements?.IsEmpty != false
			|| OngoingTagRequirements.Value.RequirementsMet(
				ResolveTags(activeEffectEvaluatedData.EffectEvaluatedData.Effect.Ownership, target));
	}

	/// <inheritdoc/>
	public void OnActiveEffectUnapplied(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData,
		bool removed,
		EffectRemovalReason reason)
	{
		if (removed && _handler is not null && _subscribedTags is not null)
		{
			_subscribedTags.OnTagsChanged -= _handler;
			_subscribedTags = null;
			_handler = null;
		}
	}

	private IForgeEntity? ResolveEntity(EffectOwnership ownership)
	{
		return OwnershipEntity == OwnershipEntity.Owner ? ownership.Owner : ownership.Source;
	}

	private TagContainer ResolveTags(EffectOwnership ownership, IForgeEntity target)
	{
		// Falling back to an empty container keeps the semantics honest: required tags cannot be satisfied by a
		// missing source, while ignored tags trivially are.
		return ResolveEntity(ownership)?.Tags.AllTags
			?? new TagContainer(target.Tags.BaseTags.TagsManager);
	}
}
