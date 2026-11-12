// Copyright © Gamesmiths Guild.

using System.Runtime.ExceptionServices;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Networking.Replication;

// The entities a client predicts, on one timeline. They advance through each tick together, every one beginning the
// tick before any advances, and roll back and run again together, so what one does to another comes out the same each
// time. A spawn the client makes ahead of the server joins in the tick it was made in, recorded as that tick left it
// without advancing through it, and one freed leaves in the tick it was freed in. One that left, or that a rollback
// went back to before, waits in limbo, out of the simulation and having let go of everything, until a rollback to a
// tick it was in, or a run of the tick it was made in, brings it back.
internal sealed class PredictionTimeline(int capacity, double fixedDeltaTime)
{
	private readonly List<Prediction> _members = [];
	private readonly List<Prediction> _joining = [];
	private IForgeEntity[] _live = [];
	private long _newestTick;
	private int _count;

	// Raised as a member goes to limbo, once it let go of everything.
	public event Action<IForgeEntity>? EnteredLimbo;

	// Raised as a member comes back from limbo, before it's restored.
	public event Action<IForgeEntity>? LeftLimbo;

	public int Capacity { get; } = capacity;

	public double FixedDeltaTime { get; } = fixedDeltaTime;

	// Every member, in the order they joined, the ones in limbo too.
	public IReadOnlyList<Prediction> Members => _members;

	// The members advancing with the timeline, in the order they joined.
	public IReadOnlyList<IForgeEntity> Live => _live;

	public long? NewestTick => _count == 0 ? null : _newestTick;

	public long? OldestTick => _count == 0 ? null : _newestTick - _count + 1;

	// The tick the members are at: the one last recorded, rolled back to or run again.
	public long CurrentTick { get; private set; }

	// The tick the members are advancing through, while they are.
	public long? Advancing { get; private set; }

	// Adds a member. One made while the members advance through a tick joins as the tick is recorded, as the tick left
	// it, since it was made part way through it; one added between ticks joins at the tick given.
	public void Join(Prediction member, long tick)
	{
		_members.Add(member);

		if (Advancing is not null)
		{
			_joining.Add(member);
			return;
		}

		member.JoinTick = tick;
		member.History.Record(tick);

		if (_count == 0)
		{
			_count = 1;
			_newestTick = tick;
			CurrentTick = tick;
		}

		RefreshLive();
	}

	// Takes a member out as the tick it's freed in is recorded, or the next one: it goes to limbo, for a rollback to a
	// tick it was in to bring it back.
	public void Leave(Prediction member)
	{
		if (member.State == PredictionState.InWorld)
		{
			member.State = PredictionState.Leaving;
			RefreshLive();
		}
	}

	// Takes a member off the timeline for good.
	public void Remove(Prediction member)
	{
		_members.Remove(member);
		_joining.Remove(member);
		RefreshLive();
	}

	// Puts a member in limbo now, between ticks, and keeps it there through rollbacks: this client made it, and the
	// server's word is that it didn't.
	public void Shelve(Prediction member)
	{
		member.Unconfirmed = true;

		if (member.State == PredictionState.Limbo)
		{
			return;
		}

		EntitySimulation.ThrowIfCannotRestore([member.Entity], []);
		ExceptionDispatchInfo? failure = EntitySimulation.RestoreSnapshots([member.Entity], []);
		member.State = PredictionState.Limbo;
		RefreshLive();
		EnteredLimbo?.Invoke(member.Entity);
		failure?.Throw();
	}

	public void Tick(long tick, double deltaTime)
	{
		Advance(tick, deltaTime);
		Record(tick, again: false);
	}

	// Advances the members through a tick no input was submitted for, each holding what it held, without presses or
	// releases, as the server runs a tick whose input never arrived. A frame waiting for a later tick waits on.
	public void TickHeld(long tick, double deltaTime)
	{
		List<WaitingFrame> waiting = WaitingFrame.Of(_live);

		foreach (WaitingFrame frame in waiting)
		{
			frame.Input.Waiting = null;
		}

		try
		{
			Tick(tick, deltaTime);
		}
		finally
		{
			foreach (WaitingFrame frame in waiting)
			{
				frame.PutBack();
			}
		}
	}

	// Puts the members back as the tick left them, as one: the ones in it then are restored, coming back from limbo if
	// they left since, and the others go to limbo.
	public void Rollback(long tick)
	{
		var detaching = _members.Where(x => x.State != PredictionState.Limbo).Select(x => x.Entity).ToList();
		var restored = _members.Where(x => x.WasIn(tick)).ToList();
		List<EntitySnapshot> snapshots = restored.ConvertAll(x => x.History.SnapshotAt(tick));
		EntitySimulation.ThrowIfCannotRestore(detaching, snapshots);

		foreach (Prediction member in restored.Where(x => x.State == PredictionState.Limbo))
		{
			LeftLimbo?.Invoke(member.Entity);
		}

		ExceptionDispatchInfo? failure = EntitySimulation.RestoreSnapshots(detaching, snapshots);
		List<IForgeEntity> entered = [];

		// The ticks after it run again, so a member that left in them is in until it leaves again.
		foreach (Prediction member in _members)
		{
			if (member.LeaveTick > tick)
			{
				member.LeaveTick = null;
			}

			if (restored.Contains(member))
			{
				member.State = PredictionState.InWorld;
			}
			else if (member.State != PredictionState.Limbo)
			{
				member.State = PredictionState.Limbo;
				entered.Add(member.Entity);
			}
		}

		_joining.Clear();
		CurrentTick = tick;
		RefreshLive();

		foreach (IForgeEntity entity in entered)
		{
			EnteredLimbo?.Invoke(entity);
		}

		failure?.Throw();
	}

