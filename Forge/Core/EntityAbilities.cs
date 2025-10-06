// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Manager for handling an entity's abilities.
/// </summary>
public class EntityAbilities
{
	private readonly Dictionary<Ability, List<ActiveEffectHandle>?> _grantSources = [];

	private readonly Dictionary<Ability, List<ActiveEffectHandle>?> _inhibitSources = [];

	/// <summary>
	/// Gets the set of abilities currently granted to the entity.
	/// </summary>
	public HashSet<AbilityHandle> GrantedAbilities { get; } = [];

	internal void GrantAbilityPermanently(
		AbilityData abilityData,
		int abilityLevel,
		AbilityDeactivationPolicy removalPolicy,
		AbilityDeactivationPolicy inhibitionPolicy,
		LevelComparison levelOverridePolicy,
		IForgeEntity? sourceEntity)
	{
		Ability? existingAbility =
			GrantedAbilities.FirstOrDefault(x => x?.Ability?.AbilityData == abilityData)?.Ability;

		if (existingAbility is not null && existingAbility.SourceEntity == sourceEntity)
		{
			_grantSources[existingAbility] = null;
			_inhibitSources.Remove(existingAbility);

			// If the ability was fully inhibited, this permanent grant should re-enable it.
			existingAbility.IsInhibited = false;

			var shouldOverride =
				(levelOverridePolicy.HasFlag(LevelComparison.Higher) && abilityLevel > existingAbility.Level) ||
				(levelOverridePolicy.HasFlag(LevelComparison.Lower) && abilityLevel < existingAbility.Level) ||
				(levelOverridePolicy.HasFlag(LevelComparison.Equal) && abilityLevel == existingAbility.Level);

			if (shouldOverride)
			{
				existingAbility.Level = abilityLevel;
			}

			return;
		}

		var newAbility = new Ability(abilityData, abilityLevel, removalPolicy, inhibitionPolicy, sourceEntity);
		GrantedAbilities.Add(newAbility.Handle);
	}

	internal AbilityHandle GrantAbility(
		AbilityData abilityData,
		int abilityLevel,
		AbilityDeactivationPolicy removalPolicy,
		AbilityDeactivationPolicy inhibitionPolicy,
		LevelComparison levelOverridePolicy,
		ActiveEffectHandle sourceActiveEffectHandle,
		IForgeEntity? sourceEntity)
	{
		Ability? existingAbility =
			GrantedAbilities.FirstOrDefault(x => x?.Ability?.AbilityData == abilityData)?.Ability;

		if (existingAbility is not null && existingAbility.SourceEntity == sourceEntity)
		{
			List<ActiveEffectHandle>? grantSources = _grantSources[existingAbility];
			if (grantSources is not null)
			{
				List<ActiveEffectHandle>? inhibitSources = _inhibitSources[existingAbility];

				Debug.Assert(
					inhibitSources is not null,
					"InhibitAbilityBasedOnPolicy grantSources should not be null if grant grantSources are not null.");

				// Ability already granted, just add the new source to the mapping.
				grantSources.Add(sourceActiveEffectHandle);

				if (sourceActiveEffectHandle.IsInhibited)
				{
					inhibitSources.Add(sourceActiveEffectHandle);
				}

				// If the ability was fully inhibited, this new grant may need to re-enable it.
				existingAbility.IsInhibited = inhibitSources.Count == grantSources.Count;
			}

			var shouldOverride =
				(levelOverridePolicy.HasFlag(LevelComparison.Higher) && abilityLevel > existingAbility.Level) ||
				(levelOverridePolicy.HasFlag(LevelComparison.Lower) && abilityLevel < existingAbility.Level) ||
				(levelOverridePolicy.HasFlag(LevelComparison.Equal) && abilityLevel == existingAbility.Level);

			if (shouldOverride)
			{
				existingAbility.Level = abilityLevel;
			}

			return existingAbility.Handle;
		}

		var newAbility = new Ability(abilityData, abilityLevel, removalPolicy, inhibitionPolicy, sourceEntity);
		GrantedAbilities.Add(newAbility.Handle);
		_grantSources[newAbility] = [sourceActiveEffectHandle];

		newAbility.IsInhibited = sourceActiveEffectHandle.IsInhibited;
		_inhibitSources[newAbility] = newAbility.IsInhibited ? [sourceActiveEffectHandle] : [];

		return newAbility.Handle;
	}

	internal void RemoveGrantedAbility(AbilityHandle abilityHandle, ActiveEffectHandle effectHandle)
	{
		RemoveGrantedAbility(GrantedAbilities.FirstOrDefault(x => x == abilityHandle)?.Ability, effectHandle);
	}

