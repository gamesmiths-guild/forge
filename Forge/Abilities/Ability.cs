// Copyright © Gamesmiths Guild.

using System.Reflection;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Calculator;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Instance of an ability that has been granted to an entity.
/// </summary>
internal sealed class Ability
{
	private record struct BehaviorBinding(IAbilityBehavior Behavior, AbilityBehaviorContext Context);

	/// <summary>
	/// What applying the cost effect would do to one attribute: the net change it produces, and whether every step of
	/// it lands inside the attribute's bounds.
	/// </summary>
	/// <param name="Attribute">The attribute the cost is charged against.</param>
	/// <param name="Cost">The nominal net change the cost effect asks of the attribute, whether or not it fits.</param>
	/// <param name="AppliesInFull">Whether the cost lands without being clamped, which is what makes it affordable.
	/// </param>
	private readonly record struct CostProjection(StringKey Attribute, int Cost, bool AppliesInFull);

	private readonly Effect[]? _cooldownEffects;

	private readonly ActiveEffectHandle?[]? _activeCooldownHandles;

	private readonly Effect? _costEffect;

	private readonly TagContainer? _abilityTags;

	private readonly List<AbilityInstance> _activeInstances = [];

	private readonly Dictionary<AbilityInstance, BehaviorBinding> _behaviors = [];

	private readonly Action<TagContainer>? _tagChangedHandler;

	private readonly EventSubscriptionToken? _eventSubscriptionToken;

	private AbilityInstance? _persistentInstance;

	internal event Action<Ability>? OnAbilityDeactivated;

	/// <summary>
	/// Gets the owner of this ability.
	/// </summary>
	public IForgeEntity Owner { get; }

	internal AbilityData AbilityData { get; }

	internal int Level { get; set; }

	internal IForgeEntity? SourceEntity { get; }

	internal AbilityHandle Handle { get; }

	internal bool IsInhibited { get; set; }

	internal bool IsActive => _activeInstances.Count > 0;

	/// <summary>
	/// Initializes a new instance of the <see cref="Ability"/> class.
	/// </summary>
	/// <param name="owner">The entity that owns this ability.</param>
	/// <param name="abilityData">The data defining this ability.</param>
	/// <param name="level">The level of the ability.</param>
	/// <param name="sourceEntity">The entity that granted us this ability.</param>
	internal Ability(
		IForgeEntity owner,
		AbilityData abilityData,
		int level,
		IForgeEntity? sourceEntity = null)
	{
		Owner = owner;
		AbilityData = abilityData;
		Level = level;
		SourceEntity = sourceEntity;
		IsInhibited = false;

		if (abilityData.CooldownEffects is not null)
		{
			_cooldownEffects = new Effect[abilityData.CooldownEffects.Length];
			_activeCooldownHandles = new ActiveEffectHandle[abilityData.CooldownEffects.Length];

			for (int i = 0; i < abilityData.CooldownEffects.Length; i++)
			{
				_cooldownEffects[i] = new Effect(
					abilityData.CooldownEffects[i],
					new EffectOwnership(owner, sourceEntity),
					level);
			}
		}

		if (abilityData.CostEffect is not null)
		{
			_costEffect = new Effect(
				abilityData.CostEffect.Value,
				new EffectOwnership(owner, sourceEntity),
				level);
		}

		if (abilityData.AbilityTags is not null)
		{
			_abilityTags = abilityData.AbilityTags;
		}

		if (abilityData.AbilityTriggerData is not null)
		{
			AbilityTriggerData triggerData = abilityData.AbilityTriggerData.Value;

			switch (triggerData.TriggerSource)
			{
				case AbilityTriggerSource.TagAdded:
					_tagChangedHandler = TagAdded_OnTagChanged;
					owner.Tags.OnTagsChanged += _tagChangedHandler;
					break;
				case AbilityTriggerSource.TagPresent:
					_tagChangedHandler = TagPresent_OnTagChanged;
					owner.Tags.OnTagsChanged += _tagChangedHandler;
					break;
				case AbilityTriggerSource.Event:
					if (triggerData.PayloadType is not null)
					{
						_eventSubscriptionToken = SubscribeTypedEvent(triggerData);
					}
					else
					{
						_eventSubscriptionToken = owner.Events.Subscribe(
							triggerData.TriggerTag,
							x => TryActivateAbility(x.Target, out _, x.EventMagnitude),
							triggerData.Priority);
					}

					break;
			}
		}

		Handle = new AbilityHandle(this);
	}

