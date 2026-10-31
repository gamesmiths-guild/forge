// Copyright © Gamesmiths Guild.

using System.Runtime.ExceptionServices;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Keeps the recent ticks of a world of entities, so the whole world can go back to one of them as one: every entity
/// as the tick left it, with entities that left the world since brought back and entities that joined after it set
/// aside.
/// </summary>
/// <remarks>
/// <para>Advance the <see cref="Entities"/> through each tick with
/// <see cref="EntitySimulation.Tick(IReadOnlyList{IForgeEntity}, long, double)"/>, which begins every entity's tick
/// before any of them advances, then call <see cref="Record"/>. <see cref="Rollback"/> puts every entity back as a
/// recorded tick left it, and recording the tick after it continues the world from there, leaving the ticks that
/// followed it behind.</para>
/// <para>Entities join with <see cref="Add"/> and leave with <see cref="Remove"/>. One that leaves after it was
/// recorded goes to limbo instead of away: it lets go of everything it held on to and is kept as it was, out of the
/// simulation, so a rollback to a tick it was in brings it back. An entity also waits in limbo while the world is
/// rolled back to before the tick it joined in. The world lets an entity in limbo go for good, raising
/// <see cref="Released"/>, once no tick the history holds has it, or once recording after a rollback leaves behind the
/// ticks it was in.</para>
/// </remarks>
public sealed class WorldHistory
{
	private enum MemberState
	{
		// In the world, holding on to what it holds.
		InWorld = 0,

		// Removed, still holding on until the next tick recorded or rollback lets it go.
		Leaving = 1,

		// Out of the simulation, having let go of everything.
		Limbo = 2,
	}

	private enum Change
	{
		EnteredLimbo = 0,
		Released = 1,
	}

	private readonly List<Member> _members = [];

	private readonly Dictionary<IForgeEntity, Member> _membersByEntity = [];

	private IForgeEntity[] _entities = [];

	private long _newestTick;

	private int _count;

	// The tick the world is at: the one last recorded or rolled back to.
	private long _currentTick;

	/// <summary>
	/// Raised when an entity goes to limbo, after it has let go of everything it held on to.
	/// </summary>
	public event Action<IForgeEntity>? EnteredLimbo;

	/// <summary>
	/// Raised when a rollback brings an entity back from limbo, before the entity is restored, so the host can make its
	/// object live again for the restore to reach it.
	/// </summary>
	public event Action<IForgeEntity>? LeftLimbo;

	/// <summary>
	/// Raised when the world lets an entity go for good: one in limbo that nothing can bring back anymore, or one that
	/// left before it was ever recorded.
	/// </summary>
	public event Action<IForgeEntity>? Released;

	/// <summary>
	/// Gets how many ticks the history keeps.
	/// </summary>
	public int Capacity { get; }

	/// <summary>
	/// Gets the length of the fixed step the world's ticks run with, in seconds.
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
	/// Gets the tick the world is at, the one last recorded or rolled back to, or <see langword="null"/> when the
	/// history holds none.
	/// </summary>
	public long? CurrentTick => _count == 0 ? null : _currentTick;

	/// <summary>
	/// Gets the entities in the world, in the order they joined it, without the ones in limbo.
	/// </summary>
	/// <remarks>
	/// The list is replaced rather than changed when an entity joins or leaves, so a tick advancing it isn't disturbed:
	/// an entity that joins part way through a tick first advances in the next one, and one that leaves part way
	/// through still finishes it.
	/// </remarks>
	public IReadOnlyList<IForgeEntity> Entities => _entities;

	/// <summary>
	/// Gets how many entity snapshots the history holds, counting the ones of entities in limbo.
	/// </summary>
	public int SnapshotCount => _members.Sum(x => x.History.OldestTick is long oldest
		? (int)(x.History.NewestTick!.Value - oldest + 1)
		: 0);

	/// <summary>
	/// Initializes a new instance of the <see cref="WorldHistory"/> class.
	/// </summary>
	/// <param name="capacity">How many ticks to keep.</param>
	/// <param name="fixedDeltaTime">The length of the fixed step the world's ticks run with, in seconds.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity"/> isn't positive.
	/// </exception>
	public WorldHistory(int capacity, double fixedDeltaTime)
	{
		if (capacity <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "A history keeps at least one tick.");
		}

