# Custom Calculators System

The Custom Calculators system in Forge enables developers to implement complex, dynamic calculations for effect modifiers. These calculators provide a powerful way to create game-specific logic that goes beyond the built-in modifier types.

## Core Concepts

At the foundation of the system is the `CustomCalculator` abstract class:

```csharp
public abstract class CustomCalculator
{
    public List<AttributeCaptureDefinition> AttributesToCapture { get; } = [];
    public Dictionary<StringKey, object> CustomCueParameters { get; } = [];

    protected static int CaptureAttributeMagnitude(
        AttributeCaptureDefinition capturedAttribute,
        Effect effect,
        IForgeEntity? target);

    // Additional implementation...
}
```

This base class provides:
- Management of attribute captures for accessing attribute values during calculations
- Custom parameters that can be passed to cues when effects are applied
- Helper methods for retrieving attribute values from targets or sources

Forge offers two primary calculator types that inherit from CustomCalculator:

1. **CustomModifierMagnitudeCalculator**: For modifying a single attribute with complex logic. Returns a single float value.
2. **CustomExecution**: For modifying multiple attributes in a coordinated way. Returns an array of ModifierEvaluatedData.

## Key Components

### Attribute Capture

The `AttributeCaptureDefinition` struct is central to retrieving attribute values:

```csharp
public readonly struct AttributeCaptureDefinition(
    StringKey attribute,
    AttributeCaptureSource source,
    bool snapshot = true)
{
    public StringKey Attribute { get; }
    public AttributeCaptureSource Source { get; }
    public bool Snapshot { get; }

    public readonly bool TryGetAttribute(IForgeEntity? source, [NotNullWhen(true)] out EntityAttribute? attribute);
}
```

Parameters:
- `attribute`: The key of the attribute to capture (e.g., "CombatAttributeSet.CurrentHealth")
- `source`: Where to capture from either `AttributeCaptureSource.Source` (the effect owner) or `AttributeCaptureSource.Target` (the effect target)
- `snapshot`: If true, captures the value once when the effect is applied; if false, continuously updates when the source attribute changes

To use attribute capture properly:

1. Define attribute capture definitions as properties in your calculator class
2. Register them in the `AttributesToCapture` list in the constructor
3. Use `CaptureAttributeMagnitude` to safely retrieve values during calculation

```csharp
// Example of proper attribute capture setup
public class MyCalculator : CustomModifierMagnitudeCalculator
{
    // 1. Define attribute captures as properties
    public AttributeCaptureDefinition SourceHealth { get; }
    public AttributeCaptureDefinition TargetArmor { get; }

    public MyCalculator()
    {
        // 2. Initialize attribute definitions
        SourceHealth = new AttributeCaptureDefinition(
            "CombatAttributeSet.CurrentHealth",
            AttributeCaptureSource.Source,
            snapshot: false);

        TargetArmor = new AttributeCaptureDefinition(
            "DefenseAttributeSet.Armor",
            AttributeCaptureSource.Target,
            snapshot: true);

        // 3. Register them for capture
        AttributesToCapture.Add(SourceHealth);
        AttributesToCapture.Add(TargetArmor);
    }

    public override float CalculateBaseMagnitude(Effect effect, IForgeEntity target)
    {
        // 4. Use CaptureAttributeMagnitude to safely get values
        int health = CaptureAttributeMagnitude(SourceHealth, effect, target);
        int armor = CaptureAttributeMagnitude(TargetArmor, effect, target);

        // Your calculation logic...
        return health * (1.0f - (armor / 200.0f));
    }
}
```

The `CaptureAttributeMagnitude` method:
- Safely retrieves attribute values based on the capture definition
- Returns 0 if the attribute doesn't exist (avoids null reference exceptions)
- Handles attribute lookup from the correct entity (source or target)
- Returns the current value of the attribute

Even when not using non-snapshot functionality, it's recommended to follow this pattern to ensure consistent and safe attribute access.

### ModifierEvaluatedData

