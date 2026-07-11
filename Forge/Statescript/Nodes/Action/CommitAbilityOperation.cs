// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Nodes.Action;

/// <summary>
/// Selects what a <see cref="CommitAbilityNode"/> commits when it executes.
/// </summary>
public enum CommitAbilityOperation : byte
{
	/// <summary>
	/// Commits both the ability's cost and its cooldown.
	/// </summary>
	CostAndCooldown = 0,

	/// <summary>
	/// Commits only the ability's cooldown.
	/// </summary>
	CooldownOnly = 1,

	/// <summary>
	/// Commits only the ability's cost.
	/// </summary>
	CostOnly = 2,
}
