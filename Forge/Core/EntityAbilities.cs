// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Abilities;
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
	private readonly Dictionary<Ability, List<IAbilityGrantSource>> _grantSources = [];

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
	/// <param name="failureFlags">Flags indicating the failure reasons for the abilities activation.</param>
	/// <returns>Returns <see langword="true"/> if any abilities were activated; otherwise, <see langword="false"/>.
	/// </returns>
	public bool TryActivateAbilitiesByTag(
		TagContainer tagsToActivate,
		IForgeEntity? target,
		out AbilityActivationFailures[] failureFlags)
	{
		if (tagsToActivate is null)
		{
			failureFlags =
				[.. Enumerable.Repeat(AbilityActivationFailures.InvalidTagConfiguration, GrantedAbilities.Count)];
			return false;
		}

		var anyActivated = false;
		failureFlags =
				[.. Enumerable.Repeat(AbilityActivationFailures.TargetTagNotPresent, GrantedAbilities.Count)];

		AbilityHandle[] array = [.. GrantedAbilities];
		for (var i = 0; i < array.Length; i++)
		{
			AbilityHandle? handle = array[i];
			Ability? ability = handle?.Ability;
			if (ability is null)
			{
				continue;
			}

			TagContainer? abilityTags = ability.AbilityData.AbilityTags;
			if (abilityTags?.HasAny(tagsToActivate) == true)
			{
				anyActivated |= ability.TryActivateAbility(target, out failureFlags[i]);
			}
		}

		return anyActivated;
	}

	/// <summary>
	/// Grants an ability and activates it once. The ability grant will be removed if activation fails or after it ends.
	/// </summary>
	/// <param name="abilityData">The configuration data of the ability to grant and activate.</param>
	/// <param name="abilityLevel">The level at which to grant the ability.</param>
	/// <param name="levelOverridePolicy">The policy for overriding the level of an existing granted ability.</param>
	/// <param name="failureFlags">Flags indicating the failure reasons for the ability activation.</param>
	/// <param name="targetEntity">The target entity for the ability activation, if any.</param>
	/// <param name="sourceEntity">The source entity of the granted ability, if any.</param>
	/// <returns>The handle of the granted and activated ability, or <see langword="null"/> if activation failed.
	/// </returns>
	public AbilityHandle? GrantAbilityAndActivateOnce(
		AbilityData abilityData,
		int abilityLevel,
		LevelComparison levelOverridePolicy,
		out AbilityActivationFailures failureFlags,
		IForgeEntity? targetEntity = null,
		IForgeEntity? sourceEntity = null)
	{
		var grantSource = new TransientGrantSource();

		AbilityHandle abilityHandle = GrantAbility(
			abilityData,
			abilityLevel,
			levelOverridePolicy,
			grantSource,
			sourceEntity);

		abilityHandle.Activate(out failureFlags, targetEntity);

		RemoveGrantedAbility(abilityHandle, grantSource);

		return abilityHandle.IsValid ? abilityHandle : null;
	}

	/// <summary>
	/// Grants an ability permanently.
	/// </summary>
	/// <remarks>
	/// Abilities granted permanently cannot be removed nor inhibited.
	/// </remarks>
	/// <param name="abilityData">The configuration data of the ability to grant.</param>
	/// <param name="abilityLevel">The level at which to grant the ability.</param>
	/// <param name="levelOverridePolicy">The policy for overriding the level of an existing granted ability.</param>
	/// <param name="sourceEntity">The source entity of the granted ability, if any.</param>
	/// <returns>The handle of the granted ability.</returns>
	public AbilityHandle GrantAbilityPermanently(
		AbilityData abilityData,
		int abilityLevel,
		LevelComparison levelOverridePolicy,
		IForgeEntity? sourceEntity)
	{
		Ability? existingAbility =
			GrantedAbilities.FirstOrDefault(x => x?.Ability?.AbilityData == abilityData)?.Ability;

		if (existingAbility is not null && existingAbility.SourceEntity == sourceEntity)
		{
			_grantSources[existingAbility].Add(new PermanentGrantSource());

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

			return existingAbility.Handle;
		}

		var newAbility = new Ability(Owner, abilityData, abilityLevel, sourceEntity);
		GrantedAbilities.Add(newAbility.Handle);
		_grantSources[newAbility] = [new PermanentGrantSource()];

		return newAbility.Handle;
	}

	internal AbilityHandle GrantAbility(
		AbilityData abilityData,
		int abilityLevel,
		LevelComparison levelOverridePolicy,
		IAbilityGrantSource grantSource,
		IForgeEntity? sourceEntity)
	{
		Ability? existingAbility =
			GrantedAbilities.FirstOrDefault(x => x?.Ability?.AbilityData == abilityData)?.Ability;

		if (existingAbility is not null && existingAbility.SourceEntity == sourceEntity)
		{
			// Ability already granted, just add the new source to the mapping.
			_grantSources[existingAbility].Add(grantSource);

			// If the ability was fully inhibited, this new grant may need to re-enable it.
			existingAbility.IsInhibited = CheckIsInhibited();

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

		var newAbility = new Ability(Owner, abilityData, abilityLevel, sourceEntity);
		GrantedAbilities.Add(newAbility.Handle);
		_grantSources[newAbility] = [grantSource];

		newAbility.IsInhibited = grantSource.IsInhibited;

		return newAbility.Handle;
	}

	internal void RemoveGrantedAbility(AbilityHandle abilityHandle, IAbilityGrantSource grantSource)
	{
		RemoveGrantedAbility(GrantedAbilities.FirstOrDefault(x => x == abilityHandle)?.Ability, grantSource);
	}

	internal void RemoveGrantedAbility(Ability? abilityToRemove, IAbilityGrantSource grantSource)
	{
		if (abilityToRemove is null || grantSource.RemovalPolicy == AbilityDeactivationPolicy.Ignore)
		{
			return;
		}

		List<IAbilityGrantSource> grantSources = _grantSources[abilityToRemove];

		grantSources.Remove(grantSource);

		if (grantSources.Count > 0)
		{
			if (CheckIsInhibited())
			{
				InhibitAbilityBasedOnPolicy(abilityToRemove, grantSource.InhibitionPolicy);
			}

			return;
		}

		switch (grantSource.RemovalPolicy)
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
					return;
				}

				RemoveAbility(abilityToRemove);
				return;
		}
	}

	internal void InhibitGrantedAbility(AbilityHandle abilityHandle, IAbilityGrantSource grantSource)
	{
		InhibitGrantedAbility(GrantedAbilities.FirstOrDefault(x => x == abilityHandle)?.Ability, grantSource);
	}

	internal void InhibitGrantedAbility(Ability? abilityToInhibit, IAbilityGrantSource grantSource)
	{
		if (abilityToInhibit is null || grantSource.InhibitionPolicy == AbilityDeactivationPolicy.Ignore)
		{
			return;
		}

		InhibitAbilityBasedOnPolicy(abilityToInhibit, grantSource.InhibitionPolicy);
	}

	internal void NotifyAbilityEnded(AbilityEndedData abilityEndedData)
	{
		OnAbilityEnded?.Invoke(abilityEndedData);
	}

	private void InhibitAbilityBasedOnPolicy(Ability abilityToInhibit, AbilityDeactivationPolicy inhibitionPolicy)
	{
		switch (inhibitionPolicy)
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

		if (_grantSources.TryGetValue(abilityToRemove, out List<IAbilityGrantSource>? grantSources)
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
		abilityToInhibit.IsInhibited = CheckIsInhibited();
	}

	private bool CheckIsInhibited()
	{
		return _grantSources.Values.All(x =>
		{
			return x.TrueForAll(source => source.IsInhibited);
		});
	}
}
