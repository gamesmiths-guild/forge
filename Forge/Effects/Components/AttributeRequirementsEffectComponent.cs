// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Component that gates an effect on the target's attribute values. The target must satisfy the
/// <paramref name="applicationRequirements"/> at the moment of application. The <paramref name="removalRequirements"/>
/// define the conditions under which the effect should be removed, while the <paramref name="ongoingRequirements"/>
/// specify conditions for toggling effect inhibition.
/// </summary>
/// <remarks>
/// <para>
/// This is the attribute-side twin of <see cref="TargetTagRequirementsEffectComponent"/>, and behaves the same way:
/// it is reactive, re-evaluating whenever any watched attribute changes, rather than only at application.
/// </para>
/// <para>
/// The requirements inside each bucket are AND-combined — every one of them must be met. An empty or omitted bucket
/// is not evaluated at all.
/// </para>
/// <para>
/// This component maintains per-effect-instance state (event subscriptions). When used in <see cref="EffectData"/>,
/// each effect application will create its own instance via <see cref="CreateInstance"/> to isolate state between
/// different effect applications.
/// </para>
/// </remarks>
/// <param name="applicationRequirements">Attribute conditions required for the effect to be applied.</param>
/// <param name="removalRequirements">Attribute conditions that, if met, trigger effect removal.</param>
/// <param name="ongoingRequirements">Attribute conditions that, if unmet, inhibit the effect.</param>
public class AttributeRequirementsEffectComponent(
	AttributeRequirement[]? applicationRequirements = null,
	AttributeRequirement[]? removalRequirements = null,
	AttributeRequirement[]? ongoingRequirements = null) : IEffectComponent
{
	private readonly List<EntityAttribute> _subscribedAttributes = [];

	private Action<EntityAttribute, int>? _handler;

	internal AttributeRequirement[] ApplicationRequirements { get; } = applicationRequirements ?? [];

	internal AttributeRequirement[] RemovalRequirements { get; } = removalRequirements ?? [];

	internal AttributeRequirement[] OngoingRequirements { get; } = ongoingRequirements ?? [];

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
		return new AttributeRequirementsEffectComponent(
			ApplicationRequirements,
			RemovalRequirements,
			OngoingRequirements);
	}

	/// <inheritdoc/>
	public bool CanApplyEffect(in IForgeEntity target, in Effect effect)
	{
		if (!AttributeRequirement.RequirementsMet(ApplicationRequirements, target))
		{
			return false;
		}

		return !AttributeRequirement.RequirementsMet(RemovalRequirements, target, emptyResult: false);
	}

	/// <inheritdoc/>
	public bool OnActiveEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		ActiveEffectHandle handle = activeEffectEvaluatedData.ActiveEffectHandle;

		// Captures this 'target' and 'handle' so the same reaction runs for any watched attribute. Which attribute
		// changed is irrelevant — each bucket is re-evaluated as a whole. Stored so we can unsubscribe later.
		_handler = (_, _) =>
		{
			if (AttributeRequirement.RequirementsMet(RemovalRequirements, target, emptyResult: false))
			{
				target.EffectsManager.RemoveEffect(handle, true);
				return;
			}

			if (OngoingRequirements.Length > 0)
			{
				handle.SetInhibit(!AttributeRequirement.RequirementsMet(OngoingRequirements, target));
			}
		};

		SubscribeToWatchedAttributes(target, _handler);

		return AttributeRequirement.RequirementsMet(OngoingRequirements, target);
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
	public void OnTargetAttributesChanged(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		if (_handler is null)
		{
			return;
		}

		foreach (EntityAttribute attribute in _subscribedAttributes)
		{
			attribute.OnValueChanged -= _handler;
		}

		_subscribedAttributes.Clear();
		SubscribeToWatchedAttributes(target, _handler);

		if (AttributeRequirement.RequirementsMet(RemovalRequirements, target, emptyResult: false))
		{
			target.EffectsManager.RemoveEffect(activeEffectEvaluatedData.ActiveEffectHandle, true);
			return;
		}

		if (OngoingRequirements.Length > 0)
		{
			activeEffectEvaluatedData.ActiveEffectHandle.SetInhibit(
				!AttributeRequirement.RequirementsMet(OngoingRequirements, target));
		}
	}

	private void SubscribeToWatchedAttributes(IForgeEntity target, Action<EntityAttribute, int> handler)
	{
		// Only the removal and ongoing buckets are reactive. Application requirements are consulted once, in
		// CanApplyEffect, so watching their attributes would only produce no-op callbacks.
		foreach (StringKey attributeKey in RemovalRequirements
			.Concat(OngoingRequirements)
			.Select(requirement => requirement.Attribute))
		{
			if (!target.Attributes.ContainsAttribute(attributeKey))
			{
				continue;
			}

			EntityAttribute attribute = target.Attributes[attributeKey];

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
