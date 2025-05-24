// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects.Duration;

/// <summary>
/// The different types of duration supported by effects.
/// </summary>
public enum DurationType : byte
{
	/// <summary>
	/// Instant effects apply their modifiers instantly on the base value.
	/// </summary>
	Instant = 0,

	/// <summary>
	/// Infinite effects have no exit time, they last as long as programatically allowed.
	/// </summary>
	Infinite = 1,

	/// <summary>
	/// Effects with duration will remove themselves after some time.
	/// </summary>
	HasDuration = 2,
}
