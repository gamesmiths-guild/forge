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
	private readonly HashSet<AbilityHandle> _grantedAbilities = [];
	private Action<Ability>? _removeAbility;
	private Action<Ability>? _inhibitAbility;

	/// <summary>
	/// Event invoked when an ability is granted to the entity, carrying its handle.
	/// </summary>
	/// <remarks>
	/// Raised once per <see cref="Ability"/>, after the grant has settled, so the handle already reports its level and
	/// inhibition state. Granting an ability the entity already has adds a grant source instead of a second ability,
	/// and raises <see cref="OnAbilityChanged"/> if that changed anything.
	/// </remarks>
	public event Action<AbilityHandle>? OnAbilityGranted;

	/// <summary>
	/// Event invoked when a granted ability's level or inhibition changes, carrying its handle.
	/// </summary>
	/// <remarks>
	/// A change that resolves to the same values — a repeat grant that neither overrides the level nor flips
	/// inhibition — raises nothing.
	/// </remarks>
	public event Action<AbilityHandle>? OnAbilityChanged;

	/// <summary>
	/// Event invoked when an ability is removed from the entity, carrying its handle.
	/// </summary>
	/// <remarks>
	/// Raised once the last grant source is gone, before the handle is invalidated, so the handle can still be read
	/// inside the handler and reports <see cref="AbilityHandle.IsValid"/> as <see langword="false"/> afterwards.
	/// Losing one of several grant sources keeps the ability and raises <see cref="OnAbilityChanged"/> instead.
	/// </remarks>
	public event Action<AbilityHandle>? OnAbilityRemoved;

	/// <summary>
	/// Event invoked when an ability becomes active, carrying its handle.
	/// </summary>
	/// <remarks>
	/// The exact counterpart of <see cref="OnAbilityEnded"/>: both track the ability, not its instances, so a second
	/// concurrent instance of a per-execution ability raises neither. Raised as the ability becomes active and before
	/// its behavior starts, so it always precedes the matching <see cref="OnAbilityEnded"/> — including for a behavior
	/// that finishes synchronously.
	/// </remarks>
	public event Action<AbilityHandle>? OnAbilityActivated;

	/// <summary>
	/// Event invoked when an ability ends.
	/// </summary>
	public event Action<AbilityEndedData>? OnAbilityEnded;

	/// <summary>
	/// Event invoked when an activation attempt is refused, carrying the ability's handle and every reason it failed.
	/// </summary>
	/// <remarks>
	/// The direct callers of the activation API already receive these flags as an out parameter. This event covers the
	/// activations nobody is holding the result of — those driven by <see cref="AbilityTriggerData"/> tags and events,
	/// and by the Statescript activation nodes — which are otherwise silent.
	/// </remarks>
	public event Action<AbilityHandle, AbilityActivationFailures>? OnAbilityActivationFailed;

	/// <summary>
	/// Gets the owner of this effects manager.
	/// </summary>
	public IForgeEntity Owner { get; } = owner;

	/// <summary>
	/// Gets the set of abilities currently granted to the entity.
	/// </summary>
	/// <remarks>
	/// Read-only: the manager keeps this set in step with the grant sources behind each ability, so grant and removal
	/// go through <see cref="GrantAbilityPermanently"/>, <c>TryGrantAbilityAndActivateOnce</c> and the effect
	/// components rather than through this set. The collection is live, so a handle removed while it is being
	/// enumerated invalidates the enumeration; copy it first when the loop body can remove abilities.
	/// </remarks>
	public IReadOnlyCollection<AbilityHandle> GrantedAbilities => _grantedAbilities;

	/// <summary>
	/// Gets the tags that block abilities from being used.
	/// </summary>
	public EntityTags BlockedAbilityTags { get; } = new EntityTags(new TagContainer(owner.Tags.BaseTags.TagsManager));

	/// <summary>
	/// Tries to get a granted ability from its data.
	/// </summary>
	/// <remarks>
	/// When <paramref name="source"/> is <see langword="null"/> the lookup matches regardless of the granting source;
	/// if the same ability data was granted by multiple sources, which instance is returned is unspecified. Pass a
	/// source entity to find the instance granted by that specific source. Since <see langword="null"/> is itself a
	/// valid granting source, pass <paramref name="exactSourceMatch"/> as <see langword="true"/> to find the instance
	/// granted without a source.
	/// </remarks>
	/// <param name="abilityData">The data of the ability to find.</param>
	/// <param name="abilityHandle">The handle of the found ability.</param>
	/// <param name="source">The granting source entity to filter by, or <see langword="null"/> to match any source
	/// (or, when <paramref name="exactSourceMatch"/> is <see langword="true"/>, only the sourceless instance).</param>
	/// <param name="exactSourceMatch">When <see langword="true"/>, only the instance whose granting source is exactly
	/// <paramref name="source"/> matches, including <see langword="null"/> for abilities granted without a source.
	/// </param>
	/// <returns><see>true</see> if the ability was found; otherwise, <c>false</c>.</returns>
	public bool TryGetAbility(
		AbilityData abilityData,
		[NotNullWhen(true)] out AbilityHandle? abilityHandle,
		IForgeEntity? source = null,
		bool exactSourceMatch = false)
	{
		Ability? ability = GrantedAbilities.FirstOrDefault(
			x => x?.Ability?.AbilityData == abilityData
				&& (exactSourceMatch
					? x.Ability?.SourceEntity == source
					: source is null || x.Ability?.SourceEntity == source))?.Ability;
		if (ability is not null)
		{
			abilityHandle = ability.Handle;
			return true;
		}

		abilityHandle = null;
		return false;
	}

	/// <summary>
	/// Cancels all active abilities that carry any of <paramref name="withTags"/> and none of
	/// <paramref name="withoutTags"/>.
	/// For PerEntity abilities, cancels the single active instance.
	/// For per-execution abilities, cancels all active instances.
	/// </summary>
	/// <remarks>
	/// <para>Each container is an independent filter: a <see langword="null"/> or empty container means "don't filter
	/// on this side". Passing nothing for either one therefore cancels every active ability, so guard against empty
	/// containers at the call site when they stand for "cancel nothing".</para>
	/// <para>An ability with no AbilityTags is never matched by <paramref name="withTags"/>, but it always satisfies
	/// <paramref name="withoutTags"/>, since it carries none of them.</para>
	/// </remarks>
	/// <param name="withTags">Abilities must have any of these tags to be canceled, or <see langword="null"/> to not
	/// filter by required tags.</param>
	/// <param name="withoutTags">Abilities having any of these tags are spared, or <see langword="null"/> to not filter
	/// by blocking tags.</param>
	public void CancelAbilities(TagContainer? withTags, TagContainer? withoutTags)
	{
		TagContainer? requiredTags = withTags?.IsEmpty == false ? withTags : null;
		TagContainer? blockingTags = withoutTags?.IsEmpty == false ? withoutTags : null;

		foreach (AbilityHandle? handle in GrantedAbilities.ToArray())
		{
			Ability? ability = handle?.Ability;
			if (ability is null)
			{
				continue;
			}

			TagContainer? abilityTags = ability.AbilityData.AbilityTags;

			if (requiredTags is not null && abilityTags?.HasAny(requiredTags) != true)
			{
				continue;
			}

			if (blockingTags is not null && abilityTags?.HasAny(blockingTags) == true)
			{
				continue;
			}

			ability.CancelAllInstances();
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
		if (!TryBeginActivationByTag(tagsToActivate, out AbilityHandle[] handles, out failureFlags))
		{
			return false;
		}

		bool anyActivated = false;

		for (int i = 0; i < handles.Length; i++)
		{
			Ability? ability = handles[i]?.Ability;
			if (ability is null || !MatchesTags(ability, tagsToActivate))
			{
				continue;
			}

			anyActivated |= ability.TryActivateAbility(target, out failureFlags[i], 0f);
		}

		return anyActivated;
	}

	/// <summary>
	/// Tries to activate all abilities whose AbilityTags overlap the provided tags, passing strongly-typed activation
	/// data to each of them.
	/// </summary>
	/// <remarks>
	/// Because a tag can select several abilities, <typeparamref name="TData"/> is not guaranteed to match every one of
	/// them. Abilities whose behavior does not accept <typeparamref name="TData"/> simply ignore it and start through
	/// the untyped path, so mismatched data is never an error.
	/// </remarks>
	/// <typeparam name="TData">The type of the data to pass to the ability behaviors.</typeparam>
	/// <param name="tagsToActivate">Tags that identify abilities to activate.</param>
	/// <param name="target">Optional target for the abilities.</param>
	/// <param name="data">Additional data to pass to the behaviors.</param>
	/// <param name="failureFlags">Flags indicating the failure reasons for the abilities activation.</param>
	/// <returns>Returns <see langword="true"/> if any abilities were activated; otherwise, <see langword="false"/>.
	/// </returns>
	public bool TryActivateAbilitiesByTag<TData>(
		TagContainer tagsToActivate,
		IForgeEntity? target,
		TData data,
		out AbilityActivationFailures[] failureFlags)
	{
		if (!TryBeginActivationByTag(tagsToActivate, out AbilityHandle[] handles, out failureFlags))
		{
			return false;
		}

		bool anyActivated = false;

		for (int i = 0; i < handles.Length; i++)
		{
			Ability? ability = handles[i]?.Ability;
			if (ability is null || !MatchesTags(ability, tagsToActivate))
			{
				continue;
			}

			anyActivated |= ability.TryActivateAbility(target, out failureFlags[i], data, 0f);
		}

		return anyActivated;
	}

	/// <summary>
	/// Grants an ability and activates it once. The ability grant will be removed if activation fails or after it ends.
	/// </summary>
	/// <remarks>
	/// <paramref name="grantedAbility"/> is only a handle to an ability that is <b>still running</b>. An ability that
	/// activates and ends within the call — the common one-shot case — leaves it <see langword="null"/> even though the
	/// activation succeeded, so read the return value, not the handle, to know whether the ability activated.
	/// </remarks>
	/// <param name="abilityData">The configuration data of the ability to grant and activate.</param>
	/// <param name="abilityLevel">The level at which to grant the ability.</param>
	/// <param name="levelOverridePolicy">The policy for overriding the level of an existing granted ability.</param>
	/// <param name="failureFlags">Flags indicating the failure reasons for the ability activation.</param>
	/// <param name="grantedAbility">The handle of the granted ability while it remains active, or
	/// <see langword="null"/> once the grant has been removed.</param>
	/// <param name="targetEntity">The target entity for the ability activation, if any.</param>
	/// <param name="sourceEntity">The source entity of the granted ability, if any.</param>
	/// <returns>Returns <see langword="true"/> if the ability was activated; otherwise, <see langword="false"/>.
	/// </returns>
	public bool TryGrantAbilityAndActivateOnce(
		AbilityData abilityData,
		int abilityLevel,
		LevelComparison levelOverridePolicy,
		out AbilityActivationFailures failureFlags,
		out AbilityHandle? grantedAbility,
		IForgeEntity? targetEntity = null,
		IForgeEntity? sourceEntity = null)
	{
		AbilityHandle abilityHandle = GrantTransiently(
			abilityData,
			abilityLevel,
			levelOverridePolicy,
			sourceEntity,
			out TransientGrantSource grantSource);

		bool activated = abilityHandle.TryActivate(out failureFlags, targetEntity);

		RemoveGrantedAbility(abilityHandle, grantSource);

		grantedAbility = abilityHandle.IsValid ? abilityHandle : null;

		return activated;
	}

	/// <summary>
	/// Grants an ability and activates it once with strongly-typed activation data. The ability grant will be removed
	/// if activation fails or after it ends.
	/// </summary>
	/// <remarks>
	/// <para>Unlike <see cref="TryActivateAbilitiesByTag{TData}"/>, the activated ability is known up front, so
	/// <typeparamref name="TData"/> can be matched to it. An ability whose behavior does not accept
	/// <typeparamref name="TData"/> still activates, ignoring the data.</para>
	/// <para><paramref name="grantedAbility"/> is only a handle to an ability that is <b>still running</b>. An ability
	/// that activates and ends within the call — the common one-shot case — leaves it <see langword="null"/> even
	/// though the activation succeeded, so read the return value, not the handle, to know whether the ability
	/// activated.</para>
	/// </remarks>
	/// <typeparam name="TData">The type of the data to pass to the ability behavior.</typeparam>
	/// <param name="abilityData">The configuration data of the ability to grant and activate.</param>
	/// <param name="abilityLevel">The level at which to grant the ability.</param>
	/// <param name="levelOverridePolicy">The policy for overriding the level of an existing granted ability.</param>
	/// <param name="data">Additional data to pass to the behavior.</param>
	/// <param name="failureFlags">Flags indicating the failure reasons for the ability activation.</param>
	/// <param name="grantedAbility">The handle of the granted ability while it remains active, or
	/// <see langword="null"/> once the grant has been removed.</param>
	/// <param name="targetEntity">The target entity for the ability activation, if any.</param>
	/// <param name="sourceEntity">The source entity of the granted ability, if any.</param>
	/// <returns>Returns <see langword="true"/> if the ability was activated; otherwise, <see langword="false"/>.
	/// </returns>
	public bool TryGrantAbilityAndActivateOnce<TData>(
		AbilityData abilityData,
		int abilityLevel,
		LevelComparison levelOverridePolicy,
		TData data,
		out AbilityActivationFailures failureFlags,
		out AbilityHandle? grantedAbility,
		IForgeEntity? targetEntity = null,
		IForgeEntity? sourceEntity = null)
	{
		AbilityHandle abilityHandle = GrantTransiently(
			abilityData,
			abilityLevel,
			levelOverridePolicy,
			sourceEntity,
			out TransientGrantSource grantSource);

		bool activated = abilityHandle.TryActivate(data, out failureFlags, targetEntity);

		RemoveGrantedAbility(abilityHandle, grantSource);

		grantedAbility = abilityHandle.IsValid ? abilityHandle : null;

		return activated;
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
			bool wasInhibited = existingAbility.IsInhibited;
			int previousLevel = existingAbility.Level;

			_grantSources[existingAbility].Add(new PermanentGrantSource());

			// If the ability was fully inhibited, this permanent grant should re-enable it.
			existingAbility.IsInhibited = false;

			bool shouldOverride =
				(levelOverridePolicy.HasFlag(LevelComparison.Higher) && abilityLevel > existingAbility.Level) ||
				(levelOverridePolicy.HasFlag(LevelComparison.Lower) && abilityLevel < existingAbility.Level) ||
				(levelOverridePolicy.HasFlag(LevelComparison.Equal) && abilityLevel == existingAbility.Level);

			if (shouldOverride)
			{
				existingAbility.Level = abilityLevel;
			}

			NotifyAbilityChanged(existingAbility, wasInhibited, previousLevel);

			return existingAbility.Handle;
		}

		var newAbility = new Ability(Owner, abilityData, abilityLevel, sourceEntity);
		_grantedAbilities.Add(newAbility.Handle);
		_grantSources[newAbility] = [new PermanentGrantSource()];

		OnAbilityGranted?.Invoke(newAbility.Handle);

		return newAbility.Handle;
	}

	/// <summary>
	/// Updates all active ability behaviors. Call this once per frame alongside
	/// <see cref="Effects.EffectsManager.UpdateEffects(double)"/>.
	/// </summary>
	/// <param name="deltaTime">The time elapsed since the last update, in seconds.</param>
	public void UpdateAbilities(double deltaTime)
	{
		foreach (AbilityHandle handle in GrantedAbilities)
		{
			handle.Ability?.UpdateBehaviors(deltaTime);
		}
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
			bool wasInhibited = existingAbility.IsInhibited;
			int previousLevel = existingAbility.Level;

			// Ability already granted, just add the new source to the mapping.
			_grantSources[existingAbility].Add(grantSource);

			// If the ability was fully inhibited, this new grant may need to re-enable it.
			existingAbility.IsInhibited = CheckIsInhibited(existingAbility);

			bool shouldOverride =
				(levelOverridePolicy.HasFlag(LevelComparison.Higher) && abilityLevel > existingAbility.Level) ||
				(levelOverridePolicy.HasFlag(LevelComparison.Lower) && abilityLevel < existingAbility.Level) ||
				(levelOverridePolicy.HasFlag(LevelComparison.Equal) && abilityLevel == existingAbility.Level);

			if (shouldOverride)
			{
				existingAbility.Level = abilityLevel;
			}

			NotifyAbilityChanged(existingAbility, wasInhibited, previousLevel);

			return existingAbility.Handle;
		}

		var newAbility = new Ability(Owner, abilityData, abilityLevel, sourceEntity);
		_grantedAbilities.Add(newAbility.Handle);
		_grantSources[newAbility] = [grantSource];

		// Set before announcing the grant, so the handle already reports its settled inhibition state and an inhibited
		// grant never reads as a change to an ability nobody has been told about yet.
		newAbility.IsInhibited = grantSource.IsInhibited;

		OnAbilityGranted?.Invoke(newAbility.Handle);

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
			if (CheckIsInhibited(abilityToRemove))
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
					_removeAbility = RemoveAbility;
					abilityToRemove.OnAbilityDeactivated += _removeAbility;
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

	internal void NotifyAbilityActivated(AbilityHandle abilityHandle)
	{
		OnAbilityActivated?.Invoke(abilityHandle);
	}

	internal void NotifyAbilityActivationFailed(AbilityHandle abilityHandle, AbilityActivationFailures failureFlags)
	{
		OnAbilityActivationFailed?.Invoke(abilityHandle, failureFlags);
	}

	private static bool MatchesTags(Ability ability, TagContainer tagsToActivate)
	{
		return ability.AbilityData.AbilityTags?.HasAny(tagsToActivate) == true;
	}

	// Snapshots the granted abilities and seeds the per-ability failure flags for a tag-driven activation. The snapshot
	// keeps the indices stable while activations grant or remove other abilities.
	private bool TryBeginActivationByTag(
		TagContainer tagsToActivate,
		out AbilityHandle[] handles,
		out AbilityActivationFailures[] failureFlags)
	{
		if (tagsToActivate is null)
		{
			handles = [];
			failureFlags =
				[.. Enumerable.Repeat(AbilityActivationFailures.InvalidTagConfiguration, GrantedAbilities.Count)];
			return false;
		}

		handles = [.. GrantedAbilities];
		failureFlags = [.. Enumerable.Repeat(AbilityActivationFailures.TargetTagNotPresent, GrantedAbilities.Count)];
		return true;
	}

	private AbilityHandle GrantTransiently(
		AbilityData abilityData,
		int abilityLevel,
		LevelComparison levelOverridePolicy,
		IForgeEntity? sourceEntity,
		out TransientGrantSource grantSource)
	{
		grantSource = new TransientGrantSource();

		return GrantAbility(
			abilityData,
			abilityLevel,
			levelOverridePolicy,
			grantSource,
			sourceEntity);
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
					_inhibitAbility = InhibitAbility;
					abilityToInhibit.OnAbilityDeactivated += _inhibitAbility;
				}

				return;
		}
	}

	private void RemoveAbility(Ability abilityToRemove)
	{
		if (_removeAbility is not null)
		{
			abilityToRemove.OnAbilityDeactivated -= _removeAbility;
			_removeAbility = null;
		}

		if (_grantSources.TryGetValue(abilityToRemove, out List<IAbilityGrantSource>? grantSources)
			&& grantSources?.Count > 0)
		{
			return;
		}

		abilityToRemove.Cleanup();

		// Raised before the handle is freed, so handlers can still read what went away.
		OnAbilityRemoved?.Invoke(abilityToRemove.Handle);

		abilityToRemove.Handle.Free();
		_grantedAbilities.Remove(abilityToRemove.Handle);
	}

	private void InhibitAbility(Ability abilityToInhibit)
	{
		if (_inhibitAbility is not null)
		{
			abilityToInhibit.OnAbilityDeactivated -= _inhibitAbility;
			_inhibitAbility = null;
		}

		bool wasInhibited = abilityToInhibit.IsInhibited;

		abilityToInhibit.IsInhibited = CheckIsInhibited(abilityToInhibit);

		NotifyAbilityChanged(abilityToInhibit, wasInhibited, abilityToInhibit.Level);
	}

	private void NotifyAbilityChanged(Ability ability, bool wasInhibited, int previousLevel)
	{
		// Announces a change only when one of the two observable pieces of ability state actually moved: the grant
		// paths run unconditionally, and a repeat grant that overrides nothing must stay silent.
		if (ability.IsInhibited == wasInhibited && ability.Level == previousLevel)
		{
			return;
		}

		OnAbilityChanged?.Invoke(ability.Handle);
	}

	private bool CheckIsInhibited(Ability ability)
	{
		// Whether an ability is inhibited is decided by its own grant sources, never by the other abilities on the
		// entity. A source that is not inhibited keeps the ability enabled, and so does one whose InhibitionPolicy is
		// Ignore, since that policy means the grant does not react to its effect being inhibited at all.
		return _grantSources.TryGetValue(ability, out List<IAbilityGrantSource>? grantSources)
			&& grantSources.Count > 0
			&& grantSources.TrueForAll(
				source => source.IsInhibited && source.InhibitionPolicy != AbilityDeactivationPolicy.Ignore);
	}
}