The `ModifierEvaluatedData` struct represents the complete data needed to apply a modifier to an attribute:

```csharp
public readonly struct ModifierEvaluatedData
{
    public EntityAttribute Attribute { get; }
    public ModifierOperation ModifierOperation { get; }
    public float Magnitude { get; }
    public int Channel { get; }
    public AttributeOverride? AttributeOverride { get; }

    public ModifierEvaluatedData(
        EntityAttribute attribute,
        ModifierOperation modifierOperation,
        float magnitude,
        int channel)
    {
        // Implementation...
    }
}
```

This struct contains:
- **Attribute**: The target EntityAttribute to be modified
- **ModifierOperation**: The operation type (FlatBonus, PercentBonus, or Override)
- **Magnitude**: The calculated value to be applied
- **Channel**: The attribute channel to apply the modifier to
- **AttributeOverride**: Special override data (only used with ModifierOperation.Override)

`ModifierEvaluatedData` is particularly important for `CustomExecution` implementers, as you'll be creating these objects directly to specify what attributes to modify and how.

To create a `ModifierEvaluatedData` instance in your custom execution:

```csharp
// Direct creation of ModifierEvaluatedData
new ModifierEvaluatedData(
    attributeInstance,     // An EntityAttribute instance
    ModifierOperation.FlatBonus,
    calculatedValue,       // Your calculated float value
    0                      // Channel to apply to
)
```

### Custom Cue Parameters

The `CustomCueParameters` dictionary allows calculators to pass additional data to the cues system:

```csharp
public Dictionary<StringKey, object> CustomCueParameters { get; } = [];
```

Custom cue parameters enable:
- Passing calculation results to visual/audio effects
- Providing context about how the calculation was performed
- Enabling dynamic cue behavior based on calculator results

Usage:
1. Add parameters in the constructor for default values
2. Update parameters during calculation with dynamic values
3. The parameters are automatically passed to the cues system

```csharp
public class DamageCalculator : CustomModifierMagnitudeCalculator
{
    public AttributeCaptureDefinition AttackerStrength { get; }

    public DamageCalculator()
    {
        AttackerStrength = new AttributeCaptureDefinition(
            "StatAttributeSet.Strength",
            AttributeCaptureSource.Source,
            snapshot: true);

        AttributesToCapture.Add(AttackerStrength);

        // Set default cue parameters
        CustomCueParameters.Add("damage.type", "physical");
        CustomCueParameters.Add("attack.critical", false);
    }

    public override float CalculateBaseMagnitude(Effect effect, IForgeEntity target)
    {
        int strength = CaptureAttributeMagnitude(AttackerStrength, effect, target);

        // Check for critical hit
        bool isCritical = Random.NextDouble() < 0.2;
        float damage = strength * (isCritical ? 2.0f : 1.0f);

        // Update cue parameters with dynamic values
        CustomCueParameters["attack.critical"] = isCritical;
        CustomCueParameters["damage.amount"] = damage;

        return -damage;  // Negative for damage
    }
}
```

These parameters can then be used by the cues system to spawn appropriate visual effects, play sounds, or trigger animations based on the calculation results.

## CustomModifierMagnitudeCalculator

Use this calculator type when you need a single modifier value that:
- Depends on multiple source attributes
- Requires complex game-specific logic
- Needs access to additional game state information

The key distinction is that `CustomModifierMagnitudeCalculator` only returns a single float value, which is used to modify a single attribute specified in the effect's modifier.

### Implementation

To create a custom magnitude calculator:

