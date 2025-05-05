// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.GameplayEffects.Stacking;

/// <summary>
/// Defines what happens when stack limit is reached and a new application happens.
/// </summary>
public enum StackOverflowPolicy : byte
{
	/// <summary>
	/// Allows a new stack application.
	/// </summary>
	AllowApplication = 0,

	/// <summary>
	/// Denies the new stack application.
	/// </summary>
	DenyApplication = 1,
}
