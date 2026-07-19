// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Represents data associated with the ending of an ability within the Forge framework.
/// </summary>
/// <param name="Ability">The handle of the ability that has ended. May already be freed when the ability is removed
/// on end.</param>
/// <param name="AbilityData">The data of the ability that has ended, captured before the handle can be freed.</param>
/// <param name="WasCanceled">Whether the ability was cancelled or ended gracefully or got cancelled.</param>
public readonly record struct AbilityEndedData(
	AbilityHandle Ability,
	AbilityData AbilityData,
	bool WasCanceled);
