// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// An <see cref="IAbilityBehavior"/> whose run an entity snapshot captures and a restore puts back, so a client can
/// predict the ability and roll it back. Statescript graphs implement it; a coded behavior opts in by implementing it
/// too.
/// </summary>
/// <remarks>
/// <para>A run of a behavior that doesn't implement it is left out of snapshots: a restore neither ends nor brings it
/// back, and a client never predicts it. It runs on the server, and its owner sees what it did through the entity's
/// replicated state.</para>
/// <para>The behavior holds its run's state, so one behavior object serves one run at a time.</para>
/// </remarks>
public interface ISnapshotAbilityBehavior : IAbilityBehavior
{
	/// <summary>
	/// Captures the run as it is now, for an entity snapshot.
	/// </summary>
	/// <remarks>
	/// Capture everything the run needs to go on, the context it was started with included: a restore can bring back a
	/// run after <see cref="DropRun"/> let it go. Return copies of what the run changes in place, such as a list, since
	/// a snapshot never changes and can be restored any number of times. Counting time in fixed steps, as the fixed
	/// rail does, keeps a restored run coming out the same when the ticks after it run again.
	/// </remarks>
	/// <returns>What <see cref="RestoreSnapshot"/> needs to put the run back.</returns>
	object? CaptureSnapshot();

	/// <summary>
	/// Puts the run back as it was when <see cref="CaptureSnapshot"/> returned <paramref name="snapshot"/>.
	/// </summary>
	/// <remarks>
	/// A restore calls it between <see cref="OnRestoreDetach"/> and <see cref="OnRestoreAttach"/>, and it must be
	/// silent: change only the behavior's own fields, and apply no effect, raise no event and fire no cue. Copy out of
	/// the snapshot anything the run goes on changing in place.
	/// </remarks>
	/// <param name="snapshot">What <see cref="CaptureSnapshot"/> returned.</param>
	void RestoreSnapshot(object? snapshot);

	/// <summary>
	/// Stops the run without ending it, for a restore to a moment before it started: no callback runs and nothing it
	/// started is undone through it, since the restore puts the entity back as it was. What it shows ends in the next
	/// presentation pass.
	/// </summary>
	void DropRun();

	/// <summary>
	/// Lets go of what the run holds outside itself, such as an event subscription, before a restore puts the entity's
	/// state back. A restore lets go of every hold on the entity newest first and takes hold again oldest first, the
	/// run counted from when it started.
	/// </summary>
	void OnRestoreDetach()
	{
	}

	/// <summary>
	/// Takes hold again of what the run holds outside itself, after a restore put the entity's state back, from the
	/// restored state. Like <see cref="RestoreSnapshot"/>, it applies no effect, raises no event and fires no cue.
	/// </summary>
	void OnRestoreAttach()
	{
	}
}
