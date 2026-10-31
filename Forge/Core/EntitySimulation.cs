// Copyright © Gamesmiths Guild.

using System.Runtime.ExceptionServices;
using Gamesmiths.Forge.Abilities;

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
/// <item><description>The movement stage, for an entity with an <see cref="IForgeEntity.Mover"/>: the mover moves the
/// body by the fixed step, seeing the abilities the input stage just activated.</description></item>
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
	/// Runs the entity's input and movement stages, then advances its effects and fixed-rail abilities by one fixed
	/// step, without restarting its random.
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

		entity.Mover?.Move(fixedDeltaTime);
		entity.EffectsManager.UpdateEffects(fixedDeltaTime);
		entity.Abilities.FixedUpdateAbilities(fixedDeltaTime);
	}

	/// <summary>
	/// Takes a snapshot of the entity's simulation, to put it back later with <see cref="RestoreSnapshot"/>.
	/// </summary>
	/// <remarks>
	/// <para>The snapshot holds the entity's attributes, tags, active effects, granted abilities with their running
	/// instances and graphs, shared variables, input, random source, body, through its
	/// <see cref="IForgeEntity.Mover"/>, and state kept outside Forge, through its
	/// <see cref="IForgeEntity.StateHook"/>. See <see cref="EntitySnapshot"/>.</para>
	/// <para>Take it from the host, between ticks, rather than from inside an ability, an effect or a graph, where the
	/// entity is part way through changing.</para>
	/// </remarks>
	/// <param name="entity">The entity to take the snapshot of.</param>
	/// <returns>The snapshot.</returns>
	/// <exception cref="InvalidOperationException">Thrown while the entity's abilities, effects or attributes are
	/// changing.</exception>
	/// <exception cref="NotSupportedException">Thrown while one of the entity's abilities runs a behavior other than a
	/// Statescript graph.</exception>
	public static EntitySnapshot CaptureSnapshot(IForgeEntity entity)
	{
		ThrowIfCannotSnapshot(entity, "taken");

		return new EntitySnapshot(entity);
	}

	/// <summary>
	/// Puts the entity's simulation back as it was when <see cref="CaptureSnapshot"/> took <paramref name="snapshot"/>,
	/// silently: no event, cue, callback or graph node runs.
	/// </summary>
	/// <remarks>
	/// <para>Active effects, abilities and ability instances come back as the same objects, so the handles that point
	/// at them are valid again, and ones added since the snapshot go, with their handles freed and none of their
	/// removal or end callbacks run. The presentation pass after the restore shows only what changed.</para>
	/// <para>The restore happens in three steps. Everything on the entity that holds something outside itself - each
	/// active effect and its components, each granted ability's trigger, each running state node of its graphs - lets
	/// go of it, newest first. Then the state is put back. Then everything takes hold again, oldest first, in the order
	/// it first did across the entity's effects, abilities and graphs, so handlers that share an event run in the same
	/// order as before. A hook that throws keeps neither the others from running nor the restore from completing, and
	/// the first exception is rethrown after.</para>
	/// <para>Restore it from the host, between ticks, like <see cref="CaptureSnapshot"/>.</para>
	/// </remarks>
	/// <param name="entity">The entity to restore.</param>
	/// <param name="snapshot">A snapshot taken from <paramref name="entity"/>.</param>
	/// <exception cref="ArgumentException">Thrown when the snapshot was taken from another entity.</exception>
	/// <exception cref="InvalidOperationException">Thrown while the entity's abilities, effects or attributes are
	/// changing.</exception>
	/// <exception cref="NotSupportedException">Thrown while one of the entity's abilities runs a behavior other than a
	/// Statescript graph.</exception>
	public static void RestoreSnapshot(IForgeEntity entity, EntitySnapshot snapshot)
	{
		if (snapshot.Entity != entity)
		{
			throw new ArgumentException("The snapshot was taken from another entity.", nameof(snapshot));
		}

		ThrowIfCannotRestore([entity], [snapshot]);
		RestoreSnapshots([entity], [snapshot])?.Throw();
	}

	// Puts several entities back as one. Every hold of the detaching entities lets go, newest first across all of them,
	// each snapshot puts its entity's state back, then every hold of the snapshots' entities takes hold again, oldest
	// first across all of them, so handlers sharing an event run in the order they first took hold in, whichever entity
	// they belong to. With no snapshots, the detaching entities only let go. Callers check the entities first with
	// ThrowIfCannotRestore, and the first exception a hook threw is handed back.
	internal static ExceptionDispatchInfo? RestoreSnapshots(
		IReadOnlyList<IForgeEntity> detaching,
		IReadOnlyList<EntitySnapshot> snapshots)
	{
		ExceptionDispatchInfo? failure = null;
		List<EntityHold> holds = [];

		foreach (IForgeEntity entity in detaching)
		{
			AddHolds(entity, holds);
		}

		holds.Sort(static (x, y) => x.Order.CompareTo(y.Order));

		for (int i = holds.Count - 1; i >= 0; i--)
		{
			try
			{
				holds[i].Detach();
			}
			catch (Exception exception)
			{
				failure ??= ExceptionDispatchInfo.Capture(exception);
			}
		}

		holds.Clear();

		foreach (EntitySnapshot snapshot in snapshots)
		{
			try
			{
				ExceptionDispatchInfo? stateFailure = snapshot.RestoreState();
				failure ??= stateFailure;
			}
			catch (Exception exception)
			{
				// What came back still takes hold again.
				failure ??= ExceptionDispatchInfo.Capture(exception);
			}

			AddHolds(snapshot.Entity, holds);
		}

		holds.Sort(static (x, y) => x.Order.CompareTo(y.Order));

		foreach (EntityHold hold in holds)
		{
			try
			{
				hold.Attach();
			}
			catch (Exception exception)
			{
				failure ??= ExceptionDispatchInfo.Capture(exception);
			}
		}

		return failure;
	}

	// Throws if one of the entities can't let go, or one of the snapshots can't be put back, so a caller can act
	// between the check and the restore.
	internal static void ThrowIfCannotRestore(
		IReadOnlyList<IForgeEntity> detaching,
		IReadOnlyList<EntitySnapshot> snapshots)
	{
		foreach (IForgeEntity entity in detaching)
		{
			ThrowIfCannotSnapshot(entity, "restored");
		}

		foreach (EntitySnapshot snapshot in snapshots)
		{
			ThrowIfCannotSnapshot(snapshot.Entity, "restored");
		}
	}

	private static void ThrowIfChanging(IForgeEntity entity, string action)
	{
		if (entity.Abilities.IsChanging || entity.EffectsManager.IsChanging || entity.Attributes.IsChanging)
		{
			throw new InvalidOperationException(
				$"An entity snapshot cannot be {action} while the entity's abilities, effects or attributes are " +
				"changing. Take and restore snapshots from the host, between ticks.");
		}
	}

	private static void ThrowIfCannotSnapshot(IForgeEntity entity, string action)
	{
		ThrowIfChanging(entity, action);

		if (entity.Abilities.FindUnsnapshottableAbility() is AbilityData abilityData)
		{
			throw new NotSupportedException(
				$"An entity snapshot cannot be {action} while the ability '{abilityData.Name}' runs a behavior that " +
				"isn't a Statescript graph, since a snapshot can't reach what such a behavior keeps.");
		}
	}

	private static void AddHolds(IForgeEntity entity, List<EntityHold> holds)
	{
		entity.EffectsManager.AddHolds(holds);
		entity.Abilities.AddHolds(holds);
	}
}
