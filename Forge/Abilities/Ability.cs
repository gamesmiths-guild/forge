// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Instance of an ability that has been granted to an entity.
/// </summary>
internal class Ability
{
	private readonly Effect? _cooldownEffect;

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
	/// <param name="grantedAbilityRemovalPolicy">The policy that determines when this granted ability should be removed.
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

		_activeCount++;
		Console.WriteLine($"Ability {AbilityData.Name} activated. Active count: {_activeCount}");
	}

	// TODO: Might need to return reasons why it can't be activated, including relevant tags.
	internal bool CanActivate()
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

		// Check tags condition.

		return true;
	}

	internal bool TryActivateAbility()
	{
		if (CanActivate())
		{
			Activate();
			return true;
		}

		return false;
	}

	internal void CommitAbility()
	{
		CommitCooldown();
	}

	internal void CommitCooldown()
	{
		if (_cooldownEffect is not null)
		{
			Owner.EffectsManager.ApplyEffect(_cooldownEffect);
		}
	}

	internal void CancelAbility()
	{
		// TODO: Set flags for cancellation.
		End();
	}

	internal void End()
	{
		OnAbilityDeactivated?.Invoke(this);

		if (_activeCount > 0)
		{
			_activeCount--;
			Console.WriteLine($"Ability {AbilityData.Name} deactivated. Active count: {_activeCount}");
		}
		else
		{
			Console.WriteLine($"Ability {AbilityData.Name} is not active.");
		}
	}
}
