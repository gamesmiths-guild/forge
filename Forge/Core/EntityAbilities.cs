// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Manager for handling an entity's abilities.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EntityAbilities"/> class.
/// </remarks>
/// <param name="owner">The owner of this manager.</param>
public class EntityAbilities(IForgeEntity owner)
{
	private readonly Dictionary<Ability, List<ActiveEffectHandle>?> _grantSources = [];

	private readonly Dictionary<Ability, List<ActiveEffectHandle>?> _inhibitSources = [];

	/// <summary>
	/// Event invoked when an ability ends.
	/// </summary>
	public event Action<AbilityEndedData>? OnAbilityEnded;

	/// <summary>
	/// Gets the owner of this effects manager.
	/// </summary>
	public IForgeEntity Owner { get; } = owner;

	/// <summary>
	/// Gets the set of abilities currently granted to the entity.
	/// </summary>
	public HashSet<AbilityHandle> GrantedAbilities { get; } = [];

	/// <summary>
	/// Gets the tags that block abilities from being used.
	/// </summary>
	public EntityTags BlockedAbilityTags { get; } = new EntityTags(new TagContainer(owner.Tags.BaseTags.TagsManager));

	/// <summary>
	/// Tries to get a granted ability from its data.
	/// </summary>
	/// <param name="abilityData">The data of the ability to find.</param>
	/// <param name="abilityHandle">The handle of the found ability.</param>
	/// <param name="source">The source entity of the ability, if any.</param>
	/// <returns><see>true</see> if the ability was found; otherwise, <c>false</c>.</returns>
	public bool TryGetAbility(
		AbilityData abilityData,
		[NotNullWhen(true)] out AbilityHandle? abilityHandle,
		IForgeEntity? source = null)
	{
		Ability? ability = GrantedAbilities.FirstOrDefault(
			x => x?.Ability?.AbilityData == abilityData && x.Ability?.SourceEntity == source)?.Ability;
		if (ability is not null)
		{
			abilityHandle = ability.Handle;
			return true;
		}

		abilityHandle = null;
		return false;
	}

	/// <summary>
	/// Cancels all active abilities whose AbilityTags overlap the provided tags.
	/// For PerEntity abilities, cancels the single active instance.
	/// For per-execution abilities, cancels all active instances.
	/// </summary>
	/// <param name="tagsToCancel">Tags that identify abilities to cancel.</param>
	public void CancelAbilitiesWithTag(TagContainer tagsToCancel)
	{
		if (tagsToCancel is null)
		{
			return;
		}

		// Enumerate snapshot to avoid modification during cancel.
		foreach (AbilityHandle? handle in GrantedAbilities.ToArray())
		{
			Ability? ability = handle?.Ability;
			if (ability is null)
			{
				continue;
			}

			TagContainer? abilityTags = ability.AbilityData.AbilityTags;
			if (abilityTags?.HasAny(tagsToCancel) == true)
			{
				ability.CancelAllInstances();
			}
		}
	}

	/// <summary>
	/// Tries to activate all abilities whose AbilityTags overlap the provided tags.
	/// </summary>
	/// <param name="tagsToActivate">Tags that identify abilities to activate.</param>
	/// <param name="target">Optional target for the abilities.</param>
	/// <param name="activationResult">The result of the ability activation attempt.</param>
	/// <returns>Returns <see langword="true"/> if any abilities were activated; otherwise, <see langword="false"/>.
	/// </returns>
	public bool TryActivateAbilitiesByTag(
		TagContainer tagsToActivate,
		IForgeEntity? target,
		out AbilityActivationResult activationResult)
	{
		if (tagsToActivate is null)
		{
			activationResult = AbilityActivationResult.FailedInvalidTagConfiguration;
			return false;
		}

		var anyActivated = false;
		activationResult = AbilityActivationResult.FailedTargetTagNotPresent;

		// Enumerate snapshot to avoid modification during activation.
		foreach (AbilityHandle? handle in GrantedAbilities.ToArray())
		{
			Ability? ability = handle?.Ability;
			if (ability is null)
			{
				continue;
			}

			TagContainer? abilityTags = ability.AbilityData.AbilityTags;
			if (abilityTags?.HasAny(tagsToActivate) == true)
			{
				anyActivated |= ability.TryActivateAbility(target, out activationResult);
			}
		}

		return anyActivated;
	}

