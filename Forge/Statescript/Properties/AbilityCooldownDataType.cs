// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Selects which cooldown value an <see cref="AbilityCooldownResolver"/> reads from an ability.
/// </summary>
public enum AbilityCooldownDataType : byte
{
	/// <summary>
	/// The remaining cooldown time in seconds. Returns <c>0</c> when the ability is not on cooldown.
	/// </summary>
	RemainingTime = 0,

	/// <summary>
	/// The total cooldown duration in seconds.
	/// </summary>
	TotalTime = 1,

	/// <summary>
	/// The remaining cooldown time as a fraction of the total duration (0 to 1). Returns <c>0</c> when the ability is
	/// not on cooldown.
	/// </summary>
	RemainingFraction = 2,
}
