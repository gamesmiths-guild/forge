// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Abilities;

public class Ability(AbilityData abilityData, float cooldown, int level = 1)
{
	/// <summary>
	/// Gets the ability data for this ability.
	/// </summary>
	public AbilityData AbilityData { get; } = abilityData;

	/// <summary>
	/// Gets the current cooldown o this ability.
	/// </summary>
	public float Cooldown { get; } = cooldown;

	/// <summary>
	/// Gets the current level o this ability.
	/// </summary>
	public int Level { get; } = level;
}
