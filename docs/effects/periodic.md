# Periodic Effects

Periodic Effects in Forge enables [effects](README.md) to execute repeatedly at specified intervals. This is essential for implementing damage-over-time, healing-over-time, resource regeneration, and other recurring gameplay mechanics.

For a practical guide on using periodic effects, see the [Quick Start Guide](../quick-start.md).

## Core Components

### PeriodicData

`PeriodicData` defines when and how an effect executes on a repeating schedule:

```csharp
public readonly struct PeriodicData(
    ScalableFloat period,
    bool executeOnApplication,
    PeriodInhibitionRemovedPolicy periodInhibitionRemovedPolicy)
{
    public ScalableFloat Period { get; } = period;
    public bool ExecuteOnApplication { get; } = executeOnApplication;
    public PeriodInhibitionRemovedPolicy PeriodInhibitionRemovedPolicy { get; } = periodInhibitionRemovedPolicy;
}
```

### PeriodInhibitionRemovedPolicy

This enum determines what happens when an effect's inhibition is removed (e.g., when a status preventing the effect from ticking is cleared):

```csharp
public enum PeriodInhibitionRemovedPolicy
{
    NeverReset = 0,            // Continue with the original timing
    ResetPeriod = 1,           // Reset the period timer
    ExecuteAndResetPeriod = 2  // Execute immediately and reset the timer
}
```

## How Periodic Effects Work

A periodic effect:

1. Applies to a target entity.
2. Optionally executes immediately (if `ExecuteOnApplication` is true).
3. Waits for the `Period` duration.
4. Executes its effect.
5. Repeats steps 3-4 until the effect ends or is removed.

**Important**: When a periodic effect executes, it directly modifies the `BaseValue` of attributes, similar to instant effects. It does not apply temporary modifiers like duration-based effects. Each execution makes a permanent change to the attribute's base value.

Periodic effects are particularly useful for gameplay mechanics that occur over time, such as:

- Poison or burning damage that ticks every second.
- Healing potions that restore health gradually.
- Mana or stamina regeneration effects.
- Resource generation or consumption over time.
- Area effects that pulse at regular intervals.

## Configuring Periodic Effects

### Basic Configuration

To create a periodic effect, specify a period duration, whether it executes on application, and how it handles inhibition removal:

```csharp
// Create a poison effect that deals damage every 2 seconds
var poisonEffectData = new EffectData(
    "Poison",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(10.0f) // 10 second duration
        )
    ),
    new[] {
        new Modifier("CombatAttributeSet.CurrentHealth", ModifierOperation.Add, new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-5)))
    },
    periodicData: new PeriodicData(
        period: new ScalableFloat(2.0f),                           // Execute every 2 seconds
        executeOnApplication: true,                                // Apply damage immediately
        periodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
    )
);
```

### Scaling with Level

The `Period` property uses `ScalableFloat`, allowing periods to change based on effect level:

```csharp
// Create a healing effect with frequency that scales with level
var healingEffectData = new EffectData(
    "Healing Aura",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(30.0f) // 30 second duration
        )
    ),
    new[] {
        new Modifier("CombatAttributeSet.CurrentHealth", ModifierOperation.Add, new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10)))
    },
    periodicData: new PeriodicData(
        // Period decreases as level increases (faster ticks at higher levels)
        period: new ScalableFloat(2.0f, new Curve([
            new CurveKey(1, 1.0f),   // Level 1: 2.0 seconds
            new CurveKey(5, 0.75f),  // Level 5: 1.5 seconds
            new CurveKey(10, 0.5f)   // Level 10: 1.0 second
        ])),
        executeOnApplication: true,
        periodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
    )
);
```

### Handling Inhibition

The `PeriodInhibitionRemovedPolicy` controls what happens when an inhibited effect becomes active again:

```csharp
// Different inhibition handling policies
var neverResetPolicy = new PeriodicData(
    period: new ScalableFloat(5.0f),
    executeOnApplication: false,
    periodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.NeverReset
    // When re-enabled, continues with original timing - might execute immediately if period elapsed
);

var resetPolicy = new PeriodicData(
    period: new ScalableFloat(5.0f),
    executeOnApplication: false,
    periodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
    // When re-enabled, restarts the period counter
);

var executeAndResetPolicy = new PeriodicData(
    period: new ScalableFloat(5.0f),
    executeOnApplication: false,
    periodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ExecuteAndResetPeriod
    // When re-enabled, executes immediately and restarts the period counter
);
```

