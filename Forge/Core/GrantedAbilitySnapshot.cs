// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;

namespace Gamesmiths.Forge.Core;

// A granted ability as an entity snapshot took it: the ability, what grants it, and its own snapshot.
internal readonly record struct GrantedAbilitySnapshot(
	Ability Ability,
	IAbilityGrantSource[] GrantSources,
	Ability.Snapshot State);
