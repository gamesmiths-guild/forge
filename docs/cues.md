# Cues System

The Cues system in Forge provides a mechanism for triggering visual, audio, and other feedback in response to gameplay events. Cues create a clean separation between game logic and presentation, enabling consistent feedback across your game.

For a practical guide on using cues, see the [Quick Start Guide](quick-start.md).

## Core Concepts

- Cues respond to specific trigger events.
- They handle visual and audio feedback without affecting gameplay mechanics.
- Cues are identified by [tags](tags.md) for easy organization.
- Cue handlers implement feedback logic with flexible trigger points.

## Defining Cues

### Tag-Based Cue Identification

Cues are identified using tags, which allows for flexible organization and dispatch:

1. **Tag Naming Convention**: While not enforced by the system, using a `cues.` prefix (or similar convention) for cue tags is recommended to distinguish them from gameplay tags.
2. **Hierarchical Organization**: Organize cues in logical categories (e.g., `cues.damage.fire`, `cues.status.poison`).
3. **Tag Registration**: All cue tags must be registered with the `TagsManager`, just like gameplay tags.

```csharp
// Register cue tags during TagsManager initialization
var tagsManager = new TagsManager([
    // Gameplay tags
    "enemy.undead.zombie",
    "character.player",

    // Cue tags - recommended with "cues." prefix
    "cues.damage.physical",
    "cues.damage.fire",
    "cues.status.stun",
    "cues.status.poison"
]);
```

### CueData

`CueData` configures how a cue behaves, including magnitude calculation:

```csharp
// Configure a cue for a damage effect
var cueTags = tagsManager.RequestTagContainer(["cues.damage.physical"]);
var cueData = new CueData(
    cueTags: cueTags,
    minValue: 0,
    maxValue: 100,
    magnitudeType: CueMagnitudeType.AttributeValueChange,
    magnitudeAttribute: "CombatAttributeSet.CurrentHealth",  // Required for attribute-based magnitude types
    finalChannel: 0  // Required for AttributeMagnitudeEvaluatedUpToChannel type
);
```

### Cue Magnitude Types

`CueMagnitudeType` determines how a cue's magnitude is calculated:

- **EffectLevel**: Uses the effect's level.
- **StackCount**: Uses the effect's stack count.
- **AttributeValueChange**: Uses the change in an attribute's value (requires `magnitudeAttribute`).
- **AttributeBaseValue**: Uses an attribute's base value (requires `magnitudeAttribute`).
- **AttributeCurrentValue**: Uses an attribute's current value (requires `magnitudeAttribute`).
- **AttributeModifier**: Uses an attribute's current modifier value (requires `magnitudeAttribute`).
- **AttributeOverflow**: Uses an attribute's current overflow value (requires `magnitudeAttribute`).
- **AttributeValidModifier**: Uses an attribute's effective modifier value excluding overflow (requires `magnitudeAttribute`).
- **AttributeMin**: Uses an attribute's minimum value constraint (requires `magnitudeAttribute`).
- **AttributeMax**: Uses an attribute's maximum value constraint (requires `magnitudeAttribute`).
- **AttributeMagnitudeEvaluatedUpToChannel**: Uses an attribute's magnitude calculated up to a specific channel (requires `magnitudeAttribute` and `finalChannel`).

```csharp
// Magnitude based on effect level
var levelBasedCue = new CueData(
    cueTags: effectLevelTags,
    minValue: 1,
    maxValue: 10,
    magnitudeType: CueMagnitudeType.EffectLevel  // No attribute needed
);

// Magnitude based on health change
var healthChangeCue = new CueData(
    cueTags: healthChangeTags,
    minValue: 0,
    maxValue: 100,
    magnitudeType: CueMagnitudeType.AttributeValueChange,
    magnitudeAttribute: "CombatAttributeSet.CurrentHealth"  // Required parameter
);

// Magnitude based on valid modifier (without overflow)
var modifierCue = new CueData(
    cueTags: modifierTags,
    minValue: -50,
    maxValue: 50,
    magnitudeType: CueMagnitudeType.AttributeValidModifier,
    magnitudeAttribute: "CombatAttributeSet.AttackPower"  // Required parameter
);

// Magnitude based on calculation up to channel 2
var channelMagnitudeCue = new CueData(
    cueTags: calculationTags,
    minValue: 0,
    maxValue: 200,
    magnitudeType: CueMagnitudeType.AttributeMagnitudeEvaluatedUpToChannel,
    magnitudeAttribute: "CombatAttributeSet.DamageOutput",  // Required parameter
    finalChannel: 2  // Required for this magnitude type
);
```

