# Effects System

The Effects system in Forge provides a powerful framework for implementing gameplay mechanics such as damage, healing, buffs, debuffs, and other status effects. It offers a data-driven approach to modifying attributes and applying status changes to game entities.

## Core Concepts

- **Effects** are self-contained gameplay rules that can modify attributes, apply tags, or trigger cues
- **Modifiers** define how effects change attributes (add, multiply, override)
- **Durations** control how long effects remain active (instant, timed, infinite)
- **Stacking** rules determine how multiple instances of similar effects combine
- **Executions** allow for custom logic beyond simple attribute modifications

## Key Components

### Effect and EffectData

The Effect system revolves around two core types:

- **EffectData**: An immutable definition of how an effect behaves (blueprint)
- **Effect**: A runtime instance based on EffectData with level and ownership information

```csharp
// Create an effect definition
var effectData = new EffectData(
    "Fire Damage",              // Effect name
    [                           // Modifiers
        new Modifier(
            "CombatAttributeSet.CurrentHealth",
            ModifierOperation.FlatBonus,
            new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-10)))
    ],
    new DurationData(DurationType.Instant),  // Duration settings
    null,                       // Stacking settings (null = no stacking)
    null                        // Periodic settings (null = not periodic)
);

// Create an effect instance from effect data
var effect = new Effect(
    effectData,
    new EffectOwnership(sourceEntity, ownerEntity)
);
```

While you can create `EffectData` at runtime, it's generally better to define them ahead of time and serialize them for consistency and balancing, or at least create them at runtime based on serialized data.

**Note:** If you'll apply an effect repeatedly, keep a reference to the `Effect` instance to maintain its level.

### EffectsManager

The `EffectsManager` is responsible for applying, tracking, and updating effects on an entity. Every `IForgeEntity` has an EffectsManager accessible through its interface.

```csharp
// Apply an effect to an entity
ActiveEffectHandle handle = entity.EffectsManager.ApplyEffect(effect);

// Remove an effect by its handle
bool removed = entity.EffectsManager.UnapplyEffect(handle);

// Update all active effects on the entity
entity.EffectsManager.UpdateEffects(deltaTime);
```

#### Updating Effects

The `UpdateEffects` method must be called regularly to process all active effects:

- For real-time games: Call in your update loop with the frame's delta time
  ```csharp
  void Update()
  {
      entity.EffectsManager.UpdateEffects(Time.deltaTime);
  }
  ```

- For turn-based games: Call with a fixed value representing a turn
  ```csharp
  void EndTurn()
  {
      // Update effects for one full turn
      entity.EffectsManager.UpdateEffects(1.0f);
  }
  ```

### ActiveEffectHandle

When a non-instant effect is applied, the `EffectsManager` returns an `ActiveEffectHandle` that provides control over the effect's lifecycle:

```csharp
// Apply a buff and get its handle
ActiveEffectHandle buffHandle = target.EffectsManager.ApplyEffect(buffEffect);

// Check if application was successful
if (buffHandle != null)
{
    // Store the handle for later control
    _activeBuffs.Add(buffHandle);

    // Remove the effect using the handle
    target.EffectsManager.UnapplyEffect(buffHandle);
}
```

Other removal methods exist but are less precise:
```csharp
// Removes first effect instance matching the Effect
entity.EffectsManager.UnapplyEffect(effect);

// Removes first effect instance matching the EffectData
entity.EffectsManager.UnapplyEffectData(effectData);
```

## Effect Lifecycle

### Application vs. Execution

Effects have two distinct phases:

**Application:** The process of registering an effect with the target's `EffectsManager`
- Occurs when `ApplyEffect()` is called
- Checks tag requirements
- For non-instant effects, creates an `ActiveEffect` and returns an `ActiveEffectHandle`
- For instant effects, no handle is returned (returns null)

**Execution:** The actual modification of base attribute values (not through temporary modifiers)
- Only occurs for instant and periodic effects
- Instant effects execute immediately upon application
- Periodic effects execute on intervals defined by their `PeriodicData`
- Duration effects don't execute at all - they apply temporary modifiers instead

