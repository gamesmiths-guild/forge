// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Represents data associated with the ending of an ability within the Forge framework.
/// </summary>
/// <param name="Ability">The handle of the ability that has ended.</param>
/// <param name="WasCanceled">Whether the ability was cancelled or ended gracefully or got cancelled.</param>
public readonly record struct AbilityEndedData(
	AbilityHandle Ability,
	bool WasCanceled);
