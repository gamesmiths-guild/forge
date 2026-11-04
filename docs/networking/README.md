# Networking

Forge's networking puts one server in charge of a game that several machines play. The server simulates every entity from the players' input and sends the result down; each client sends only what its player pressed. This page covers the foundation that milestone 7 lays in `Gamesmiths.Forge.Networking`: how bytes travel, how a session starts, how clocks agree and how input gets to the server. What the server sends down, and how a client predicts its own player, come with the milestones after it.

Nothing here depends on an engine. An engine integration provides a transport over its own sockets and drives the session from its loop; Forge for Godot does that with a session node.

## The model in one paragraph

Time is a fixed tick, the same rate on every machine. The server's clock is the session's time. A client runs its clock a little ahead of the server's, by about a round trip plus a margin, and sends the server one **input command** per tick: which actions are held, pressed and released, the analog vectors, the aim, and any discrete input events, all for the tick the client's clock named. The server keeps each client's commands by tick in a **playout buffer** and, when it simulates a tick, takes that tick's command for the client's entity. Because the client was ahead, the command is there. If it isn't, the tick runs with what the client last held, and the server tells the client, through its pings, to run further ahead.

## Transport

`INetworkTransport` moves bytes between peers. The engine provides one; the core never opens a socket.

- **Polled, not called back.** The host takes what arrived with `TryReceive`, once per tick, before simulating it. Nothing changes the simulation from inside a socket callback, and a test steps delivery by hand.
- **Reliable or unreliable, per send.** Reliable sends arrive in order with the other reliable sends on the same channel. Unreliable sends arrive at most once, possibly late, out of order, or never. Connection control is reliable; everything else is unreliable on purpose, since a late packet about a tick that has passed is worthless and nothing should wait for it.
- **Peers.** The server is always `PeerId.Server`. A client talks only to the server.

Channels (`Protocol`): `ControlChannel` for the handshake and pings, `InputChannel` for clients' input, `StateChannel` for the server's state from milestone 8.

`LoopbackNetwork` is a whole session in one process, for tests and for a listen host's own connection. `ConditionedTransport` wraps any transport and treats what it sends the way a real link would: it holds packets for a latency plus jitter, loses a share of the unreliable ones, and keeps reliable ones in order. It takes time from the host and draws from an `IRandom`, so a run under loss repeats with a seed. `NetworkConditions.Poor` is 200 ms each way, 50 ms of jitter and 5% loss, the conditions the plan tests prediction under.

## Encoding

`BitWriter` and `BitReader` write and read packets bit by bit: a flag costs one bit, a value with a known range exactly the bits it needs, and a count or a delta as many bytes as its magnitude needs (`WriteVarUInt`, `WriteVarInt`). A float is sent rounded to a step (`WriteQuantized(value, 0.001f)` for thousandths), and `BitWriter.Quantize` rounds the same way, so the sender simulates exactly what the receiver gets.

A reader that runs out of data, or meets a value no writer produces, throws `MalformedPacketException`. A packet from the network is untrusted: the session catches it, drops the packet and counts it, and trusts nothing read before it.

## Identity

Two machines must mean the same thing by every index they exchange.

- **Assets.** `NetworkAssetRegistry` numbers every effect, ability, graph and networked scene the game can send. Each is registered under a key the host chooses, the keys of each kind are sorted and numbered, and the kinds and keys are hashed. An engine integration keys on its own stable asset ids (Godot's resource UIDs, Unity's asset GUIDs), so renaming or moving a file changes nothing on the wire; a bare host keys on names. The registry is sealed before the first handshake and never changes after.
- **Entities.** `NetworkEntityId` is the id an entity is known by on every machine. The server assigns it as the entity spawns and sends it with the spawn. `IForgeEntity.NetworkId` returns `None` by default, which every entity is offline; a host that networks its entities overrides it with the id it was given.
- **Tags** already have net indices from the `TagsManager`. Discrete input events travel as them. `Protocol.HashOf(tagsManager)` is what the handshake compares.
- **Input actions** travel as slots of the `InputActionSet`, so both ends must list the same actions in the same order. `Protocol.HashOf(actions)` is what the handshake compares.

### Checksums

`EntitySimulation.Checksum(entity)` hashes an entity's whole simulation state into 64 bits that come out the same on every machine whose copy is in the same state. Another entity it refers to, an effect's source or a variable, counts by its `NetworkId`, or as local when it has none, so a server's and a client's checksum of the same entity agree. `WorldHistory.ChecksumAt` does the same for a whole world, where entities without ids count by their place. The network uses them sampled, every so many ticks, to notice a client that has drifted from the server.

## Session

`NetworkSession` is one end of a session over a transport: the server, or a client. The host calls `Update(localTime)` once per tick, before simulating it.