	internal bool TryActivateAbility(
		IForgeEntity? abilityTarget,
		out AbilityActivationFailures failureFlags,
		float magnitude)
	{
		if (CanActivate(abilityTarget, out failureFlags))
		{
			Activate(abilityTarget, magnitude);
			return true;
		}

		Owner.Abilities.NotifyAbilityActivationFailed(Handle, failureFlags);
		return false;
	}

	internal bool TryActivateAbility<TData>(
		IForgeEntity? abilityTarget,
		out AbilityActivationFailures failureFlags,
		TData data,
		float magnitude)
	{
		if (CanActivate(abilityTarget, out failureFlags))
		{
			Activate(abilityTarget, data, magnitude);
			return true;
		}

		Owner.Abilities.NotifyAbilityActivationFailed(Handle, failureFlags);
		return false;
	}

	internal bool TryCommitAbility()
	{
		// Both checks run before either application so the commit is all-or-nothing: an affordable cost must never be
		// paid for a cooldown that is still running, and vice versa.
		if (!CanCommitCooldown() || !CanCommitCost())
		{
			return false;
		}

		// The cost goes first because it is the half that cannot be undone. An effect component or a registered
		// application blocker can still turn either of them away, and an instant attribute change cannot be rolled
		// back, so a cost turned away leaves nothing spent and no cooldown running, while the reverse would not.
		return ApplyCost() && ApplyCooldown();
	}

	internal bool TryCommitCooldown()
	{
		return CanCommitCooldown() && ApplyCooldown();
	}

	internal bool TryCommitCost()
	{
		return CanCommitCost() && ApplyCost();
	}

	internal void End()
	{
		if (_activeInstances.Count == 0)
		{
			return;
		}

		// OnInstanceEnded raises the ability-ended notification when the last instance ends.
		AbilityInstance last = _activeInstances[^1];
		last.End();
	}

	internal void CancelAllInstances()
	{
		// Copy to avoid modification during iteration. OnInstanceEnded raises the ability-ended notification (with
		// the canceled flag) when the last instance ends.
		foreach (AbilityInstance instance in _activeInstances.ToArray())
		{
			instance.Cancel();
		}
	}

	internal void Cleanup()
	{
		if (_tagChangedHandler is not null)
		{
			Owner.Tags.OnTagsChanged -= _tagChangedHandler;
		}

		if (_eventSubscriptionToken is not null)
		{
			Owner.Events.Unsubscribe(_eventSubscriptionToken.Value);
		}
	}

	internal void OnInstanceStarted(AbilityInstance instance, float magnitude)
	{
		if (AbilityData.BehaviorFactory is null)
		{
			return;
		}

		IAbilityBehavior? behavior = AbilityData.BehaviorFactory.Invoke();
		if (behavior is null)
		{
			return;
		}

		var context = new AbilityBehaviorContext(this, instance, magnitude);
		_behaviors[instance] = new BehaviorBinding(behavior, context);

		try
		{
			behavior.OnStarted(context);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ability behavior threw on start: {ex}");
			instance.Cancel();
		}
	}

