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
/// <item><description>The input stage, for an entity with an <see cref="IForgeEntity.Input"/>: the frame submitted for
/// the tick becomes current, and abilities bound to an input action that was pressed or released try to
/// activate.</description></item>
/// <item><description>Effects advance by the fixed step: durations count down, periods execute, expired effects
/// end.</description></item>
/// <item><description>Abilities advance their fixed-rail behaviors, so graphs react to the effects as they stand after
/// this tick's expirations.</description></item>
/// </list>
/// <para>Call it once per tick from the host's fixed callback, with the same tick number and step on every machine
/// that simulates the entity. The frame rail stays separate: call <see cref="EntityAbilities.UpdateAbilities"/> from
/// the frame callback.</para>
/// <para>Entities that affect each other must all begin the tick before any of them advances, or a roll one entity
/// makes on another, such as a <see cref="Effects.Components.ChanceToApplyEffectComponent"/>, would draw from the
/// other's previous tick, and a graph reading another entity's input would see its previous frame.
/// <see cref="Tick(IReadOnlyList{IForgeEntity}, long, double)"/> does that for a list; a host that advances each entity
/// from its own callback calls <see cref="BeginTick"/> for all of them first, then <see cref="Advance"/> for each.
/// </para>
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
		BeginTick(entity, tick);
		Advance(entity, fixedDeltaTime);
	}

	/// <summary>
	/// Advances the entities by one fixed tick, restarting every entity's random before any of them advances.
	/// </summary>
	/// <param name="entities">The entities to advance, in the order they advance.</param>
	/// <param name="tick">The number of the tick being simulated.</param>
	/// <param name="fixedDeltaTime">The length of the fixed step, in seconds.</param>
	public static void Tick(IReadOnlyList<IForgeEntity> entities, long tick, double fixedDeltaTime)
	{
		foreach (IForgeEntity entity in entities)
		{
			BeginTick(entity, tick);
		}

		foreach (IForgeEntity entity in entities)
		{
			Advance(entity, fixedDeltaTime);
		}
	}

	/// <summary>
	/// Restarts the entity's <see cref="IForgeEntity.Random"/>, if any, for the tick, and makes the frame submitted to
	/// its <see cref="IForgeEntity.Input"/>, if any, current.
	/// </summary>
	/// <remarks>
	/// The frame becomes current here rather than when the entity advances, so a graph on another entity that reads
	/// this one's input sees the same tick's frame whichever entity advances first. Calling it again before the entity
	/// advances changes nothing.
	/// </remarks>
	/// <param name="entity">The entity about to be advanced.</param>
	/// <param name="tick">The number of the tick being simulated.</param>
	public static void BeginTick(IForgeEntity entity, long tick)
	{
		entity.Random?.BeginTick(tick);
		entity.Input?.BeginTick(tick);
	}

	/// <summary>
	/// Runs the entity's input stage, then advances its effects and fixed-rail abilities by one fixed step, without
	/// restarting its random.
	/// </summary>
	/// <remarks>
	/// The input stage makes the submitted frame current unless <see cref="BeginTick"/> already did for this tick, then
	/// activates the abilities bound to input.
	/// </remarks>
	/// <param name="entity">The entity to advance.</param>
	/// <param name="fixedDeltaTime">The length of the fixed step, in seconds.</param>
	public static void Advance(IForgeEntity entity, double fixedDeltaTime)
	{
		if (entity.Input is EntityInput input)
		{
			input.BeginInputStage();
			entity.Abilities.ActivateBoundAbilities(input);
		}

		entity.EffectsManager.UpdateEffects(fixedDeltaTime);
		entity.Abilities.FixedUpdateAbilities(fixedDeltaTime);
	}
}
