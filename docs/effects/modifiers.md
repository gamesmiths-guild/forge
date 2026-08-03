# Effect Modifiers

Effect Modifiers in Forge provide a flexible way to modify entity [attributes](../attributes.md) through [effects](README.md). Modifiers define how an effect changes attribute values, with support for different operation types and magnitude calculations.

For a practical guide on using modifiers, see the [Quick Start Guide](../quick-start.md).

## Core Concepts

At its core, a modifier represents a mathematical operation that changes the value of a specific attribute on a target entity. Each modifier consists of:

```csharp
public readonly record struct Modifier(
    StringKey Attribute,
    ModifierOperation Operation,
    ModifierMagnitude Magnitude,
    int Channel = 0,
    AggregationMode AggregationMode = AggregationMode.Sum)
{
    // Implementation...
}
```

> Most of the magnitude types below are **positional records**, so their parameter names are PascalCase. When you pass them as named arguments, write `Snapshot: false` and `Coefficient: ...`, not `snapshot:` / `coefficient:`. `ModifierMagnitude`, `EffectData` and `AbilityData` declare explicit constructors instead, so those take the usual camelCase names.

- **Attribute**: The target attribute to modify (using a string key).
- **Operation**: How the modifier affects the attribute (flat, percentage, or override).
- **Magnitude**: How to calculate the value that will be applied.
- **Channel**: Which attribute [channel](../attributes.md#attribute-channels) to affect (defaults to 0).
- **AggregationMode**: How this modifier combines with the other modifiers of its group — all of them summed (the default), or only the strongest one (see [Modifier Aggregation](#modifier-aggregation)).

## Modifier Operations

The `ModifierOperation` enum defines how a modifier changes an attribute's value:

```csharp
public enum ModifierOperation : byte
{
    FlatBonus = 0,    // Add or subtract a flat value
    PercentBonus = 1, // Add or subtract a percentage of the current value
    Override = 2      // Completely replace the current value
}
```

### Operation Types

- **FlatBonus**: Adds (or subtracts) a fixed value to the attribute.
  - Example: `+5 Attack Power`, `-10 Movement Speed`.
  - Calculation: `CurrentValue + FlatValue`.
  - Multiple flat bonuses are summed together before being applied, unless they opt into another [aggregation mode](#modifier-aggregation).

- **PercentBonus**: Adds (or subtracts) a percentage modifier that is applied after flat bonuses.
  - Example: `+25% Critical Chance`, `-15% Damage Taken`.
  - Formula: `(BaseValue + FlatBonus) * (1 + PercentBonus)`.
  - Multiple percentage bonuses are added together, not multiplied.
  - Example: A +10% and a +20% bonus results in a total of +30% (1 + 0.1 + 0.2 = 1.3).
  - Example: A +10% and a -5% modifier results in a +5% total bonus (1 + 0.1 - 0.05 = 1.05).
  - This additive approach ensures consistent results regardless of application order.
  - Percentages that should compete instead of adding up use another [aggregation mode](#modifier-aggregation).

- **Override**: Replaces the attribute's value entirely.
  - Example: `Set Max Health to 100`.
  - Calculation: `NewValue` (ignores current value entirely).
  - There is no priority system for overrides: the **most recently applied** override on a channel wins (unless it opts into [aggregation](#aggregating-overrides)).
  - Overrides are tracked as a stack per channel. When the active override is removed, the previously applied override on that channel (if one is still active) takes over again; the attribute only stops being overridden once every override on that channel is gone.

## Evaluation Order

Evaluation happens per [channel](../attributes.md#attribute-channels), and the result of each channel feeds the next. Within a single Channel:

1. First, the channel's override is checked. If an override is active, it **replaces** the value entering the channel and the channel's flat and percentage modifiers are skipped entirely.
2. If no override is active on the channel, flat bonuses are [aggregated](#modifier-aggregation) and added.
3. Finally, percentage modifiers are aggregated and applied to the result.

The final value is then clamped between the attribute's `Min` and `Max`.

An override only short-circuits the channel it belongs to. Modifiers in later channels still apply on top of the overridden value, which is how you compose "set to X, then apply a penalty" without a dedicated primitive.

## Modifier Aggregation

By default every modifier affecting an attribute contributes: flat bonuses are summed, percentage bonuses are summed into a single multiplier. `AggregationMode` changes that, so a set of competing modifiers contributes only its **strongest** value:

```csharp
public enum AggregationMode : byte
{
    Sum = 0, // Every modifier in the group contributes, summed together
    Max = 1, // Only the highest valued modifier in the group contributes
    Min = 2  // Only the lowest valued modifier in the group contributes
}
```

Modifiers are grouped by **attribute, channel, operation and aggregation mode**. Each group contributes exactly one value, and those contributions are then combined the usual way:

```
ChannelFlat    = sum(Sum group) + max(Max group) + min(Min group)
ChannelPercent = 1 + sum(Sum group) + max(Max group) + min(Min group)
```

An empty group contributes nothing, so `Sum` behaves exactly as it always did and mixing modes is well defined: a `Sum` bonus, a strongest-only buff and a strongest-only slow can all be active on the same attribute at the same time.

### Strongest Wins

This is the "only the biggest movement speed buff applies" family of mechanics — ubiquitous in ARPGs and MOBAs, and awkward to build any other way:

```csharp
// Every movement speed buff in the game is authored like this. Only the strongest is ever active,
// and when it expires the next strongest takes over on the same frame.
new Modifier(
    "MovementAttributeSet.Speed",
    ModifierOperation.PercentBonus,
    new ModifierMagnitude(
        MagnitudeCalculationType.ScalableFloat,
        scalableFloatMagnitude: new ScalableFloat(0.3f)),
    Channel: 0,
    AggregationMode: AggregationMode.Max)
```

`Min` is the same mechanic in the other direction — "only the strongest slow applies":

```csharp
// A -30% slow and a -15% slow are both active; only the -30% one is felt.
new Modifier(
    "MovementAttributeSet.Speed",
    ModifierOperation.PercentBonus,
    new ModifierMagnitude(
        MagnitudeCalculationType.ScalableFloat,
        scalableFloatMagnitude: new ScalableFloat(-0.3f)),
    Channel: 0,
    AggregationMode: AggregationMode.Min)
```

Note that `Max` and `Min` compare **signed values**, not magnitudes: `Max` picks the highest number, so in a group of negative modifiers it selects the *weakest* penalty. Use `Max` for bonuses and `Min` for penalties.

Nothing else changes about the effects themselves. They stack, expire, get dispelled, and re-evaluate exactly as before — the group is simply recomputed whenever a modifier is added, removed or re-evaluated, so removal of the current winner immediately promotes the runner-up.

### Aggregating Overrides

Overrides can only ever produce a single value per channel, so aggregation arbitrates between them instead of combining them. The **most recently applied** override on the channel decides the policy:

- If it uses `Sum` (the default), it wins outright — the usual last-applied-wins behavior.
- If it uses `Max` or `Min`, the channel goes to the extreme override **of that same mode**.

So a group of `Min` overrides behaves like "the most restrictive one wins" (a root setting speed to `0` beats a snare setting it to `10`), while a plain override applied afterwards still takes precedence over the whole group for as long as it's active.

### When Aggregation Doesn't Apply

Aggregation only applies to modifiers applied by **active effects** — that is, non-instant, non-periodic effects. Instant and periodic effects execute their modifiers against the attribute's `BaseValue` as a permanent change, so there's no group of active modifiers to arbitrate between. Configuring an aggregation mode other than `Sum` on such an effect is rejected by validation rather than being silently ignored.

## Magnitude Calculation

The `ModifierMagnitude` struct determines how the magnitude of a modifier is calculated. This value is what gets used in the operation to modify the target attribute.

```csharp
public readonly record struct ModifierMagnitude
{
    public readonly MagnitudeCalculationType MagnitudeCalculationType { get; }
    public readonly ScalableFloat? ScalableFloatMagnitude { get; }
    public readonly AttributeBasedFloat? AttributeBasedFloat { get; }
    public readonly CustomCalculationBasedFloat? CustomCalculationBasedFloat { get; }
    public readonly SetByCallerFloat? SetByCallerFloat { get; }

    // Constructor ensures only the appropriate property is set based on the calculation type
    public ModifierMagnitude(
        MagnitudeCalculationType magnitudeCalculationType,
        ScalableFloat? scalableFloatMagnitude = null,
        AttributeBasedFloat? attributeBasedFloat = null,
        CustomCalculationBasedFloat? customCalculationBasedFloat = null,
        SetByCallerFloat? setByCallerFloat = null)
    {
        // Implementation with validation...
    }
}
```

The constructor performs validation to ensure that only the appropriate property is provided for the selected calculation type. For example, if you choose `MagnitudeCalculationType.ScalableFloat`, you must provide a non-null `scalableFloatMagnitude` parameter and all others must be null.

### Magnitude Calculation Types

```csharp
public enum MagnitudeCalculationType : byte
{
    ScalableFloat = 0,         // Fixed value that scales with level
    AttributeBased = 1,        // Based on another attribute's value
    CustomCalculatorClass = 2, // Custom calculation logic
    SetByCaller = 3            // Value provided externally
}
```

### ScalableFloat

Fixed values that can scale with effect level:

```csharp
// Damage that increases with level: 10 at level 1, 20 at level 5
var damageModifier = new Modifier(
    "CombatAttributeSet.Health",
    ModifierOperation.FlatBonus,
    new ModifierMagnitude(
        MagnitudeCalculationType.ScalableFloat,
        scalableFloatMagnitude: new ScalableFloat(-10.0f, new Curve([ // Negative for damage
            new CurveKey(1, 1.0f),
            new CurveKey(5, 2.0f),
            new CurveKey(10, 3.0f)
        ]))
    )
);
```

The `ScalableFloat` has two key properties:

- **BaseValue**: The base magnitude value.
- **ScalingCurve**: Optional curve that scales the base value by the effect's level.

When evaluated, the formula is: `BaseValue * ScalingCurve.Evaluate(level)`, or just `BaseValue` if no curve is provided.

### AttributeBasedFloat

`AttributeBasedFloat` computes its magnitude from another attribute (including snapshot logic for effect context).

```csharp
public readonly record struct AttributeBasedFloat(
    AttributeCaptureDefinition BackingAttribute,
    AttributeCalculationType AttributeCalculationType,
    ScalableFloat Coefficient,
    ScalableFloat PreMultiplyAdditiveValue,
    ScalableFloat PostMultiplyAdditiveValue,
    int FinalChannel = 0,
    ICurve? LookupCurve = null)
{
    // Implementation...
}
```

The magnitude is calculated using this formula:
```
finalValue = (coefficient * (attributeMagnitude + preMultiply)) + postMultiply
```

If a `lookupCurve` is provided, the result is further processed:
```
finalValue = lookupCurve.Evaluate(finalValue)
```

Properties in detail:

- **BackingAttribute**: Defines which attribute to capture and from where (source or target).
- **AttributeCalculationType**: Determines which value from the attribute to use (current value, base value, etc.).
- **Coefficient**: A scaling factor (possibly level-scaled) that multiplies the captured attribute value.
- **PreMultiplyAdditiveValue**: A value added to the attribute magnitude before multiplication.
- **PostMultiplyAdditiveValue**: A value added after the multiplication.
- **FinalChannel**: Only used with `AttributeCalculationType.MagnitudeEvaluatedUpToChannel`.
- **LookupCurve**: Optional curve used to remap the final calculated value.

Example:

```csharp
// Bonus damage equal to 50% of the source's Strength, plus 5 base damage
var strengthBasedDamage = new Modifier(
    "CombatAttributeSet.DamageOutput",
    ModifierOperation.FlatBonus,
    new ModifierMagnitude(
        MagnitudeCalculationType.AttributeBased,
        attributeBasedFloat: new AttributeBasedFloat(
            new AttributeCaptureDefinition(
                "StatAttributeSet.Strength",
                AttributeCaptureSource.Owner
            ),
            AttributeCalculationType.CurrentValue,
            new ScalableFloat(0.5f),        // Coefficient: 50% of strength
            new ScalableFloat(0),           // PreMultiply: no additional value
            new ScalableFloat(5)            // PostMultiply: +5 flat bonus
        )
    )
);
```

This creates a damage modifier that adds `(0.5 * Strength) + 5` to the target's damage output.

The `AttributeCalculationType` enum provides various ways to access different aspects of an attribute:

```csharp
public enum AttributeCalculationType : byte
{
    CurrentValue = 0,                     // Use current value (base + all modifiers)
    BaseValue = 1,                        // Use only base value
    Modifier = 2,                         // Use total modifier value
    Overflow = 3,                         // Use overflow value (exceeding min/max)
    ValidModifier = 4,                    // Use effective modifier (excluding overflow)
    Min = 5,                              // Use minimum value constraint
    Max = 6,                              // Use maximum value constraint
    MagnitudeEvaluatedUpToChannel = 7     // Use value calculated up to a specific channel
}
```

The attribute can be captured from different sources:

```csharp
public enum AttributeCaptureSource : byte
{
    Target = 0,  // The entity receiving the effect
    Source = 1,   // EffectOwnership.Source — what actually caused the effect
    Owner = 2   // EffectOwnership.Owner — who triggered the action that caused the effect
}
```

The member names line up with [`EffectApplicationTarget`](components/additional-effects-effect-component.md) and `OwnershipEntity`: each member that names an ownership entity resolves to the matching `EffectOwnership` property.

`Owner` is the usual choice — "damage scales with the caster's Strength". Reach for `Source` when the object that caused the effect is itself an entity with meaningful stats:

```csharp
// Damage scaling off the weapon's own attribute rather than the wielder's
new Modifier(
    "CombatAttributeSet.CurrentHealth",
    ModifierOperation.FlatBonus,
    new ModifierMagnitude(
        MagnitudeCalculationType.AttributeBased,
        attributeBasedFloat: new AttributeBasedFloat(
            new AttributeCaptureDefinition(
                "WeaponAttributeSet.Damage",
                AttributeCaptureSource.Source,
                Snapshot: false),          // an enchantment mid-fight changes the magnitude
            AttributeCalculationType.CurrentValue,
            new ScalableFloat(-1),
            new ScalableFloat(0),
            new ScalableFloat(0))))
```

This is what makes weapons, turrets, traps and summons work as stat carriers in their own right: apply the effect with `new EffectOwnership(wielder, weapon)` and the wielder still gets kill credit and tag attribution while the magnitude comes off the weapon. It pairs with [`SourceAttributeRequirementsEffectComponent`](components/source-attribute-requirements-effect-component.md), which gates application on the same entity.

An effect whose `Source` is `null`, or whose source lacks the captured attribute, captures **zero** — the capture never silently falls back to another entity, since that would produce a plausible but wrong magnitude.

The `AttributeCaptureDefinition` struct controls how attributes are captured:

```csharp
public readonly record struct AttributeCaptureDefinition(
    StringKey Attribute,
    AttributeCaptureSource Source,
    bool Snapshot = true)
{
    // Implementation...
}
```

- **Attribute**: Which attribute to capture.
- **Source**: Which entity to capture from — the effect's `Owner`, its `Source`, or its `Target`.
- **Snapshot**: If true, captures the value at the time of effect application; if false, continuously updates as the captured attribute changes.

### CustomCalculationBasedFloat

For complex calculations requiring custom logic, see the [Custom Calculators documentation](calculators.md).

```csharp
public readonly record struct CustomCalculationBasedFloat(
    CustomModifierMagnitudeCalculator MagnitudeCalculatorClass,
    ScalableFloat Coefficient,
    ScalableFloat PreMultiplyAdditiveValue,
    ScalableFloat PostMultiplyAdditiveValue,
    ICurve? LookupCurve = null)
{
    // Implementation...
}
```

The magnitude is calculated using the same formula as `AttributeBasedFloat`, but with a custom calculator providing the base magnitude:

```
baseMagnitude = magnitudeCalculatorClass.CalculateBaseMagnitude(effect, target, effectEvaluatedData)
finalValue = (coefficient * (baseMagnitude + preMultiply)) + postMultiply
```

If a `lookupCurve` is provided:
```
finalValue = lookupCurve.Evaluate(finalValue)
```

Properties in detail:

- **MagnitudeCalculatorClass**: Your custom calculator class implementing `CustomModifierMagnitudeCalculator`.
- **Coefficient**: A scaling factor (possibly level-scaled) that multiplies the calculated magnitude.
- **PreMultiplyAdditiveValue**: A value added to the custom magnitude before multiplication.
- **PostMultiplyAdditiveValue**: A value added after the multiplication.
- **LookupCurve**: Optional curve used to remap the final calculated value.

`CustomCalculationBasedFloat` is especially useful when:

- You need to modify one attribute based on multiple other attributes.
- Your calculation needs complex game-specific logic.
- You need access to additional game state information.

Use `AttributeBasedFloat` when modifying an attribute based on a single other attribute, but switch to `CustomCalculationBasedFloat` when you need to consider multiple attributes in your calculation.

Note: If you need to modify multiple attributes in a single operation, you should use a `CustomExecution` instead. See the [Custom Calculators documentation](calculators.md) for more details.

Example:

```csharp
// Custom calculation that scales with missing health percentage
var missingHealthDamage = new Modifier(
    "CombatAttributeSet.CurrentHealth",
    ModifierOperation.FlatBonus,
    new ModifierMagnitude(
        MagnitudeCalculationType.CustomCalculatorClass,
        customCalculationBasedFloat: new CustomCalculationBasedFloat(
            new MissingHealthDamageCalculator(),    // Your custom calculator class
            new ScalableFloat(1.0f),                // Coefficient: full damage
            new ScalableFloat(0),                   // PreMultiply: no additional value
            new ScalableFloat(0),                   // PostMultiply: no additional value
            new Curve([                             // LookupCurve: exponential scaling
                new CurveKey(0.0f, 1.0f),           // At 0% missing health: normal damage
                new CurveKey(0.5f, 1.5f),           // At 50% missing health: 1.5x damage
                new CurveKey(1.0f, 3.0f)            // At 100% missing health: 3x damage
            ])
        )
    )
);
```

### SetByCallerFloat

`SetByCallerFloat` is a magnitude type that allows the caller to provide a custom value when applying an effect.

```csharp
public readonly record struct SetByCallerFloat(Tag Tag, bool Snapshot = true);
```

#### Tag

The `Tag` property is used as a key to look up the magnitude value that must be set before applying the effect.

#### Snapshot

The `Snapshot` parameter controls whether the provided value is captured at application time or evaluated dynamically for non-instant effects.

- When `Snapshot` is set to `true`, the value associated with the tag is captured when the effect is applied and remains fixed for the lifetime of the effect.
- When `Snapshot` is set to `false`, the effect always uses the latest value associated with the tag, allowing the magnitude to change if the caller updates the value after the effect has already been applied.

```csharp
// Magnitude will be set before the effect is applied
var variableDamageModifier = new Modifier(
    "CombatAttributeSet.CurrentHealth",
    ModifierOperation.FlatBonus,
    new ModifierMagnitude(
        MagnitudeCalculationType.SetByCaller,
        setByCallerFloat: new SetByCallerFloat(
            Tag.RequestTag(tagsManager, "damage.amount"),
            Snapshot: true 
        )
    )
);

var effectData = new EffectData("Variable Damage", new DurationData(DurationType.Instant), [variableDamageModifier]);
var effect = new Effect(effectData, new EffectOwnership(caster, caster));

// Set the caller-provided magnitude before applying the effect:
effect.SetSetByCallerMagnitude(Tag.RequestTag(tagsManager, "damage.amount"), 25.5f);
target.EffectsManager.ApplyEffect(effect);
```

Important notes about `SetByCallerFloat`:

- Values must be set on the `Effect` instance before it is applied.
- Values are identified by tags.
- It's recommended to use a consistent naming pattern for these tags (e.g., "magnitudes.parameter_name") similar to how cues are identified.

## Channel System

Modifiers can be applied to different "channels" of an attribute, allowing for more complex layered calculations beyond the default order (flat bonuses then percentage modifiers). For more details, see the [Attribute Channels documentation](../attributes.md#attribute-channels).

### How Channels Work

Each attribute has multiple calculation channels that are processed in sequence. The attribute value flows through each channel, with the result of each channel becoming the input to the next:

```
Channel 1:  (BaseValue + FlatMod1) * PercentMod1  →  Result1
Channel 2:  (Result1 + FlatMod2) * PercentMod2    →  Result2
Channel 3:  (Result2 + FlatMod3) * PercentMod3    →  FinalValue
```

### When to Use Channels

Channels are particularly useful for:

1. **Creating multi-step calculations** - For example, applying base bonuses in channel 0, then applying "increased/more" bonuses in channel 1.
2. **Categorizing modifier sources** - Such as permanent bonuses in channel 0, temporary buffs in channel 1, and debuffs in channel 2.
3. **Implementing compound calculations** - Like applying percentage bonuses, then applying flat bonuses on top of that result, then applying another percentage.

```csharp
// Example of a multi-stage calculation using channels
// Channel 0: Apply base damage from weapon (flat)
var weaponDamage = new Modifier(
    "CombatAttributeSet.DamageOutput",
    ModifierOperation.FlatBonus,
    new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, scalableFloatMagnitude: new ScalableFloat(20)),
    Channel: 0
);

// Channel 1: Apply skill damage bonus (percentage)
var skillDamageBonus = new Modifier(
    "CombatAttributeSet.DamageOutput",
    ModifierOperation.PercentBonus,
    new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, scalableFloatMagnitude: new ScalableFloat(0.5f)),
    Channel: 1
);

// Channel 2: Apply flat bonus from passive ability (flat bonus applied AFTER percentage from channel 1)
var passiveDamageBonus = new Modifier(
    "CombatAttributeSet.DamageOutput",
    ModifierOperation.FlatBonus,
    new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, scalableFloatMagnitude: new ScalableFloat(10)),
    Channel: 2
);

// Channel 3: Apply critical hit multiplier (percentage applied to the result of channels 0-2)
var criticalHitMultiplier = new Modifier(
    "CombatAttributeSet.DamageOutput",
    ModifierOperation.PercentBonus,
    new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, scalableFloatMagnitude: new ScalableFloat(1.0f)),
    Channel: 3
);
```

## Common Modifier Patterns

### Basic Stat Buff

```csharp
// +10 Strength
new Modifier(
    "StatAttributeSet.Strength",
    ModifierOperation.FlatBonus,
    new ModifierMagnitude(
        MagnitudeCalculationType.ScalableFloat,
        scalableFloatMagnitude: new ScalableFloat(10)
    )
)
```

### Percentage-based Buff

```csharp
// +20% Movement Speed
new Modifier(
    "MovementAttributeSet.Speed",
    ModifierOperation.PercentBonus,
    new ModifierMagnitude(
        MagnitudeCalculationType.ScalableFloat,
        scalableFloatMagnitude: new ScalableFloat(0.2f)
    )
)
```

### Damage Over Time

```csharp
// -5 Health (negative values for damage)
new Modifier(
    "CombatAttributeSet.CurrentHealth",
    ModifierOperation.FlatBonus,
    new ModifierMagnitude(
        MagnitudeCalculationType.ScalableFloat,
        scalableFloatMagnitude: new ScalableFloat(-5)
    )
)
```

### Stat-Based Buff

```csharp
// Add 30% of the caster's Intelligence to the target's Spell Power
new Modifier(
    "CombatAttributeSet.SpellPower",
    ModifierOperation.FlatBonus,
    new ModifierMagnitude(
        MagnitudeCalculationType.AttributeBased,
        attributeBasedFloat: new AttributeBasedFloat(
            new AttributeCaptureDefinition("StatAttributeSet.Intelligence", AttributeCaptureSource.Owner),
            AttributeCalculationType.CurrentValue,
            new ScalableFloat(0.3f),  // 30% of intelligence
            new ScalableFloat(0),
            new ScalableFloat(0)
        )
    )
)
```

### Override with Minimum Value

```csharp
// Set Movement Speed to 0 (stun effect)
new Modifier(
    "MovementAttributeSet.Speed",
    ModifierOperation.Override,
    new ModifierMagnitude(
        MagnitudeCalculationType.ScalableFloat,
        scalableFloatMagnitude: new ScalableFloat(0)
    )
)
```

## Best Practices

1. **Consider Operation Order**: Flat bonuses are typically applied before percentage bonuses; use channels to control this order.

2. **Be Careful with Overrides**: Override operations completely replace attribute values, so use them cautiously.

3. **Use Appropriate Magnitude Types**:
   - `ScalableFloat` for simple fixed values.
   - `AttributeBasedFloat` for dynamic values based on a single attribute.
   - `CustomCalculationBasedFloat` for complex logic involving multiple attributes.
   - `SetByCallerFloat` for runtime-determined values.

4. **Mind Your Channels**: Keep a consistent channel convention across your game to avoid confusion.

5. **Negative vs. Positive Values**: For effects like damage, decide whether to use negative values or handle the sign conversion elsewhere.

6. **Snapshot Considerations**: When using attribute-based magnitudes, consider whether you want a snapshot or a live value that updates when the source attribute changes.

7. **Balance Stack Interactions**: Consider how multiple modifiers will interact when they [stack](stacking.md) on the same attribute. Reach for `AggregationMode.Max`/`Min` when they should compete instead of adding up, and apply the mode consistently across every effect in that family — a single buff left on `Sum` silently bypasses the rule.

8. **Document Your Attribute Keys**: Maintain a central registry of attribute keys to avoid typos and inconsistencies.

9. **Test Edge Cases**: Verify behavior with extreme values, multiple stacking effects, and effect removal.