```csharp
public class MyDamageCalculator : CustomModifierMagnitudeCalculator
{
    // Define which attributes to capture
    public AttributeCaptureDefinition StrengthAttribute { get; }
    public AttributeCaptureDefinition AgilityAttribute { get; }

    public MyDamageCalculator()
    {
        // Capture source's Strength and Agility attributes (non-snapshot, will update)
        StrengthAttribute = new AttributeCaptureDefinition(
            "StatAttributeSet.Strength",
            AttributeCaptureSource.Source,
            snapshot: false);

        AgilityAttribute = new AttributeCaptureDefinition(
            "StatAttributeSet.Agility",
            AttributeCaptureSource.Source,
            snapshot: false);

        // Register attributes for capture
        AttributesToCapture.Add(StrengthAttribute);
        AttributesToCapture.Add(AgilityAttribute);

        // Add custom parameters for cues
        CustomCueParameters.Add("damage.type", "physical");
    }

    public override float CalculateBaseMagnitude(Effect effect, IForgeEntity target)
    {
        // Get attribute values
        int strength = CaptureAttributeMagnitude(StrengthAttribute, effect, target);
        int agility = CaptureAttributeMagnitude(AgilityAttribute, effect, target);

        // Custom calculation logic
        float baseDamage = strength * 0.7f + agility * 0.3f;

        // Add game-specific conditions
        if (target.Tags.Has("status.vulnerable"))
        {
            baseDamage *= 1.5f;
        }

        return baseDamage;
    }
}
```

### Using with Modifiers

Once defined, you can use your custom calculator in a modifier:

```csharp
// Create an effect that applies calculated damage
var damageEffect = new EffectData(
    "Physical Attack",
    new[] {
        new Modifier(
            "CombatAttributeSet.CurrentHealth",
            ModifierOperation.FlatBonus,
            new ModifierMagnitude(
                MagnitudeCalculationType.CustomCalculatorClass,
                customCalculationBasedFloat: new CustomCalculationBasedFloat(
                    new MyDamageCalculator(),           // Your custom calculator
                    new ScalableFloat(1.0f),            // Coefficient
                    new ScalableFloat(0.0f),            // PreMultiply additive
                    new ScalableFloat(-5.0f)            // PostMultiply additive (negative for damage)
                )
            )
        )
    },
    new DurationData(DurationType.Instant),
    null,
    null
);
```

### Processing Flow

When a `CustomModifierMagnitudeCalculator` is used:

1. The effect system gathers all attributes specified in `AttributesToCapture`
2. When the effect is applied, `CalculateBaseMagnitude()` is called
3. The returned value is processed through the formula:
   ```
   finalValue = (coefficient * (calculatedMagnitude + preMultiply)) + postMultiply
   ```
4. If a lookup curve is provided, the value is mapped through that curve
5. The final value is used as the magnitude for the modifier

## CustomExecution

Use this calculator type when you need to:
- Modify multiple attributes with a single calculation
- Create coordinated changes across different attributes
- Implement complex game systems like combo effects or resource conversions

Unlike `CustomModifierMagnitudeCalculator` which returns a single float value to modify one attribute, `CustomExecution` returns an array of `ModifierEvaluatedData` objects that can modify multiple attributes with different operations and magnitudes. It can also modify attributes from different entities simultaneously (both the target and the source).

### Implementation

To create a custom execution calculator:

