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

`DurationData` encapsulates the duration configuration for an effect. Durations use `ModifierMagnitude`, enabling scalable, attribute-driven, custom-calculated, or set-by-caller values.

```csharp
public readonly record struct DurationData(DurationType durationType, ModifierMagnitude? durationMagnitude = null)
{
    public DurationType DurationType { get; }
    public ModifierMagnitude? DurationMagnitude { get; }
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
    new DurationData(DurationType.Instant),
    new[] {
        new Modifier(
            "CombatAttributeSet.CurrentHealth",
            ModifierOperation.FlatBonus,
            new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-25)))
    }
);
```

### Infinite

Infinite effects have no built-in expiration time and remain active until manually removed. They:

- Apply their modifiers continuously.
- Remain active indefinitely.
- Must be explicitly removed using `EffectsManager.RemoveEffect`.
- Are useful for permanent buffs, persistent status effects, and equipment bonuses.

Equipment-based buffs are a perfect use case for Infinite effects:
```csharp
// Create an equipment buff that lasts until the item is unequipped
var swordBuffEffectData = new EffectData(
    "Magic Sword Bonus",
    new DurationData(DurationType.Infinite),
    new[] {
        new Modifier("CombatAttributeSet.AttackPower", ModifierOperation.FlatBonus, new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10))),
        new Modifier("CombatAttributeSet.CriticalChance", ModifierOperation.PercentBonus, new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(0.05f)))
    }
);

var swordBuffEffect = new Effect(swordBuffEffectData, new EffectOwnership(character, character));

// When equipping the item
ActiveEffectHandle? equipmentBuffHandle = character.EffectsManager.ApplyEffect(swordBuffEffect);

// When unequipping the item
if (equipmentBuffHandle is not null)
{
    character.EffectsManager.RemoveEffect(equipmentBuffHandle);
}
```

### HasDuration

Duration-based effects automatically expire after a specific amount of time. They:

- Apply their modifiers for a limited period.
- Automatically remove themselves when their duration ends.
- Are often used for temporary buffs and debuffs.
- Can use any `ModifierMagnitude`, so durations can scale with level, attributes, custom calculators, or set-by-caller values.
- Re-evaluate while active if the duration depends on non-snapshot inputs (live attribute captures or non-snapshot set-by-caller values).

```csharp
// Create a temporary buff that lasts 30 seconds
var temporaryBuffEffectData = new EffectData(
    "Temporary Speed Boost",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            scalableFloatMagnitude: new ScalableFloat(30.0f))),
    new[] {
        new Modifier("MovementAttributeSet.Speed", ModifierOperation.PercentBonus, new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(0.3f)))
    }
);

// Attribute-driven duration (live capture)
var attributeDurationEffectData = new EffectData(
    "Attribute-Based Shield",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.AttributeBased,
            attributeBasedFloat: new AttributeBasedFloat(
                new AttributeCaptureDefinition("StatAttributeSet.Strength", AttributeCaptureSource.Source, snapshot: false),
                AttributeCalculationType.CurrentValue,
                coefficient: new ScalableFloat(0.2f),
                preMultiplyAdditiveValue: new ScalableFloat(0),
                postMultiplyAdditiveValue: new ScalableFloat(5)))),
    new[] {
        new Modifier("CombatAttributeSet.CurrentHealth", ModifierOperation.FlatBonus, new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(25)))
    }
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
       snapshotLevel: false // Error
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

1. **Duration Required**: `HasDuration` effects must provide a valid `DurationMagnitude`.
   ```csharp
   // INVALID - HasDuration requires a duration value
   new EffectData(
       "Invalid Effect",
       new DurationData(DurationType.HasDuration), // Error: missing duration
       [/*...*/]
   );

   // VALID - Providing required duration magnitude
   new EffectData(
       "Valid Effect",
       new DurationData(
           DurationType.HasDuration,
           new ModifierMagnitude(
               MagnitudeCalculationType.ScalableFloat,
               scalableFloatMagnitude: new ScalableFloat(10.0f))),
      [/*...*/]
   );
   ```

2. **Stacking Requirement**: If stacking is configured, `ApplicationRefreshPolicy` must be defined.
   ```csharp
   // VALID - HasDuration with stacking needs ApplicationRefreshPolicy
   new EffectData(
       "Valid Effect",
       new DurationData(
                  DurationType.HasDuration,
                  new ModifierMagnitude(
                      MagnitudeCalculationType.ScalableFloat,
                      scalableFloatMagnitude: new ScalableFloat(10.0f))),
       [/*...*/],
       stackingData: new StackingData(
           stackLimit: new ScalableInt(3),
           initialStack: new ScalableInt(1),
           // ... other stacking data
       applicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication
       )
   );
   ```

3. **Periodic + Stacking**: When both periodic and stacking are present, `ExecuteOnSuccessfulApplication` and `ApplicationResetPeriodPolicy` must be defined in stacking data.

### General Constraints

1. **DurationMagnitude Scope**: `DurationMagnitude` is only valid when `DurationType` is `HasDuration`.
   ```csharp
   // INVALID - Can't provide duration magniteude for non-HasDuration effects
   new DurationData(
       DurationType.Infinite,
       new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10.0f)) // Invalid
   );
   ```

2. **Dynamic Re-evaluation**: Active effects re-evaluate duration when non-snapshot inputs used by `DurationMagnitude` change (live attribute captures or non-snapshot set-by-caller values). Remaining duration adjusts accordingly.

## Best Practices

1. **Choose Appropriate Types**:
   - Use `Instant` for one-time effects like damage or healing.
   - Use `HasDuration` for temporary buffs and debuffs.
   - Use `Infinite` for permanent effects or those requiring manual removal.

2. **Choose Appropriate ModifierMagnitudes**:
   - Use `ScalableFloat` with curves for level-based duration scaling.
   - Use `AttributeBased` for attribute-driven duration.
   - Use `CustomCalculatorClass` for custom logic duration.
   - Use `SetByCaller` for duration based on external inputs.

3. **Snapshot Attributes**:
   - Capture only the attributes needed.
   - Decide between snapshot and live captures based on whether the duration should update while active.

4. **Handle Effect Removal**:
   - Always store `ActiveEffectHandle` for `Infinite` effects.
   - Consider early removal conditions for `HasDuration` effects.
   - Use `EffectsManager.RemoveEffect` appropriately.

5. **Consider Performance**:
   - Minimize the number of long-duration effects active simultaneously.
   - Use `Instant` effects when appropriate to avoid tracking overhead.
   - Be cautious with infinitely stacking `HasDuration` effects.
