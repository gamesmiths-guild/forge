# Periodic Effects

Periodic Effects in Forge enables [effects](docs/effects/README.md) to execute repeatedly at specified intervals. This is essential for implementing damage-over-time, healing-over-time, resource regeneration, and other recurring gameplay mechanics.

For a practical guide on using periodic effects, see the [Quick Start Guide](quick-start.md).

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
    new DurationData(DurationType.HasDuration, new ScalableFloat(10.0f)), // 10 second duration
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
    new DurationData(DurationType.HasDuration, new ScalableFloat(30.0f)), // 30 second duration
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
    // When uninhibited, continues with original timing - might execute immediately if period elapsed
);

var resetPolicy = new PeriodicData(
    period: new ScalableFloat(5.0f),
    executeOnApplication: false,
    periodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
    // When uninhibited, restarts the period counter
);

var executeAndResetPolicy = new PeriodicData(
    period: new ScalableFloat(5.0f),
    executeOnApplication: false,
    periodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ExecuteAndResetPeriod
    // When uninhibited, executes immediately and restarts the period counter
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
       new DurationData(DurationType.HasDuration, new ScalableFloat(10.0f)),
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
    new DurationData(DurationType.HasDuration, new ScalableFloat(8.0f)),
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
    new DurationData(DurationType.HasDuration, new ScalableFloat(5.0f)),
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
    new DurationData(DurationType.HasDuration, new ScalableFloat(10.0f)),
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
    new DurationData(DurationType.HasDuration, new ScalableFloat(6.0f)),
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
