// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Nodes.Condition;

/// <summary>
/// Selects which grant sources a <see cref="TryRevokeAbilityNode"/> removes when it executes.
/// </summary>
public enum AbilityRevokeScope : byte
{
	/// <summary>
	/// Removes only the permanent grants, leaving grants owned by effects and by graphs in place. The ability goes
	/// away only when nothing else is granting it.
	/// </summary>
	PermanentGrants = 0,

	/// <summary>
	/// Removes every grant source, so the ability always goes away. Effects that were granting it will not re-grant it
	/// when they end.
	/// </summary>
	AllGrants = 1,
}
