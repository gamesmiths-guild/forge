// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Keeps an entity's recent ticks, each as a snapshot of how the tick left the entity and the input frame it ran with,
/// so the entity can be rolled back to one of them and the ticks after it run again.
/// </summary>
/// <remarks>
/// <para>Call <see cref="Record"/> after advancing the entity through each tick. <see cref="Rollback"/> then puts the
/// entity back as a recorded tick left it, and <see cref="Replay"/> runs the ticks after it again, each with the input
/// frame it first ran with, recording them anew.</para>
/// <para>The history keeps the most recent <see cref="Capacity"/> ticks. A replayed tick runs the way
/// <see cref="EntitySimulation.Tick(IForgeEntity, long, double)"/> runs it, so only the fixed rail replays: anything
/// that must come out the same on a replay belongs on it.</para>
/// </remarks>
public sealed class EntityHistory
{
	private readonly EntitySnapshot?[] _snapshots;

	private readonly InputFrame[] _inputs;

	private long _newestTick;

	private int _count;

	// The tick the entity's state is at: the one last recorded, rolled back to or replayed.
	private long _currentTick;

	/// <summary>
	/// Gets the entity whose ticks this history keeps.
	/// </summary>
	public IForgeEntity Entity { get; }

	/// <summary>
	/// Gets how many ticks the history keeps.
	/// </summary>
	public int Capacity { get; }

	/// <summary>
	/// Gets the length of the fixed step replayed ticks run with, in seconds.
	/// </summary>
	public double FixedDeltaTime { get; }

	/// <summary>
	/// Gets the oldest tick the history holds, or <see langword="null"/> when it holds none.
	/// </summary>
	public long? OldestTick => _count == 0 ? null : _newestTick - _count + 1;

	/// <summary>
	/// Gets the newest tick the history holds, or <see langword="null"/> when it holds none.
	/// </summary>
	public long? NewestTick => _count == 0 ? null : _newestTick;

	/// <summary>
	/// Initializes a new instance of the <see cref="EntityHistory"/> class.
	/// </summary>
	/// <param name="entity">The entity whose ticks to keep.</param>
	/// <param name="capacity">How many ticks to keep.</param>
	/// <param name="fixedDeltaTime">The length of the fixed step the entity's ticks run with, in seconds.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity"/> isn't positive.
	/// </exception>
	public EntityHistory(IForgeEntity entity, int capacity, double fixedDeltaTime)
	{
		if (capacity <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "A history keeps at least one tick.");
		}

		Entity = entity;
		Capacity = capacity;
		FixedDeltaTime = fixedDeltaTime;
		_snapshots = new EntitySnapshot?[capacity];
		_inputs = new InputFrame[capacity];
	}

	/// <summary>
	/// Records how the tick left the entity, with the input frame it ran with. Call it after advancing the entity
	/// through the tick.
	/// </summary>
	/// <remarks>
	/// A tick that is already held, such as the one after a tick rolled back to, is recorded anew and the ticks after
	/// it are forgotten, since they belong to the run it replaces. A tick that doesn't follow the newest one held
	/// starts the history over.
	/// </remarks>
	/// <param name="tick">The number of the tick the entity was just advanced through.</param>
	public void Record(long tick)
	{
		// Taken first, so a snapshot that can't be taken leaves the history as it was.
		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(Entity);
		long? oldestTick = OldestTick;
		int kept = oldestTick is long oldest && tick >= oldest && tick <= _newestTick + 1 ? (int)(tick - oldest) : 0;

		_count = Math.Min(kept + 1, Capacity);
		_newestTick = tick;
		Store(tick, snapshot, Entity.Input?.Current ?? default);
		_currentTick = tick;
	}

	/// <summary>
	/// Puts the entity back as the tick left it, through <see cref="EntitySimulation.RestoreSnapshot"/>.
	/// </summary>
	/// <remarks>
	/// The ticks recorded after it are kept, so <see cref="Replay"/> can run them again with the input frames they
	/// ran with.
	/// </remarks>
	/// <param name="tick">A tick the history holds.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the history doesn't hold the tick.</exception>
	public void Rollback(long tick)
	{
		if (_count == 0 || tick < OldestTick || tick > _newestTick)
		{
			throw new ArgumentOutOfRangeException(nameof(tick), tick, "The history doesn't hold that tick.");
		}

		EntitySimulation.RestoreSnapshot(Entity, _snapshots[Slot(tick)]!);
		_currentTick = tick;
	}

	/// <summary>
	/// Runs the ticks after the one the entity is at again, up to and including <paramref name="toTick"/>, each with
	/// the input frame it first ran with, and records how each now leaves the entity.
	/// </summary>
	/// <remarks>
	/// Each tick runs through <see cref="EntitySimulation.Tick(IForgeEntity, long, double)"/>, with its frame made the
	/// tick's input as it is rather than merged with one already waiting.
	/// </remarks>
	/// <param name="toTick">The last tick to run again, at most the newest the history holds.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="toTick"/> isn't after the tick the
	/// entity is at, or is newer than the newest tick the history holds.</exception>
	public void Replay(long toTick)
	{
		if (_count == 0 || toTick <= _currentTick || toTick > _newestTick)
		{
			throw new ArgumentOutOfRangeException(
				nameof(toTick),
				toTick,
				"Only ticks after the one the entity is at, up to the newest one held, can be replayed.");
		}

		for (long tick = _currentTick + 1; tick <= toTick; tick++)
		{
			int slot = Slot(tick);
			Entity.Input?.SubmitForReplay(_inputs[slot]);
			EntitySimulation.Tick(Entity, tick, FixedDeltaTime);
			_snapshots[slot] = EntitySimulation.CaptureSnapshot(Entity);
			_currentTick = tick;
		}
	}

	private void Store(long tick, EntitySnapshot snapshot, InputFrame input)
	{
		int slot = Slot(tick);
		_snapshots[slot] = snapshot;
		_inputs[slot] = input;
	}

	private int Slot(long tick)
	{
		return (int)(((tick % Capacity) + Capacity) % Capacity);
	}
}