## Constraints and Interactions

Periodic effects have specific constraints and interactions with other systems:

### Duration Constraints

1. **No Instant Periodic Effects**: Periodic effects cannot have `DurationType.Instant`. See the [Duration documentation](duration.md) for more details.
   ```csharp
   // INVALID - Periodic effects can't be instant
   new EffectData(
       "Invalid Effect",
       new DurationData(DurationType.Instant), // Error with periodic data
       [/*...*/],
       periodicData: new PeriodicData(new ScalableFloat(1.0f))
   );
   ```

2. **Must be either `HasDuration` or `Infinite`**: Periodic effects must have a duration type of either `HasDuration` or `Infinite`.
   ```csharp
   // VALID - HasDuration periodic effect
   new EffectData(
       "Valid Effect",
       new DurationData(
           DurationType.HasDuration,
           new ModifierMagnitude(
               MagnitudeCalculationType.ScalableFloat,
               new ScalableFloat(10.0f))
       ),
       [/*...*/],
       periodicData: new PeriodicData(new ScalableFloat(1.0f))
   );

   // VALID - Infinite periodic effect
   new EffectData(
       "Valid Effect",
       new DurationData(DurationType.Infinite),
       [/*...*/],
       periodicData: new PeriodicData(new ScalableFloat(1.0f))
   );
   ```

### Stacking Interactions

When combining periodic effects with [stacking](stacking.md):

1. **Stack and Period Synchronization**: If an effect has both stacking and periodic data:

   - `ExecuteOnSuccessfulApplication` must be defined in the stacking data.
   - `ApplicationResetPeriodPolicy` must be defined in the stacking data.

```csharp
// VALID - Stacking periodic effect
var stackingPeriodicEffect = new EffectData(
    "Bleeding",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(8.0f))
    ),
    [/*...*/],
    stackingData: new StackingData(
        stackLimit: new ScalableInt(3),
        initialStack: new ScalableInt(1),
        executeOnSuccessfulApplication: true,
        // ... other stacking data
        // Required for periodic effects with stacking
        applicationResetPeriodPolicy: StackApplicationResetPeriodPolicy.ResetOnSuccessfulApplication,
        applicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication
    ),
    periodicData: new PeriodicData(
        period: new ScalableFloat(2.0f),
        executeOnApplication: true,
        periodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
    )
);
```

## Common Use Cases

### Damage Over Time (DoT)

```csharp
// Burning effect that deals damage every second for 5 seconds
var burningEffectData = new EffectData(
    "Burning",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(5.0f))
    ),
    new[] {
        new Modifier("CombatAttributeSet.CurrentHealth", ModifierOperation.Add, new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-8)))
    },
    periodicData: new PeriodicData(
        period: new ScalableFloat(1.0f),
        executeOnApplication: true, // Apply first tick immediately
        periodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
    )
);
```

### Healing Over Time (HoT)

```csharp
// Regeneration effect that heals every 2 seconds for 10 seconds
var regenerationEffectData = new EffectData(
    "Regeneration",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(10.0f))
    ),
    new[] {
        new Modifier("CombatAttributeSet.CurrentHealth", ModifierOperation.Add, new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(15)))
    },
    periodicData: new PeriodicData(
        period: new ScalableFloat(2.0f),
        executeOnApplication: false, // First heal happens after 2 seconds
        periodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
    )
);
```

### Resource Generation

```csharp
// Mana regeneration aura that restores mana every 3 seconds indefinitely
var manaRegenEffectData = new EffectData(
    "Mana Aura",
    new DurationData(DurationType.Infinite),
    new[] {
        new Modifier("ResourceAttributeSet.CurrentMana", ModifierOperation.Add, new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(5)))
    },
    periodicData: new PeriodicData(
        period: new ScalableFloat(3.0f),
        executeOnApplication: false,
        periodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
    )
);
```

### Stacking Periodic Effect

