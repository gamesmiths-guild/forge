// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking;

/// <summary>
/// The kinds of asset a session refers to by index rather than by name.
/// </summary>
public enum AssetKind : byte
{
	/// <summary>
	/// An effect's data.
	/// </summary>
	Effect = 0,

	/// <summary>
	/// An ability's data.
	/// </summary>
	Ability = 1,

	/// <summary>
	/// A Statescript graph.
	/// </summary>
	Graph = 2,

	/// <summary>
	/// A scene a networked entity spawns from.
	/// </summary>
	Scene = 3,
}
