// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Instance of an ability that has been granted to an entity.
/// </summary>
internal class Ability
{
	private record struct BehaviorBinding(IAbilityBehavior Behavior, AbilityBehaviorContext Context);

	private readonly Effect[]? _cooldownEffects;

	private readonly ActiveEffectHandle?[]? _activeCooldownHandles;

	private readonly Effect? _costEffect;

	private readonly TagContainer? _abilityTags;

	private readonly List<AbilityInstance> _activeInstances = [];

	private readonly Dictionary<AbilityInstance, BehaviorBinding> _behaviors = [];

	private AbilityInstance? _persistentInstance;

	internal event Action<Ability>? OnAbilityDeactivated;

	/// <summary>
	/// Gets the owner of this ability.
	/// </summary>
	public IForgeEntity Owner { get; }

	internal AbilityData AbilityData { get; }

	internal int Level { get; set; }

	internal AbilityDeactivationPolicy GrantedAbilityRemovalPolicy { get; }

	internal AbilityDeactivationPolicy GrantedAbilityInhibitionPolicy { get; }

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
	/// <param name="grantedAbilityRemovalPolicy">The policy that determines when this granted ability should be
	/// removed.
	/// </param>
	/// <param name="grantedAbilityInhibitionPolicy">The policy that determines how this ability behaves when it is
	/// inhibited.</param>
	/// <param name="sourceEntity">The entity that granted us this ability.</param>
	internal Ability(
		IForgeEntity owner,
		AbilityData abilityData,
		int level,
		AbilityDeactivationPolicy grantedAbilityRemovalPolicy = AbilityDeactivationPolicy.CancelImmediately,
		AbilityDeactivationPolicy grantedAbilityInhibitionPolicy = AbilityDeactivationPolicy.CancelImmediately,
		IForgeEntity? sourceEntity = null)
	{
		Owner = owner;
		AbilityData = abilityData;
		Level = level;
		GrantedAbilityRemovalPolicy = grantedAbilityRemovalPolicy;
		GrantedAbilityInhibitionPolicy = grantedAbilityInhibitionPolicy;
		SourceEntity = sourceEntity;
		IsInhibited = false;

		if (abilityData.CooldownEffects is not null)
		{
			_cooldownEffects = new Effect[abilityData.CooldownEffects.Length];
			_activeCooldownHandles = new ActiveEffectHandle[abilityData.CooldownEffects.Length];

			for (var i = 0; i < abilityData.CooldownEffects.Length; i++)
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

		Handle = new AbilityHandle(this);
	}

	internal bool TryActivateAbility(IForgeEntity? abilityTarget, out AbilityActivationResult activationResult)
	{
		if (CanActivate(abilityTarget, out activationResult))
		{
			Activate(abilityTarget);
			return true;
		}

		return false;
	}

	internal void CommitAbility()
	{
		CommitCooldown();
		CommitCost();
	}

	internal void CommitCooldown()
	{
		if (_cooldownEffects is not null)
		{
			Validation.Assert(
				_activeCooldownHandles is not null
				&& _activeCooldownHandles.Length == _cooldownEffects.Length,
				"Active cooldown handles array should have been properly initialized.");

			for (var i = 0; i < _cooldownEffects.Length; i++)
			{
				Effect effect = _cooldownEffects[i];
				_activeCooldownHandles[i] = Owner.EffectsManager.ApplyEffect(effect);
			}
		}
	}

	internal void CommitCost()
	{
		if (_costEffect is not null)
		{
			Owner.EffectsManager.ApplyEffect(_costEffect);
		}
	}

	internal void CancelAbility()
	{
		CancelAllInstances();
	}

	internal void End()
	{
		// End the most recent active instance, if any.
		if (_activeInstances.Count == 0)
		{
			Console.WriteLine($"Ability {AbilityData.Name} is not active.");
			return;
		}

		AbilityInstance last = _activeInstances[^1];
		last.End();
	}

	internal void CancelAllInstances()
	{
		if (_activeInstances.Count == 0)
		{
			return;
		}

		// Copy to avoid modification during iteration.
		foreach (AbilityInstance instance in _activeInstances.ToArray())
		{
			instance.Cancel();
		}
	}

	internal void OnInstanceStarted(AbilityInstance instance)
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

		var context = new AbilityBehaviorContext(this, instance);
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

	internal void OnInstanceEnded(AbilityInstance instance)
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

		if (_activeInstances.Count == 0)
		{
			OnAbilityDeactivated?.Invoke(this);
		}
	}

