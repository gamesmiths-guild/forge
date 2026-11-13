# Spawning

Networked entities are spawned by the server: the host registers each one (`ReplicationServer.Register`), and every client makes its own copy as the spawn arrives (`IReplicationHost.Spawn`, see [Replication](replication.md)). An entity the server spawns for a client's peer is [predicted](prediction.md) there. This page covers the spawns a predicted entity's own graphs make: the client makes them as the graph runs, ahead of the server, so a shot leaves the moment it's fired, and the server's spawn, arriving later, binds the one the client made instead of making another.

## Spawn keys

Both machines run the entity's graph through the same ticks from the same input, so both make the same spawns, in the same order. A `SpawnKey` names a spawn the way both machines see it:

| Part | What it is |
|---|---|
| `Spawner` | The id of the entity whose graph made it. |
| `Tick` | The tick it was made in. |
| `Ability` | The ability running the graph, by a hash of its name. |
| `Node` | The index of the node that made it, in its graph. |
| `Sequence` | How many spawns that node made before it in the tick. |

A node that makes a spawn asks for its key as it makes it: `ReplicationServer.KeySpawn` on the server, `ReplicationClient.KeySpawn` on the client. Each ask takes the next key, so ask once per spawn, whether or not the node goes on to make it. The server registers the spawned entity with its key, and the key goes with the spawn to the entity's owner alone. A spawn's random rolls from its key: seed it with `SpawnKey.SeedFor(session.SessionSeed)` on both machines, since the client makes it before the server gives it an id.

```csharp
// The server's node:
SpawnKey? key = server.Replication!.KeySpawn(graphContext, Index);
Bolt bolt = MakeBolt(seed: key?.SeedFor(server.SessionSeed));
bolt.NetworkId = server.Replication.AllocateId();
bolt.RunSetup();
server.Replication.Register(bolt, BoltScene, shooterOwner, key);

// The client's node, for a graph it predicts:
if (client.Mirror!.KeySpawn(graphContext, Index) is SpawnKey key)
{
    if (client.Mirror.FindSpawn(key) is not null)
    {
        return;                                     // a correction running the tick again: it's made already
    }

    Bolt bolt = MakeBolt(seed: key.SeedFor(client.SessionSeed));

    if (!client.Mirror.PredictSpawn(bolt, key, id => bolt.NetworkId = id))
    {
        FreeBolt(bolt);                             // the server despawned it already: a correction doesn't make it again
        return;
    }

    bolt.RunSetup();                                // after PredictSpawn, so it runs as a predicted entity's
}
```

`KeySpawn` gives `null` for a graph that isn't an ability's run, or, on the client, for one it doesn't predict: a mirror's copy of the server's graph, or a graph of a spawn the server hasn't bound yet. Such spawns are the server's to make.

## On the client's timeline

The entities a client predicts share one timeline. `Predict` advances them all through each tick together, every one beginning the tick before any advances, and a correction rolls them all back and runs their ticks again together, so what one does to another comes out the same each time.

A spawn joins the timeline in the tick it was made in, as that tick left it, and advances with it from the next tick on. Until the server's spawn of it arrives it has no id. The server's spawn record carries the key, which finds it: `PredictSpawn`'s `bind` gives it the server's id, and from then on each packet checks it against the server's word like any entity the client predicts. It stays on the timeline for good, predicted.

## Limbo

An entity that leaves the timeline waits in **limbo**: out of the simulation, having let go of everything it held. `EnteredLimbo` asks the host to hide it and take it out of what isn't Forge's, such as physics, and `LeftLimbo` to show it again, before it's restored.

- **A correction rolls back to before the tick it was made in.** It goes to limbo, and the tick running again makes it again: `FindSpawn` brings it back as it was made, instead of the host making another.
- **It's freed.** `PredictDespawn` takes it out as the tick is recorded, as when its graph's Queue Free runs. A correction to a tick it was in brings it back. The server's despawn hands it to `IReplicationHost.Despawn`.
- **The server never made it.** Once a packet of the tick it was made in or later arrives without its spawn, the server didn't make it there, as when its press arrived too late: it goes to limbo, and no correction brings it back. If its spawn arrives after all, from a packet the budget left it out of, it comes back from the tick it was made in, or, once the timeline can't go back that far, as it was last recorded, which the server's word then puts right. Once no correction can reach that tick, it's handed to `IReplicationHost.Despawn`.

A spawn of the server's that arrives with a key this client made nothing under, as when the server's tick made a spawn the client's didn't, is made through `IReplicationHost.SpawnPredicted`, with the key to seed it, and joins the timeline at the tick the server made it in, as if it had been predicted from there. A spawn the server despawned isn't made again by a correction that runs its tick again: `PredictSpawn` refuses its key.

## What only this machine shows

A predicted graph also makes things nothing networks: a burst, a sound's emitter, a decal. A correction runs the ticks after the one it rolled back to again, and the node making one runs again in them. A host keeps what such a node makes by its key (`KeySpawn` again, on the client), finds it there while `GraphContext.IsCorrecting` instead of making another, and makes nothing new during a correction. `Replayed` is raised after each correction with the tick it rolled back to: what the host kept from a later tick that the correction didn't find again, no tick made again, so the host frees it.

## Limits

- A spawn whose spawner the server hasn't bound yet, such as a shot's own spawn before the shot is bound, isn't predicted: it's the server's.
- A spawn a correction makes again comes back as it was first made. One the correction moved the spawner before is where it was first made, and the server's word puts it right once it's bound.
- A predicted entity freed here that the server doesn't free stays in limbo, hidden, until the server's despawn of it.

## The wire

The spawn of an entity a graph made carries, for its owner alone, the key: the spawner's id, how many ticks before the packet's it was made in, the ability's hash, the node and the sequence.
