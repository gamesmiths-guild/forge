# Simulation Tick

A game that runs on a fixed step advances each entity once per step with `EntitySimulation.Tick`. One call runs the entity's effects and fixed-rail abilities in a fixed order and restarts its random source. Every machine that runs the same entity with the same tick numbers and the same step then gets the same result, which networking and replays depend on.

## Advancing an entity

Call `EntitySimulation.Tick` from the fixed callback (a physics step or a network tick), once per entity, and keep the frame rail in the frame callback:

```csharp
// From the fixed callback, with a tick number that counts up by one per step:
EntitySimulation.Tick(entity, tick, fixedDeltaTime);

// From the frame callback:
entity.Abilities.UpdateAbilities(deltaTime);
```

Each tick runs, in order:

1. **The random source restarts.** If the entity has a `Random` (see below), it restarts for this tick.
2. **Effects advance by the step.** Durations count down, periodic effects execute, and expired effects end.
3. **Abilities advance their fixed rail.** Graphs react to the effects as they stand after this tick's expirations. See [Two update rails](statescript/nodes/state/README.md#two-update-rails).

### Entities that affect each other

When one entity's tick can roll on another, for example by applying an effect with a `ChanceToApplyEffectComponent`, every entity's random must restart before any of them advances. Otherwise the roll draws from the target's previous tick. Pass them all in one call:

```csharp
EntitySimulation.Tick(entities, tick, fixedDeltaTime);
```

It restarts every entity's random, then advances each in list order. A host that advances each entity from its own callback does the same in two steps: `EntitySimulation.BeginTick(entity, tick)` for every entity first, then `EntitySimulation.Advance(entity, fixedDeltaTime)` for each.

A game without a fixed step, such as a turn-based one, keeps calling `EffectsManager.UpdateEffects` and `Abilities.UpdateAbilities` from its own loop, as the [Quick Start](quick-start.md#5-drive-time) shows.

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
