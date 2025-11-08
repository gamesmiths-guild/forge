// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Represents a handle to a granted ability.
/// </summary>
public class AbilityHandle
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
	/// Gets a value indicating the level of the ability associated with this handle.
	/// </summary>
	public int Level => Ability?.Level ?? 0;

	internal Ability? Ability { get; private set; }

	internal AbilityHandle(Ability ability)
	{
		Ability = ability;
	}

	/// <summary>
	/// Activates the ability associated with this handle.
	/// </summary>
	/// <param name="activationResult">The result of the ability activation attempt.</param>
	/// <param name="target">The target entity for the ability activation.</param>
	/// <returns>Return <see langword="true"/> if the ability was successfully activated;
	/// otherwise, <see langword="false"/>.</returns>
	public bool Activate(out AbilityActivationResult activationResult, IForgeEntity? target = null)
	{
		activationResult = AbilityActivationResult.FailedInvalidHandler;
		return Ability?.TryActivateAbility(target, out activationResult) ?? false;
	}

	/// <summary>
	/// End the ability associated with this handle.
	/// </summary>
	public void End()
	{
		Ability?.End();
	}

	/// <summary>
	/// Cancels all instances of the ability associated with this handle.
	/// </summary>
	public void Cancel()
	{
		Ability?.CancelAbility();
	}

	/// <summary>
	/// Commits the ability cooldown and cost.
	/// </summary>
	public void CommitAbility()
	{
		Ability?.CommitAbility();
	}

	/// <summary>
	/// Commits the ability cooldown.
	/// </summary>
	public void CommitCooldown()
	{
		Ability?.CommitCooldown();
	}

	/// <summary>
	/// Commits the ability cost.
	/// </summary>
	public void CommitCost()
	{
		Ability?.CommitCost();
	}

	internal void Free()
	{
		Ability = null;
	}
}