		Capacity = capacity;
		FixedDeltaTime = fixedDeltaTime;
	}

	/// <summary>
	/// Adds an entity to the world. It's recorded from the next tick recorded on.
	/// </summary>
	/// <remarks>
	/// Until then it has no tick to come back to, so a rollback releases it, as it does one that joins while the world
	/// is rolled back.
	/// </remarks>
	/// <param name="entity">The entity to add.</param>
	/// <exception cref="ArgumentException">Thrown when the entity is already in the world or in its limbo.</exception>
	public void Add(IForgeEntity entity)
	{
		if (_membersByEntity.ContainsKey(entity))
		{
			throw new ArgumentException("The entity is already in the world or in its limbo.", nameof(entity));
		}

		var member = new Member(entity, new EntityHistory(entity, Capacity, FixedDeltaTime));
		_members.Add(member);
		_membersByEntity.Add(entity, member);
		RefreshEntities();
	}

	/// <summary>
	/// Takes an entity out of the world, as when it dies. It leaves <see cref="Entities"/> at once, and goes to limbo
	/// when the next tick is recorded.
	/// </summary>
	/// <remarks>
	/// An entity that was never recorded has no tick to come back to, so it's released instead. Removing an entity
	/// that is already out of the world changes nothing.
	/// </remarks>
	/// <param name="entity">The entity to take out.</param>
	/// <exception cref="ArgumentException">Thrown when the world doesn't keep the entity.</exception>
	public void Remove(IForgeEntity entity)
	{
		Member member = Find(entity);

		if (member.State == MemberState.InWorld)
		{
			member.State = MemberState.Leaving;
			RefreshEntities();
		}
	}

	/// <summary>
	/// Forgets an entity for good, such as one whose host object is gone, so no rollback brings it back. One still in
	/// the world leaves <see cref="Entities"/> at once and lets go of what it holds on to when the next tick is
	/// recorded or the world is rolled back.
	/// </summary>
	/// <param name="entity">The entity to forget.</param>
	/// <exception cref="ArgumentException">Thrown when the world doesn't keep the entity.</exception>
	public void Forget(IForgeEntity entity)
	{
		Member member = Find(entity);

		if (member.State == MemberState.Limbo)
		{
			Drop(member);
			return;
		}

		member.State = MemberState.Leaving;
		member.Forgotten = true;
		RefreshEntities();
	}

	/// <summary>
	/// Records how the tick left every entity in the world, with the input frame each ran with. Call it after
	/// advancing the world's <see cref="Entities"/> through the tick.
	/// </summary>
	/// <remarks>
	/// <para>Entities removed since the last tick recorded go to limbo now. The tick that follows the one the world is
	/// at continues the history; after a rollback, the ticks after the one rolled back to are left behind with the
	/// entities that joined in them, and entities that left in them are back in the world. A tick that doesn't
	/// follow the one the world is at starts the history over.</para>
	/// <para>Every snapshot is taken first, so a tick that can't be recorded leaves the history as it was.</para>
	/// </remarks>
	/// <param name="tick">The number of the tick the world was just advanced through.</param>
	/// <exception cref="InvalidOperationException">Thrown while one of the entities' abilities, effects or attributes
	/// are changing.</exception>
	/// <exception cref="NotSupportedException">Thrown while one of the entities' abilities runs a behavior other than
	/// a Statescript graph.</exception>
	public void Record(long tick)
	{
		bool continues = _count > 0 && tick == _currentTick + 1;
		List<RecordedSnapshot> recorded = [];
		List<IForgeEntity> leaving = [];

		foreach (Member member in _members)
		{
			if (member.State == MemberState.InWorld)
			{
				recorded.Add(new RecordedSnapshot(member, EntitySimulation.CaptureSnapshot(member.Entity)));
			}
			else if (member.State == MemberState.Leaving)
			{
				leaving.Add(member.Entity);
			}
		}

		EntitySimulation.ThrowIfCannotRestore(leaving, []);
		ExceptionDispatchInfo? failure = EntitySimulation.RestoreSnapshots(leaving, [], Forgotten());
		List<PendingChange> changes = [];

		if (continues)
		{
			LeaveFutureBehind(changes);
		}
		else
		{
			StartOver(changes);
		}

		foreach (Member member in _members.ToArray())
		{
			if (member.State != MemberState.Leaving)
			{
				continue;
			}

			if (member.Forgotten)
			{
				Drop(member);
			}
			else if (member.JoinTick is null)
			{
				Release(member, changes);
			}
			else
			{
				member.State = MemberState.Limbo;
				member.LeaveTick = tick;
				member.LeaveInput = member.Entity.Input?.Current ?? default;
				changes.Add(new PendingChange(Change.EnteredLimbo, member.Entity));
			}
		}

		foreach (RecordedSnapshot entry in recorded)
		{
			entry.Member.JoinTick ??= tick;
			entry.Member.History.Record(tick, entry.Snapshot);
		}

		int kept = continues ? (int)(tick - OldestTick!.Value) : 0;
		_count = Math.Min(kept + 1, Capacity);
		_newestTick = tick;
		_currentTick = tick;

		long oldestTick = OldestTick!.Value;

		foreach (Member member in _members.ToArray())
		{
			if (member.State == MemberState.Limbo && member.LeaveTick <= oldestTick)
			{
				Release(member, changes);
			}
		}

		RefreshEntities();
		Raise(changes);
		failure?.Throw();
	}

	/// <summary>
	/// Puts the world back as the tick left it: every entity that was in it then is restored, coming back from limbo
	/// if it left since, and every entity that wasn't goes to limbo.
	/// </summary>
	/// <remarks>
	/// <para>The entities are restored as one, through the same steps as
	/// <see cref="EntitySimulation.RestoreSnapshot"/>: everything they hold on to lets go, newest first across all of
	/// them, their state comes back, then everything takes hold again, oldest first across all of them. Handlers that
	/// share an event run in the order they first took hold in, whichever entity they belong to.</para>
	/// <para>The ticks recorded after it are kept, so the world can be rolled back to any of them too. Recording the
	/// tick after it continues the world from there instead. Every entity's input comes back as the tick left it, so
	/// submit the next tick's frames after rolling back, not before.</para>
	/// </remarks>
	/// <param name="tick">A tick the history holds.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the history doesn't hold the tick.</exception>
	/// <exception cref="InvalidOperationException">Thrown while one of the entities' abilities, effects or attributes
	/// are changing.</exception>
	/// <exception cref="NotSupportedException">Thrown while one of the entities' abilities runs a behavior other than
	/// a Statescript graph.</exception>
	public void Rollback(long tick)
	{
		if (_count == 0 || tick < OldestTick || tick > _newestTick)
		{
			throw new ArgumentOutOfRangeException(nameof(tick), tick, "The history doesn't hold that tick.");
		}

		List<IForgeEntity> detaching = [];
		List<EntitySnapshot> snapshots = [];
		List<IForgeEntity> returning = [];

		foreach (Member member in _members)
		{
			bool restored = !member.Forgotten && member.WasIn(tick);

			if (member.State != MemberState.Limbo)
			{
				detaching.Add(member.Entity);
			}
			else if (restored)
			{
				returning.Add(member.Entity);
			}

			if (restored)
			{
				snapshots.Add(member.History.SnapshotAt(tick));
			}
		}

		EntitySimulation.ThrowIfCannotRestore(detaching, snapshots);

		foreach (IForgeEntity entity in returning)
		{
			LeftLimbo?.Invoke(entity);
		}

		ExceptionDispatchInfo? failure = EntitySimulation.RestoreSnapshots(detaching, snapshots, Forgotten());
		List<PendingChange> changes = [];

		foreach (Member member in _members.ToArray())
		{
			if (member.Forgotten)
			{
				Drop(member);
			}
			else if (member.WasIn(tick))
			{
				member.State = MemberState.InWorld;
			}
			else if (member.JoinTick is null)
			{
				Release(member, changes);
			}
			else if (member.State != MemberState.Limbo)
			{
				member.State = MemberState.Limbo;
				changes.Add(new PendingChange(Change.EnteredLimbo, member.Entity));
			}
		}

		_currentTick = tick;
		RefreshEntities();
		Raise(changes);
		failure?.Throw();
	}

	/// <summary>
	/// Runs the ticks after the one the world is at again, up to and including <paramref name="toTick"/>, each from how
	/// the tick before it was recorded and with the input frames it was recorded with, and checks that each comes out
	/// the way it was recorded.
	/// </summary>
	/// <remarks>
	/// <para>The recording stays the truth. Once a tick has run again, its <see cref="ChecksumAt">checksum</see> is
	/// compared with the recorded one and the world is put back as the tick was recorded, the way
	/// <see cref="Rollback"/> puts it. A tick that came out differently is reported, and the next one runs from the
	/// recording rather than from the difference, so each report is about its own tick. The history doesn't change.
	/// </para>
	/// <para>An entity that joins while a tick runs again, such as one a graph spawns, is compared with the one that
	/// joined the first time by its place in the world, and is released when the world is put back, so the recorded
	/// one comes back in its place. An entity that left in the tick runs it with the input frame it left with.</para>
	/// <para>Each tick runs through <paramref name="advance"/>, which advances <see cref="Entities"/> through the tick
	/// it's given, or through <see cref="EntitySimulation.Tick(IReadOnlyList{IForgeEntity}, long, double)"/> without
	/// one. A host that advances its entities its own way passes the method it advances live ticks with, so a tick runs
	/// again the way it ran the first time.</para>
	/// </remarks>
	/// <param name="toTick">The last tick to run again, at most the newest the history holds.</param>
	/// <param name="advance">Advances the world's entities through the tick it's given.</param>
	/// <returns>The ticks that came out differently, oldest first; empty when every tick came out as recorded.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="toTick"/> isn't after the tick the
	/// world is at, or is newer than the newest tick the history holds.</exception>
	/// <exception cref="InvalidOperationException">Thrown while one of the entities' abilities, effects or attributes
	/// are changing.</exception>
	/// <exception cref="NotSupportedException">Thrown while one of the entities' abilities runs a behavior other than
	/// a Statescript graph.</exception>
	public IReadOnlyList<WorldDesync> Replay(long toTick, Action<long>? advance = null)
	{
		if (_count == 0 || toTick <= _currentTick || toTick > _newestTick)
		{
			throw new ArgumentOutOfRangeException(
				nameof(toTick),
				toTick,
				"Only ticks after the one the world is at, up to the newest one held, can be replayed.");
		}

		List<WorldDesync> desyncs = [];

		for (long tick = _currentTick + 1; tick <= toTick; tick++)
		{
			foreach (Member member in _members)
			{
				if (member.State != MemberState.InWorld || member.Entity.Input is not EntityInput input)
				{
					continue;
				}

				if (member.LeaveTick == tick)
				{
					input.SubmitForReplay(member.LeaveInput);
				}
				else if (member.WasIn(tick))
				{
					input.SubmitForReplay(member.History.InputAt(tick));
				}
			}

			EntitySnapshot[] replayed;

			try
			{
				if (advance is null)
				{
					EntitySimulation.Tick(Entities, tick, FixedDeltaTime);
				}
				else
				{
					advance(tick);
				}

				replayed = [.. Entities.Select(EntitySimulation.CaptureSnapshot)];
			}
			catch
			{
				// The world goes back as the tick was recorded either way.
				Rollback(tick);
				throw;
			}

			Rollback(tick);
			EntitySnapshot[] recorded = RecordedSnapshots(tick);

			if (Checksum(recorded) != Checksum(replayed))
			{
				desyncs.Add(new WorldDesync(tick, recorded, replayed));
			}
		}

		return desyncs;
	}

	/// <summary>
	/// Gets the checksum of the world as a tick held left it: a 64-bit hash of the state of every entity in the world
	/// then, in the order they joined it, that comes out the same on every machine and in every run that leaves the
	/// world the same.
	/// </summary>
	/// <remarks>
	/// <para>It covers each entity's attributes, tags, active effects, granted abilities with the running nodes and
	/// variables of their graphs, shared variables, input, random and outside state, value for value, and its body to
	/// a thousandth of a unit, the precision the network sends it at, so float noise below that isn't a difference. An
	/// entity that an effect or a variable points at counts by its place in the world, so a world that spawned an equal
	/// entity in the same place comes out the same. What a running node or an effect component keeps for itself counts
	/// through what it changes.</para>
	/// <para><see cref="Replay"/> compares these checksums to find the ticks that ran differently.</para>
	/// </remarks>
	/// <param name="tick">A tick the history holds.</param>
	/// <returns>The checksum.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the history doesn't hold the tick.</exception>
	public ulong ChecksumAt(long tick)
	{
		if (_count == 0 || tick < OldestTick || tick > _newestTick)
		{
			throw new ArgumentOutOfRangeException(nameof(tick), tick, "The history doesn't hold that tick.");
		}

		return Checksum(RecordedSnapshots(tick));
	}

	// The world's state, each entity by its place in the world the snapshots make up.
	internal static ulong Checksum(EntitySnapshot[] snapshots)
	{
		var checksum = new StateChecksum([.. snapshots.Select(x => x.Entity)]);

		for (int i = 0; i < snapshots.Length; i++)
		{
			checksum.Open("Entities", i);
			snapshots[i].Write(checksum);
			checksum.Close();
		}

		return checksum.Value;
	}

	// After a rollback, the ticks after the one the world is at are left behind: entities that joined in them are let
	// go, and entities that left in them are in the world again.
	private void LeaveFutureBehind(List<PendingChange> changes)
	{
		foreach (Member member in _members.ToArray())
		{
			if (member.State == MemberState.Limbo && member.JoinTick > _currentTick)
			{
				Release(member, changes);
			}
			else if (member.LeaveTick > _currentTick)
			{
				member.LeaveTick = null;
			}
		}
	}

	// A tick that doesn't follow the one the world is at has no history to continue: limbo lets everything go, and
	// every entity joins anew with the tick.
	private void StartOver(List<PendingChange> changes)
	{
		foreach (Member member in _members.ToArray())
		{
			if (member.State == MemberState.Limbo)
			{
				Release(member, changes);
				continue;
			}

			member.JoinTick = null;
			member.LeaveTick = null;
			member.History = new EntityHistory(member.Entity, Capacity, FixedDeltaTime);
		}
	}

	// The snapshots of the entities that were in the world on the tick, in the order they joined it.
	private EntitySnapshot[] RecordedSnapshots(long tick)
	{
		return [.. _members.Where(x => !x.Forgotten && x.WasIn(tick)).Select(x => x.History.SnapshotAt(tick))];
	}

	// The entities the host forgot, whose cues go with them untouched when they let go.
	private List<IForgeEntity>? Forgotten()
	{
		return _members.Exists(x => x.Forgotten) ? [.. _members.Where(x => x.Forgotten).Select(x => x.Entity)] : null;
	}

	private Member Find(IForgeEntity entity)
	{
		return _membersByEntity.TryGetValue(entity, out Member? member)
			? member
			: throw new ArgumentException("The world doesn't keep the entity.", nameof(entity));
	}

	private void Release(Member member, List<PendingChange> changes)
	{
		Drop(member);
		changes.Add(new PendingChange(Change.Released, member.Entity));
	}

	private void Drop(Member member)
	{
		_members.Remove(member);
		_membersByEntity.Remove(member.Entity);
	}

	private void RefreshEntities()
	{
		List<IForgeEntity> entities = [];

		foreach (Member member in _members)
		{
			if (member.State == MemberState.InWorld)
			{
				entities.Add(member.Entity);
			}
		}

		_entities = [.. entities];
	}

	private void Raise(List<PendingChange> changes)
	{
		foreach (PendingChange change in changes)
		{
			IForgeEntity entity = change.Entity;

			switch (change.Change)
			{
				case Change.EnteredLimbo:
					EnteredLimbo?.Invoke(entity);
					break;

				case Change.Released:
					Released?.Invoke(entity);
					break;
			}
		}
	}

	private readonly record struct RecordedSnapshot(Member Member, EntitySnapshot Snapshot);

	private readonly record struct PendingChange(Change Change, IForgeEntity Entity);

	private sealed class Member(IForgeEntity entity, EntityHistory history)
	{
		public IForgeEntity Entity { get; } = entity;

		public EntityHistory History { get; set; } = history;

		public MemberState State { get; set; }

		// Whether the host asked for it to be dropped rather than kept in limbo.
		public bool Forgotten { get; set; }

		// The first tick recorded with it, or null until it's recorded.
		public long? JoinTick { get; set; }

		// The first tick recorded without it after it left, or null while it hasn't.
		public long? LeaveTick { get; set; }

		// The input frame it ran the tick it left in with, which no snapshot of it holds.
		public InputFrame LeaveInput { get; set; }

		public bool WasIn(long tick)
		{
			return JoinTick <= tick && (LeaveTick is null || tick < LeaveTick);
		}
	}
}
