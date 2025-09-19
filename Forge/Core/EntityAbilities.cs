// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Manager for handling an entity's abilities.
/// </summary>
public class EntityAbilities
{
	/// <summary>
	/// Gets the set of abilities currently granted to the entity.
	/// </summary>
	public HashSet<AbilityHandle> GrantedAbilities { get; } = [];

	/// <summary>
	/// Grants a new ability to the entity.
	/// </summary>
	/// <param name="abilityData">Ability data defining the ability to be granted.</param>
	/// <param name="abilityLevel">The level of the granted ability.</param>
	/// <param name="removalPolicy">Removal policy for the granted ability.</param>
	/// <param name="source">The entity that is the source of this ability.</param>
	/// <returns>Returns the newly granted ability instance.</returns>
	internal AbilityHandle GrantAbility(
		AbilityData abilityData,
		int abilityLevel,
		GrantedAbilityRemovalPolicy removalPolicy,
		IForgeEntity? source)
	{
		var newAbility = new Ability(abilityData, abilityLevel, removalPolicy, source);
		GrantedAbilities.Add(newAbility.Handle);
		return newAbility.Handle;
	}

	internal void RemoveGrantedAbility(AbilityData abilityData)
	{
		// TODO: Implement removal policies.
		AbilityHandle? abilityToRemove = GrantedAbilities.FirstOrDefault(x => x.Ability?.AbilityData == abilityData);

		if (abilityToRemove is not null)
		{
			abilityToRemove.Free();
			GrantedAbilities.Remove(abilityToRemove);
		}
	}

	internal void RemoveGrantedAbility(AbilityHandle abilityHandle)
	{
		// TODO: Implement removal policies.
		if (GrantedAbilities.Contains(abilityHandle))
		{
			abilityHandle.Free();
			GrantedAbilities.Remove(abilityHandle);
		}
	}
}
