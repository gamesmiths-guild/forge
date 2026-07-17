// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Nodes.Action;

/// <summary>
/// Selects how an <see cref="SetEffectLevelNode"/> changes effect levels when it executes.
/// </summary>
public enum SetEffectLevelOperation : byte
{
	/// <summary>
	/// Increases the effect's level by one.
	/// </summary>
	LevelUp = 0,

	/// <summary>
	/// Sets the effect's level to the value resolved from the level input.
	/// </summary>
	SetLevel = 1,
}
