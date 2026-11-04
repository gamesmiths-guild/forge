// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Networking.Replication;

/// <summary>
/// Decides which entities a client hears about. An entity that stops being relevant to a client is despawned there,
/// and one that becomes relevant again is spawned afresh, with its whole state. Entities placed in the scene are always
/// relevant to everyone.
/// </summary>
public interface IRelevancy
{
	/// <summary>
	/// Whether a client should hear about an entity now.
	/// </summary>
	/// <param name="peer">The client.</param>
	/// <param name="entity">The entity, with the body the server last captured for it.</param>
	/// <param name="server">The server, for the other entities and whose they are.</param>
	/// <returns><see langword="true"/> to replicate the entity to the client.</returns>
	bool IsRelevant(PeerId peer, ReplicatedEntity entity, ReplicationServer server);
}
