# Effect Duration System

The Duration system in Forge controls how long effects remain active on target entities. It provides three distinct duration types that determine an effect's lifecycle and behavior.

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
- Execute once at the moment of application
- Don't remain active in the effects manager
- Modify base attribute values directly
- Don't return an ActiveEffectHandle when applied

```csharp
// Create an instant damage effect
var damageEffectData = new EffectData(
    "Direct Damage",
    new[] {
        new Modifier("CombatAttributeSet.CurrentHealth", ModifierOperation.Add, -25)
    },
    new DurationData(DurationType.Instant),
    null,  // No stacking data
    null   // No periodic data
);
```

### Infinite

Infinite effects have no built-in expiration time and remain active until manually removed:
- Apply their modifiers continuously
- Remain active indefinitely
- Must be explicitly removed using RemoveEffect methods
- Useful for permanent buffs, persistent status effects, and equipment bonuses

Equipment-based buffs are a perfect use case for Infinite effects:
```csharp
// Create an equipment buff that lasts until the item is unequipped
var swordBuffEffectData = new EffectData(
    "Magic Sword Bonus",
    new[] {
        new Modifier("CombatAttributeSet.AttackPower", ModifierOperation.Add, 10),
        new Modifier("CombatAttributeSet.CriticalChance", ModifierOperation.Percentage, 0.05f)
    },
    new DurationData(DurationType.Infinite),
    null,  // No stacking data
    null   // No periodic data
);

// When equipping the item
ActiveEffectHandle equipmentBuffHandle = character.EffectsManager.ApplyEffect(swordBuffEffect);

// When unequipping the item
character.EffectsManager.UnapplyEffect(equipmentBuffHandle);
```

### HasDuration

Duration-based effects automatically expire after a specific amount of time:
- Apply their modifiers for a limited period
- Automatically remove themselves when their duration ends
- Can use ScalableFloat for level-based duration scaling
- Often used for temporary buffs and debuffs

```csharp
// Create a temporary buff that lasts 30 seconds
var temporaryBuffEffectData = new EffectData(
    "Temporary Speed Boost",
    new[] {
        new Modifier("MovementAttributeSet.Speed", ModifierOperation.Percentage, 0.3f)
    },
    new DurationData(DurationType.HasDuration, new ScalableFloat(30.0f)),
    null,  // No stacking data
    null   // No periodic data
);
```

## Duration Constraints

When working with durations, several constraints apply to ensure effects behave consistently:

### Instant Effects Constraints

1. **No Periodic Data**: Instant effects cannot be periodic
   ```csharp
   // INVALID - Instant effects can't have periodic data
   new EffectData(
       "Invalid Effect",
       [...],
       new DurationData(DurationType.Instant),
       null,
       new PeriodicData(new ScalableFloat(1.0f)) // Error
   );
   ```

2. **No Stacking**: Instant effects cannot stack
   ```csharp
   // INVALID - Instant effects can't have stacking data
   new EffectData(
       "Invalid Effect",
       [...],
       new DurationData(DurationType.Instant),
       new StackingData(...), // Error
       null
   );
   ```

3. **Snapshot Requirements**: Instant effects must use snapshot attributes and level
   ```csharp
   // INVALID - Instant effects must snapshot their level
   new EffectData(
       "Invalid Effect",
       [...],
       new DurationData(DurationType.Instant),
       null,
       null,
       snapshopLevel: false // Error
   );
   ```

4. **No ModifierTags**: Instant effects cannot apply tags through ModifierTagsEffectComponent
   ```csharp
   // INVALID - Instant effects can't apply modifier tags
   new EffectData(
       "Invalid Effect",
       [...],
       new DurationData(DurationType.Instant),
       null,
       null,
       true,
       new[] { new ModifierTagsEffectComponent(...) } // Error
   );
   ```

### HasDuration Effects Constraints

1. **Duration Required**: HasDuration effects must provide a valid Duration property
   ```csharp
   // INVALID - HasDuration requires a duration value
   new EffectData(
       "Invalid Effect",
       [...],
       new DurationData(DurationType.HasDuration), // Error: missing duration
       null,
       null
   );

   // VALID - Providing required duration
   new EffectData(
       "Valid Effect",
       [...],
       new DurationData(DurationType.HasDuration, new ScalableFloat(10.0f)), // Correct
       null,
       null
   );
   ```

2. **Stacking Requirements**: HasDuration effects with stacking must define ApplicationRefreshPolicy
   ```csharp
   // VALID - HasDuration with stacking needs ApplicationRefreshPolicy
   new EffectData(
       "Valid Effect",
       [...],
       new DurationData(DurationType.HasDuration, new ScalableFloat(10.0f)),
       new StackingData(
           stackLimit: new ScalableInt(3),
           initialStack: new ScalableInt(1),
           // Must define for HasDuration effects with stacking
           applicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshDuration
       ),
       null
   );
   ```

### General Constraints

1. **Duration Value Requirement**: Duration value is only valid for HasDuration effects
   ```csharp
   // INVALID - Can't provide duration for non-HasDuration effects
   new DurationData(
       DurationType.Infinite,
       new ScalableFloat(10.0f) // Error
   );
   ```

## Best Practices

1. **Choose Appropriate Types**:
   - Use `Instant` for one-time effects like damage or healing
   - Use `HasDuration` for temporary buffs and debuffs
   - Use `Infinite` for permanent effects or those requiring manual removal

2. **Scale Durations with Level**:
   - Use ScalableFloat with curves for level-based duration scaling
   ```csharp
   new DurationData(
       DurationType.HasDuration,
       new ScalableFloat(10.0f, durationCurve) // Scale with effect level
   )
   ```

3. **Handle Effect Removal**:
   - Always store ActiveEffectHandles for Infinite effects
   - Consider early removal conditions for HasDuration effects
   - Use RemoveEffect methods appropriately

4. **Consider Performance**:
   - Minimize the number of long-duration effects active simultaneously
   - Use Instant effects when appropriate to avoid tracking overhead
   - Be cautious with infinitely stacking HasDuration effects
