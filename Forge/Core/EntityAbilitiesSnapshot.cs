// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;

namespace Gamesmiths.Forge.Core;

// An entity's abilities as an entity snapshot took them: each granted ability in grant order, and the removal and
// inhibition that wait for an ability to end.
internal sealed record EntityAbilitiesSnapshot(
	GrantedAbilitySnapshot[] Abilities,
	Action<Ability>? RemoveAbility,
	Action<Ability>? InhibitAbility);
