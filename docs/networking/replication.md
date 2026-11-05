# Replication

Replication is how the server's world reaches every client. The server simulates every entity; each client keeps a **mirror** of each entity it hears about, holding what the server's entity has, and simulates none of them. This page covers `Gamesmiths.Forge.Networking.Replication`, which a [network session](README.md) runs on both ends: the server's half is `NetworkSession.Replication`, a client's is `NetworkSession.Mirror`.

## The model

The server sends each client one unreliable packet every few ticks. For each entity, the packet carries what changed since the newest packet the client **acknowledged**, written as the values as they are now, not as the changes that led there. A client acknowledges every packet it reads. When a packet is lost, nothing is resent: the next packet carries the same changes again, together with anything newer, because the client still hasn't acknowledged them. Nothing waits for a resend, and a client that reads a packet always ends up where the server was when it sent it.

Under the hood every value the server sends is stamped with the send it changed in. Each client's view of an entity remembers the newest send the client acknowledged, so "what changed since" is every value whose stamp is newer. A view that has acknowledged nothing, a client that just joined or an entity that just came into range, gets the entity whole.

## What replicates

| What | How |
|---|---|
| Attributes | Each attribute's current value, modifiers included, and its minimum and maximum when they change. A mirror holds the value as its base value, with no modifiers. |
| Tags | The final set, as the net indices added and removed since the client last heard. Removals the client can't have seen are never sent. |
| Persistent cues | As state: each one applied on the entity is numbered, and the client applies, updates and removes its own copy as the server's does. |
| Executed cues | As events, numbered per entity and sent until the client acknowledges them or they're `EventExpirySeconds` (0.2 s) old. A cue arriving later than that is never played. |
| The body | Position in thousandths of a unit, rotation as its three smallest components in 9 bits each, velocity in hundredths, and whether it stands on the ground (`BodyCodec`). A client shows it between the states the server sent. |

Effects, abilities, graphs, shared variables and input don't replicate: a mirror holds what they produced, not what produced it. Because a mirror's attributes and tags are what the server sends, queries on them, UI bound to them and cue handlers work on a client as they do on the server.

A cue's parameters reach a client as its magnitude, its normalized magnitude and its source, with these limits: `CustomParameters` is always `null`, and `Source` is `null` when the client has no mirror of the source at the moment the cue arrives, and stays so until the server next updates the cue.

## The server

```csharp
ReplicationServer replication = server.Replication!;
server.PeerJoined += peer =>
{
    Player player = SpawnPlayer();
    player.NetworkId = replication.AllocateId();
    replication.Register(player, sceneIndex: playerScene, owner: peer.Id);
};

// Every tick:
server.Update(localTime);
server.TakeInput(clock.Tick, inputEvents);
EntitySimulation.Tick(entities, clock.Tick, clock.FixedDeltaTime);
replication.Send(clock.Tick);
clock.Advance();
```

- **Registering.** An entity replicates once it's registered, with its `IForgeEntity.NetworkId` set first. A **spawned** entity gets an id from `AllocateId` and the index of the scene clients make it from, in the registry's `AssetKind.Scene` kind. An entity **placed** in the scene, which every machine already has, is registered with scene index -1 and an id every machine gives it the same way; it's always relevant. `Unregister` despawns an entity on every client that has it, and a client that joins later and placed it despawns it too.
- **Owners.** An entity's owner is the peer whose player it is, or `PeerId.None` for the world's. `TakeInput` hands each client's command for the tick to the input of every entity the client owns that has one, and raises the command's events on it when they're among `raisableEvents`. `InputTaken` reports each command and how it arrived.
- **Sending.** Call `Send` once per tick, after simulating it. Every `SendInterval` ticks (2 by default, so 30 times a second at 60 Hz) each client gets a packet, one with no entries when nothing changed, so a client knows the ticks in which nothing did.
- **Budget and priority.** A packet stays under `BudgetBytes` (1200) where it can. Entities with changes are written highest accumulated priority first; one left out keeps its priority and gains its `ReplicatedEntity.Priority` again each send until it goes, so nothing starves. An entity whose changes alone are over the budget still goes, on its own.
- **Relevancy.** `Relevancy` decides which entities each client hears about: `AlwaysRelevant` by default, or `DistanceRelevancy`, which keeps an entity relevant while it's within a radius of any entity the client owns. An entity that stops being relevant is despawned on that client, and spawned again, whole, when it's relevant again. Implement `IRelevancy` for anything else.
- **Bodies.** `BodySource` reads an entity's body state, from its mover by default. A host whose bodies move without a mover reads them there.

## A client

```csharp
ReplicationClient mirror = client.Mirror!;
mirror.Host = new SceneSpawner();      // IReplicationHost: makes and frees the server's entities
foreach (IForgeEntity placed in level.NetworkedEntities)
{
    mirror.BindPlaced(placed);         // before the session is ready
}

// Every frame, to show bodies 100 ms behind the newest state, which is about half a round trip old:
double newest = client.ClockSync.EstimateServerTick(now) - client.Clock.TicksIn(client.ClockSync.RoundTripTime / 2);
double shown = newest - client.Clock.TicksIn(0.1);
foreach (IForgeEntity entity in mirror.Entities)
{
    if (mirror.BodyOf(entity)?.TrySample(shown, out BodyState body) == true)
    {
        PlaceBody(entity, body);
    }
}
```

- **Spawning and despawning.** The host's `IReplicationHost.Spawn` makes the entity the server spawned, from the scene index, and gives it the id; `Despawn` takes one away. Entities placed in the scene are bound with `BindPlaced`, under the same id the server gives them, before the session is ready.
- **Mirrors.** A mirror is torn down as it's bound (see [teardown](../simulation.md#ending-an-entity)): it runs no abilities and refuses every effect. From then on its attributes, tags and persistent cues are what the server last sent, and executed cues play once as they arrive. A host doesn't advance its mirrors.
- **Bodies.** `BodyOf` gives the `RemoteBody`: the states the server sent, by server tick. `TrySample` interpolates between the two around a tick, which may fall between ticks. The newest state arrives about half a round trip after the server's tick it's of, so showing bodies a little behind that, by more than the send interval, keeps a state on either side of the moment shown. A body that rested and then moved is held where it stood until the send before it moved, so it isn't shown drifting there all along. A body that goes away on the server, when `BodySource` stops giving one, goes here too.
- **Late packets.** A packet older than one already read is dropped and not acknowledged, so the server sends what it carried again. `PacketsLate` counts them. So is a packet that can't be read to its end, though what came before the bad part stays applied, as the same values arrive again.

## The wire

A state packet is the send's sequence and tick, then one entry per entity, then a zero. An entry is the entity's id, its kind (state, spawn or despawn) in 2 bits, for a spawn whether it's placed, its scene index, its owner and its attribute count, then a flag and the data for each of attributes, tags, persistent cues, executed cues and the body, padded to a whole byte. A client's acknowledgement goes up on the state channel once per update: the newest sequence it read and a bit for each of the 32 before it, so a lost acknowledgement costs nothing.

A packet a client can't read is dropped and counted, as every packet is (see [the session](README.md#session)).

## A server with nothing to show

A dedicated server sets `CuesManager.Presents` to `false`: no cue handler runs and nothing is queued for presentation, even from a restore, while replication still sees every cue and sends it. It never runs `PresentAbilities`, and that keeps nothing waiting either: only a pass starts presenting a node, so an ability that ends with no pass behind it has nothing left to end.
