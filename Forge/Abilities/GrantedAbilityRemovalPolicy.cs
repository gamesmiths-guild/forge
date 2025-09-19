// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Specifies the policy for removing a granted ability when the effect that granted it ends.
/// </summary>
/// <remarks>
/// This enumeration defines the behavior for handling the removal of abilities that are granted by a specific effect.
/// The policy determines whether the ability is removed immediately, allowed to complete its current execution, or
/// retained indefinitely.
/// </remarks>
public enum GrantedAbilityRemovalPolicy : byte
{
	/// <summary>
	/// Ability is removed immediately when the granting effect ends.
	/// </summary>
	CancelImmediately = 0,

	/// <summary>
	/// Ability is removed when the granting effect ends, but it is allowed to finish its current execution first.
	/// </summary>
	RemoveOnEnd = 1,

	/// <summary>
	/// Granted ability is not removed when the granting effect ends.
	/// </summary>
	DoNotRemove = 2,
}
