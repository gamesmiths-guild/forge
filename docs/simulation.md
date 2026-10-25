# Simulation Tick

A game that runs on a fixed step advances each entity once per step with `EntitySimulation.Tick`. One call runs the entity's input, movement, effects and fixed-rail abilities in a fixed order and restarts its random source. Every machine that runs the same entity with the same tick numbers and the same step then gets the same result, which networking and replays depend on.

## Advancing an entity

Call `EntitySimulation.Tick` from the fixed callback (a physics step or a network tick), once per entity, and keep the frame rail in the frame callback:

```csharp
// From the fixed callback, with a tick number that counts up by one per step:
EntitySimulation.Tick(entity, tick, fixedDeltaTime);

// From the frame callback:
entity.Abilities.UpdateAbilities(deltaTime);
```

Each tick runs, in order:

1. **The tick begins.** If the entity has a `Random` (see below), it restarts for this tick. If it has an `Input`, the frame submitted for this tick becomes current.
2. **The input stage runs.** Abilities bound to an input action that was pressed or released try to activate. See [Input](input.md).
3. **The movement stage runs.** If the entity has a `Mover`, it moves the body by the step. See [Movement](#movement).
4. **Effects advance by the step.** Durations count down, periodic effects execute, and expired effects end.
5. **Abilities advance their fixed rail.** Graphs react to the effects as they stand after this tick's expirations. See [Two update rails](statescript/nodes/state/README.md#two-update-rails).

### Entities that affect each other

When one entity's tick can reach another, for example by applying an effect with a `ChanceToApplyEffectComponent` or by a graph reading the other's input, every entity must begin the tick before any of them advances. Otherwise the roll draws from the target's previous tick, and the graph sees the other entity's previous input frame. Pass them all in one call:

```csharp
EntitySimulation.Tick(entities, tick, fixedDeltaTime);
```

It begins every entity's tick, then advances each in list order. A host that advances each entity from its own callback does the same in two steps: `EntitySimulation.BeginTick(entity, tick)` for every entity first, then `EntitySimulation.Advance(entity, fixedDeltaTime)` for each.

A game without a fixed step, such as a turn-based one, keeps calling `EffectsManager.UpdateEffects` and `Abilities.UpdateAbilities` from its own loop, as the [Quick Start](quick-start.md#5-drive-time) shows.

## Movement

An entity whose body the simulation moves provides an `IEntityMover` through `IForgeEntity.Mover`. It's a default member that returns `null`, so existing entities are unaffected. The interface is the mover contract:

| Member | Does |
|---|---|
| `Move(fixedDeltaTime)` | Moves the body by one step. The movement stage calls it once per tick. |
| `CaptureState()` | Returns the body's `BodyState`: its position, rotation and velocity, and whether it stands on the ground. |
| `RestoreState(state)` | Puts the body back in a state `CaptureState` returned. |

The mover reads what drives it, such as the entity's input, attributes and tags, inside `Move`. The movement stage runs after the input stage and before effects advance:

- **An ability the tick's input activated** has already applied its tags, so a dash that blocks walking blocks it from its first tick.
- **An effect that ends this tick** still applies to this tick's step. An effect lasting a number of ticks shapes exactly that many steps.
- **Graphs advance after the mover**, so a graph that moves the body itself, such as a dash, moves it after the mover's step.

`CaptureState` and `RestoreState` let the body go back to an earlier tick and simulate it again: restoring a captured state and moving with the same input gives the same result. A 2D body uses the X and Y components and turns about the Z axis.

```csharp
// A body without an engine behind it, walking on the ground plane.
public class PointMover(IForgeEntity entity) : IEntityMover
{
    private BodyState _body = new(Vector3.Zero, Quaternion.Identity, Vector3.Zero, Grounded: true);

    public void Move(double fixedDeltaTime)
    {
        Vector2 move = entity.Input?.GetVector("move_left", "move_right", "move_forward", "move_back")
            ?? Vector2.Zero;
        var velocity = new Vector3(move.X, 0, move.Y) * entity.Attributes["Movement.Speed"].CurrentValue;

        _body = _body with { Position = _body.Position + (velocity * (float)fixedDeltaTime), Velocity = velocity };
    }

    public BodyState CaptureState() => _body;

    public void RestoreState(in BodyState state) => _body = state;
}
```

## Presentation pass

Simulation decides what happens; presentation shows it. A game that shows anything ends each frame with a presentation pass, after every tick and frame update of that frame:

```csharp
// At the end of the frame, once per entity:
entity.Abilities.PresentAbilities();

// And once per frame for the cues, when dispatch is deferred:
cuesManager.PresentCues();
```

The pass shows the net change since the previous one. An animation node that started and ended within the frame plays nothing, and one still running is started once, however many ticks it lasts. Nothing the pass runs may change gameplay, so a host with nothing to show, such as a dedicated server, skips it and still simulates the same game. See [Simulation and presentation](statescript/nodes/state/README.md#simulation-and-presentation) for the node side and [Deferred dispatch](cues.md#deferred-dispatch) for cues.

## Deterministic randomness

`DeterministicRandom` is an `IRandom` whose numbers depend only on its seed and on the tick it was last started for:

```csharp
var random = new DeterministicRandom(seed);

random.BeginTick(42);
int roll = random.NextInt(1, 7); // The same roll on every machine, for this seed and tick.
```

- **Per-tick restarts.** `BeginTick` restarts the sequence from the seed and the tick. A tick's rolls never depend on how many numbers earlier ticks drew, so a roll one machine made and another didn't can't shift the rolls after it.
- **The generator.** It's xoshiro256**, seeded through SplitMix64, with integer arithmetic only. The sequence is identical on every platform.

An entity provides one through `IForgeEntity.Random`. It's a default member that returns `null`, so existing entities are unaffected:

```csharp
public class Hero : IForgeEntity
{
    // ...the other IForgeEntity members...

    public DeterministicRandom Random { get; } = new(seed: 1234);
}
```

`EntitySimulation.Tick` restarts it every tick. These draw from it instead of the provider they were built with:

| Consumer | Draws from |
| --- | --- |
| Ability graphs | `GraphContext.Random`, which `GraphAbilityBehavior` sets to the owner's `Random` when the graph starts. |
| `RandomBranchNode` and the random resolvers | `GraphContext.Random`, when set. |
| `ChanceToApplyEffectComponent` | The target's `Random`, when it has one. |

Without one, each falls back to the provider it was constructed with. For a standalone `GraphProcessor`, set `GraphContext.Random` yourself.

Give an entity the same seed on every machine that simulates it.

## Deterministic order

Updates follow orders that depend only on what was built and granted, never on hash codes or on the history of a set:

- **Graphs:** a graph walks its active state nodes by `Node.Index`. The entry node is 0, and the others count up in the order they were added to the graph. Nodes that activated in a different order still update in graph order.
- **Abilities:** an entity updates its abilities in the order they were granted. `EntityAbilities.GrantedAbilities` lists them in that order too, and a removal never lets a later grant take an earlier one's place.
- **Effects:** effects update in the order they were applied.

## Timing granularity

Durations and periods count down by the fixed step, so a duration that isn't a whole number of steps ends on the first tick that reaches it.

Durations authored as `float` are converted to `double`. A `float` 1/60 s is slightly longer than the `double` 1/60 s step it's counted down by, so it ends one tick later than you might expect. The result is the same on every machine. When exact tick boundaries matter, author durations as whole numbers of steps, or use a step that's a power of two, such as 1/64 s.