```csharp
public class ElementalReactionExecution : CustomExecution
{
    // Define attributes to capture and modify
    public AttributeCaptureDefinition TargetFireResist { get; }
    public AttributeCaptureDefinition TargetWaterResist { get; }
    public AttributeCaptureDefinition TargetHealth { get; }
    public AttributeCaptureDefinition SourceSpellPower { get; }

    public ElementalReactionExecution()
    {
        // Capture target resistances and source spell power
        TargetFireResist = new AttributeCaptureDefinition(
            "ResistAttributeSet.FireResistance",
            AttributeCaptureSource.Target,
            snapshot: false);

        TargetWaterResist = new AttributeCaptureDefinition(
            "ResistAttributeSet.WaterResistance",
            AttributeCaptureSource.Target,
            snapshot: false);

        TargetHealth = new AttributeCaptureDefinition(
            "CombatAttributeSet.CurrentHealth",
            AttributeCaptureSource.Target,
            snapshot: false);

        SourceSpellPower = new AttributeCaptureDefinition(
            "StatAttributeSet.SpellPower",
            AttributeCaptureSource.Source,
            snapshot: true);

        // Register attributes for capture
        AttributesToCapture.Add(TargetFireResist);
        AttributesToCapture.Add(TargetWaterResist);
        AttributesToCapture.Add(TargetHealth);
        AttributesToCapture.Add(SourceSpellPower);

        // Add custom parameters for cues
        CustomCueParameters.Add("reaction.type", "steam");
    }

    public override ModifierEvaluatedData[] EvaluateExecution(Effect effect, IForgeEntity target)
    {
        var results = new List<ModifierEvaluatedData>();

        // Get attribute values
        int fireResist = CaptureAttributeMagnitude(TargetFireResist, effect, target);
        int waterResist = CaptureAttributeMagnitude(TargetWaterResist, effect, target);
        int spellPower = CaptureAttributeMagnitude(SourceSpellPower, effect, target);

        // Calculate steam reaction damage
        float steamDamage = spellPower * 0.5f * (2.0f - (fireResist + waterResist) / 200.0f);

        // Apply health damage if attribute exists
        if (TargetHealth.TryGetAttribute(target, out EntityAttribute? healthAttribute))
        {
            results.Add(new ModifierEvaluatedData(
                healthAttribute,
                ModifierOperation.FlatBonus,
                -steamDamage,  // Negative for damage
                channel: 0
            ));
        }

        // Apply temporary resistance reduction if attributes exist
        if (TargetFireResist.TryGetAttribute(target, out EntityAttribute? fireResistAttribute))
        {
            results.Add(new ModifierEvaluatedData(
                fireResistAttribute,
                ModifierOperation.FlatBonus,
                -10,  // Reduce fire resistance
                channel: 0
            ));
        }

        if (TargetWaterResist.TryGetAttribute(target, out EntityAttribute? waterResistAttribute))
        {
            results.Add(new ModifierEvaluatedData(
                waterResistAttribute,
                ModifierOperation.FlatBonus,
                -10,  // Reduce water resistance
                channel: 0
            ));
        }

        // Update custom cue parameters with calculated values
        CustomCueParameters["reaction.damage"] = steamDamage;

        return results.ToArray();
    }
}
```

### Using with Effects

Once defined, you can use your custom execution in an effect:

```csharp
// Create an effect that applies the elemental reaction
var steamReactionEffect = new EffectData(
    "Steam Reaction",
    [],  // No standard modifiers, all handled by custom execution
    new DurationData(DurationType.Instant),
    null,
    null,
    customExecutions: new[] {
        new ElementalReactionExecution()
    }
);
```

### Processing Flow

When a `CustomExecution` is used:

1. The effect system gathers all attributes specified in `AttributesToCapture`
2. When the effect is applied, `EvaluateExecution()` is called
3. The method returns an array of `ModifierEvaluatedData` objects
4. Each modifier is applied to its target attribute with its specified operation, magnitude, and channel
5. Any custom cue parameters are passed to the cues system

## Advanced Techniques

### Combining with Other Systems

Custom calculators can integrate with other systems in your game:

```csharp
public class QuestDamageCalculator : CustomModifierMagnitudeCalculator
{
    private readonly QuestManager _questManager;

    public AttributeCaptureDefinition BaseDamage { get; }

    public QuestDamageCalculator(QuestManager questManager)
    {
        _questManager = questManager;
        BaseDamage = new AttributeCaptureDefinition(
            "CombatAttributeSet.BaseDamage",
            AttributeCaptureSource.Source,
            snapshot: true);

        AttributesToCapture.Add(BaseDamage);
    }

    public override float CalculateBaseMagnitude(Effect effect, IForgeEntity target)
    {
        float baseDamage = CaptureAttributeMagnitude(BaseDamage, effect, target);

        // Check if target is related to an active quest
        if (target is IQuestTarget questTarget && _questManager.IsTargetForActiveQuest(questTarget.QuestTargetId))
        {
            baseDamage *= 1.5f;  // 50% bonus damage to quest targets
            CustomCueParameters["quest.target_bonus"] = true;
        }

        return -baseDamage;  // Negative for damage
    }
}
```

