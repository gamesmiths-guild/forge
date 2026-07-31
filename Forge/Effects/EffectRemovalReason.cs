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
	/// <see cref="Removed"/>.
	/// </remarks>
	Expired = 0,

	/// <summary>
	/// The effect was taken away before it could expire, through one of the <see cref="EffectsManager"/> removal
	/// methods.
	/// </summary>
	Removed = 1,
}
