// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Networking.Replication;

/// <summary>
/// What a spawn a graph makes is known by on the server and on the client that predicts the graph's entity, before the
/// server gives the spawned entity an id: the entity whose graph made it, the tick, the ability, the node, and the
/// spawn's place among that node's spawns in the tick. Both machines run the same ticks from the same input, so they
/// key the same spawns the same, and the server's spawn of an entity finds the one its owner's client made ahead of it.
/// </summary>
/// <param name="Spawner">The entity whose graph made the spawn.</param>
/// <param name="Tick">The tick the spawn was made in.</param>
/// <param name="Ability">The ability whose graph made it, by a hash of its name.</param>
/// <param name="Node">The index of the node that made it, in its graph.</param>
/// <param name="Sequence">How many spawns the node made before it in the tick.</param>
public readonly record struct SpawnKey(NetworkEntityId Spawner, long Tick, uint Ability, int Node, int Sequence)
{
	/// <summary>
	/// Gives a seed for the spawned entity's random, from the key and the session's seed: the same on every machine
	/// that makes the entity, so its setup and its ticks roll the same there.
	/// </summary>
	/// <param name="sessionSeed">The session's seed, <see cref="NetworkSession.SessionSeed"/>.</param>
	/// <returns>The seed.</returns>
	public ulong SeedFor(ulong sessionSeed)
	{
		ulong hash = Fnv64.Add(Fnv64.Offset, sessionSeed);
		hash = Fnv64.Add(hash, Spawner.Value);
		hash = Fnv64.Add(hash, (ulong)Tick);
		hash = Fnv64.Add(hash, Ability);
		hash = Fnv64.Add(hash, (ulong)Node);
		return Fnv64.Add(hash, (ulong)Sequence);
	}
}
