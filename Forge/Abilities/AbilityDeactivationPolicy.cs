// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Specifies the policy for removing or inhibiting a granted ability based on its activation state.
/// </summary>
/// <remarks>
/// This enumeration defines the behavior for handling the removal and inhibition of abilities.
/// The policy determines whether the ability is removed or inhibited immediately, allowed to complete its current
/// execution, or retained indefinitely.
/// </remarks>
public enum AbilityDeactivationPolicy : byte
{
	/// <summary>
	/// Ability is removed or inhibited immediately.
	/// </summary>
	CancelImmediately = 0,

	/// <summary>
	/// Ability is removed or inhibited, but it is allowed to finish its current execution
	/// first.
	/// </summary>
	RemoveOnEnd = 1,

	/// <summary>
	/// Granted ability is not removed or inhibited.
	/// </summary>
	Ignore = 2,
}