	/// <summary>
	/// Grants an ability and activates it once.
	/// </summary>
	/// <param name="abilityData">The configuration data of the ability to grant and activate.</param>
	/// <param name="abilityLevel">The level at which to grant the ability.</param>
	/// <param name="removalPolicy">The policy for removing the granted ability.</param>
	/// <param name="inhibitionPolicy">The policy for inhibiting the granted ability.</param>
	/// <param name="levelOverridePolicy">The policy for overriding the level of an existing granted ability.</param>
	/// <param name="sourceActiveEffectHandle">The handle of the active effect that is the source of this granted
	/// ability.</param>
	/// <param name="sourceEntity">The source entity of the granted ability, if any.</param>
	/// <param name="activationResult">The result of the ability activation attempt.</param>
	/// <returns>Returns <see langword="true"/> if the ability was successfully activated; otherwise,
	/// <see langword="false"/>.</returns>
	public bool GrantAbilityAndActivateOnce(
		AbilityData abilityData,
		int abilityLevel,
		AbilityDeactivationPolicy removalPolicy,
		AbilityDeactivationPolicy inhibitionPolicy,
		LevelComparison levelOverridePolicy,
		ActiveEffectHandle sourceActiveEffectHandle,
		IForgeEntity? sourceEntity,
		out AbilityActivationResult activationResult)
	{
		AbilityHandle abilityHandle = GrantAbility(
			abilityData,
			abilityLevel,
			removalPolicy,
			inhibitionPolicy,
			levelOverridePolicy,
			sourceActiveEffectHandle,
			sourceEntity);

		return abilityHandle.Activate(out activationResult, null);
	}

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

		var newAbility = new Ability(Owner, abilityData, abilityLevel, removalPolicy, inhibitionPolicy, sourceEntity);
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
			if (_grantSources.TryGetValue(existingAbility, out List<ActiveEffectHandle>? grantSources)
				&& grantSources is not null)
			{
				List<ActiveEffectHandle>? inhibitSources = _inhibitSources[existingAbility];

				Validation.Assert(
					inhibitSources is not null,
					"inhibitSources should not be null if grant grantSources are not null.");

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

		var newAbility = new Ability(Owner, abilityData, abilityLevel, removalPolicy, inhibitionPolicy, sourceEntity);
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
			|| !_grantSources.TryGetValue(abilityToRemove, out List<ActiveEffectHandle>? grantSources)
			|| grantSources is null)
		{
			return;
		}

		List<ActiveEffectHandle>? inhibitSources = _inhibitSources[abilityToRemove];

		Validation.Assert(
			inhibitSources is not null,
			"InhibitAbilityBasedOnPolicy inhibitSources should not be null if grant grantSources are not null.");

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
					abilityToRemove.End();
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
			|| !_inhibitSources.TryGetValue(abilityToInhibit, out List<ActiveEffectHandle>? inhibitSources)
			|| inhibitSources is null)
		{
			return;
		}

		if (inhibit)
		{
			inhibitSources.Add(effectHandle);

			InhibitAbilityBasedOnPolicy(abilityToInhibit);
		}
		else
		{
			inhibitSources.Remove(effectHandle);

			if (_inhibitSources[abilityToInhibit]?.Count < _grantSources[abilityToInhibit]?.Count)
			{
				abilityToInhibit.IsInhibited = false;
			}
		}
	}

	internal void NotifyAbilityEnded(AbilityEndedData abilityEndedData)
	{
		OnAbilityEnded?.Invoke(abilityEndedData);
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
					abilityToInhibit.End();
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

		if (_grantSources.TryGetValue(abilityToRemove, out List<ActiveEffectHandle>? grantSources)
			&& grantSources?.Count > 0)
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
