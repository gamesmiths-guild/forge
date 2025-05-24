// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects.Stacking;

/// <summary>
/// Define what happens to the stack when the duration of the effect ends.
/// </summary>
public enum StackExpirationPolicy : byte
{
	/// <summary>
	/// Clear all stacks.
	/// </summary>
	ClearEntireStack = 0,

	/// <summary>
	/// Removes a single stack and refreshes the effect duration.
	/// </summary>
	RemoveSingleStackAndRefreshDuration = 1,
}