```csharp
// Server.
var registry = new NetworkAssetRegistry();
registry.Register(AssetKind.Effect, "uid://b3k2...", burningData);
registry.Seal();

var clock = new SimulationClock(tickRate: 60);
var server = new NetworkSession(serverTransport, clock, registry, tagsManager, actions, sessionSeed: seed, maxClients: 4);
server.PeerJoined += peer => SpawnPlayerFor(peer);
var inputEvents = new TagContainer(tagsManager, [Tag.RequestTag(tagsManager, "input")]);

// Every tick:
server.Update(localTime);
foreach (NetworkPeer peer in server.Peers)
{
    peer.Input.Take(clock.Tick, out InputCommand command);
    PlayerOf(peer).Input!.Submit(command.Frame);
    command.RaiseEvents(PlayerOf(peer), inputEvents);
}
EntitySimulation.Tick(entities, clock.Tick, clock.FixedDeltaTime);
clock.Advance();
```

```csharp
// Client.
var client = new NetworkSession(clientTransport, new SimulationClock(60), registry, tagsManager, actions);

// Every tick:
client.Update(localTime);
if (client.State == SessionState.Ready)
{
    client.SendInput(new InputCommand(client.Clock.Tick, collectedFrame));
    // ...simulate the local player's tick, from milestone 11...
    client.Clock.Advance();
}
```

### Handshake

A client connects and sends `Hello`: its protocol version and the hashes of its registry, its tags and its input action set. The server answers `Welcome`, with the tick rate, its current tick and the session seed, or `Refused`, with why (`RefusalReason`: protocol version, asset registry, tags, input actions, or a full session). A refused client leaves once it knows why; the server disconnects one that hasn't after `RefusalLinger` (2 s). A client that never sends `Hello` is disconnected after `HandshakeTimeout` (5 s). A client whose clock runs at another rate than the server's leaves too. Being welcomed sets the client's clock to the server's tick and drops any input it recorded before. `SessionState` says where a client stands: connecting, handshaking, ready, refused or disconnected.

### Clock sync

Once welcomed, a client pings every `PingInterval` (100 ms). The server answers each `Ping` with a `Pong` carrying its tick, the newest input tick it has from the client and how many input ticks it holds ahead of the one it last ran. From the pings, `ClockSync` estimates the round trip and the server's tick:

- A ping that left at local time `t` and was answered when the server was at tick `s` says the server was at `s` at about `t + rtt / 2`. The server's tick at any later local time follows from that.
- Samples are noisy, since a packet held up by jitter reports a round trip it didn't have. The estimate keeps the last few samples and uses the one with the shortest round trip, the one least delayed by anything but the wire. `RoundTripTime` is the smoothed figure for display, `BestRoundTripTime` the one the estimate stands on, and `Jitter` how far the samples spread.
- `TargetClientTick(localTime, marginTicks)` is the tick a client's clock should be at: the server's estimated tick plus a round trip plus the margin. The host moves its clock toward it; milestone 11 does so smoothly, by dilating time.

### Input upstream

Each tick, a client adds its `InputCommand` to its `InputWindow` and sends the whole window, the last 8 ticks, in one unreliable packet. A lost packet costs nothing: the next one carries the same ticks again. The `Pong`'s acknowledgement drops the ticks the server already has from the window. Frames are written against the one before them (`InputCodec`), so a tick in which nothing changed costs a bit, and a tick in which only the aim moved costs the aim's movement; a frame read back is bit for bit the frame written.

On the server, each client's `InputPlayoutBuffer` keeps the first copy of each tick and hands the simulation that tick's command in order. A tick that never arrived runs with the previous frame's held actions and no presses or releases, the same rule `EntityInput` applies to a tick nothing was submitted for. The buffer reports whether the gap was a loss (a later tick had arrived) or lateness (nothing had), since the first is the link's fault and the second means the client's lead is too short. Its `Depth`, how many ticks ahead have arrived, goes back in the pongs: zero means late, a steady few is right, growing means the client runs too far ahead. A command more than `MaxTicksAhead` (128) ticks past the tick last taken, or past the tick the client joined at, is dropped.

A command's **events** are discrete input events, identified by tags, for things like a menu choice that aren't held buttons. The server raises each as a Forge event on the client's entity, with the entity as source and target, so an ability triggered by the tag activates from it. `InputCommand.EventIndex` gives the index a tag travels as. A client can send any index, so `RaiseEvents` takes the tags clients may raise and skips the rest, along with indices that name no tag: a client can't trigger an ability meant for a server event.

## What's next

- **Milestone 8** sends the world down: attributes, tags, cues and bodies as deltas against what each client acknowledged, with spawns and despawns as state.
- **Milestone 11** makes the client run its own player ahead of the server and reconcile it against what the server sends back.

See the [plan](https://github.com/gamesmiths-guild/forge/tree/task/networking-plan/plans/networking) for the whole design and its decisions.
