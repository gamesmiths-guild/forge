// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Specifies what happens to an effect applied by a <see cref="ConditionalEffect"/> when the effect that applied it
/// ends.
/// </summary>
public enum ConditionalEffectRemovalPolicy : byte
{
	/// <summary>
	/// The applied effect is left alone and lives out its own duration.
	/// </summary>
	Ignore = 0,

	/// <summary>
	/// The applied effect is removed together with the effect that applied it.
	/// </summary>
	/// <remarks>
	/// Requires both effects to be non-<see cref="Duration.DurationType.Instant"/>: an instant applier never becomes
	/// active, so it has no end to hook, and an instant child is already gone by the time that end arrives.
	/// </remarks>
	RemoveOnEnd = 1,
}
