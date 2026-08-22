// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Represents a handle to a granted ability.
/// </summary>
public class AbilityHandle : IValidatable
{
	/// <summary>
	/// Gets a value indicating whether the ability associated with this handle is valid and active.
	/// </summary>
	public bool IsActive => Ability?.IsActive == true;

	/// <summary>
	/// Gets a value indicating whether the ability associated with this handle is currently inhibited.
	/// </summary>
	public bool IsInhibited => Ability?.IsInhibited == true;

	/// <summary>
	/// Gets a value indicating whether the handle is valid.
	/// </summary>
	public bool IsValid => Ability is not null;

	/// <summary>
	/// Gets a value indicating the level of the ability associated with this handle.
	/// </summary>
	public int Level => Ability?.Level ?? 0;

	internal Ability? Ability { get; private set; }

	internal AbilityHandle(Ability ability)
	{
		Ability = ability;
	}

	/// <summary>
	/// Tries to activate the ability associated with this handle.
	/// </summary>
	/// <param name="failureFlags">Flags indicating the failure reasons for the ability activation.</param>
	/// <param name="target">Optional target entity for the ability activation.</param>
	/// <param name="magnitude">Optional magnitude value for the ability activation.</param>
	/// <returns>Return <see langword="true"/> if the ability was successfully activated;
	/// otherwise, <see langword="false"/>.</returns>
	public bool TryActivate(
		out AbilityActivationFailures failureFlags,
		IForgeEntity? target = null,
		float magnitude = 0)
	{
		failureFlags = AbilityActivationFailures.InvalidHandler;
		return Ability?.TryActivateAbility(target, out failureFlags, magnitude) ?? false;
	}

	/// <summary>
	/// Tries to activate the ability associated with this handle with strongly-typed additional data.
	/// </summary>
	/// <typeparam name="TData">The type of the data to pass to the ability behavior.</typeparam>
	/// <param name="data">Additional data to pass to the behavior.</param>
	/// <param name="failureFlags">Flags indicating the failure reasons for the ability activation.</param>
	/// <param name="target">Optional target entity for the ability activation.</param>
	/// <param name="magnitude">Optional magnitude value for the ability activation.</param>
	/// <returns>Return <see langword="true"/> if the ability was successfully activated; otherwise,
	/// <see langword="false"/>.</returns>
	public bool TryActivate<TData>(
		TData data,
		out AbilityActivationFailures failureFlags,
		IForgeEntity? target = null,
		float magnitude = 0f)
	{
		if (Ability is null)
		{
			failureFlags = AbilityActivationFailures.InvalidHandler;
			return false;
		}

		return Ability.TryActivateAbility(target, out failureFlags, data, magnitude);
	}

	/// <summary>
	/// Cancels all instances of the ability associated with this handle.
	/// </summary>
	public void Cancel()
	{
		Ability?.CancelAllInstances();
	}

	/// <summary>
	/// Tries to commit the ability cooldown and cost.
	/// </summary>
	/// <remarks>
	/// A commit can happen long after activation, by which point the cooldown may have started or the resources may
	/// have been spent elsewhere, so the cooldown and the cost are both re-checked here. The commit is
	/// all-or-nothing: when either check fails, neither the cooldown nor the cost is applied.
	/// </remarks>
	/// <returns>Return <see langword="true"/> if both the cooldown and the cost were committed; otherwise,
	/// <see langword="false"/>.</returns>
	public bool TryCommitAbility()
	{
		return Ability?.TryCommitAbility() ?? false;
	}

	/// <summary>
	/// Tries to commit the ability cooldown.
	/// </summary>
	/// <returns>Return <see langword="true"/> if the cooldown was committed; <see langword="false"/> when the ability
	/// is already on cooldown or the handle is invalid.</returns>
	public bool TryCommitCooldown()
	{
		return Ability?.TryCommitCooldown() ?? false;
	}

	/// <summary>
	/// Tries to commit the ability cost.
	/// </summary>
	/// <returns>Return <see langword="true"/> if the cost was committed; <see langword="false"/> when the owner can no
	/// longer afford it or the handle is invalid.</returns>
	public bool TryCommitCost()
	{
		return Ability?.TryCommitCost() ?? false;
	}

	/// <summary>
	/// Checks if the ability can be activated for the given target.
	/// </summary>
	/// <param name="failureFlags">Flags indicating the failure reasons for the ability activation.</param>
	/// <param name="abilityTarget">Optional target entity for the ability activation check.</param>
	/// <returns>Returns <see langword="true"/> if the ability can be activated; otherwise, <see langword="false"/>.
	/// </returns>
	public bool CanActivate(out AbilityActivationFailures failureFlags, IForgeEntity? abilityTarget = null)
	{
		failureFlags = AbilityActivationFailures.InvalidHandler;
		return Ability?.CanActivate(abilityTarget, out failureFlags) ?? false;
	}

	/// <summary>
	/// Gets the cooldown data for the ability associated with this handle.
	/// </summary>
	/// <returns>A list of cooldown data. Returns <see langword="null"/> if the ability is invalid.</returns>
	public CooldownData[]? GetCooldownData()
	{
		return Ability?.GetCooldownData();
	}

	/// <summary>
	/// Gets the remaining cooldown time for a specific tag.
	/// </summary>
	/// <param name="tag">The tag to check for remaining cooldown time.</param>
	/// <returns>The remaining cooldown time in seconds. Returns 0 if there is no cooldown or the ability is invalid.
	/// </returns>
	public float GetRemainingCooldownTime(Tag tag)
	{
		return Ability?.GetRemainingCooldownTime(tag) ?? 0f;
	}

	/// <summary>
	/// Gets the cost data for the ability associated with this handle.
	/// </summary>
	/// <returns>The cost data. Returns <see langword="null"/> if the ability is invalid or has no cost.</returns>
	public CostData[]? GetCostData()
	{
		return Ability?.GetCostData();
	}

	/// <summary>
	/// Gets the cost for a specific attribute.
	/// </summary>
	/// <param name="attribute">The attribute key to get the cost for.</param>
	/// <returns>The cost for the specified attribute. Returns 0 if the ability is invalid or has no cost for the
	/// attribute.</returns>
	public int GetCostForAttribute(StringKey attribute)
	{
		return Ability?.GetCostForAttribute(attribute) ?? 0;
	}

	internal void Free()
	{
		Ability = null;
	}
}
