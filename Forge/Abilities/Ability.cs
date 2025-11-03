// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Instance of an ability that has been granted to an entity.
/// </summary>
internal class Ability
{
	private readonly Effect? _cooldownEffect;

	private readonly Effect? _costEffect;

	private readonly Effect? _activationOwnedTagsEffect;

	private readonly TagContainer? _abilityTags;

	private ActiveEffectHandle? _activationOwnedTagsEffectHandle;

	private int _activeCount;

	internal event Action<Ability>? OnAbilityDeactivated;

	/// <summary>
	/// Gets the owner of this ability.
	/// </summary>
	public IForgeEntity Owner { get; }

	/// <summary>
	/// Gets the ability data for this ability.
	/// </summary>
	internal AbilityData AbilityData { get; }

	/// <summary>
	/// Gets or sets the current level o this ability.
	/// </summary>
	internal int Level { get; set; }

	/// <summary>
	/// Gets the policy that determines when this granted ability should be removed.
	/// </summary>
	internal AbilityDeactivationPolicy GrantedAbilityRemovalPolicy { get; }

	/// <summary>
	/// Gets the policy that determines how this ability behaves when it is inhibited.
	/// </summary>
	internal AbilityDeactivationPolicy GrantedAbilityInhibitionPolicy { get; }

	/// <summary>
	/// Gets the entity that is the source of this ability.
	/// </summary>
	internal IForgeEntity? SourceEntity { get; }

	internal AbilityHandle Handle { get; }

	internal bool IsInhibited { get; set; }

	internal bool IsActive => _activeCount > 0;

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

		_activeCount = 0;
		IsInhibited = false;

		if (abilityData.CooldownEffect is not null)
		{
			_cooldownEffect = new Effect(
				abilityData.CooldownEffect.Value,
				new EffectOwnership(owner, sourceEntity),
				level);
		}

		if (abilityData.CostEffect is not null)
		{
			_costEffect = new Effect(
				abilityData.CostEffect.Value,
				new EffectOwnership(owner, sourceEntity),
				level);
		}

		if (abilityData.ActivationOwnedTags is not null)
		{
			_activationOwnedTagsEffect = new Effect(
				new EffectData(
					name: $"{abilityData.Name}_ActivationOwnedTagsEffect",
					new DurationData
					{
						DurationType = DurationType.Infinite,
					},
					effectComponents:
					[
						new ModifierTagsEffectComponent(abilityData.ActivationOwnedTags)
					]),
				new EffectOwnership(owner, sourceEntity),
				level);
		}

		if (abilityData.AbilityTags is not null)
		{
			_abilityTags = abilityData.AbilityTags;
		}

		Handle = new AbilityHandle(this);
	}

	/// <summary>
	/// Activates the ability and increments the active count.
	/// </summary>
	internal void Activate()
	{
		if (IsInhibited)
		{
			Console.WriteLine($"Ability {AbilityData.Name} is inhibited and cannot be activated.");
			return;
		}

		if (_activationOwnedTagsEffect is not null)
		{
			_activationOwnedTagsEffectHandle = Owner.EffectsManager.ApplyEffect(_activationOwnedTagsEffect);
		}

		if (AbilityData.CancelAbilitiesWithTag is not null)
		{
			//AbilityData.CancelAbilitiesWithTag
		}

		if (AbilityData.BlockAbilitiesWithTag is not null)
		{
			Owner.Abilities.BlockedAbilityTags.AddModifierTags(AbilityData.BlockAbilitiesWithTag);
		}

		_activeCount++;
		Console.WriteLine($"Ability {AbilityData.Name} activated. Active count: {_activeCount}");
	}

	// TODO: Might need to return reasons why it can't be activated, including relevant tags.
	internal bool CanActivate(IForgeEntity? abilityTarget)
	{
		if (IsInhibited)
		{
			return false;
		}

		// Check instance.

		// Check cooldown.
		if (_cooldownEffect?.CachedGrantedTags is not null
			&& Owner.Tags.CombinedTags.HasAny(_cooldownEffect.CachedGrantedTags))
		{
			return false;
		}

		// Check resources.
		if (_costEffect is not null
			&& !Owner.EffectsManager.CanApplyEffect(_costEffect, Level))
		{
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
			return false;
		}

		// Source tags.
		if (FailsRequiredTags(AbilityData.SourceRequiredTags, sourceTags)
			|| HasBlockedTags(AbilityData.SourceBlockedTags, sourceTags))
		{
			return false;
		}

		// Target tags.
		if (FailsRequiredTags(AbilityData.TargetRequiredTags, targetTags)
			|| HasBlockedTags(AbilityData.TargetBlockedTags, targetTags))
		{
			return false;
		}

		// Check ability tags against BlockAbilitiesWithTag
		if (_abilityTags?.HasAny(Owner.Abilities.BlockedAbilityTags.CombinedTags) == true)
		{
			return false;
		}

		return true;
	}

	internal bool TryActivateAbility(IForgeEntity? abilityTarget)
	{
		if (CanActivate(abilityTarget))
		{
			Activate();
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
		if (_cooldownEffect is not null)
		{
			Owner.EffectsManager.ApplyEffect(_cooldownEffect);
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
		// TODO: Set flags for cancellation.
		End();
	}

	internal void End()
	{
		if (_activeCount <= 0)
		{
			Console.WriteLine($"Ability {AbilityData.Name} is not active.");
			return;
		}

		if (_activationOwnedTagsEffectHandle is not null)
		{
			Owner.EffectsManager.UnapplyEffect(_activationOwnedTagsEffectHandle);
			_activationOwnedTagsEffectHandle.Free();
		}

		// Unblock abilities with tags.
		if (AbilityData.BlockAbilitiesWithTag is not null)
		{
			Owner.Abilities.BlockedAbilityTags.RemoveModifierTags(AbilityData.BlockAbilitiesWithTag);
		}

		_activeCount--;

		OnAbilityDeactivated?.Invoke(this);
		Console.WriteLine($"Ability {AbilityData.Name} deactivated. Active count: {_activeCount}");
	}

	private static bool FailsRequiredTags(TagContainer? required, TagContainer? present)
	{
		return required is not null && (present?.HasAll(required) != true);
	}

	private static bool HasBlockedTags(TagContainer? blocked, TagContainer? present)
	{
		return blocked is not null && (present?.HasAny(blocked) == true);
	}
}