**Important**:
- The `magnitudeAttribute` parameter is required when using any attribute-based magnitude types.
- The `finalChannel` parameter is required specifically for `AttributeMagnitudeEvaluatedUpToChannel` type.
- Omitting required parameters will result in errors.

### CueParameters and Magnitude

`CueParameters` carry information to cue handlers when triggered:

```csharp
var parameters = new CueParameters(
    magnitude: 25,              // Raw magnitude value
    normalizedMagnitude: 0.25f, // Normalized magnitude (0-1)
    source: attacker,           // Source entity (optional)
    customParameters: new Dictionary<StringKey, object>
    {
        { "DamageType", "Fire" },
        { "IsCritical", true }
    }
);
```

#### Magnitude and Normalization

Depending on the `CueMagnitudeType` selected, the `magnitude` and `normalizedMagnitude` parameters behave differently:

| MagnitudeType                        | Magnitude Source                      | Normalized Range             | Example Use Case                       |
|--------------------------------------|---------------------------------------|------------------------------|----------------------------------------|
| EffectLevel                          | Effect's current level                | 0 to 1 (relative to min/max) | Effect strength scaling with level     |
| StackCount                           | Effect's stack count                  | 0 to 1 (relative to min/max) | Visual intensity based on stacks       |
| AttributeValueChange                 | Change applied to attribute           | 0 to 1 (relative to min/max) | Damage/healing visual scale            |
| AttributeBaseValue                   | Attribute's base value                | 0 to 1 (relative to min/max) | Visuals based on base stat             |
| AttributeCurrentValue                | Current value of attribute            | 0 to 1 (relative to min/max) | Health bar fill amount                 |
| AttributeModifier                    | Total modifier value on attribute     | 0 to 1 (relative to min/max) | Buff/debuff intensity                  |
| AttributeOverflow                    | Overflow value of attribute           | 0 to 1 (relative to min/max) | Visuals for overcharged, overheal      |
| AttributeValidModifier               | Effective modifier (minus overflow)   | 0 to 1 (relative to min/max) | Visual effect for valid stat changes   |
| AttributeMin                         | Minimum value constraint              | 0 to 1 (relative to min/max) | Visualizing attribute boundaries       |
| AttributeMax                         | Maximum value constraint              | 0 to 1 (relative to min/max) | Visualizing attribute limits           |
| AttributeMagnitudeEvaluatedUpToChannel | Value calculated to specific channel | 0 to 1 (relative to min/max) | Visual effects for partial calculations|

The normalization process maps raw magnitude values into the 0-1 range:

```csharp
// How normalization works:
float normalizedMagnitude = Math.Clamp((magnitude - minValue) / (maxValue - minValue), 0f, 1f);
```

## Handling Cues

### CuesManager

`CuesManager` is responsible for registering cue handlers and dispatching cue events. It should typically be implemented as a singleton for your game:

```csharp
// Initialize the CuesManager
var cuesManager = new CuesManager();

// Register cue handlers
cuesManager.RegisterCue(Tag.RequestTag(tagsManager, "cues.damage.fire"), new FireDamageCueHandler());
cuesManager.RegisterCue(Tag.RequestTag(tagsManager, "cues.status.poison"), new PoisonEffectCueHandler());
```

### ICueHandler

The `ICueHandler` interface defines how cue handlers respond to different events:

```csharp
public interface ICueHandler
{
    void OnExecute(IForgeEntity? target, CueParameters? parameters);
    void OnApply(IForgeEntity? target, CueParameters? parameters);
    void OnRemove(IForgeEntity? target, bool interrupted);
    void OnUpdate(IForgeEntity? target, CueParameters? parameters);
}
```

Each method is called at a specific point in a cue's lifecycle:

- **OnExecute**: Called for one-shot cues or when a periodic effect executes.
- **OnApply**: Called when a persistent cue is first applied to an entity.
- **OnRemove**: Called when a persistent cue is removed (due to expiration or manual removal).
- **OnUpdate**: Called when a persistent cue's parameters change (e.g., due to effect level changes).

### Implementing Cue Handlers

Cue handlers implement the `ICueHandler` interface to respond to cue events:

