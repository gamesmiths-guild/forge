// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Why a state node activated.
/// </summary>
public enum NodeActivationReason : byte
{
	/// <summary>
	/// A message reached its input along a link.
	/// </summary>
	Link = 0,

	/// <summary>
	/// The server said it's active: the node runs on a client's copy of the server's graph, where nothing travels along
	/// links into it.
	/// </summary>
	Replication = 1,
}