```csharp
// Apply an instant effect (applies and executes immediately)
var instantHandle = target.EffectsManager.ApplyEffect(instantEffect);
// instantHandle will be null as instant effects don't remain active

// Apply a duration effect (applies modifiers but doesn't execute)
var durationHandle = target.EffectsManager.ApplyEffect(durationEffect);
// durationHandle provides a reference to control the active effect
```

### EffectOwnership

`EffectOwnership` specifies the relationship of the effect to game entities:

- **Owner:** The entity responsible for causing the action or event (e.g., a player character, NPC, environment)
- **Source:** The actual entity that caused the effect (e.g., a weapon, projectile, trap, or the owner itself)

This distinction becomes important for attribute-based calculations and gameplay logic:

```csharp
// Player casts a spell (player is both owner and source)
var playerCastEffect = new Effect(
    spellEffectData,
    new EffectOwnership(playerEntity, playerEntity)
);

// Player's weapon causes damage (player is owner, weapon is source)
var weaponDamageEffect = new Effect(
    weaponEffectData,
    new EffectOwnership(playerEntity, weaponEntity)
);
```

## Effect Levels and Scaling

Effect levels provide a powerful way to scale an effect's potency without creating multiple effect definitions. A single `EffectData` can be used to create effects at different power levels, with the scaling behavior defined by curves.

### What Effect Level Affects

The level of an effect can influence many aspects of its behavior in either direction. Values can increase, decrease, or change in more complex ways as levels change:

- **Modifier Magnitudes**: Scale damage up or reduce healing efficiency at higher levels
- **Duration**: Make effects last longer or shorter with level changes
- **Period**: Adjust frequency of periodic triggers (faster or slower)
- **Stack Limits**: Modify maximum stack counts based on level
- **Stack Initial Count**: Change starting stack count with level

### ScalableFloat and ScalableInt

`ScalableFloat` and `ScalableInt` are the foundation of level-based scaling in the Effects system. They consist of:

1. A base value
2. An optional curve that maps the effect level to a scaling factor

```csharp
// Create a scalable value with a base of 10 and no curve (no level scaling)
var fixedDamage = new ScalableFloat(10.0f);

// Create a scalable value that increases with level
var scalingDamage = new ScalableFloat(10.0f, new Curve([
    new CurveKey(1, 1.0f),   // Level 1: base value × 1 = 10
    new CurveKey(2, 1.5f),   // Level 2: base value × 1.5 = 15
    new CurveKey(3, 2.25f),  // Level 3: base value × 2.25 = 22.5
    new CurveKey(5, 4.0f)    // Level 5: base value × 4 = 40
]));

// Create a scalable value that decreases with level (diminishing returns)
var decreasingDuration = new ScalableFloat(10.0f, new Curve([
    new CurveKey(1, 1.0f),   // Level 1: 10 seconds
    new CurveKey(3, 0.8f),   // Level 3: 8 seconds
    new CurveKey(5, 0.6f)    // Level 5: 6 seconds
]));
```

**Note:** The `Curve` and `CurveKey` types shown in the examples are simplified implementations provided in the test project. The actual implementations might vary, but they follow the `ICurve` interface which defines the `Evaluate(float)` method for mapping an input value to a scaling factor.

### Understanding Curves

Curves map an input value (like effect level) to an output scaling factor. The `ICurve` interface abstracts how this evaluation happens, allowing different implementations:

```csharp
public interface ICurve
{
    float Evaluate(float value);
}
```

Curves can be implemented in different ways:

1. **Point-based interpolation**: Define specific key points and interpolate between them (as shown in examples)
2. **Continuous functions**: Implement mathematical functions that compute values for any input
3. **Custom logic**: Implement any custom evaluation logic needed for specific gameplay mechanics

### Leveraging Levels in Effect Design

```csharp
// Create the effect at level 1
var level1Fireball = new Effect(fireballEffect, new EffectOwnership(caster, caster));

// Create a more powerful version at level 3
var level3Fireball = new Effect(fireballEffect, new EffectOwnership(caster, caster));
level3Fireball.SetLevel(3);

// Leveling up an existing effect
effect.LevelUp();         // Increase level by 1
effect.SetLevel(5);       // Directly set to level 5
```

### Data-Driven Design Benefits

Using effect levels and curves provides several advantages:

