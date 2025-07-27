# Effect Duration

Effect Duration in Forge controls how long [effects](README.md) remain active on target entities. It provides three distinct duration types that determine an effect's lifecycle and behavior.

For a practical guide on using durations, see the [Quick Start Guide](../quick-start.md).

## Core Components

### DurationType

The `DurationType` enum defines how an effect persists over time:

```csharp
public enum DurationType : byte
{
    Instant = 0,
    Infinite = 1,
    HasDuration = 2
}
```

### DurationData

`DurationData` encapsulates the duration configuration for an effect:

```csharp
public readonly struct DurationData(DurationType durationType, ScalableFloat? duration = null)
{
    public DurationType Type { get; } = durationType;
    public ScalableFloat? Duration { get; } = duration;
}
```

## Duration Types

### Instant

Instant effects apply their changes immediately and then end. They:

- Execute once at the moment of application.
- Don't remain active in the effects manager.
- Modify base attribute values directly.
- Don't return an `ActiveEffectHandle` when applied.

```csharp
// Create an instant damage effect
var damageEffectData = new EffectData(
    "Direct Damage",
    new[] {
        new Modifier("CombatAttributeSet.CurrentHealth", ModifierOperation.Add, new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-25)))
    },
    new DurationData(DurationType.Instant)
);
```

### Infinite

Infinite effects have no built-in expiration time and remain active until manually removed. They:

- Apply their modifiers continuously.
- Remain active indefinitely.
- Must be explicitly removed using `EffectsManager.UnapplyEffect`.
- Are useful for permanent buffs, persistent status effects, and equipment bonuses.

Equipment-based buffs are a perfect use case for Infinite effects:
```csharp
// Create an equipment buff that lasts until the item is unequipped
var swordBuffEffectData = new EffectData(
    "Magic Sword Bonus",
    new[] {
        new Modifier("CombatAttributeSet.AttackPower", ModifierOperation.Add, new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10))),
        new Modifier("CombatAttributeSet.CriticalChance", ModifierOperation.PercentBonus, new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(0.05f)))
    },
    new DurationData(DurationType.Infinite)
);

var swordBuffEffect = new Effect(swordBuffEffectData, new EffectOwnership(character, character));

// When equipping the item
ActiveEffectHandle? equipmentBuffHandle = character.EffectsManager.ApplyEffect(swordBuffEffect);

// When unequipping the item
if (equipmentBuffHandle is not null)
{
    character.EffectsManager.UnapplyEffect(equipmentBuffHandle);
}
```

### HasDuration

Duration-based effects automatically expire after a specific amount of time. They:

- Apply their modifiers for a limited period.
- Automatically remove themselves when their duration ends.
- Can use `ScalableFloat` for level-based duration scaling.
- Are often used for temporary buffs and debuffs.

```csharp
// Create a temporary buff that lasts 30 seconds
var temporaryBuffEffectData = new EffectData(
    "Temporary Speed Boost",
    new[] {
        new Modifier("MovementAttributeSet.Speed", ModifierOperation.PercentBonus, new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(0.3f)))
    },
    new DurationData(DurationType.HasDuration, new ScalableFloat(30.0f))
);
```

## Duration Constraints

When working with durations, several constraints apply to ensure effects behave consistently:

### Instant Effects Constraints

1. **No Periodic Data**: Instant effects cannot be [periodic](periodic.md).
   ```csharp
   // INVALID - Instant effects can't have periodic data
   new EffectData(
       "Invalid Effect",
       new DurationData(DurationType.Instant),
       [/*...*/],
       periodicData: new PeriodicData(new ScalableFloat(1.0f)) // Error
   );
   ```

2. **No Stacking**: Instant effects cannot have [stacking data](stacking.md#stackingdata).
   ```csharp
   // INVALID - Instant effects can't have stacking data
   new EffectData(
       "Invalid Effect",
       new DurationData(DurationType.Instant),
       [/*...*/],
       stackingData: new StackingData(/*...*/) // Error
   );
   ```

3. **Snapshot Requirements**: Instant effects must use snapshot attributes and level.
   ```csharp
   // INVALID - Instant effects must snapshot their level
   new EffectData(
       "Invalid Effect",
       new DurationData(DurationType.Instant),
       [/*...*/],
       snapshopLevel: false // Error
   );
   ```

4. **No ModifierTags**: Instant effects cannot apply tags through `ModifierTagsEffectComponent`.
   ```csharp
   // INVALID - Instant effects can't apply modifier tags
   new EffectData(
       "Invalid Effect",
       new DurationData(DurationType.Instant),
       [/*...*/],
       effectComponents: new[] { new ModifierTagsEffectComponent(new TagContainer()) } // Error
   );
   ```

### HasDuration Effects Constraints

1. **Duration Required**: `HasDuration` effects must provide a valid `Duration` property.
   ```csharp
   // INVALID - HasDuration requires a duration value
   new EffectData(
       "Invalid Effect",
       new DurationData(DurationType.HasDuration), // Error: missing duration
       [/*...*/]
   );

   // VALID - Providing required duration
   new EffectData(
       "Valid Effect",
       new DurationData(DurationType.HasDuration, new ScalableFloat(10.0f)), // Correct
       [/*...*/]
   );
   ```

2. **Stacking Requirements**: `HasDuration` effects with stacking must define `ApplicationRefreshPolicy`.
   ```csharp
   // VALID - HasDuration with stacking needs ApplicationRefreshPolicy
   new EffectData(
       "Valid Effect",
       new DurationData(DurationType.HasDuration, new ScalableFloat(10.0f)),
       [/*...*/],
       stackingData: new StackingData(
           stackLimit: new ScalableInt(3),
           initialStack: new ScalableInt(1),
           // ... other stacking data
           applicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication
       )
   );
   ```

### General Constraints

1. **Duration Value Requirement**: The `duration` value is only valid for `HasDuration` effects.
   ```csharp
   // INVALID - Can't provide duration for non-HasDuration effects
   new DurationData(
       DurationType.Infinite,
       new ScalableFloat(10.0f) // Error
   );
   ```

## Best Practices

1. **Choose Appropriate Types**:
   - Use `Instant` for one-time effects like damage or healing.
   - Use `HasDuration` for temporary buffs and debuffs.
   - Use `Infinite` for permanent effects or those requiring manual removal.

2. **Scale Durations with Level**:
   - Use `ScalableFloat` with curves for level-based duration scaling.
   ```csharp
   new DurationData(
       DurationType.HasDuration,
       new ScalableFloat(10.0f, durationCurve) // Scale with effect level
   )
   ```

3. **Handle Effect Removal**:
   - Always store `ActiveEffectHandle` for `Infinite` effects.
   - Consider early removal conditions for `HasDuration` effects.
   - Use `EffectsManager.UnapplyEffect` appropriately.

4. **Consider Performance**:
   - Minimize the number of long-duration effects active simultaneously.
   - Use `Instant` effects when appropriate to avoid tracking overhead.
   - Be cautious with infinitely stacking `HasDuration` effects.
