# Effect Modifiers

Effect Modifiers in Forge provides a flexible way to modify entity [attributes](../attributes.md) through [effects](README.md). Modifiers define how an effect changes attribute values, with support for different operation types and magnitude calculations.

For a practical guide on using modifiers, see the [Quick Start Guide](../quick-start.md).

## Core Concepts

At its core, a modifier represents a mathematical operation that changes the value of a specific attribute on a target entity. Each modifier consists of:

```csharp
public readonly struct Modifier(
    StringKey attribute,
    ModifierOperation operation,
    ModifierMagnitude magnitude,
    int channel = 0)
{
    // Implementation...
}
```

- **Attribute**: The target attribute to modify (using a string key).
- **Operation**: How the modifier affects the attribute (flat, percentage, or override).
- **Magnitude**: How to calculate the value that will be applied.
- **Channel**: Which attribute [channel](../attributes.md#attribute-channels) to affect (defaults to 0).

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
  - Multiple flat bonuses are summed together before being applied.

- **PercentBonus**: Adds (or subtracts) a percentage modifier that is applied after flat bonuses.
  - Example: `+25% Critical Chance`, `-15% Damage Taken`.
  - Formula: `(BaseValue + FlatBonus) * (1 + PercentBonus)`.
  - Multiple percentage bonuses are added together, not multiplied.
  - Example: A +10% and a +20% bonus results in a total of +30% (1 + 0.1 + 0.2 = 1.3).
  - Example: A +10% and a -5% modifier results in a +5% total bonus (1 + 0.1 - 0.05 = 1.05).
  - This additive approach ensures consistent results regardless of application order.

- **Override**: Replaces the attribute's value entirely.
  - Example: `Set Max Health to 100`.
  - Calculation: `NewValue` (ignores current value entirely).
  - Overrides from higher priority sources take precedence.

## Evaluation Order

When calculating the final value of an attribute:

1. First, overrides are checked (highest priority override wins).
2. If no override exists, flat bonuses are summed and applied.
3. Finally, percentage modifiers are applied to the result.

This order can be customized using [Attribute Channels](../attributes.md#attribute-channels).

## Magnitude Calculation

The `ModifierMagnitude` struct determines how the magnitude of a modifier is calculated. This value is what gets used in the operation to modify the target attribute.

```csharp
public readonly struct ModifierMagnitude
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

Calculates magnitude based on another attribute's value using a powerful formula:

```csharp
public readonly struct AttributeBasedFloat(
    AttributeCaptureDefinition backingAttribute,
    AttributeCalculationType attributeCalculationType,
    ScalableFloat coefficient,
    ScalableFloat preMultiplyAdditiveValue,
    ScalableFloat postMultiplyAdditiveValue,
    int? finalChannel = null,
    ICurve? lookupCurve = null)
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
                AttributeCaptureSource.Source
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
    Source = 0,  // The entity that applied the effect
    Target = 1   // The entity receiving the effect
}
```

The `AttributeCaptureDefinition` struct controls how attributes are captured:

```csharp
public readonly struct AttributeCaptureDefinition(
    StringKey attribute,
    AttributeCaptureSource source,
    bool snapshot = true)
{
    // Implementation...
}
```

- **Attribute**: Which attribute to capture.
- **Source**: Whether to capture from the source or target entity.
- **Snapshot**: If true, captures the value at the time of effect application; if false, continuously updates as the source attribute changes.

### CustomCalculationBasedFloat

For complex calculations requiring custom logic, see the [Custom Calculators documentation](calculators.md).

```csharp
public readonly struct CustomCalculationBasedFloat(
    CustomModifierMagnitudeCalculator magnitudeCalculatorClass,
    ScalableFloat coefficient,
    ScalableFloat preMultiplyAdditiveValue,
    ScalableFloat postMultiplyAdditiveValue,
    ICurve? lookupCurve = null)
{
    // Implementation...
}
```

The magnitude is calculated using the same formula as `AttributeBasedFloat`, but with a custom calculator providing the base magnitude:

```
baseMagnitude = magnitudeCalculatorClass.CalculateBaseMagnitude()
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

Allows the effect's magnitude to be set externally before it's applied:

```csharp
public readonly struct SetByCallerFloat(Tag tag)
{
    // Implementation...
}
```

The `Tag` property is used as a key to look up the magnitude value that must be set before applying the effect:

```csharp
// Magnitude will be set before the effect is applied
var variableDamageModifier = new Modifier(
    "CombatAttributeSet.CurrentHealth",
    ModifierOperation.FlatBonus,
    new ModifierMagnitude(
        MagnitudeCalculationType.SetByCaller,
        setByCallerFloat: new SetByCallerFloat(
            Tag.RequestTag(tagsManager, "damage.amount")
        )
    )
);

var effectData = new EffectData("Variable Damage", new DurationData(DurationType.Instant), [variableDamageModifier]);
var effect = new Effect(effectData, new EffectOwnership(caster, caster));

// Before applying the effect:
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
    channel: 0
);

// Channel 1: Apply skill damage bonus (percentage)
var skillDamageBonus = new Modifier(
    "CombatAttributeSet.DamageOutput",
    ModifierOperation.PercentBonus,
    new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, scalableFloatMagnitude: new ScalableFloat(0.5f)),
    channel: 1
);

// Channel 2: Apply flat bonus from passive ability (flat bonus applied AFTER percentage from channel 1)
var passiveDamageBonus = new Modifier(
    "CombatAttributeSet.DamageOutput",
    ModifierOperation.FlatBonus,
    new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, scalableFloatMagnitude: new ScalableFloat(10)),
    channel: 2
);

// Channel 3: Apply critical hit multiplier (percentage applied to the result of channels 0-2)
var criticalHitMultiplier = new Modifier(
    "CombatAttributeSet.DamageOutput",
    ModifierOperation.PercentBonus,
    new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, scalableFloatMagnitude: new ScalableFloat(1.0f)),
    channel: 3
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
            new AttributeCaptureDefinition("StatAttributeSet.Intelligence", AttributeCaptureSource.Source),
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

7. **Balance Stack Interactions**: Consider how multiple modifiers will interact when they [stack](stacking.md) on the same attribute.

8. **Document Your Attribute Keys**: Maintain a central registry of attribute keys to avoid typos and inconsistencies.

9. **Test Edge Cases**: Verify behavior with extreme values, multiple stacking effects, and effect removal.