```csharp
// Bleeding effect that stacks up to 3 times, each stack does damage every second
var bleedingEffectData = new EffectData(
    "Bleeding",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(6.0f))
    ),
    new[] {
        new Modifier("CombatAttributeSet.CurrentHealth", ModifierOperation.Add, new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-3)))
    },
    new StackingData(
        stackLimit: new ScalableInt(3),
        initialStack: new ScalableInt(1),
        stackPolicy: StackPolicy.AggregateBySource,
        stackLevelPolicy: StackLevelPolicy.AggregateLevels,
        magnitudePolicy: StackMagnitudePolicy.Sum, // Each stack adds to the damage
        overflowPolicy: StackOverflowPolicy.AllowApplication,
        expirationPolicy: StackExpirationPolicy.ClearEntireStack,
        executeOnSuccessfulApplication: true,
        applicationResetPeriodPolicy: StackApplicationResetPeriodPolicy.ResetOnSuccessfulApplication,
        applicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication
    ),
    new PeriodicData(
        period: new ScalableFloat(1.0f),
        executeOnApplication: true,
        periodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
    )
);
```

## Best Practices

1. **Choose Appropriate Period**: Consider gameplay pace when setting periods.
   - For fast-paced games, shorter periods feel more responsive.
   - For slower games, longer periods reduce system overhead.

2. **Consider Execute on Application**: Decide whether effects should "tick" immediately.
   - Damage effects often execute immediately for responsive gameplay.
   - Beneficial effects might delay the first tick for balance.

3. **Handle Inhibition Appropriately**:
   - `NeverReset`: Use when timing consistency is important.
   - `ResetPeriod`: Use for most standard periodic effects.
   - `ExecuteAndResetPeriod`: Use when missing ticks would be problematic for gameplay.

4. **Coordinate with Duration**:
   - Ensure total duration is appropriate for the number of expected ticks.
   - For a 10-second effect with a 2-second period, expect around 5-6 ticks.

5. **Balance Performance**:
   - Very short periods (<0.1s) can impact performance with many active effects.
   - Consider consolidating similar periodic effects when possible.

6. **Handle Edge Cases**:
   - Consider what happens if an entity becomes immune during a periodic effect.
   - Test inhibition and removal of effects thoroughly.

7. **Synchronize with Turn-Based Gameplay**:
   - In turn-based games, call `entity.EffectsManager.UpdateEffects(1.0f)` at the end of each turn.
   - Set period to exactly `1.0f` for effects that should trigger once per turn.
   - Use values like `2.0f` or `3.0f` for effects that trigger every few turns.
