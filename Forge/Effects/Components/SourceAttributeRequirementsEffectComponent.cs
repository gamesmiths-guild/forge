// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Component that gates an effect on the attribute values of its *source* rather than its target. The source must
/// satisfy the <paramref name="applicationRequirements"/> at the moment of application. The
/// <paramref name="removalRequirements"/> define the conditions under which the effect should be removed, while the
/// <paramref name="ongoingRequirements"/> specify conditions for toggling effect inhibition.
/// </summary>
/// <remarks>
/// <para>
/// This is <see cref="AttributeRequirementsEffectComponent"/> pointed at the other end of the effect, completing the
/// pair that <see cref="TargetTagRequirementsEffectComponent"/> and <see cref="SourceTagRequirementsEffectComponent"/>
/// form for tags. It expresses conditions the target's own attributes cannot reach — "this execute only lands while
/// the attacker is above half rage", or "the channelled beam suppresses itself when its caster runs out of mana".
/// </para>
/// <para>
/// Which entity counts as the source is chosen with <paramref name="ownershipEntity"/>. A <see langword="null"/>
/// source satisfies nothing, so any non-empty bucket evaluated against it is unmet.
/// </para>
/// <para>
/// This component maintains per-effect-instance state (event subscriptions). When used in <see cref="EffectData"/>,
/// each effect application will create its own instance via <see cref="CreateInstance"/> to isolate state between
/// different effect applications.
/// </para>
/// </remarks>
/// <param name="applicationRequirements">Attribute conditions required on the source for the effect to be applied.
/// </param>
/// <param name="removalRequirements">Attribute conditions that, if met on the source, trigger effect removal.</param>
/// <param name="ongoingRequirements">Attribute conditions that, if unmet on the source, inhibit the effect.</param>
/// <param name="ownershipEntity">Which ownership entity supplies the attributes.</param>
public class SourceAttributeRequirementsEffectComponent(
	AttributeRequirement[]? applicationRequirements = null,
	AttributeRequirement[]? removalRequirements = null,
	AttributeRequirement[]? ongoingRequirements = null,
	OwnershipEntity ownershipEntity = OwnershipEntity.Source) : IEffectComponent
{
	private readonly List<EntityAttribute> _subscribedAttributes = [];

	private Action<EntityAttribute, int>? _handler;

	internal AttributeRequirement[] ApplicationRequirements { get; } = applicationRequirements ?? [];

	internal AttributeRequirement[] RemovalRequirements { get; } = removalRequirements ?? [];

	internal AttributeRequirement[] OngoingRequirements { get; } = ongoingRequirements ?? [];

	internal OwnershipEntity OwnershipEntity { get; } = ownershipEntity;

	// A requirement with neither bound always passes, which is never what the author meant. EffectData rejects it.
	internal bool HasVacuousRequirement =>
		Array.Exists(ApplicationRequirements, x => x.IsEmpty)
		|| Array.Exists(RemovalRequirements, x => x.IsEmpty)
		|| Array.Exists(OngoingRequirements, x => x.IsEmpty);

	// Ongoing requirements inhibit an active effect, which an instant effect never becomes. EffectData rejects it.
	internal bool HasOngoingRequirements => OngoingRequirements.Length > 0;

	/// <inheritdoc/>
	public IEffectComponent CreateInstance()
	{
		// Create a new instance for each effect application to isolate event subscription state.
		return new SourceAttributeRequirementsEffectComponent(
			ApplicationRequirements,
			RemovalRequirements,
			OngoingRequirements,
			OwnershipEntity);
	}

	/// <inheritdoc/>
	public bool CanApplyEffect(in IForgeEntity target, in Effect effect)
	{
		IForgeEntity? sourceEntity = ResolveEntity(effect.Ownership);

		if (!AttributeRequirement.RequirementsMet(ApplicationRequirements, sourceEntity))
		{
			return false;
		}

		return !AttributeRequirement.RequirementsMet(RemovalRequirements, sourceEntity, emptyResult: false);
	}

	/// <inheritdoc/>
	public bool OnActiveEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		ActiveEffectHandle handle = activeEffectEvaluatedData.ActiveEffectHandle;
		IForgeEntity? sourceEntity = ResolveEntity(activeEffectEvaluatedData.EffectEvaluatedData.Effect.Ownership);

		// Captures the target only to remove through its manager; every requirement reads the *source*.
		_handler = (_, _) =>
		{
			if (AttributeRequirement.RequirementsMet(RemovalRequirements, sourceEntity, emptyResult: false))
			{
				target.EffectsManager.RemoveEffect(handle, true);
				return;
			}

			if (OngoingRequirements.Length > 0)
			{
				handle.SetInhibit(!AttributeRequirement.RequirementsMet(OngoingRequirements, sourceEntity));
			}
		};

		// A null source has no attributes to watch, so the requirements stay frozen at their initial evaluation.
		if (sourceEntity is not null)
		{
			SubscribeToWatchedAttributes(sourceEntity, _handler);
		}

		return AttributeRequirement.RequirementsMet(OngoingRequirements, sourceEntity);
	}

	/// <inheritdoc/>
	public void OnActiveEffectUnapplied(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData,
		bool removed,
		EffectRemovalReason reason)
	{
		if (!removed || _handler is null)
		{
			return;
		}

		foreach (EntityAttribute attribute in _subscribedAttributes)
		{
			attribute.OnValueChanged -= _handler;
		}

		_subscribedAttributes.Clear();
		_handler = null;
	}

	/// <inheritdoc/>
	/// <remarks>
	/// Only matters when this effect's source is the entity whose sets changed, which is the common self-sourced
	/// case. An effect watching some *other* entity's attributes is not rebound when that entity's sets change, since
	/// the rebuild only walks the effects living on the entity being changed.
	/// </remarks>
	public void OnTargetAttributesChanged(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		IForgeEntity? sourceEntity = ResolveEntity(activeEffectEvaluatedData.EffectEvaluatedData.Effect.Ownership);

		if (_handler is null || sourceEntity != target)
		{
			return;
		}

		foreach (EntityAttribute attribute in _subscribedAttributes)
		{
			attribute.OnValueChanged -= _handler;
		}

		_subscribedAttributes.Clear();
		SubscribeToWatchedAttributes(sourceEntity, _handler);

		if (OngoingRequirements.Length > 0)
		{
			activeEffectEvaluatedData.ActiveEffectHandle.SetInhibit(
				!AttributeRequirement.RequirementsMet(OngoingRequirements, sourceEntity));
		}
	}

	private IForgeEntity? ResolveEntity(EffectOwnership ownership)
	{
		return OwnershipEntity == OwnershipEntity.Owner ? ownership.Owner : ownership.Source;
	}

	private void SubscribeToWatchedAttributes(IForgeEntity source, Action<EntityAttribute, int> handler)
	{
		// Only the removal and ongoing buckets are reactive. Application requirements are consulted once, in
		// CanApplyEffect, so watching their attributes would only produce no-op callbacks.
		foreach (StringKey attributeKey in RemovalRequirements
			.Concat(OngoingRequirements)
			.Select(requirement => requirement.Attribute))
		{
			if (!source.Attributes.ContainsAttribute(attributeKey))
			{
				continue;
			}

			EntityAttribute attribute = source.Attributes[attributeKey];

			// The same attribute can appear in several buckets; subscribe to it only once.
			if (_subscribedAttributes.Contains(attribute))
			{
				continue;
			}

			_subscribedAttributes.Add(attribute);
			attribute.OnValueChanged += handler;
		}
	}
}
