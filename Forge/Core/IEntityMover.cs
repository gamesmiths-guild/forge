// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Moves an entity's body one fixed step at a time, in the movement stage of the entity's tick.
/// </summary>
/// <remarks>
/// <para>This is the mover contract. A host's character movement implements it, and so can any custom movement. The
/// simulation calls <see cref="Move"/> once per tick, after the input stage and before effects and abilities advance.
/// Rollback captures the body's state and restores it to simulate ticks again.</para>
/// <para>A mover reads what drives it, such as the entity's <see cref="IForgeEntity.Input"/>, attributes and tags,
/// inside <see cref="Move"/>. The same body state and the same tick then always move the body the same way.</para>
/// </remarks>
public interface IEntityMover
{
	/// <summary>
	/// Moves the body by one fixed step.
	/// </summary>
	/// <param name="fixedDeltaTime">The length of the fixed step, in seconds.</param>
	void Move(double fixedDeltaTime);

	/// <summary>
	/// Captures the body's state.
	/// </summary>
	/// <returns>The body's state.</returns>
	BodyState CaptureState();

	/// <summary>
	/// Puts the body back in a state that <see cref="CaptureState"/> returned.
	/// </summary>
	/// <param name="state">The state to restore.</param>
	void RestoreState(in BodyState state);
}
