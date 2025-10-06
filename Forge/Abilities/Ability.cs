// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Instance of an ability that has been granted to an entity.
/// </summary>
internal class Ability
{
	private int _activeCount;

	internal event Action<Ability>? OnAbilityDeactivated;

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
	/// <param name="abilityData">The data defining this ability.</param>
	/// <param name="level">The level of the ability.</param>
	/// <param name="grantedAbilityRemovalPolicy">The policy that determines when this granted ability should be removed.
	/// </param>
	/// <param name="grantedAbilityInhibitionPolicy">The policy that determines how this ability behaves when it is
	/// inhibited.</param>
	/// <param name="sourceEntity">The entity that granted us this ability.</param>
	internal Ability(
		AbilityData abilityData,
		int level,
		AbilityDeactivationPolicy grantedAbilityRemovalPolicy = AbilityDeactivationPolicy.CancelImmediately,
		AbilityDeactivationPolicy grantedAbilityInhibitionPolicy = AbilityDeactivationPolicy.CancelImmediately,
		IForgeEntity? sourceEntity = null)
	{
		AbilityData = abilityData;
		Level = level;
		GrantedAbilityRemovalPolicy = grantedAbilityRemovalPolicy;
		GrantedAbilityInhibitionPolicy = grantedAbilityInhibitionPolicy;
		SourceEntity = sourceEntity;

		_activeCount = 0;
		IsInhibited = false;

		Handle = new AbilityHandle(this);
	}

	/// <summary>
	/// Activates the ability and increments the active count.
	/// </summary>
	internal void Activate()
	{
		_activeCount++;
		Console.WriteLine($"Ability {AbilityData.Name} activated. Active count: {_activeCount}");
	}

	internal void Deactivate()
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