```

````markdown name=docs/effects/stacking.md url=https://github.com/gamesmiths-guild/forge/blob/37b442b3f13986dcda70ed54c1d2c93e69e01c83/docs/effects/stacking.md
# Effect Stacking

Effect Stacking in Forge enables [effects](README.md) to accumulate on a target entity, allowing gameplay mechanics like poison stacks, buff/debuff stacks, or other cumulative effects. This powerful system offers extensive control over how effects combine, interact, and expire.

For a practical guide on using stacking, see the [Quick Start Guide](../quick-start.md).

## Core Components

### StackingData

`StackingData` defines how an effect behaves when multiple instances are applied to the same target:

```csharp
public readonly struct StackingData(
    ScalableInt stackLimit,
    ScalableInt initialStack,
    StackPolicy stackPolicy,
    StackLevelPolicy stackLevelPolicy,
    StackMagnitudePolicy magnitudePolicy,
    StackOverflowPolicy overflowPolicy,
    StackExpirationPolicy expirationPolicy,
    StackOwnerDenialPolicy? ownerDenialPolicy = null,
    StackOwnerOverridePolicy? ownerOverridePolicy = null,
    StackOwnerOverrideStackCountPolicy? ownerOverrideStackCountPolicy = null,
    LevelComparison? levelDenialPolicy = null,
    LevelComparison? levelOverridePolicy = null,
    StackLevelOverrideStackCountPolicy? levelOverrideStackCountPolicy = null,
    StackApplicationRefreshPolicy? applicationRefreshPolicy = null,
    StackApplicationResetPeriodPolicy? applicationResetPeriodPolicy = null,
    bool? executeOnSuccessfulApplication = null)
{
    // Properties to access each parameter...
}
```

## Basic Stacking Parameters

### Stack Limits and Counts

- **StackLimit**: Maximum number of stacks that can be applied to a target.
  ```csharp
  public ScalableInt StackLimit { get; }
  ```

- **InitialStack**: Number of stacks applied when the effect is first applied.
  ```csharp
  public ScalableInt InitialStack { get; }
  ```

- **ExecuteOnSuccessfulApplication**: For [periodic effects](periodic.md), determines whether the periodic effect executes when a new stack is applied.
  ```csharp
  public bool? ExecuteOnSuccessfulApplication { get; }
  ```

### Overflow Policy

The `StackOverflowPolicy` controls what happens when a new stack application would exceed the stack limit:

```csharp
public enum StackOverflowPolicy : byte
{
    AllowApplication = 0, // Apply the effect but maintain the stack limit
    DenyApplication = 1   // Reject the application entirely
}
```

An "overflow" occurs when an effect has reached its maximum stack count (defined by `StackLimit`) and a new application attempts to add more stacks. The overflow policy determines how this situation is handled:

- With `AllowApplication`, the new application is processed (refreshing duration, triggering events, etc.) but the stack count remains at the limit.
- With `DenyApplication`, the new application is completely rejected as if it never happened.

## Key Stacking Policies

### Stack Aggregation

The `StackPolicy` determines how stacks are aggregated on a target:

```csharp
public enum StackPolicy : byte
{
    AggregateBySource = 0, // Each source has its own stack on the target
    AggregateByTarget = 1  // Target has only one stack, shared by all sources
}
```

### Stack Level Handling

The `StackLevelPolicy` defines how effects of different levels interact:

```csharp
public enum StackLevelPolicy : byte
{
    AggregateLevels = 0, // Combine effects of different levels
    SegregateLevels = 1  // Keep effects of different levels separate
}
```

### Magnitude Policy

The `StackMagnitudePolicy` controls how effect [magnitudes](modifiers.md) are calculated when stacked:

```csharp
public enum StackMagnitudePolicy : byte
{
    DontStack = 0, // Each stack uses its original magnitude
    Sum = 1        // Sum the magnitudes of all stacks
}
```

### Expiration Policy

The `StackExpirationPolicy` determines what happens when an effect's [duration](duration.md) ends:

```csharp
public enum StackExpirationPolicy : byte
{
    ClearEntireStack = 0,                   // Remove all stacks at once
    RemoveSingleStackAndRefreshDuration = 1 // Remove one stack, refresh duration
}
```

### Owner Control Policies

When using `StackPolicy.AggregateByTarget`, these policies control how different owners' effects interact:

- **OwnerDenialPolicy**: Controls whether different owners can apply stacks.
  ```csharp
  public enum StackOwnerDenialPolicy : byte
  {
      AlwaysAllow = 0,    // Any source can add stacks
      DenyIfDifferent = 1 // Only the original source can add stacks
  }
  ```

- **OwnerOverridePolicy**: Controls whether effect ownership changes.
  ```csharp
  public enum StackOwnerOverridePolicy : byte
  {
      KeepCurrent = 0, // Original owner is always kept
      Override = 1     // New applications change ownership
  }
  ```

- **OwnerOverrideStackCountPolicy**: Controls stack behavior when ownership changes.
  ```csharp
  public enum StackOwnerOverrideStackCountPolicy : byte
  {
      IncreaseStacks = 0, // Add to existing stack count
      ResetStacks = 1     // Reset stack count to initial value
  }
  ```

### Application Policies

- **ApplicationRefreshPolicy**: Controls how duration is handled when applying new stacks.
  ```csharp
  public enum StackApplicationRefreshPolicy : byte
  {
      RefreshOnSuccessfulApplication = 0, // Reset the duration when a stack is applied
      NeverRefresh = 1                    // Keep the current duration
  }
  ```

- **ApplicationResetPeriodPolicy**: For periodic effects, controls how the period timer is handled when a new stack is applied.
  ```csharp
  public enum StackApplicationResetPeriodPolicy : byte
  {
      ResetOnSuccessfulApplication = 0, // Reset period timer when a stack is applied
      NeverReset = 1                    // Keep the current period timer
  }
  ```

## Advanced Stacking Control

### Level Comparison

`LevelComparison` is a flags enum used to compare effect levels:

```csharp
[Flags]
public enum LevelComparison : byte
{
    None = 0,
    Equal = 1 << 0,   // 1
    Higher = 1 << 1,  // 2
    Lower = 1 << 2    // 4
}
```

| Flag Combination           | Value | Description                                  |
|----------------------------|-------|----------------------------------------------|
| None                       | 0     | No comparison, ignores all levels            |
| Equal                      | 1     | Only matches equal levels                    |
| Higher                     | 2     | Only matches higher levels                   |
| Lower                      | 4     | Only matches lower levels                    |
| Equal \| Higher            | 3     | Matches equal or higher levels               |
| Equal \| Lower             | 5     | Matches equal or lower levels                |
| Higher \| Lower            | 6     | Matches higher or lower levels (not equal)   |
| Equal \| Higher \| Lower   | 7     | Matches all levels (rarely useful)           |

When used for:

- **LevelDenialPolicy**: Denies application if the level relationship matches.
- **LevelOverridePolicy**: Overrides existing stack if the level relationship matches.

### Level Override Stack Count Policy

When a level override occurs, this policy controls what happens to the stack count:

```csharp
public enum StackLevelOverrideStackCountPolicy : byte
{
    IncreaseStacks = 0, // Add to existing stack count
    ResetStacks = 1     // Reset stack count to initial value
}
```

## Configuring Stacking Effects

### Basic Stacking Effect

```csharp
// Simple poison effect that stacks up to 5 times, each stack adds to the damage
var poisonEffectData = new EffectData(
    "Poison",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(10.0f)
        )
    ),
    new[] {
        new Modifier("CombatAttributeSet.CurrentHealth", ModifierOperation.Add, new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-5)))
    },
    new StackingData(
        stackLimit: new ScalableInt(5),
        initialStack: new ScalableInt(1),
        stackPolicy: StackPolicy.AggregateBySource,
        stackLevelPolicy: StackLevelPolicy.SegregateLevels,
        magnitudePolicy: StackMagnitudePolicy.Sum,
        overflowPolicy: StackOverflowPolicy.DenyApplication,
        expirationPolicy: StackExpirationPolicy.ClearEntireStack,
        applicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication
    )
);
```

### Advanced Stacking with Level Control

```csharp
// Buff that allows higher level applications to override lower ones
var hierarchicalBuffEffect = new EffectData(
    "Strength Buff",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(30.0f))
    ),
    new[] {
        new Modifier("CombatAttributeSet.AttackPower", ModifierOperation.Add, new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10)))
    },
    new StackingData(
        stackLimit: new ScalableInt(3),
        initialStack: new ScalableInt(1),
        stackPolicy: StackPolicy.AggregateByTarget,
        stackLevelPolicy: StackLevelPolicy.AggregateLevels,
        magnitudePolicy: StackMagnitudePolicy.Sum,
        overflowPolicy: StackOverflowPolicy.DenyApplication,
        expirationPolicy: StackExpirationPolicy.RemoveSingleStackAndRefreshDuration,
        // Control how different owners interact
        ownerDenialPolicy: StackOwnerDenialPolicy.AlwaysAllow,
        ownerOverridePolicy: StackOwnerOverridePolicy.Override,
        ownerOverrideStackCountPolicy: StackOwnerOverrideStackCountPolicy.IncreaseStacks,
        // Control how different levels interact
        levelDenialPolicy: LevelComparison.None,
        levelOverridePolicy: LevelComparison.Higher,
        levelOverrideStackCountPolicy: StackLevelOverrideStackCountPolicy.ResetStacks,
        applicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication
    )
);
```

### Stacking with Periodic Effect

```csharp
// Bleeding effect that ticks every 2 seconds and stacks up to 3 times
var bleedingEffectData = new EffectData(
    "Bleeding",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(8.0f))
    ),
    new[] {
        new Modifier("CombatAttributeSet.CurrentHealth", ModifierOperation.Add, new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-3)))
    },
    new StackingData(
        stackLimit: new ScalableInt(3),
        initialStack: new ScalableInt(1),
        stackPolicy: StackPolicy.AggregateBySource,
        stackLevelPolicy: StackLevelPolicy.SegregateLevels,
        magnitudePolicy: StackMagnitudePolicy.Sum,
        overflowPolicy: StackOverflowPolicy.AllowApplication,
        expirationPolicy: StackExpirationPolicy.ClearEntireStack,
        applicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication,
        // Required for periodic effects
        applicationResetPeriodPolicy: StackApplicationResetPeriodPolicy.ResetOnSuccessfulApplication,
        executeOnSuccessfulApplication: true
    ),
    new PeriodicData(
        period: new ScalableFloat(2.0f),
        executeOnApplication: true,
        periodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
    )
);
```

## Constraints and Relationships

Stacking effects have several constraints and required relationships:

1. **No Instant Stacking**: Stacks cannot be used with `DurationType.Instant`.
   ```csharp
   // INVALID - Instant effects can't stack
   new EffectData(
       "Invalid Effect",
       new DurationData(DurationType.Instant), // Error with stacking data
       [/*...*/],
       new StackingData(/*...*/)
   );
   ```

2. **Stack Limit and Initial Stack**: The initial stack count must be greater than 0 and less than or equal to the stack limit.
   ```csharp
   // VALID - Initial stack and limit relationship
   new StackingData(
       stackLimit: new ScalableInt(5),
       initialStack: new ScalableInt(1)
       // ...
   );
   ```

3. **`ApplicationRefreshPolicy` Required**: For `HasDuration` effects with stacking.
   ```csharp
   // VALID - HasDuration requires ApplicationRefreshPolicy
   new StackingData(
       // ...
       applicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication
   );
   ```

4. **Periodic Integration**: Stacking effects with `PeriodicData` must define:
   - `ExecuteOnSuccessfulApplication`
   - `ApplicationResetPeriodPolicy`

5. **`AggregateByTarget` Requirements**:
   - Must define `OwnerDenialPolicy`.
   - If `OwnerDenialPolicy` is `AlwaysAllow`, must define `OwnerOverridePolicy`.
   - If `OwnerOverridePolicy` is `Override`, must define `OwnerOverrideStackCountPolicy`.

6. **`AggregateLevels` Requirements**:
   - Must define `LevelDenialPolicy`.
   - Must define `LevelOverridePolicy`.
   - If `LevelOverridePolicy` is not `None`, must define `LevelOverrideStackCountPolicy`.
   - `LevelDenialPolicy` and `LevelOverridePolicy` cannot have overlapping flags.

## Best Practices

1. **Use Clear Stack Limits**:
   - Choose appropriate stack limits based on your game's balance.
   - Consider using `ScalableInt` for level-based stack limits.

2. **Choose Magnitude Policy Carefully**:
   - `Sum`: Good for additive effects (damage, stat bonuses).
   - `DontStack`: Good for status effects where you want duration benefits of stacking but not increased magnitude.

3. **Consider Stack Expiration**:
   - `ClearEntireStack`: Simple but can feel abrupt to players.
   - `RemoveSingleStackAndRefreshDuration`: More gradual, better player experience.

4. **Level Control Strategies**:
   - Use `SegregateLevels` for simpler systems.
   - Use `AggregateLevels` with careful level policies for more complex behaviors.

5. **Owner Control**:
   - `AggregateBySource`: Simpler, each source gets its own stack.
   - `AggregateByTarget`: More complex, but prevents stacking abuse.

6. **Create Unique Effects**:
   - Use `StackPolicy.AggregateByTarget` with `StackLimit` of 1 to ensure only one instance of an effect exists on a target.
   - Control replacement behavior with `OwnerDenialPolicy` and `LevelDenialPolicy`.
   - Use `LevelOverridePolicy` to allow higher-level versions to replace lower ones.

7. **Test Edge Cases**:
   - Stack limit behavior.
   - Stack expiration and duration refresh.
   - Interactions with inhibitions.
   - Effects from multiple owners and levels.

8. **Document Your Stacking Rules**:
   - Clearly explain to players how stacks work for key abilities.
   - Use UI to communicate current stack counts.