	internal void OnInstanceStarted<TPayload>(AbilityInstance instance, TPayload payload, float magnitude)
	{
		if (AbilityData.BehaviorFactory is null)
		{
			return;
		}

		IAbilityBehavior? behavior = AbilityData.BehaviorFactory.Invoke();
		if (behavior is null)
		{
			return;
		}

		var context = new AbilityBehaviorContext<TPayload>(this, instance, payload, magnitude);
		_behaviors[instance] = new BehaviorBinding(behavior, context);

		try
		{
			if (behavior is IAbilityBehavior<TPayload> typedBehavior)
			{
				typedBehavior.OnStarted(context, payload);
			}
			else
			{
				behavior.OnStarted(context);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ability behavior threw on start: {ex}");
			instance.Cancel();
		}
	}

	internal void OnInstanceEnded(AbilityInstance instance, bool wasCanceled)
	{
		_activeInstances.Remove(instance);

		if (_persistentInstance == instance)
		{
			_persistentInstance = null;
		}

		if (_behaviors.Remove(instance, out BehaviorBinding binding))
		{
			try
			{
				binding.Behavior.OnEnded(binding.Context);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Ability behavior threw on end: {ex}");
			}
		}

		instance.Handle.Free();

		if (_activeInstances.Count == 0)
		{
			// OnAbilityDeactivated may free the handle (e.g. RemoveOnEnd grants), so AbilityData is captured in the
			// payload beforehand.
			AbilityEndedData endedData = new(Handle, AbilityData, wasCanceled);
			OnAbilityDeactivated?.Invoke(this);
			Owner.Abilities.NotifyAbilityEnded(endedData);
		}
	}

	internal void UpdateBehaviors(double deltaTime)
	{
		foreach (BehaviorBinding binding in _behaviors.Values)
		{
			binding.Behavior.OnUpdate(deltaTime);
		}
	}

	internal bool CanActivate(IForgeEntity? abilityTarget, out AbilityActivationFailures failureFlags)
	{
		bool canActivate = true;
		failureFlags = AbilityActivationFailures.None;

		if (IsInhibited)
		{
			failureFlags |= AbilityActivationFailures.Inhibited;
			canActivate = false;
		}

		// Check instance policy for non re-triggerable persistent instance.
		if (AbilityData.InstancingPolicy == AbilityInstancingPolicy.PerEntity
			&& !AbilityData.RetriggerInstancedAbility
			&& _persistentInstance?.IsActive == true)
		{
			failureFlags |= AbilityActivationFailures.PersistentInstanceActive;
			canActivate = false;
		}

		// Check cooldown.
		if (!CanCommitCooldown())
		{
			failureFlags |= AbilityActivationFailures.Cooldown;
			canActivate = false;
		}

		// Check resources.
		if (!CanCommitCost())
		{
			failureFlags |= AbilityActivationFailures.InsufficientResources;
			canActivate = false;
		}

		// Check tags condition.
		TagContainer ownerTags = Owner.Tags.AllTags;
		TagContainer? sourceTags = SourceEntity?.Tags.AllTags;
		TagContainer? targetTags = abilityTarget?.Tags.AllTags;

		// Owner tags.
		if (FailsRequiredTags(AbilityData.ActivationRequiredTags, ownerTags)
			|| HasBlockedTags(AbilityData.ActivationBlockedTags, ownerTags))
		{
			failureFlags |= AbilityActivationFailures.OwnerTagRequirements;
			canActivate = false;
		}

		// Source tags.
		if (FailsRequiredTags(AbilityData.SourceRequiredTags, sourceTags)
			|| HasBlockedTags(AbilityData.SourceBlockedTags, sourceTags))
		{
			failureFlags |= AbilityActivationFailures.SourceTagRequirements;
			canActivate = false;
		}

		// Target tags.
		if (FailsRequiredTags(AbilityData.TargetRequiredTags, targetTags)
			|| HasBlockedTags(AbilityData.TargetBlockedTags, targetTags))
		{
			failureFlags |= AbilityActivationFailures.TargetTagRequirements;
			canActivate = false;
		}

		// Check ability tags against BlockAbilitiesWithTag
		if (_abilityTags?.HasAny(Owner.Abilities.BlockedAbilityTags.AllTags) == true)
		{
			failureFlags |= AbilityActivationFailures.BlockedByTags;
			canActivate = false;
		}

		return canActivate;
	}

	internal CooldownData[] GetCooldownData()
	{
		var cooldownData = new CooldownData[_activeCooldownHandles?.Length ?? 0];

		for (int i = 0; i < cooldownData.Length; i++)
		{
			ActiveEffect? activeEffect = FindRunningCooldown(i);

			if (activeEffect is not null)
			{
				cooldownData[i] = new CooldownData(
					activeEffect.Effect.CachedGrantedTags!,
					activeEffect.EffectEvaluatedData.Duration,
					(float)activeEffect.RemainingDuration);

				continue;
			}

			EffectData effectData = _cooldownEffects![i].EffectData;

			ModifierTagsEffectComponent modifierTagsComponent =
				effectData.EffectComponents.OfType<ModifierTagsEffectComponent>().First();

			float totalTime = effectData.DurationData.DurationMagnitude!.Value.GetMagnitude(
				_cooldownEffects[i], Owner, Level);

			cooldownData[i] = new CooldownData(modifierTagsComponent.TagsToAdd, totalTime, 0f);
		}

		return cooldownData;
	}

	internal float GetRemainingCooldownTime(Tag tag)
	{
		for (int i = 0; i < (_activeCooldownHandles?.Length ?? 0); i++)
		{
			ActiveEffect? activeEffect = FindRunningCooldown(i);

			if (activeEffect?.Effect.CachedGrantedTags?.HasTag(tag) == true)
			{
				return (float)activeEffect.RemainingDuration;
			}
		}

		return 0f;
	}

	internal CostData[]? GetCostData(StringKey? specificAttribute = null)
	{
		if (_costEffect is null)
		{
			return null;
		}

		CostProjection[] projections = ProjectCost(specificAttribute);
		var costData = new CostData[projections.Length];

		for (int i = 0; i < projections.Length; i++)
		{
			costData[i] = new CostData(projections[i].Attribute, projections[i].Cost);
		}

		return costData;
	}

	internal int GetCostForAttribute(StringKey attributeKey)
	{
		CostData[]? costData = GetCostData(attributeKey);

		if (costData is null)
		{
			return 0;
		}

		foreach (CostData cost in costData)
		{
			if (cost.Attribute == attributeKey)
			{
				return cost.Cost;
			}
		}

		return 0;
	}

	/// <summary>
	/// Replays every modifier charged against one attribute the way an instant application would: in order, each one
	/// building on the value the previous one produced, clamped to the attribute's bounds at every step.
	/// </summary>
	/// <remarks>
	/// <para>A step landing outside the bounds is one the attribute could not absorb in full, which is exactly what
	/// makes a cost unaffordable. The projection deliberately carries the <b>unclamped</b> value forward, so the cost
	/// stays the nominal ask — a UI showing "150 mana" against a pool of 100 wants the 150, not the 100 the target
	/// could scrape together.</para>
	/// <para>When nothing falls out of bounds no clamping would happen either, so the projected cost is then exactly
	/// the change an application would make.</para>
	/// </remarks>
	/// <param name="attribute">The attribute being charged.</param>
	/// <param name="evaluatedModifiers">Every evaluated modifier of the cost effect, in application order.</param>
	/// <returns>The projected outcome for this attribute.</returns>
	private static CostProjection ProjectAttributeCost(
		EntityAttribute attribute,
		ModifierEvaluatedData[] evaluatedModifiers)
	{
		// An instant application mutates the base value, so that is the value the projection has to start from.
		int value = attribute.BaseValue;
		bool appliesInFull = true;

		foreach (ModifierEvaluatedData evaluatedModifier in evaluatedModifiers)
		{
			if (evaluatedModifier.Attribute.Key != attribute.Key)
			{
				continue;
			}

			// Mirrors EntityAttribute.Execute*: the same truncation and the same rounding.
			value = evaluatedModifier.ModifierOperation switch
			{
				ModifierOperation.FlatBonus => value + (int)evaluatedModifier.Magnitude,
				ModifierOperation.PercentBonus => (int)(value * Math.Round(1 + evaluatedModifier.Magnitude, 6)),
				ModifierOperation.Override => (int)evaluatedModifier.Magnitude,
				_ => value,
			};

			if (value < attribute.Min || value > attribute.Max)
			{
				appliesInFull = false;
			}
		}

		return new CostProjection(attribute.Key, value - attribute.BaseValue, appliesInFull);
	}

	private static bool FailsRequiredTags(TagContainer? required, TagContainer? present)
	{
		return required is not null && (present?.HasAll(required) != true);
	}

	private static bool HasBlockedTags(TagContainer? blocked, TagContainer? present)
	{
		return blocked is not null && (present?.HasAny(blocked) == true);
	}

	private EventSubscriptionToken SubscribeTypedEvent(AbilityTriggerData triggerData)
	{
#pragma warning disable IDE0370 // Remove unnecessary suppression
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
		// Do not attempt this in production environments without adult supervision.
		MethodInfo method = typeof(Ability)
			.GetMethod(nameof(SubscribeTypedEventCore), BindingFlags.NonPublic | BindingFlags.Instance)!
			.MakeGenericMethod(triggerData.PayloadType!);
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

		return (EventSubscriptionToken)method.Invoke(this, [triggerData.TriggerTag, triggerData.Priority])!;
#pragma warning restore IDE0370 // Remove unnecessary suppression
	}

	private EventSubscriptionToken SubscribeTypedEventCore<TPayload>(Tag tag, int priority)
	{
		return Owner.Events.Subscribe<TPayload>(
			tag,
			x => TryActivateAbility(x.Target, out _, x.Payload, x.EventMagnitude),
			priority: priority);
	}

	private ActiveEffect? FindRunningCooldown(int index)
	{
		ActiveEffect? activeEffect = _activeCooldownHandles?[index]?.ActiveEffect;

		return activeEffect ?? Owner.EffectsManager.FindActiveEffectByData(_cooldownEffects![index].EffectData);
	}

	private bool CanCommitCooldown()
	{
		if (_cooldownEffects is null)
		{
			return true;
		}

		foreach (Effect effect in _cooldownEffects)
		{
			if (effect?.CachedGrantedTags is not null && Owner.Tags.AllTags.HasAny(effect.CachedGrantedTags))
			{
				return false;
			}
		}

		return true;
	}

	private bool CanCommitCost()
	{
		if (_costEffect is null)
		{
			return true;
		}

		// A cost charged against an attribute the owner does not have can never be paid. Applying the effect would
		// quietly skip that modifier, so refusing here is what stops the ability from being cast for free — the check
		// is deliberately stricter than the application on this one point.
		foreach (Modifier modifier in _costEffect.EffectData.Modifiers)
		{
			if (!Owner.Attributes.ContainsAttribute(modifier.Attribute))
			{
				return false;
			}
		}

		foreach (CostProjection projection in ProjectCost(specificAttribute: null))
		{
			if (!projection.AppliesInFull)
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Projects what applying the cost effect would do, without touching a single attribute.
	/// </summary>
	/// <remarks>
	/// <para>This is the one place the cost is computed. Both the number a player is shown through
	/// <see cref="GetCostData"/> and the affordability behind <see cref="CanCommitCost"/> read it, so the cost quoted
	/// and the cost charged cannot drift apart, and neither can drift from what an instant
	/// <see cref="Effect"/> application would really do.</para>
	/// <para>Modifiers are grouped per attribute because that is the granularity the bounds apply at; several
	/// modifiers charged against the same attribute compound, exactly as they would when executed.</para>
	/// </remarks>
	/// <param name="specificAttribute">Restricts the projection to a single attribute, or <see langword="null"/> for
	/// every attribute the cost touches.</param>
	/// <returns>One projection per attribute the cost effect charges.</returns>
	private CostProjection[] ProjectCost(StringKey? specificAttribute)
	{
		Validation.Assert(_costEffect is not null, "There is no cost to project without a cost effect.");

		ModifierEvaluatedData[] evaluatedModifiers = EvaluateInstantModifiers(_costEffect, specificAttribute);

		List<CostProjection> projections = [];

		foreach (EntityAttribute attribute in evaluatedModifiers.Select(x => x.Attribute))
		{
			if (projections.Exists(x => x.Attribute == attribute.Key))
			{
				continue;
			}

			projections.Add(ProjectAttributeCost(attribute, evaluatedModifiers));
		}

		return [.. projections];
	}

	/// <summary>
	/// Applies the cooldown effects and reports whether every one of them landed.
	/// </summary>
	/// <remarks>
	/// Passing <see cref="CanCommitCooldown"/> is not enough on its own: an effect component or a registered
	/// application blocker — an immunity, typically — can still turn a cooldown effect away, and a commit must not
	/// claim to have started a cooldown that never began.
	/// </remarks>
	/// <returns><see langword="true"/> when there was nothing to apply or every cooldown effect was applied.</returns>
	private bool ApplyCooldown()
	{
		if (_cooldownEffects is null)
		{
			return true;
		}

		Validation.Assert(
			_activeCooldownHandles is not null
			&& _activeCooldownHandles.Length == _cooldownEffects.Length,
			"Active cooldown handles array should have been properly initialized.");

		bool applied = true;

		for (int i = 0; i < _cooldownEffects.Length; i++)
		{
			Effect effect = _cooldownEffects[i];
			applied &= Owner.EffectsManager.TryApplyEffect(effect, out ActiveEffectHandle? activeEffectHandle);
			_activeCooldownHandles[i] = activeEffectHandle;
		}

		return applied;
	}

	/// <summary>
	/// Applies the cost effect and reports whether it landed.
	/// </summary>
	/// <remarks>
	/// Affordability is only half the question — see <see cref="ApplyCooldown"/> for the other half.
	/// </remarks>
	/// <returns><see langword="true"/> when there was no cost or the cost effect was applied.</returns>
	private bool ApplyCost()
	{
		return _costEffect is null || Owner.EffectsManager.TryApplyEffect(_costEffect, out _);
	}

	private void Activate(IForgeEntity? abilityTarget, float magnitude)
	{
		AbilityInstance instance = CreateInstance(abilityTarget);
		_activeInstances.Add(instance);
		NotifyActivated();
		instance.Start(magnitude);
	}

	private void Activate<TData>(IForgeEntity? abilityTarget, TData data, float magnitude)
	{
		AbilityInstance instance = CreateInstance(abilityTarget);
		_activeInstances.Add(instance);
		NotifyActivated();
		instance.Start(data, magnitude);
	}

	private void NotifyActivated()
	{
		if (_activeInstances.Count != 1)
		{
			return;
		}

		Owner.Abilities.NotifyAbilityActivated(Handle);
	}

	private AbilityInstance CreateInstance(IForgeEntity? abilityTarget)
	{
		// Cancel conflicting abilities before we start this one. An empty container means this ability conflicts with
		// nothing, so it must not reach CancelAbilities, where an empty filter would match every ability instead.
		if (AbilityData.CancelAbilitiesWithTag?.IsEmpty == false)
		{
			Owner.Abilities.CancelAbilities(AbilityData.CancelAbilitiesWithTag, null);
		}

		if (AbilityData.InstancingPolicy == AbilityInstancingPolicy.PerEntity)
		{
			if (_persistentInstance?.IsActive == true)
			{
				Validation.Assert(
					AbilityData.RetriggerInstancedAbility, "Should not reach here due to CanActivate check.");

				_persistentInstance.Cancel();
				_persistentInstance = null;
			}

			_persistentInstance ??= new AbilityInstance(this, abilityTarget);
			return _persistentInstance;
		}

		return new AbilityInstance(this, abilityTarget);
	}

	private ModifierEvaluatedData[] EvaluateInstantModifiers(Effect effect, StringKey? specificAttribute = null)
	{
		var modifiersEvaluatedData = new List<ModifierEvaluatedData>(effect.EffectData.Modifiers.Length);

		foreach (Modifier modifier in effect.EffectData.Modifiers)
		{
			// Ignore modifiers for attributes not present in the target.
			if (!Owner.Attributes.ContainsAttribute(modifier.Attribute) ||
				(specificAttribute.HasValue && specificAttribute.Value != modifier.Attribute))
			{
				continue;
			}

			modifiersEvaluatedData.Add(
				new ModifierEvaluatedData(
					Owner.Attributes[modifier.Attribute],
					modifier.Operation,
					modifier.Magnitude.GetMagnitude(effect, Owner, Level, null),
					modifier.Channel,
					modifier.AggregationMode));
		}

		foreach (CustomExecution execution in effect.EffectData.CustomExecutions)
		{
			if (CustomExecution.ExecutionHasInvalidAttributeCaptures(execution, effect, Owner))
			{
				continue;
			}

			modifiersEvaluatedData.AddRange(execution.EvaluateExecution(effect, Owner, null));
		}

		return [.. modifiersEvaluatedData];
	}

	private void TagPresent_OnTagChanged(TagContainer container)
	{
		if (container.HasTag(AbilityData.AbilityTriggerData!.Value.TriggerTag))
		{
			TryActivateAbility(null, out _, 0f);
		}
		else
		{
			CancelAllInstances();
		}
	}

	private void TagAdded_OnTagChanged(TagContainer container)
	{
		if (container.HasTag(AbilityData.AbilityTriggerData!.Value.TriggerTag))
		{
			TryActivateAbility(null, out _, 0f);
		}
	}
}
