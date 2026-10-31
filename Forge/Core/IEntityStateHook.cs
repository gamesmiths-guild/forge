// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Captures and restores state an entity keeps outside Forge, so rolling the entity back covers it too.
/// </summary>
/// <remarks>
/// <para>It's shaped like the mover contract (<see cref="IEntityMover"/>): an entity snapshot captures the state with
/// <see cref="CaptureState"/>, and restoring the snapshot puts it back with <see cref="RestoreState"/>, after the rest
/// of the entity. The state is a single 128-bit value.</para>
/// <para>Gameplay state is best kept in Forge, in attributes, tags, effects and shared variables, which roll back on
/// their own. The hook is for state that can't live there.</para>
/// </remarks>
public interface IEntityStateHook
{
	/// <summary>
	/// Captures the state.
	/// </summary>
	/// <returns>The state.</returns>
	Variant128 CaptureState();

	/// <summary>
	/// Puts back a state that <see cref="CaptureState"/> returned.
	/// </summary>
	/// <param name="state">The state to restore.</param>
	void RestoreState(in Variant128 state);
}
