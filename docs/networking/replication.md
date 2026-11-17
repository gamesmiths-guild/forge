# Replication

Replication is how the server's world reaches every client. The server simulates every entity; each client keeps a **mirror** of each entity it hears about, holding what the server's entity has, and simulates none of them, except the entities it owns, which it [predicts](prediction.md). This page covers `Gamesmiths.Forge.Networking.Replication`, which a [network session](README.md) runs on both ends: the server's half is `NetworkSession.Replication`, a client's is `NetworkSession.Mirror`.

## The model

The server sends each client one unreliable packet every few ticks. For each entity, the packet carries what changed since the newest packet the client **acknowledged**, written as the values as they are now, not as the changes that led there. A client acknowledges every packet it reads. When a packet is lost, nothing is resent: the next packet carries the same changes again, together with anything newer, because the client still hasn't acknowledged them. Nothing waits for a resend, and a client that reads a packet always ends up where the server was when it sent it.

Under the hood every value the server sends is stamped with the send it changed in. Each client's view of an entity remembers the newest send the client acknowledged, so "what changed since" is every value whose stamp is newer. A view that has acknowledged nothing, a client that just joined or an entity that just came into range, gets the entity whole.

## What replicates

| What | How |
|---|---|
| Attributes | Each attribute's current value, modifiers included, and its minimum and maximum when they change. A mirror holds the value as its base value, with no modifiers. The client that owns the entity is sent its base value too, and its predicted entity takes it when a correction puts it right. The attributes are the ones the entity has when it's registered, or bound on a client: a set added later doesn't replicate. |
| Tags | The final set, as the net indices added and removed since the client last heard. Removals the client can't have seen are never sent. |
| Active effects | As records of what they are, as far as each effect's visibility allows. A mirror holds them to answer queries, and runs nothing of them. See [Effects on the network](effects.md). |
| Persistent cues | As state: each one applied on the entity is numbered, and the client applies, updates and removes its own copy as the server's does. |
| Executed cues | As events, numbered per entity and sent until the client acknowledges them or they're `EventExpirySeconds` (0.2 s) old. A cue arriving later than that is never played, and a client that hears of the entity later doesn't get the ones from before the last send. An entity's last events go with its despawn. The ones the entity's own tick executed go to everyone but its owner, who played them as it predicted them. |
| Graphs | The graphs the entity's abilities run, as far as clients run them: which presentation states run and for how long, the graph's value variables, and its presentation actions as events. A mirror runs a copy of each and follows no link. The owner gets only the runs it doesn't predict. See [Graphs on the network](statescript.md). |
| The body | Position, rotation as its three smallest components, velocity, and whether it stands on the ground (`BodyCodec`), rounded to the session's [body precision](#body-precision). A client shows it between the states the server sent. |

Abilities, shared variables and input don't replicate: a mirror holds what they produced, not what produced it. Because a mirror's attributes, tags and effects are what the server sends, queries on them, UI bound to them and cue handlers work on a client as they do on the server.

A cue's parameters reach a client as its magnitude, its normalized magnitude and its source, with these limits: `CustomParameters` is always `null`, and `Source` is `null` when the client has no mirror of the source at the moment the cue arrives, and stays so until the server next updates the cue.

## Body precision

`NetworkSession.BodyPrecision` says how finely bodies are rounded for the wire: how many steps a position is rounded to in a unit, how many a velocity is in a unit per second, and how many bits each of a rotation's three components takes. The default, `BodyPrecision.Default`, is a thousandth of a unit, a hundredth of a unit per second and 9 bits. Coarser costs fewer bytes and shows bodies less exactly, which suits a game whose unit is a pixel:

```csharp
session.BodyPrecision = new BodyPrecision(PositionSteps: 10, VelocitySteps: 10, RotationBits: 8);
```

Set it before the session connects, the same on both ends: a client whose precision differs is refused (`RefusalReason.BodyPrecision`). Zero is always a rotation level, so a body turned about one axis, or not at all, comes back upright at any precision.

## Tags that travel most

A tag travels as its net index, in one byte for the first 128 indices and two from there. A project with more tags than that keeps the ones it sends most at one byte by listing them, most common first, as it makes its `TagsManager`:

```csharp
var tags = new TagsManager(allTags, commonlyReplicatedTags: ["effect.fire", "cue.vfx.hit"]);
```

They take the first indices in the order given. Both ends must list the same ones: the handshake's tag hash covers the order. `TagsManager.GetNetworkUsage()` says which tags a machine wrote to packets and how many times, most written first, which is the list to give.

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

- **Registering.** An entity replicates once it's registered, with its `IForgeEntity.NetworkId` set first. A **spawned** entity gets an id from `AllocateId` and the index of the scene clients make it from, in the registry's `AssetKind.Scene` kind. Register it once its body is placed: the client that owns it makes its own where the body is then. An entity **placed** in the scene, which every machine already has, is registered with scene index -1 and an id every machine gives it the same way; it's always relevant. `Unregister` despawns an entity on every client that has it, and a client that joins later and placed it despawns it too.
- **Owners.** An entity's owner is the peer whose player it is, or `PeerId.None` for the world's. `TakeInput` hands each client's command for the tick to the input of every entity the client owns that has one, and raises the command's events on it when they're among `raisableEvents`. `InputTaken` reports each command and how it arrived.
- **Sending.** Call `Send` once per tick, after simulating it. Every `SendInterval` ticks (2 by default, so 32 times a second at 64 Hz) each client gets a packet, one with no entries when nothing changed, so a client knows the ticks in which nothing did.
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

mirror.HoldsUntilShown = true;         // a packet waits to be shown at its body's moment
var shown = new InterpolationClock(client.Clock);
mirror.Spawned += PlaceShownBody;      // one spawned by a shown packet, before its graphs run

