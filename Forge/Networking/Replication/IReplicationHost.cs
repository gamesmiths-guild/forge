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
	/// Makes an entity the server spawned for this client's peer, which this client predicts. It's made live, as the
	/// scene makes it, setup included, and not marked as the server's: the client runs it ahead of the server, and the
	/// replication puts it right as the server's word arrives. Place its body where <paramref name="body"/> says before
	/// anything of it runs, so its setup runs where the server's did. The default makes it through
	/// <see cref="Spawn"/>.
	/// </summary>
	/// <param name="id">The id the entity is known by on every machine; the entity's
	/// <see cref="IForgeEntity.NetworkId"/> must return it.</param>
	/// <param name="sceneIndex">The index of the scene in the registry's <see cref="AssetKind.Scene"/> kind.</param>
	/// <param name="owner">This client's peer.</param>
	/// <param name="body">Where the server's body was as it registered the entity, exactly, or <see langword="null"/>
	/// for an entity without one.</param>
	/// <returns>The entity, or <see langword="null"/> when it couldn't be made.</returns>
	IForgeEntity? SpawnPredicted(NetworkEntityId id, int sceneIndex, PeerId owner, BodyState? body)
	{
		return Spawn(id, sceneIndex, owner);
	}

	/// <summary>
	/// Takes away an entity the server despawned, or one that left this client's view. Its persistent cues were
	/// removed already, and its graphs ended: what they still show ends in its next
	/// <see cref="EntityAbilities.PresentAbilities"/>, which the host must call, here or as it frees the entity, before
	/// the entity is gone or leaves the host's presentation pass. An entity placed in the scene rather than spawned is
	/// handed here too, for the host to hide or free.
	/// </summary>
	/// <param name="entity">The entity.</param>
	void Despawn(IForgeEntity entity);
}
