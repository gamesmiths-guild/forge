// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Selects which state flag an <see cref="AbilityStateResolver"/> reads from an ability.
/// </summary>
public enum AbilityStateType : byte
{
	/// <summary>
	/// Whether the ability is currently active.
	/// </summary>
	IsActive = 0,

	/// <summary>
	/// Whether the ability is currently inhibited.
	/// </summary>
	IsInhibited = 1,

	/// <summary>
	/// Whether the handle points to a valid granted ability.
	/// </summary>
	IsValid = 2,
}