```csharp
public class FireDamageCueHandler : ICueHandler
{
    public void OnExecute(IForgeEntity? target, CueParameters? parameters)
    {
        // One-shot effect (like impact)
        // Called when an instant effect with this cue is applied
        // Also called when a periodic effect with this cue executes its period

        if (target == null || !parameters.HasValue) return;

        // Extract parameters
        float magnitude = parameters.Value.Magnitude;
        float normalizedMagnitude = parameters.Value.NormalizedMagnitude;

        // Play effects scaled by magnitude
        PlayFireImpactSound(normalizedMagnitude);
        SpawnFireImpactParticles(target, magnitude);
    }

    public void OnApply(IForgeEntity? target, CueParameters? parameters)
    {
        // Persistent effect start
        // Called when a duration/infinite effect with this cue is applied

        if (target == null) return;

        // Start burning visual effect
        AttachBurningEffect(target);
    }

    public void OnRemove(IForgeEntity? target, bool interrupted)
    {
        // Persistent effect end
        // Called when a duration effect expires or is manually removed
        // The interrupted parameter indicates if removal was due to interruption

        if (target == null) return;

        // Clean up effects
        RemoveBurningEffect(target, interrupted);
    }

    public void OnUpdate(IForgeEntity? target, CueParameters? parameters)
    {
        // Update persistent effect
        // Called when an active effect's properties change (e.g., level up)

        if (target == null || !parameters.HasValue) return;

        // Update effect intensity
        UpdateBurningEffectIntensity(target, parameters.Value.NormalizedMagnitude);
    }

    // Implementation details would depend on your rendering system
}
```

## Triggering Cues

Cues can be triggered in several ways:

### 1. Through Effects

Cues are typically triggered through the [Effects system](effects/README.md):

```csharp
// Create an effect with a cue
var effectData = new EffectData(
    "Fireball",
    new DurationData(DurationType.Instant),
    [
        new Modifier(
            "CombatAttributeSet.CurrentHealth",
            ModifierOperation.FlatBonus,
            new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-30)))
    ],
    cues: [
        new CueData(
            Tag.RequestTag(tagsManager, "cues.damage.fire").GetSingleTagContainer(),
            0, 100,
            CueMagnitudeType.AttributeValueChange,
            "CombatAttributeSet.CurrentHealth"  // Required for AttributeValueChange type
        )
    ]
);

var effect = new Effect(effectData, new EffectOwnership(sourceEntity, sourceEntity));

// Apply the effect (which will trigger the cue)
entity.EffectsManager.ApplyEffect(effect);
```

### 2. Manual Triggering

Cues can be manually triggered for specific scenarios:

```csharp
// Execute a one-shot cue
var fireImpactTag = Tag.RequestTag(tagsManager, "cues.environment.fire_impact");
cuesManager.ExecuteCue(fireImpactTag, targetEntity, parameters);

// Apply a persistent cue
var burningTag = Tag.RequestTag(tagsManager, "cues.status.burning");
cuesManager.ApplyCue(burningTag, targetEntity, parameters);

// Update a persistent cue
cuesManager.UpdateCue(burningTag, targetEntity, updatedParameters);

// Later, remove the persistent cue
cuesManager.RemoveCue(burningTag, targetEntity, interrupted: false);
```

## Cues vs Events

Cues are designed for the presentation layer:  visual effects, audio, and player feedback. For gameplay logic that affects simulation state, use the [Events system](events.md) instead.

- **Cues** handle presentation: particle effects, sounds, UI animations. In a networked context, they can use unreliable replication. 
- **Events** handle simulation: damage calculations, ability triggers, state changes. In a networked context, they require reliable replication. 

A common pattern is to raise an Event for gameplay logic, then trigger the corresponding Cue for feedback:

```csharp
// Event for gameplay (reliable, affects game state)
entity.Events. Raise(new EventData
{
    EventTags = damageEventTag.GetSingleTagContainer(),
    Source = attacker,
    Target = victim,
    EventMagnitude = damage
});

// Cue for presentation (can be unreliable)
cuesManager.ExecuteCue(damageCueTag, victim, cueParameters);
```

## Best Practices

1. **Separate Concerns**: Keep gameplay logic in effects and executions, and visual feedback in cues.
2. **Use Tag Prefix**: While not enforced, using a consistent prefix (like `cues.`) helps organize and distinguish cue tags.
3. **Handle Null Parameters**: Always validate parameters before using them.
4. **Clean Up Resources**: Ensure resources are cleaned up when cues are removed.
5. **Scale Effects Appropriately**: Use `normalizedMagnitude` to scale effect intensity.
6. **Lifecycle Management**: Consider how the cue's lifecycle should match game events, especially for persistent cues.
7. **CuesManager as Singleton**: Maintain a single `CuesManager` instance for the game.
8. **Provide Required Parameters**: Always specify `magnitudeAttribute` when using attribute-based magnitude types and `finalChannel` when using `AttributeMagnitudeEvaluatedUpToChannel` type.
