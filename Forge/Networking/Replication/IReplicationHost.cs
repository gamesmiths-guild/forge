// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Networking.Replication;

/// <summary>
/// What a client's engine integration does for the replication: it makes the entities the server spawns and takes away
/// the ones it despawns. The core doesn't know what an entity is made of on the host.
/// </summary>
public interface IReplicationHost
{
	/// <summary>
	/// Makes the entity the server spawned: instantiates the scene the asset index names, gives the entity the id and
	/// marks it as the server's, so nothing on this machine simulates it. The replication takes it from there: it tears
	/// down whatever the scene started on its own and keeps it as the server says it is.
	/// </summary>
	/// <param name="id">The id the entity is known by on every machine; the entity's
	/// <see cref="IForgeEntity.NetworkId"/> must return it.</param>
	/// <param name="sceneIndex">The index of the scene in the registry's <see cref="AssetKind.Scene"/> kind.</param>
	/// <param name="owner">The peer whose player the entity is, or <see cref="PeerId.None"/> for the world's.</param>
	/// <returns>The entity, or <see langword="null"/> when it couldn't be made; the server's state for it is then
	/// dropped until it's despawned.</returns>
	IForgeEntity? Spawn(NetworkEntityId id, int sceneIndex, PeerId owner);

	/// <summary>
	/// Takes away an entity the server despawned, or one that left this client's view. Its persistent cues were
	/// removed already. An entity placed in the scene rather than spawned is handed here too, for the host to hide or
	/// free.
	/// </summary>
	/// <param name="entity">The entity.</param>
	void Despawn(IForgeEntity entity);
}
