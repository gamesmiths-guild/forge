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
            scalableFloatMagnitude: new ScalableFloat(10.0f))),
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
            scalableFloatMagnitude: new ScalableFloat(30.0f))),
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
            scalableFloatMagnitude: new ScalableFloat(8.0f))),
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

7. **Duration Magnitude**: `DurationData` uses `ModifierMagnitude` (ScalableFloat, AttributeBased, CustomCalculatorClass, SetByCaller). For non-snapshot attribute captures or `SetByCaller` values, durations are re-evaluated at runtime. Stack refresh/reset behaviors (e.g., `ApplicationRefreshPolicy` or `RemoveSingleStackAndRefreshDuration`) use the current evaluated duration when they apply.