// Every frame, to show the server's entities 100 ms behind the newest state, which is about half a round trip old:
double newest = client.ClockSync.EstimateServerTick(now) - client.Clock.TicksIn(client.ClockSync.RoundTripTime / 2);
shown.Advance(deltaTime, newest - client.Clock.TicksIn(0.1));
foreach (IForgeEntity entity in mirror.Entities)
{
    PlaceShownBody(entity);            // the bodies first, so what Show sets off happens where they're shown
}

mirror.Show(shown.Tick);               // what the held packets of the ticks shown by now carry

void PlaceShownBody(IForgeEntity entity)
{
    if (mirror.BodyOf(entity)?.TrySample(shown.Tick, out BodyState body) == true)
    {
        PlaceBody(entity, body);
    }
}
```

- **Spawning and despawning.** The host's `IReplicationHost.Spawn` makes the entity the server spawned, from the scene index, and gives it the id; `Despawn` takes one away. Entities placed in the scene are bound with `BindPlaced`, under the same id the server gives them, before the session is ready.
- **Predicted entities.** An entity the server spawned for this client's peer isn't torn down or mirrored: the host makes it as the scene makes it, and `Predict` advances it ahead of the server. Its entry in a packet is read as it arrives, held or not, and checks the tick it's of. `BodyOf` gives no body for it: it moves itself. See [Prediction](prediction.md). An entity one of its graphs spawned is made by the client ahead of the server, and the server's spawn of it binds it by its key: see [Spawning](spawning.md).
- **Mirrors.** A mirror is torn down as it's bound (see [teardown](../simulation.md#ending-an-entity)): it runs no abilities and refuses every effect applied to it. From then on its attributes, tags, active effects and persistent cues are what the server last sent, and executed cues play once as they're applied. A host doesn't simulate its mirrors: their abilities hold copies of the server's graphs, which it advances and presents with `FixedUpdateAbilities`, `UpdateAbilities` and `PresentAbilities`, as any entity's.
- **Bodies.** `BodyOf` gives the `RemoteBody`: the states the server sent, by server tick. `TrySample` interpolates between the two around a tick, which may fall between ticks. The newest state arrives about half a round trip after the server's tick it's of, so showing bodies a little behind that, by more than the send interval, keeps a state on either side of the moment shown. A body that rested and then moved is held where it stood until the send before it moved, so it isn't shown drifting there all along. Pass a snap distance and two states further apart than it, as a respawn leaves, aren't eased between: the body holds the first until the second's tick. A body that goes away on the server, when `BodySource` stops giving one, goes here too.
- **Holding.** With `HoldsUntilShown`, a packet's bodies join their `RemoteBody` as it arrives, and it's acknowledged then, but the rest of it waits until `Show` reaches its tick: spawns, attributes, tags, cues, effects, graphs and despawns. So a mirror's dash animation starts as its body starts moving, and a death burst plays where the body fell, rather than both a delay early. A spawn's bodies wait with it. Acknowledged already, a held packet isn't sent again, so a cue handler or node that throws as it's shown costs only the rest of that entity's entry: the other entries apply, a despawn still takes its entity away, and `Show` throws the first exception after. Without `HoldsUntilShown`, a packet applies as it arrives, and turning it off applies what's held first.
- **The shown tick.** `InterpolationClock` runs with the client's own time and follows the target it's given by running at most a tenth faster or slower, so a target that moves doesn't move what's shown. Estimates of the server's tick and the round trip move with every ping, by as much as the link's jitter, and shown at the target itself every body would hitch each time. One more than 0.25 s away is jumped to.
- **How far behind.** The delay behind the newest state is the host's. A fixed one is simple. `ReplicationClient.InterpolationDelay` works one out from how the state packets arrive instead: each packet's lateness against the estimated server tick, smoothed into a jitter, the send interval as the smallest gap between recent packets' ticks, and the share of packets lost. Its `Ticks` is one send interval, plus `JitterScale` (2.5) times the jitter, plus up to one more interval as the loss grows to 5%, between `MinSeconds` and `MaxSeconds` (0 and 0.25). It rises at once and falls by `FallTicksPerSecond` (4), so a single good moment doesn't take a bad link's margin away. Call `Update` with the local time before reading it, and pass `newest - Ticks` as the shown clock's target. A client's view delay, and so how far back the server tests what its actions hit, follows whichever the host uses ([Hits on the network](lag-compensation.md)).
- **Late packets.** A packet older than one already read is dropped and not acknowledged, so the server sends what it carried again. `PacketsLate` counts them. So is a packet that can't be read to its end, though what came before the bad part stays applied, as the same values arrive again.

## The wire

A state packet is the send's sequence and tick, then one entry per entity, then a zero. An entry is the entity's id, its kind (state, spawn or despawn) in 2 bits, for a spawn whether it's placed, its scene index, its owner and its attribute count, then a flag and the data for each of attributes, tags, persistent cues, executed cues, [effects](effects.md#the-wire), [graphs](statescript.md#the-wire), the body and, for the entity's owner, the [prediction checksum](prediction.md#the-wire), padded to a whole byte. A despawn carries the entity's executed cues and its graphs' actions. A client's acknowledgement goes up on the state channel once per update: the newest sequence it read and a bit for each of the 32 before it, so a lost acknowledgement costs nothing.

A packet a client can't read is dropped and counted, as every packet is (see [the session](README.md#session)).

## A server with nothing to show

A dedicated server sets `CuesManager.Presents` to `false`: no cue handler runs and nothing is queued for presentation, even from a restore, while replication still sees every cue and sends it. It never runs `PresentAbilities`, and that keeps nothing waiting either: only a pass starts presenting a node, so an ability that ends with no pass behind it has nothing left to end.
