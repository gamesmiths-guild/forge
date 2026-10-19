// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Advances an entity's simulation by one fixed tick, in the order every host and every machine must agree on.
/// </summary>
/// <remarks>
/// <para>A tick runs, in order:</para>
/// <list type="number">
/// <item><description>The entity's <see cref="IForgeEntity.Random"/>, if any, restarts for the tick.</description>
/// </item>
/// <item><description>Effects advance by the fixed step: durations count down, periods execute, expired effects
/// end.</description></item>
/// <item><description>Abilities advance their fixed-rail behaviors, so graphs react to the effects as they stand after
/// this tick's expirations.</description></item>
/// </list>
/// <para>Call it once per entity from the host's fixed callback, with the same tick number and step on every machine
/// that simulates the entity. The frame rail stays separate: call <see cref="EntityAbilities.UpdateAbilities"/> from
/// the frame callback.</para>
/// </remarks>
public static class EntitySimulation
{
	/// <summary>
	/// Advances the entity by one fixed tick.
	/// </summary>
	/// <param name="entity">The entity to advance.</param>
	/// <param name="tick">The number of the tick being simulated.</param>
	/// <param name="fixedDeltaTime">The length of the fixed step, in seconds.</param>
	public static void Tick(IForgeEntity entity, long tick, double fixedDeltaTime)
	{
		entity.Random?.BeginTick(tick);
		entity.EffectsManager.UpdateEffects(fixedDeltaTime);
		entity.Abilities.FixedUpdateAbilities(fixedDeltaTime);
	}
}
