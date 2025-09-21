// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Manager for handling an entity's abilities.
/// </summary>
public class EntityAbilities
{
	private readonly Dictionary<Ability, int> _grantetAbilitiesCounts = [];

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
	/// <param name="levelOverridePolicy">The policy for overriding the level if the ability already exists.</param>
	/// <param name="source">The entity that is the source of this ability.</param>
	/// <returns>Returns the newly granted ability instance.</returns>
	internal AbilityHandle GrantAbility(
		AbilityData abilityData,
		int abilityLevel,
		GrantedAbilityRemovalPolicy removalPolicy,
		LevelComparison levelOverridePolicy,
		IForgeEntity? source)
	{
		Ability? abilityToAdd = GrantedAbilities.FirstOrDefault(x => x?.Ability?.AbilityData == abilityData)?.Ability;

		if (abilityToAdd is not null && abilityToAdd.SourceEntity == source)
		{
			// Ability already granted, just increment the count.
			_grantetAbilitiesCounts[abilityToAdd]++;

			var shouldOverride =
				(levelOverridePolicy.HasFlag(LevelComparison.Higher) && abilityLevel > abilityToAdd.Level) ||
				(levelOverridePolicy.HasFlag(LevelComparison.Lower) && abilityLevel < abilityToAdd.Level) ||
				(levelOverridePolicy.HasFlag(LevelComparison.Equal) && abilityLevel == abilityToAdd.Level);

			if (shouldOverride)
			{
				abilityToAdd.Level = abilityLevel;
			}

			return abilityToAdd.Handle;
		}

		var newAbility = new Ability(abilityData, abilityLevel, removalPolicy, source);
		GrantedAbilities.Add(newAbility.Handle);
		_grantetAbilitiesCounts[newAbility] = 1;
		return newAbility.Handle;
	}

	internal void RemoveGrantedAbility(AbilityData abilityData)
	{
		RemoveGrantedAbility(GrantedAbilities.FirstOrDefault(x => x?.Ability?.AbilityData == abilityData)?.Ability);
	}

	internal void RemoveGrantedAbility(AbilityHandle abilityHandle)
	{
		RemoveGrantedAbility(GrantedAbilities.FirstOrDefault(x => x == abilityHandle)?.Ability);
	}

	internal void RemoveGrantedAbility(Ability? abilityToRemove)
	{
		if (abilityToRemove is null)
		{
			return;
		}

		switch (abilityToRemove.GrantedAbilityRemovalPolicy)
		{
			case GrantedAbilityRemovalPolicy.DoNotRemove:
				return;

			case GrantedAbilityRemovalPolicy.CancelImmediately:
				if (abilityToRemove.IsActive)
				{
					abilityToRemove.Deactivate();
				}

				RemoveAbility(abilityToRemove);
				return;

			case GrantedAbilityRemovalPolicy.RemoveOnEnd:
				if (abilityToRemove.IsActive)
				{
					abilityToRemove.OnAbilityDeactivated += RemoveAbility;
				}

				return;
		}
	}

	private void RemoveAbility(Ability abilityToRemove)
	{
		abilityToRemove.OnAbilityDeactivated -= RemoveAbility;

		_grantetAbilitiesCounts[abilityToRemove]--;

		if (_grantetAbilitiesCounts[abilityToRemove] == 0)
		{
			abilityToRemove.Handle.Free();
			GrantedAbilities.Remove(abilityToRemove.Handle);
		}
	}
}