1. **Simplified Effect Management**: One effect definition can cover multiple power levels
2. **Easy Balancing**: Adjust curves rather than creating new effect variants
3. **Player Progression**: Tie effect power to character level or skill investment
4. **Visual Consistency**: Effects visually scale with their power level
5. **Performance**: Less memory usage with fewer effect definitions

## EffectData Configuration Guide

The `EffectData` struct provides extensive configuration options for defining how effects behave. Here's a comprehensive overview of each component:

### Basic Properties

#### Name and Level Snapshotting

```csharp
var effectData = new EffectData(
    name: "Fireball",          // Human-readable name for debugging and UI
    modifiers: [...],
    durationData: new DurationData(DurationType.Instant),
    stackingData: null,
    periodicData: null,
    snapshopLevel: true        // Whether to lock the effect's level when applied
);
```

- **Name**: Identifies the effect for debugging, UI display, and designer reference
- **SnapshopLevel**: Controls how the effect responds to level changes after application
  - `true`: Effect uses the level it had when initially applied (default)
  - `false`: Effect dynamically updates when the effect's level changes - all modifiers, durations, periods, and other scalable values will automatically adjust if they use curves

**Example use cases:**
- `true`: Fireball damage that's determined when cast, regardless of later effect level changes
- `false`: Blessing buff from an item that grows stronger as the effect gains levels

### Modifiers

Modifiers define how effects change attributes by adding, multiplying, or overriding their values. They are applied to specific attributes on the target entity, allowing effects to boost stats, deal damage, apply healing, or otherwise manipulate gameplay values. Each modifier can target different attributes and AttributeSets without restrictions, and modifiers are optional.

```csharp
var effectData = new EffectData(
    "Strength Potion",
    modifiers: [
        // First modifier - flat strength bonus
        new Modifier(
            "StatsAttributeSet.Strength",       // Target attribute
            ModifierOperation.FlatBonus,        // How the value is applied
            new ModifierMagnitude(
                MagnitudeCalculationType.ScalableFloat,
                new ScalableFloat(5, strengthCurve)
            ),
            channel: 0                          // Optional channel (default 0)
        ),
        // Second modifier - percentage health bonus
        new Modifier(
            "StatsAttributeSet.MaxHealth",
            ModifierOperation.PercentageBonus,
            new ModifierMagnitude(
                MagnitudeCalculationType.ScalableFloat,
                new ScalableFloat(0.1f)         // 10% bonus
            )
        ),
        // Third modifier - movement speed reduction
        new Modifier(
            "MovementAttributeSet.Speed",
            ModifierOperation.PercentageBonus,
            new ModifierMagnitude(
                MagnitudeCalculationType.ScalableFloat,
                new ScalableFloat(-0.15f)      // 15% reduction
            )
        )
    ],
    // Other parameters...
);
```

If the target entity doesn't have a referenced AttributeSet or Attribute, those modifiers are simply ignored without causing errors.

**Example use cases:**
- Health potions: `FlatBonus` to CurrentHealth
- Damage over time: Periodic negative `FlatBonus` to CurrentHealth
- Stat buffs: `FlatBonus` or `PercentageBonus` to attributes
- Crowd control: `Override` to MovementSpeed (set to 0)

For more details on working with modifiers, see [Modifiers System](modifiers.md).

### Duration Data

DurationData is required for all effects and determines how long an effect persists:

```csharp
// Instant effect (execute once and end)
var healData = new EffectData(
    "Instant Heal",
    modifiers: [...],
    durationData: new DurationData(DurationType.Instant),
    // Other parameters...
);

// Duration effect (remains active for 10 seconds)
var buffData = new EffectData(
    "Temporary Buff",
    modifiers: [...],
    durationData: new DurationData(
        DurationType.HasDuration,
        new ScalableFloat(10.0f)    // 10 second duration
    ),
    // Other parameters...
);

// Infinite effect (remains until manually removed)
var curseData = new EffectData(
    "Permanent Curse",
    modifiers: [...],
    durationData: new DurationData(DurationType.Infinite),
    // Other parameters...
);
```

**Example use cases:**
- `Instant`: Direct damage, healing, knockback
- `HasDuration`: Buffs, debuffs, temporary effects
- `Infinite`: Permanent status effects, curses that require specific removal

