# Rollback

A game that runs on a fixed step can put an entity back as it was after an earlier tick and run the ticks since then again. Networking does it when the server's word on a past tick differs from what a client predicted. A test does it to prove the game plays the same way twice. Three pieces do the work: the **entity snapshot**, which captures an entity and puts it back, the **entity history**, which keeps an entity's recent ticks to roll back to, and the **world history**, which does the same for a world of entities together, keeping the ones that die or join meanwhile in limbo.

Both build on the [simulation tick](simulation.md): a tick that runs again with the same input has to come out the same.

## Entity snapshots

`EntitySimulation.CaptureSnapshot` takes a snapshot of everything Forge simulates for an entity, and `EntitySimulation.RestoreSnapshot` puts it back:

```csharp
EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);

// ...the entity moves on...

EntitySimulation.RestoreSnapshot(entity, snapshot);
```

| Part | What comes back |
|---|---|
| Attributes | The attribute sets, and their base, current, minimum and maximum values, with the modifiers in each channel exactly as they were rather than added up again. A set added since the snapshot leaves as a removed set does, without the modifiers effects gave it. |
| Tags | The base tags, and how many effects grant each modifier tag. |
| Effects | Each active effect with its level, stacks, remaining duration, period timing, inhibition, set-by-caller values, evaluated magnitudes and the state of its [components](effects/components/README.md#rollback-hooks). |
| Abilities | The granted abilities with their grant sources, levels, inhibition and cooldowns, the blocked ability tags, and each running instance with its Statescript graph. |
| Shared variables | Their values. |
| Input | The current frame and the one waiting for the next tick, if the entity has an `Input`. |
| Random | Where the generator is, if the entity has a `Random`. |
| Body | The `BodyState` its [mover](simulation.md#movement) captures, if the entity has a `Mover`. |
| Outside state | What its [state hook](#state-outside-forge) captures, if the entity has a `StateHook`. |

**A restore is silent.** No attribute, tag, effect or ability event is raised, no cue fires, and no graph node activates or deactivates. The [presentation pass](simulation.md#presentation-pass) after it shows only the net change in what the graphs present.

**Effects and abilities keep their identity.** Active effects, abilities and ability instances come back as the same objects, so a handle from before the snapshot points at the same thing after the restore. An effect removed since the snapshot comes back, and its handle is valid again. An effect applied since then goes without a removal, and its handle is no longer valid. The same holds for granted abilities and running instances.

**What holds on comes back in its first order.** Effect components, ability triggers and running graph nodes subscribe to the entity's events, such as tag and attribute changes. A restore has every one of them let go, newest first, then puts the state back, then has them take hold again, oldest first, in the order each first took hold. Handlers that share an event then run in the same order as before the restore, which a replay depends on. One that subscribed again since it first did, as an effect watching an attribute does when an attribute set is added or removed, comes back where it first took hold. One that throws while letting go or taking hold again doesn't stop the others or the restore, and neither does a mover or [state hook](#state-outside-forge) that throws while putting its state back: the first exception is rethrown after.

**A snapshot never changes**, so it can be restored any number of times.

### What a snapshot leaves out

- **Other entities.** Each entity restores only itself. An effect it applied to another entity is the other entity's, and comes back only with a snapshot of that one. An `Effect` applied to several entities is one object, though: a restore puts its level and set by caller values back for all of them, without their effects evaluating again, so give each entity its own `Effect` when those change at runtime.
- **Handlers your code added.** Subscriptions to an entity's events from outside it, such as a health bar's, aren't the entity's to let go of, so they stay subscribed throughout. The entity's own handlers take hold again after them, so a handler of yours that subscribed after one of the entity's runs before it once the entity is restored.
- **Cues.** A restore fires no cue. A persistent cue an effect started stays shown when the restore takes the effect away, and one that ended stays off when the restore brings its effect back. Ticks replayed after a rollback fire their cues again.
- **Your own state.** Fields of your entity class, engine objects and anything else outside Forge stay as they are, unless a [state hook](#state-outside-forge) captures them.

### When a snapshot can be taken

Take and restore snapshots from the host, between ticks, never from inside an ability, an effect, a graph or a handler:

- While the entity is changing, part way through applying, removing or updating effects, changing an attribute, or activating, ending or removing an ability, both throw `InvalidOperationException`.
- While one of the entity's abilities runs a behavior that isn't a Statescript graph, both throw `NotSupportedException`: a snapshot can't reach what a coded `IAbilityBehavior` keeps. A behavior that ends within its own activation, such as the [Quick Start](quick-start.md)'s heal, never runs between ticks and never gets in the way.
- A snapshot goes back only into the entity it was taken from. Restoring it into another throws `ArgumentException`.

## Entity history

`EntityHistory` keeps an entity's recent ticks, each as a snapshot of how the tick left the entity together with the input frame it ran with. It can roll the entity back to one of them and replay the ticks after it:

```csharp
var history = new EntityHistory(entity, capacity: 64, fixedDeltaTime);

// From the fixed callback, after each tick:
EntitySimulation.Tick(entity, tick, fixedDeltaTime);
history.Record(tick);

// When tick 100 turns out to have gone differently:
history.Rollback(100);
// ...correct the entity as the server says tick 100 left it...
history.Replay(newestTick);
```

- **`Record(tick)`** keeps how the tick left the entity, with its input frame. Recording a tick the history already holds, such as the one after a tick rolled back to, forgets the ticks after it, since they belong to the run it replaces. Recording a tick that doesn't follow the newest one starts the history over.
- **`Rollback(tick)`** restores the snapshot of a tick the history holds. The ticks after it are kept for a replay. The entity's input comes back as that tick left it, so submit the next tick's frame after rolling back and replaying, not before.
- **`Replay(toTick)`** records the tick the entity is at anew, so a correction made after the rollback is kept, then runs each tick after it again, up to `toTick`, through `EntitySimulation.Tick` and with the input frame it first ran with, and records it anew.
- **`OldestTick` and `NewestTick`** say which ticks the history holds. It keeps the most recent `Capacity` of them.

**Only the fixed rail replays.** A replayed tick runs what `EntitySimulation.Tick` runs: input, movement, effects and the abilities' fixed rail. Frame updates and presentation passes aren't replayed, so anything that must come out the same belongs on the fixed rail. That's why the built-in timers run there; see [Two update rails](statescript/nodes/state/README.md#two-update-rails).

## Proving a replay

A replay that comes out differently from the first run is a bug, and the history makes it easy to find. Record a run, then roll back to each tick in turn, replay to the end, and compare the entity with how the first run left it. Forge's own tests do this across stacking, periodic and expiring effects, triggered abilities and graphs that react to each other.

```csharp
string Describe(IForgeEntity entity) =>
    $"{entity.Attributes["Vitals.Health"].CurrentValue} {entity.Tags.AllTags}";

var history = new EntityHistory(entity, ticks + 1, fixedDeltaTime);
history.Record(0);

for (long tick = 1; tick <= ticks; tick++)
{
    EntitySimulation.Tick(entity, tick, fixedDeltaTime);
    history.Record(tick);
}

string end = Describe(entity);

for (long tick = 0; tick < ticks; tick++)
{
    history.Rollback(tick);
    history.Replay(ticks);
    Debug.Assert(Describe(entity) == end, $"The replay from tick {tick} came out differently.");
}
```

## World history

`WorldHistory` keeps the recent ticks of a whole world of entities, so the world can go back to one of them together: every entity as the tick left it, the ones that left the world since brought back, and the ones that joined after it set aside. It keeps an entity history for each entity.

```csharp
var world = new WorldHistory(capacity: 300, fixedDeltaTime);
world.Add(hero);
world.Add(goblin);

// From the fixed callback, after each tick:
EntitySimulation.Tick(world.Entities, tick, fixedDeltaTime);
world.Record(tick);

// To show the world as tick 120 left it:
world.Rollback(120);
```

- **`Entities`** lists the entities in the world, in the order they joined it, without the ones in limbo. Advance them with `EntitySimulation.Tick`, which begins every entity's tick before any of them advances. The list is replaced rather than changed when an entity joins or leaves, so a tick advancing it isn't disturbed: an entity that joins part way through a tick first advances in the next one, and one that leaves part way through still finishes it.
- **`Add(entity)`** puts an entity in the world. It's recorded from the next tick recorded on, and a rollback before then releases it, since it has no tick to come back to.
- **`Remove(entity)`** takes an entity out of the world, as when it dies. It leaves `Entities` at once and goes to [limbo](#limbo) when the next tick is recorded. One that was never recorded has no tick to come back to, so it's released instead.
- **`Record(tick)`** keeps how the tick left every entity in the world, with the input frame each ran with. Every snapshot is taken before anything changes, so a tick that can't be recorded leaves the history as it was.
- **`Rollback(tick)`** puts the world back as the tick left it. The ticks after it are kept, so the world can be rolled to any tick held, back or forward, any number of times, until recording [continues from it](#continuing-after-a-rollback).
- **`OldestTick`, `NewestTick` and `CurrentTick`** say which ticks the history holds and which one the world is at. `SnapshotCount` says how many entity snapshots it holds, entities in limbo included.

**The world rolls back as one.** A rollback has everything every entity holds on to let go, newest first across all of them, puts each entity's state back, then has everything take hold again, oldest first across all of them. Handlers that share an event then run in their first order whichever entity they belong to, which restoring the entities one at a time can't promise: a graph on one entity listening to another entity's tags keeps its place among the other entity's own handlers.

### Limbo

An entity that leaves the world after it was recorded goes to **limbo** instead of away. It lets go of everything it held on to, so nothing it subscribed to reaches it, and nothing advances it. It's kept as it was, so a rollback to a tick it was in brings it back. An entity also waits in limbo while the world is rolled back to before the tick it joined in, and comes back when the world is rolled forward past it again.

`EnteredLimbo` and `LeftLimbo` tell the host when to hide an entity's object and show it again. `EnteredLimbo` comes once the entity has let go, and `LeftLimbo` before the entity is restored, so its object is live again when its state comes back, such as a body back in the physics world for its mover to move. The world lets an entity in limbo go for good, raising `Released`, once no rollback can bring it back, and the host frees its object then:

- **No tick held has it anymore:** the tick it left in is older than the oldest tick the history holds.
- **Its ticks were left behind:** the world was rolled back to before it joined, and recording [continued from there](#continuing-after-a-rollback).

`Forget(entity)` drops an entity for good, without `Released`, such as one whose object is already gone. No rollback brings it back.

### Continuing after a rollback

Recording the tick right after the one the world is at continues the world from there. The ticks after it belong to a future the world left behind, so they go: the entities that joined in them are released, and the ones that left in them are back in the world, for good this time. Rolling back and then recording is how a game resumes from an earlier moment.

A tick that doesn't follow the one the world is at starts the history over: limbo is emptied, and every entity in the world is recorded anew from that tick.

## State outside Forge

A snapshot restores Forge's own state. State a game keeps elsewhere, such as a field of its entity class or of an engine object, can take part through a hook shaped like the [mover contract](simulation.md#movement): `IForgeEntity.StateHook` returns an `IEntityStateHook`, which captures the state as one 128-bit `Variant128` and restores it.

```csharp
public sealed class DoorState : IEntityStateHook
{
    public bool Open { get; set; }

    public Variant128 CaptureState() => new(Open);

    public void RestoreState(in Variant128 state) => Open = state.AsBool();
}
```

An entity snapshot captures the state with the rest of the entity and puts it back after the rest of it, so entity histories and world histories cover it too. Gameplay state is still best kept in attributes, tags, effects and shared variables, which roll back on their own.

## Making your own code rollback-safe

What Forge restores is only as good as what it can see. Code you add to the simulation follows the same rules as the built-in pieces:

- **State nodes** keep their run state in their node context and let go of outside subscriptions across a restore. See [Writing rollback-safe nodes](statescript/nodes/state/README.md#writing-rollback-safe-nodes).
- **Effect components** that keep state or subscriptions implement the [rollback hooks](effects/components/README.md#rollback-hooks).
- **Movers** capture and restore the body through the [mover contract](simulation.md#movement).
- **State outside Forge** that can't move into attributes, tags, effects or shared variables goes through the [state hook](#state-outside-forge).
- **Ability behaviors** that run across ticks are Statescript graphs.
- **Everything a tick does** is [deterministic](simulation.md#deterministic-randomness): it draws from the entity's `Random` and reads the tick's input, never a clock.
