// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// A part of a graph a designer marked to run differently in a networked game than its nodes' kinds say: on clients by
/// themselves, or never on a client.
/// </summary>
public enum NodeRegion : byte
{
	/// <summary>
	/// In no region: the node runs where its <see cref="NodeNetworkKind"/> says.
	/// </summary>
	None = 0,

	/// <summary>
	/// Never sent to a client: every machine runs it itself, following links within the region. The server runs it as
	/// part of the graph, and a client runs it from the synced state or the entry that leads into it, for as long as
	/// that state runs. For presentation that needs graph logic of its own, such as sparks on a timer while a flame
	/// burns.
	/// </summary>
	ClientOnly = 1,

	/// <summary>
	/// Never runs on a client, whatever its nodes' kinds: what's in it isn't sent, and a client never follows a link
	/// into it.
	/// </summary>
	ServerOnly = 2,
}