For more details on duration configuration, see [Effect Duration System](duration.md).

### Periodic Data

PeriodicData controls recurring execution of effects at defined intervals, allowing effects to repeatedly apply their gameplay impact over time. It is optional, and an effect is either periodic or non-periodic. You can't have a single effect that both applies temporary modifiers and executes periodic effects:

```csharp
var dotEffectData = new EffectData(
    "Poison",
    modifiers: [...],
    durationData: new DurationData(
        DurationType.HasDuration,
        new ScalableFloat(8.0f)     // 8 second total duration
    ),
    stackingData: null,
    periodicData: new PeriodicData(
        period: new ScalableFloat(2.0f),  // Execute every 2 seconds
        executeOnApplication: true,       // Apply damage immediately on application
        periodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
    )
);
```

**Important notes:**
- Instant effects cannot have periodic data
- For effects that need both temporary modifiers and periodic executions, create separate synchronized effects

**Example use cases:**
- Damage over time effects (poison, burn, bleed)
- Healing over time effects (regeneration)
- Pulsing auras that apply effects regularly
- Resource generation/consumption over time

For more details on periodic effects, see [Periodic Effect System](periodic.md).

### Stacking Data

StackingData defines how multiple instances of similar effects combine, allowing for stacking buffs, debuffs, or creating unique effects that can only be applied once. It is optional and by default, without stacking data, each application of an effect has its own separate instance:

```csharp
var stackingEffectData = new EffectData(
    "Bleed",
    modifiers: [...],
    durationData: new DurationData(
        DurationType.HasDuration,
        new ScalableFloat(5.0f)
    ),
    stackingData: new StackingData(
        stackLimit: new ScalableInt(3),              // Max 3 stacks
        initialStack: new ScalableInt(1),            // Start with 1 stack
        stackPolicy: StackPolicy.AggregateBySource,  // Same source stacks together
        stackLevelPolicy: StackLevelPolicy.AggregateLevels, // Different levels can stack
        magnitudePolicy: StackMagnitudePolicy.Sum,   // Add magnitude for each stack
        overflowPolicy: StackOverflowPolicy.AllowApplication, // Allow applying at max stacks
        expirationPolicy: StackExpirationPolicy.RemoveSingleStackAndRefreshDuration // Remove one stack at a time
    ),
    periodicData: new PeriodicData(
        period: new ScalableFloat(1.0f),
        executeOnApplication: true,
        periodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
    )
);
```

**Important notes:**
- Instant effects cannot have stacking data
- For truly unique effects that can only be applied once, use `stackLimit: new ScalableInt(1)`

**Example use cases:**
- Bleed effects that stack multiple instances
- Unique debuffs (using `StackPolicy.AggregateByTarget` with `StackOverflowPolicy.DenyApplication` and stack limit of 1)
- Source-limited buffs (only one per source using `StackPolicy.AggregateBySource`)
- Effects that increase in power with multiple applications

For more details on stacking configuration, see [Effect Stacking System](stacking.md).

### Effect Components

Effect components extend and customize effect behavior through lifecycle hooks, enabling conditional application, tag management, and other specialized behaviors. They are optional and can be included in any number within an effect:

```csharp
var componentBasedEffectData = new EffectData(
    "Slow Aura",
    modifiers: [...],
    durationData: new DurationData(DurationType.Infinite),
    stackingData: null,
    periodicData: null,
    snapshopLevel: true,
    effectComponents: [
        // Add status tag while effect is active (automatically removed when effect ends)
        new ModifierTagsEffectComponent([
            Tag.RequestTag(tagsManager, "status.slowed")
        ]),
        // 50% chance to apply effect
        new ChanceToApplyEffectComponent(randomProvider, new ScalableFloat(0.5f)),
        // Only apply to targets with specific tags
        new TargetTagRequirementsEffectComponent(
            applicationTagRequirements: new TagRequirements(
                requiredTags: [Tag.RequestTag(tagsManager, "entity.living")],
                ignoredTags: [Tag.RequestTag(tagsManager, "status.immune.slow")]
            ),
            removalTagRequirements: new TagRequirements(
                requiredTags: [Tag.RequestTag(tagsManager, "status.cleansed")]
            ),
            ongoingTagRequirements: new TagRequirements(
                requiredTags: [],
                ignoredTags: [Tag.RequestTag(tagsManager, "status.resistant")]
            )
        )
    ]
);
```