	internal bool CanActivate(IForgeEntity? abilityTarget, out AbilityActivationResult activationResult)
	{
		if (IsInhibited)
		{
			activationResult = AbilityActivationResult.FailedInhibition;
			return false;
		}

		// Check instance policy for non re-triggerable persistent instance.
		if (AbilityData.InstancingPolicy == AbilityInstancingPolicy.PerEntity
			&& !AbilityData.RetriggerInstancedAbility
			&& _persistentInstance?.IsActive == true)
		{
			activationResult = AbilityActivationResult.FailedPersistentInstanceActive;
			return false;
		}

		// Check cooldown.
		if (_cooldownEffects is not null)
		{
			foreach (Effect effect in _cooldownEffects)
			{
				if (effect?.CachedGrantedTags is not null && Owner.Tags.CombinedTags.HasAny(effect.CachedGrantedTags))
				{
					activationResult = AbilityActivationResult.FailedCooldown;
					return false;
				}
			}
		}

		// Check resources.
		if (_costEffect is not null
			&& !Owner.EffectsManager.CanApplyEffect(_costEffect, Level))
		{
			activationResult = AbilityActivationResult.FailedInsufficientResources;
			return false;
		}

		// Check tags condition.
		TagContainer ownerTags = Owner.Tags.CombinedTags;
		TagContainer? sourceTags = SourceEntity?.Tags.CombinedTags;
		TagContainer? targetTags = abilityTarget?.Tags.CombinedTags;

		// Owner tags.
		if (FailsRequiredTags(AbilityData.ActivationRequiredTags, ownerTags)
			|| HasBlockedTags(AbilityData.ActivationBlockedTags, ownerTags))
		{
			activationResult = AbilityActivationResult.FailedOwnerTagRequirements;
			return false;
		}

		// Source tags.
		if (FailsRequiredTags(AbilityData.SourceRequiredTags, sourceTags)
			|| HasBlockedTags(AbilityData.SourceBlockedTags, sourceTags))
		{
			activationResult = AbilityActivationResult.FailedSourceTagRequirements;
			return false;
		}

		// Target tags.
		if (FailsRequiredTags(AbilityData.TargetRequiredTags, targetTags)
			|| HasBlockedTags(AbilityData.TargetBlockedTags, targetTags))
		{
			activationResult = AbilityActivationResult.FailedTargetTagRequirements;
			return false;
		}

		// Check ability tags against BlockAbilitiesWithTag
		if (_abilityTags?.HasAny(Owner.Abilities.BlockedAbilityTags.CombinedTags) == true)
		{
			activationResult = AbilityActivationResult.FailedBlockedByTags;
			return false;
		}

		activationResult = AbilityActivationResult.Success;
		return true;
	}

	internal CooldownData[] GetCooldownData()
	{
		var cooldownData = new CooldownData[_activeCooldownHandles?.Length ?? 0];

		if (_activeCooldownHandles is not null)
		{
			for (var i = 0; i < _activeCooldownHandles.Length; i++)
			{
				ActiveEffectHandle? effectHandle = _activeCooldownHandles[i];
				if (effectHandle?.ActiveEffect is not null)
				{
					ActiveEffect activeEffect = effectHandle.ActiveEffect;

					TagContainer cooldownTags = activeEffect.Effect.CachedGrantedTags!;
					var totalTime = activeEffect.EffectEvaluatedData.Duration;
					var remainingTime = (float)activeEffect.RemainingDuration;

					cooldownData[i] = new CooldownData(cooldownTags, totalTime, remainingTime);
				}
				else
				{
					EffectData effectData = _cooldownEffects![i].EffectData;

					ModifierTagsEffectComponent modifierTagsComponent =
						effectData.EffectComponents.OfType<ModifierTagsEffectComponent>().First();
					TagContainer cooldownTags = modifierTagsComponent.TagsToAdd;

					var totalTime = effectData.DurationData.DurationMagnitude!.Value.GetMagnitude(
						_cooldownEffects[i], Owner, Level);

					cooldownData[i] = new CooldownData(cooldownTags, totalTime, 0f);
				}
			}
		}

		return cooldownData;
	}

	internal float GetRemainingCooldownTime(Tag tag)
	{
		if (_activeCooldownHandles is not null)
		{
			for (var i = 0; i < _activeCooldownHandles.Length; i++)
			{
				ActiveEffectHandle? effectHandle = _activeCooldownHandles[i];
				if (effectHandle?.ActiveEffect is not null)
				{
					ActiveEffect activeEffect = effectHandle.ActiveEffect;
					TagContainer cooldownTags = activeEffect.Effect.CachedGrantedTags!;
					if (cooldownTags.HasTag(tag))
					{
						return (float)activeEffect.RemainingDuration;
					}
				}
			}
		}

		return 0f;
	}

	private static bool FailsRequiredTags(TagContainer? required, TagContainer? present)
	{
		return required is not null && (present?.HasAll(required) != true);
	}

	private static bool HasBlockedTags(TagContainer? blocked, TagContainer? present)
	{
		return blocked is not null && (present?.HasAny(blocked) == true);
	}

	private void Activate(IForgeEntity? abilityTarget)
	{
		// Cancel conflicting abilities before we start this one.
		if (AbilityData.CancelAbilitiesWithTag is not null)
		{
			Owner.Abilities.CancelAbilitiesWithTag(AbilityData.CancelAbilitiesWithTag);
		}

		if (AbilityData.InstancingPolicy == AbilityInstancingPolicy.PerEntity)
		{
			if (_persistentInstance?.IsActive == true)
			{
				Validation.Assert(
					!AbilityData.RetriggerInstancedAbility, "Should not reach here due to CanActivate check.");

				_persistentInstance.Cancel();
				_persistentInstance = null;
			}

			_persistentInstance ??= new AbilityInstance(this, abilityTarget);
			_activeInstances.Add(_persistentInstance);
			_persistentInstance.Start();
			return;
		}

		var instance = new AbilityInstance(this, abilityTarget);
		_activeInstances.Add(instance);
		instance.Start();
	}
}