### Event-Driven Updates

Custom calculators can subscribe to game events for even more dynamic behavior:

```csharp
public class ComboAttackExecution : CustomExecution
{
    private readonly ComboSystem _comboSystem;

    public AttributeCaptureDefinition TargetHealth { get; }
    public AttributeCaptureDefinition AttackerStrength { get; }

    public ComboAttackExecution(ComboSystem comboSystem)
    {
        _comboSystem = comboSystem;

        TargetHealth = new AttributeCaptureDefinition(
            "CombatAttributeSet.CurrentHealth",
            AttributeCaptureSource.Target,
            snapshot: false);

        AttackerStrength = new AttributeCaptureDefinition(
            "StatAttributeSet.Strength",
            AttributeCaptureSource.Source,
            snapshot: true);

        AttributesToCapture.Add(TargetHealth);
        AttributesToCapture.Add(AttackerStrength);
    }

    public override ModifierEvaluatedData[] EvaluateExecution(Effect effect, IForgeEntity target)
    {
        var results = new List<ModifierEvaluatedData>();

        int strength = CaptureAttributeMagnitude(AttackerStrength, effect, target);
        int comboCount = _comboSystem.GetCurrentComboCount(effect.Ownership.Owner);

        // Calculate combo damage
        float baseDamage = strength * 0.8f;
        float comboDamage = baseDamage * (1 + comboCount * 0.2f);

        // Apply health damage
        if (TargetHealth.TryGetAttribute(target, out EntityAttribute? healthAttribute))
        {
            results.Add(new ModifierEvaluatedData(
                healthAttribute,
                ModifierOperation.FlatBonus,
                -comboDamage,  // Negative for damage
                channel: 0
            ));
        }

        // Add combo to cue parameters
        CustomCueParameters["combat.combo_count"] = comboCount;
        CustomCueParameters["combat.combo_damage"] = comboDamage;

        // Increment combo counter
        _comboSystem.IncrementCombo(effect.Ownership.Owner);

        return results.ToArray();
    }
}
```

## Debugging Custom Calculators

When debugging issues with custom calculators:

1. **Log Input Values**: Verify that attribute captures return expected values
2. **Check Attribute Names**: Ensure attribute keys match exactly what's in your game
3. **Test with Simple Formulas**: Start with simple calculations and build up complexity
4. **Verify Processing Order**: Remember that snapshots happen at effect application time
5. **Test Edge Cases**: Handle null values, zero values, and extremely large/small values

## Best Practices

### When to Use CustomCalculationBasedFloat vs CustomExecution

- **Use CustomCalculationBasedFloat when**:
  - You need to modify a single attribute
  - Your calculation depends on multiple other attributes
  - You want the standard modifier framework to handle application

- **Use CustomExecution when**:
  - You need to modify multiple attributes at once
  - You need coordinated changes across different attributes
  - You want complete control over how modifiers are created and applied

### Attribute Capture Considerations

1. **Snapshot vs Live Updates**:
   - `snapshot: true`: Captures the attribute value once when the effect is applied
   - `snapshot: false`: Continuously updates when the source attribute changes

2. **Source vs Target**:
   - Choose `AttributeCaptureSource.Source` for values from the effect owner
   - Choose `AttributeCaptureSource.Target` for values from the effect target

3. **Error Handling**:
   - Always handle cases where attributes might not exist
   - Use `TryGetAttribute()` to safely access attributes

### Performance Tips

1. **Minimize Attribute Dependencies**: Each non-snapshot attribute creates a dependency that triggers recalculations

2. **Optimize Calculations**: Custom calculators can run frequently, keep calculations efficient

3. **Cache Complex Values**: If your calculator performs expensive operations, consider caching results

4. **Batch Related Changes**: Use CustomExecution to apply multiple changes at once instead of multiple effects
