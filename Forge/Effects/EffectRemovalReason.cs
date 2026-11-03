// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// Describes why an <see cref="ActiveEffect"/> stopped being applied to a target.
/// </summary>
public enum EffectRemovalReason : byte
{
	/// <summary>
	/// The effect ran out of duration and ended on its own.
	/// </summary>
	/// <remarks>
	/// Only effects with <see cref="Duration.DurationType.HasDuration"/> can expire. Effects set as
	/// <see cref="Duration.DurationType.Infinite"/> have no natural end, so every removal of one is reported as
	/// <see cref="Removed"/> or <see cref="Destroyed"/>.
	/// </remarks>
	Expired = 0,

	/// <summary>
	/// The effect was taken away before it could expire, through one of the <see cref="EffectsManager"/> removal
	/// methods.
	/// </summary>
	Removed = 1,

	/// <summary>
	/// The effect's target was torn down, through <see cref="Core.EntitySimulation.Teardown"/>, before the effect could
	/// expire.
	/// </summary>
	/// <remarks>
	/// Like <see cref="Removed"/>, it ends the effect early, so its persistent cues are removed as interrupted. A
	/// handler that shouldn't react to its target's end, such as one that pays the caster back when a curse is
	/// dispelled, checks for it.
	/// </remarks>
	Destroyed = 2,
}
