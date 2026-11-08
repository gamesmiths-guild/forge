# Graphs on the network

[Replication](replication.md) sends a client what an entity's graphs did to the entity: attribute values, tags, effects and cues. It also sends what they show, so a client sees the server's entities roll, glow and play their sounds. A mirror's abilities run a copy of each graph the server's entity runs, as far as clients run it: its presentation states start and end as the server's do, its presentation actions run as the server ran them, and no link is followed. This page covers what a client runs, what travels, regions, and writing nodes that run on a client.

## What a client runs

| In the server's graph | On a client |
|---|---|
| A [presentation](../statescript/nodes/state/README.md#simulation-and-presentation) state node | Starts when the server's starts and ends when the server's ends, with `StateNodeContext.ActivationReason` `Replication`. Its `OnActivate`, its fixed and frame updates and its presentation run as they do anywhere. Ending itself does nothing here: `DeactivateNode` and `DeactivateNodeAndEmitMessage` wait for the server's word, so a node never ends twice. |
| A presentation action node | Runs when the server's ran, among the states in the order the server ran them. |
| A simulation or authority node | Doesn't run. What it did arrives as the entity's state. |
| A node in a `ClientOnly` region | Runs here by itself, following links within the region, from the synced state or the entry that leads into it, and ends with that state. |
| A node in a `ServerOnly` region | Never runs. |

The entity a client owns is [predicted](prediction.md) rather than mirrored: its graphs run there whole, as on the server, following links. What its owner doesn't predict reaches it as copies, as above: the runs of `ServerOnly` abilities and of coded behaviors that can't be predicted. A run of one it predicts that started from elsewhere, such as by another entity's event, the owner takes in at the tick it started after and runs from there. A node whose `NetworkKind` is `Authority` doesn't execute on any client, in a copy or a predicted run, and the graph goes on as if it had. While a correction runs ticks again, `GraphContext.IsCorrecting` is true, and a presentation action that shows something as it executes, such as a sound played once, shows nothing.

A state that has been running for a while when a client hears of it, as when the client joins late, is caught up: it gets one fixed update as long as it already ran on the server before it's presented. A node that counts its progress in its context, as Play Animation counts a clip's elapsed time, then presents part way through, the same way it does after a [restore](../rollback.md). One that already played out by then shows nothing.

A packet is applied in the order the server's graph had things: the variables first, so a state that starts here reads what it read there, then what the server ended or started over ends here, newest first, then the states that started and the actions that ran, each action after the activations it followed. An action that makes something a state then acts on, such as a cosmetic scene that a Move To flies, runs before that state starts.

### Presentation in a graph

A node acting on something a presentation node made is presentation too, in that graph: a simulation Move To flying the instance a cosmetic scene node spawned only moves something cosmetic, so a client runs it as the server says. `Graph.NetworkKindOf(node)` gives each node's kind in its graph.

A node acts on what another made when one of its inputs is bound to a graph variable the other writes as an output, directly or through a property that reads the variable as it is. Presentation spreads that way along a chain of nodes. It stops at authority nodes, at a variable read deeper inside a property, and at nodes in a `ServerOnly` region, whose work never reaches a client.

## What travels

For each graph an entity's abilities run that clients run something of, the server sends, as each changes:

- **What the run is**, once: the ability's index in the [asset registry](README.md#identity), and the activation's target, source, level and magnitude. A client's nodes read them from the `AbilityBehaviorContext` as they do on the server, with the mirror as the owner.
- **Which presentation states run**, as the set the server has now, each with how long it has run.
- **The graph's value variables**, single values and arrays, as they change. Object variables, such as an entity or a node, stay on each machine: a client's nodes read its own.
- **The presentation actions it ran**, as events numbered with the entity's executed cues, sent until the client acknowledges them or they're `EventExpirySeconds` (0.2 s) old.
- **That the run ended.**

Every value variable of a graph clients run something of travels to every client that has the entity, whichever region writes it: a `ServerOnly` region keeps its nodes on the server, not its variables. A value no client may see doesn't belong in such a graph's variables.

Shared variables and activation data don't travel. A graph whose presentation needs them copies them into a graph variable, with a Set Variable node or the data binder of `GraphAbilityBehavior<TData>`.

The owner of an entity gets only the runs it doesn't predict. A graph that clients run nothing of sends nothing at all. A run too short for any send to see it, one that started and ended between two, still brings its actions. So does an entity that goes: its last events, the burst it made as it died, go with its despawn, and play before it's taken away.

An ability's graph travels as the ability's index, so one the registry doesn't list runs on the server alone. `ReplicationServer.AbilityNotListed` is raised the first time it runs a graph clients would run something of.

## Regions

`Node.Region` puts a node in a region, set while the graph is built, before it first runs:

| `NodeRegion` | What it does |
|---|---|
| `None` | Nothing: the node runs where its kind says. |
| `ClientOnly` | Never sent. Every machine runs the region by itself, the server as part of the graph and a client from the synced state or the entry that leads into it, for as long as that state runs. For presentation with graph logic of its own, such as sparks on a timer while a flame burns. |
| `ServerOnly` | Never runs on a client, whatever its nodes' kinds, and nothing of it is sent. For work that's the server's alone, such as spawning a networked entity. |

```csharp
var flame = new FlameNode();                                   // a presentation state
var sparkle = new LoopTimerNode() { Region = NodeRegion.ClientOnly };
var spark = new SparkNode() { Region = NodeRegion.ClientOnly }; // a presentation action
graph.AddNode(flame);
graph.AddNode(sparkle);
graph.AddNode(spark);
graph.AddConnection(new Connection(flame.OutputPorts[StateNode<StateNodeContext>.SubgraphPort], sparkle.InputPorts[0]));
graph.AddConnection(new Connection(sparkle.OutputPorts[LoopTimerNode.OnIntervalPort], spark.InputPorts[0]));
```

The server runs the flame and its sparks; a client starts the flame when the server's starts, and runs its own sparks under it, at its own times, until the server's flame ends.

## Writing nodes that run on clients

A presentation node's simulation half runs on clients: its `OnActivate`, `OnRestart`, `OnFixedUpdate`, `OnUpdate` and `OnDeactivate`. That's why the [rule](../statescript/nodes/state/README.md#simulation-and-presentation) that presentation never changes gameplay covers the whole node. On a client the node reads what its inputs give there: the mirror's attributes, tags and effects, the graph variables the server sent, and the client's own objects. A node that has to know where it runs reads `ActivationReason`.

A host runs a mirror's copies with its abilities, as any entity's: `EntityAbilities.FixedUpdateAbilities` on its fixed step, `UpdateAbilities` each frame and `PresentAbilities` at the end of it. Nothing else of a mirror advances.

## Limits

- Only graphs that abilities run travel, through `GraphAbilityBehavior`. A `GraphProcessor` a host runs on its own doesn't.
- A state that started and ended between two sends isn't seen by a client, as with every value that changed and changed back. An action always is.
- An action reads the graph's values as its packet carries them: the newest the server's run had by then. Two actions between sends that read different values both read the later one.
- A state the server restarted, through `RestartOnRetrigger`, ends and starts again on a client, and what its subgraph runs there starts over with it.
- A run's target or source that a client doesn't have yet reads as `null` until it arrives, in the same packet or a later one.
- The server runs `ClientOnly` regions too, a dedicated server included, so they must not change gameplay.

## The wire

A graph run is a record in the entity's entry, after its effects: its instance number, then whichever of these the client hasn't acknowledged.

- **What it is**, once: the ability's index, the target's and source's ids, the level and the magnitude.
- **Its running states**, when they change: each one's number in the graph, the server's activation number, and how many ticks before this packet's it started.
- **Its variables** that changed, by number, as their raw bits.
- **Its actions**: each one's event number, its number in the graph, and the activation it followed.
- **Whether it ended.**

Numbers are varints, a byte each up to 127. A despawn carries the entity's executed cues and its graphs' actions. A graph in steady state costs nothing.