	// Runs the ticks after the one the members are at again, up to the given one, each member with the input frame it
	// first ran with, and records each tick anew. The tick they're at is recorded anew first, so what was put right
	// there is what a later rollback to it brings back.
	public void Replay(long toTick)
	{
		RecordAgain();

		for (long tick = CurrentTick + 1; tick <= toTick; tick++)
		{
			foreach (Prediction member in InWorld())
			{
				member.Entity
					.Input?.SubmitForReplay(member.History.Holds(tick) ? member.History.InputAt(tick) : default);
			}

			Advance(tick, FixedDeltaTime);
			Record(tick, again: true);
		}
	}

	// Records the tick the members are at anew, as it leaves the ones in the world now.
	public void RecordAgain()
	{
		foreach (Recorded recorded in Capture(InWorld()))
		{
			Store(recorded.Member, CurrentTick, recorded.Snapshot, again: true);
		}
	}

	// Brings back, as it was made, a member made under the key that a rollback went back to before, now that a run of
	// the tick it was made in makes it again: it joins again as the tick is recorded. Returns the member, or null when
	// none was made under the key.
	public Prediction? Remake(SpawnKey key)
	{
		Prediction? member = _members.Find(x => x.Key == key);

		if (member?.State != PredictionState.Limbo || Advancing is null)
		{
			return member;
		}

		EntitySnapshot snapshot = member.History.SnapshotAt(member.JoinTick);
		EntitySimulation.ThrowIfCannotRestore([], [snapshot]);
		LeftLimbo?.Invoke(member.Entity);
		ExceptionDispatchInfo? failure = EntitySimulation.RestoreSnapshots([], [snapshot]);
		member.State = PredictionState.InWorld;
		member.Unconfirmed = false;
		member.LeaveTick = null;
		_joining.Add(member);
		failure?.Throw();
		return member;
	}

	// After ticks ran again from a rollback: a member the rollback went back to before that no run made again is no
	// longer what this client predicts.
	public void ForgetUnmade(long from)
	{
		foreach (Prediction member in _members)
		{
			if (member.State == PredictionState.Limbo && member.JoinTick > from && member.LeaveTick is null)
			{
				member.Unconfirmed = true;
			}
		}
	}

	private static List<Recorded> Capture(IEnumerable<Prediction> members)
	{
		return [.. members.Select(x => new Recorded(x, EntitySimulation.CaptureSnapshot(x.Entity)))];
	}

	private static void Store(Prediction member, long tick, EntitySnapshot snapshot, bool again)
	{
		if (again && member.History.Holds(tick))
		{
			member.History.Rewrite(tick, snapshot);
		}
		else
		{
			member.History.Record(tick, snapshot);
		}
	}

	private void Advance(long tick, double deltaTime)
	{
		Advancing = tick;

		try
		{
			EntitySimulation.Tick(_live, tick, deltaTime);
		}
		finally
		{
			Advancing = null;
		}
	}

	// Records the tick the members just advanced through: the ones freed in it go to limbo, the others keep how it
	// left them, and the ones made in it join, as it left them. Every snapshot is taken first, so a tick that can't be
	// recorded changes nothing.
	private void Record(long tick, bool again)
	{
		List<Recorded> kept = Capture(InWorld());
		List<Recorded> joined = Capture(_joining.Where(x => x.State == PredictionState.InWorld));
		var leaving = _members.Where(x => x.State == PredictionState.Leaving).ToList();

		EntitySimulation.ThrowIfCannotRestore([.. leaving.Select(x => x.Entity)], []);
		ExceptionDispatchInfo? failure = EntitySimulation.RestoreSnapshots([.. leaving.Select(x => x.Entity)], []);

		foreach (Prediction member in leaving)
		{
			member.State = PredictionState.Limbo;
			member.LeaveTick = tick;

			if (_joining.Contains(member))
			{
				member.JoinTick = tick;
			}
		}

		foreach (Recorded recorded in kept)
		{
			Store(recorded.Member, tick, recorded.Snapshot, again);
		}

		foreach (Recorded recorded in joined)
		{
			recorded.Member.JoinTick = tick;
			recorded.Member.History.Record(tick, recorded.Snapshot);
		}

		_joining.Clear();

		if (!again)
		{
			bool continues = _count > 0 && tick == _newestTick + 1;
			_count = continues ? Math.Min(_count + 1, Capacity) : 1;
			_newestTick = tick;
		}

		CurrentTick = tick;
		RefreshLive();

		foreach (Prediction member in leaving)
		{
			EnteredLimbo?.Invoke(member.Entity);
		}

		failure?.Throw();
	}

	private List<Prediction> InWorld()
	{
		return [.. _members.Where(x => x.State == PredictionState.InWorld && !_joining.Contains(x))];
	}

	private void RefreshLive()
	{
		_live = [.. InWorld().Select(x => x.Entity)];
	}

	private readonly record struct Recorded(Prediction Member, EntitySnapshot Snapshot);
}
