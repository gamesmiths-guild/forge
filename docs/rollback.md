# Rollback

A game that runs on a fixed step can put an entity back as it was after an earlier tick and run the ticks since then again. Networking does it when the server's word on a past tick differs from what a client predicted. A test does it to prove the game plays the same way twice. Two pieces do the work: the **entity snapshot**, which captures an entity and puts it back, and the **entity history**, which keeps the recent ticks to roll back to.

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
| Attributes | Base, current, minimum and maximum values, with the modifiers in each channel exactly as they were rather than added up again. |
| Tags | The base tags, and how many effects grant each modifier tag. |
| Effects | Each active effect with its level, stacks, remaining duration, period timing, inhibition, set-by-caller values, evaluated magnitudes and the state of its [components](effects/components/README.md#rollback-hooks). |
| Abilities | The granted abilities with their grant sources, levels, inhibition and cooldowns, the blocked ability tags, and each running instance with its Statescript graph. |
| Shared variables | Their values. |
| Input | The current frame and the one waiting for the next tick, if the entity has an `Input`. |
| Random | Where the generator is, if the entity has a `Random`. |
| Body | The `BodyState` its [mover](simulation.md#movement) captures, if the entity has a `Mover`. |

**A restore is silent.** No attribute, tag, effect or ability event is raised, no cue fires, and no graph node activates or deactivates. The [presentation pass](simulation.md#presentation-pass) after it shows only the net change in what the graphs present.

**Effects and abilities keep their identity.** Active effects, abilities and ability instances come back as the same objects, so a handle from before the snapshot points at the same thing after the restore. An effect removed since the snapshot comes back, and its handle is valid again. An effect applied since then goes without a removal, and its handle is no longer valid. The same holds for granted abilities and running instances.

**What holds on comes back in its first order.** Effect components, ability triggers and running graph nodes subscribe to the entity's events, such as tag and attribute changes. A restore has every one of them let go, newest first, then puts the state back, then has them take hold again, oldest first, in the order each first took hold. Handlers that share an event then run in the same order as before the restore, which a replay depends on. A hook that throws doesn't stop the others or the restore, and the first exception is rethrown after.

**A snapshot never changes**, so it can be restored any number of times.

### What a snapshot leaves out

- **Other entities.** Each entity restores only itself. An effect it applied to another entity is the other entity's, and comes back only with a snapshot of that one.
- **Handlers your code added.** Subscriptions to an entity's events from outside it, such as a health bar's, aren't the entity's to let go of, so they stay subscribed throughout. The entity's own handlers take hold again after them, so a handler of yours that subscribed after one of the entity's runs before it once the entity is restored.
- **Cues.** A restore fires no cue. A persistent cue an effect started stays shown when the restore takes the effect away, and one that ended stays off when the restore brings its effect back. Ticks replayed after a rollback fire their cues again.
- **Your own state.** Fields of your entity class, engine objects and anything else outside Forge stay as they are.

### When a snapshot can be taken

Take and restore snapshots from the host, between ticks, never from inside an ability, an effect, a graph or a handler:

- While the entity is changing, part way through applying an effect, changing an attribute or activating an ability, both throw `InvalidOperationException`.
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
- **`Rollback(tick)`** restores the snapshot of a tick the history holds. The ticks after it are kept for a replay.
- **`Replay(toTick)`** runs each tick after the one the entity is at again, up to `toTick`, through `EntitySimulation.Tick` and with the input frame it first ran with, and records it anew.
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

## Making your own code rollback-safe

What Forge restores is only as good as what it can see. Code you add to the simulation follows the same rules as the built-in pieces:

- **State nodes** keep their run state in their node context and let go of outside subscriptions across a restore. See [Writing rollback-safe nodes](statescript/nodes/state/README.md#writing-rollback-safe-nodes).
- **Effect components** that keep state or subscriptions implement the [rollback hooks](effects/components/README.md#rollback-hooks).
- **Movers** capture and restore the body through the [mover contract](simulation.md#movement).
- **Ability behaviors** that run across ticks are Statescript graphs.
- **Everything a tick does** is [deterministic](simulation.md#deterministic-randomness): it draws from the entity's `Random` and reads the tick's input, never a clock.