	internal void RemoveGrantedAbility(Ability? abilityToRemove, ActiveEffectHandle effectHandle)
	{
		if (abilityToRemove is null
			|| abilityToRemove.GrantedAbilityRemovalPolicy == AbilityDeactivationPolicy.Ignore
			|| _grantSources[abilityToRemove] is null)
		{
			return;
		}

		List<ActiveEffectHandle>? grantSources = _grantSources[abilityToRemove];
		List<ActiveEffectHandle>? inhibitSources = _inhibitSources[abilityToRemove];

		Debug.Assert(
			grantSources is not null,
			"Grant grantSources should not be null at this point.");
		Debug.Assert(
			inhibitSources is not null,
			"InhibitAbilityBasedOnPolicy grantSources should not be null if grant grantSources are not null.");

		grantSources.Remove(effectHandle);
		inhibitSources.Remove(effectHandle);

		if (grantSources.Count > 0)
		{
			if (inhibitSources.Count == grantSources.Count)
			{
				InhibitAbilityBasedOnPolicy(abilityToRemove);
			}

			return;
		}

		switch (abilityToRemove.GrantedAbilityRemovalPolicy)
		{
			case AbilityDeactivationPolicy.Ignore:
				return;

			case AbilityDeactivationPolicy.CancelImmediately:
				if (abilityToRemove.IsActive)
				{
					abilityToRemove.Deactivate();
				}

				RemoveAbility(abilityToRemove);
				return;

			case AbilityDeactivationPolicy.RemoveOnEnd:
				if (abilityToRemove.IsActive)
				{
					abilityToRemove.OnAbilityDeactivated += RemoveAbility;
				}

				return;
		}
	}

	internal void InhibitGrantedAbility(AbilityHandle abilityHandle, bool inhibit, ActiveEffectHandle effectHandle)
	{
		InhibitGrantedAbility(GrantedAbilities.FirstOrDefault(x => x == abilityHandle)?.Ability, inhibit, effectHandle);
	}

	internal void InhibitGrantedAbility(Ability? abilityToInhibit, bool inhibit, ActiveEffectHandle effectHandle)
	{
		if (abilityToInhibit is null
			|| abilityToInhibit.GrantedAbilityInhibitionPolicy == AbilityDeactivationPolicy.Ignore
			|| _grantSources[abilityToInhibit] is null)
		{
			return;
		}

		List<ActiveEffectHandle>? inhibitSources = _inhibitSources[abilityToInhibit];

		Debug.Assert(
			inhibitSources is not null,
			"InhibitAbilityBasedOnPolicy grantSources should not be null if grant grantSources are not null.");

		if (inhibit)
		{
			inhibitSources.Add(effectHandle);

			if (inhibitSources.Count > 1)
			{
				return;
			}

			InhibitAbilityBasedOnPolicy(abilityToInhibit);
		}
		else
		{
			inhibitSources.Remove(effectHandle);

			if (inhibitSources.Count == 0)
			{
				abilityToInhibit.IsInhibited = false;
			}
		}
	}

	private void InhibitAbilityBasedOnPolicy(Ability abilityToInhibit)
	{
		switch (abilityToInhibit.GrantedAbilityInhibitionPolicy)
		{
			case AbilityDeactivationPolicy.Ignore:
				return;

			case AbilityDeactivationPolicy.CancelImmediately:
				if (abilityToInhibit.IsActive)
				{
					abilityToInhibit.Deactivate();
				}

				InhibitAbility(abilityToInhibit);
				return;

			case AbilityDeactivationPolicy.RemoveOnEnd:
				if (abilityToInhibit.IsActive)
				{
					abilityToInhibit.OnAbilityDeactivated += InhibitAbility;
				}

				return;
		}
	}

	private void RemoveAbility(Ability abilityToRemove)
	{
		abilityToRemove.OnAbilityDeactivated -= RemoveAbility;

		if (_grantSources[abilityToRemove]?.Count > 0)
		{
			return;
		}

		abilityToRemove.Handle.Free();
		GrantedAbilities.Remove(abilityToRemove.Handle);
	}

	private void InhibitAbility(Ability abilityToInhibit)
	{
		abilityToInhibit.OnAbilityDeactivated -= InhibitAbility;

		if (_grantSources[abilityToInhibit]?.Count == _inhibitSources[abilityToInhibit]?.Count)
		{
			abilityToInhibit.IsInhibited = true;
		}
	}
}