**Built-in components:**
- **ChanceToApplyEffectComponent**: Provides random chance for effect application
- **ModifierTagsEffectComponent**: Adds tags while effect is active, which are automatically removed when the effect ends
- **TargetTagRequirementsEffectComponent**: Checks tag conditions for application, removal, and inhibition

You can also create custom components by implementing the `IEffectComponent` interface.

For more details on effect components, see [Effect Components System](components.md).

### Custom Executions

Custom Executions implement complex logic beyond simple attribute modifications, allowing for sophisticated effects that can modify multiple attributes, transfer values between entities, or implement specialized gameplay mechanics. They are optional and can be included in any number within an effect:

```csharp
var executionBasedEffectData = new EffectData(
    "Life Transfer",
    modifiers: [],  // No pre-defined modifiers
    durationData: new DurationData(DurationType.Instant),
    stackingData: null,
    periodicData: null,
    snapshopLevel: true,
    effectComponents: null,
    requireModifierSuccessToTriggerCue: false,
    suppressStackingCues: false,
    customExecutions: [
        new HealthTransferExecution(),  // Custom execution logic
        new KnockbackExecution()        // Another execution in the same effect
    ]
);
```

**Important notes:**
- Custom Executions are evaluated for all effect types, even duration effects that aren't periodic
- For calculations using multiple attributes as input to calculate a single modifier that affects one attribute, consider using `CustomCalculationBasedFloat`

**Example use cases:**
- Complex calculations that modify multiple attributes at once
- Effects that transfer values between entities
- Physics-based effects like knockback
- Status effect management (dispelling, transforming)
- Conditional effects with complex logic

For more details on custom executions, see [Custom Calculators System](calculators.md).

### Cue Configurations

Cues provide visual and audio feedback for effects, helping communicate gameplay changes to players through effects, animations, sounds, or UI elements. They are optional and can be included in any number within an effect:

```csharp
var cueEnabledEffectData = new EffectData(
    "Fire Strike",
    modifiers: [...],
    durationData: new DurationData(DurationType.Instant),
    stackingData: null,
    periodicData: null,
    snapshopLevel: true,
    effectComponents: null,
    requireModifierSuccessToTriggerCue: true,  // Only trigger if damage was dealt
    suppressStackingCues: false,               // Always trigger cues on stack changes
    customExecutions: null,
    cues: [
        new CueData(
            Tag.RequestTag(tagsManager, "cues.damage.fire").GetSingleTagContainer(),
            0, 100,                             // Min/max values for magnitude normalization
            CueMagnitudeType.AttributeValueChange,
            "CombatAttributeSet.CurrentHealth"  // Attribute to monitor for changes
        )
    ]
);
```

**Cue-related properties:**
- **RequireModifierSuccessToTriggerCue**: When true, cues only trigger if at least one attribute was successfully modified
- **SuppressStackingCues**: When true, cues don't trigger when stacks are applied (only on initial application)

**Example use cases:**
- Visual effects for damage, healing, buffs
- Sound effects for status changes
- Camera effects for important gameplay moments
- UI indicators for effect application/removal

For more details on working with cues, see [Cues System](cues.md).

## Best Practices

1. **Reuse Effect Definitions**: Create a library of effect templates for consistency
2. **Layer Simple Effects**: Combine simple effects instead of creating overly complex ones
3. **Use Tag Requirements**: Make effects conditional with tag requirements
4. **Balance Attributes and Executions**: Use attributes for common mechanics and executions for complex ones
5. **Handle Edge Cases**: Consider immunity, resistance, and other special cases
6. **Modular Design**: Split functionality into separate effects for better maintainability
7. **Test Interactions**: Effects can interact in unexpected ways; test combinations thoroughly
8. **Think About Stacking**: Carefully define stacking policies to prevent unintended power scaling
9. **Design with Curves**: Use curves for balancing and easier configuration of effect progression
10. **Document Effects**: Create clear documentation for effects to aid debugging and balancing
11. **Optimize Updates**: Be mindful of performance with many active effects
12. **Store Effect Handles**: Keep references to ActiveEffectHandles for manually controlled effects
