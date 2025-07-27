# Attributes System

The Attributes system in Forge provides a robust framework for managing numeric properties of game entities. Attributes represent any quantifiable characteristic like health, strength, movement speed, or any other property that can be represented numerically.

For a practical guide on using attributes, see the [Quick Start Guide](quick-start.md).

## Core Concepts

### EntityAttribute

An `EntityAttribute` represents a single numeric property with constraints and modification tracking:

- **BaseValue**: The fundamental value before modifications.
- **CurrentValue**: The actual value after all modifications, constrained by Min/Max.
- **Min/Max**: The lower and upper bounds for the attribute.
- **Modifier**: The cumulative modification applied to the BaseValue.
- **Overflow**: Value that exceeds Min/Max constraints (useful for effects like shield overflow).
- **ValidModifier**: The effective modifier value that isn't causing overflow (Modifier - Overflow).

### AttributeSet

AttributeSets group related attributes together and can establish relationships between them:

```csharp
public class CombatAttributeSet : AttributeSet
{
    public EntityAttribute MaxHealth { get; }
    public EntityAttribute CurrentHealth { get; }
    public EntityAttribute AttackPower { get; }

    public CombatAttributeSet()
    {
        // Initialize attributes with (name, defaultValue, minValue, maxValue)
        MaxHealth = InitializeAttribute(nameof(MaxHealth), 100, 0, 1000);
        CurrentHealth = InitializeAttribute(nameof(CurrentHealth), 100, 0, MaxHealth.CurrentValue);
        AttackPower = InitializeAttribute(nameof(AttackPower), 10, 0, 100);
    }

    // Respond to attribute changes
    protected override void AttributeOnValueChanged(EntityAttribute attribute, int change)
    {
        if (attribute == MaxHealth)
        {
            // Update CurrentHealth's maximum when MaxHealth changes
            SetAttributeMaxValue(CurrentHealth, MaxHealth.CurrentValue);
        }
    }
}
```

### EntityAttributes

`EntityAttributes` is a container class that manages all AttributeSets for an entity and provides access to individual attributes:

```csharp
public class PlayerCharacter : IForgeEntity
{
    public EntityAttributes Attributes { get; }
    // Other IForgeEntity properties...

    public PlayerCharacter()
    {
        // Create attribute sets
        var combatStats = new CombatAttributeSet();
        var resourceStats = new ResourceAttributeSet();

        // Initialize EntityAttributes with the attribute sets
        Attributes = new EntityAttributes([combatStats, resourceStats]);
    }
}
```

## Attribute Identification

Attributes are identified by their fully qualified name using the pattern: `AttributeSetName.AttributeName`

```csharp
// Example of accessing an attribute through EntityAttributes indexer
var healthAttribute = entity.Attributes["CombatAttributeSet.CurrentHealth"];
var currentHealth = healthAttribute.CurrentValue;
```

**Important**: Although this uses dot notation similar to [Tags](tags.md), these are not tags and do not need to be registered with the `TagsManager`.

## Attribute Channels

Channels provide powerful, layered attribute calculation with clearly defined order of operations. Each attribute has one or more channels, which process [modifiers](modifiers.md) in sequence.

### How Channels Work

1. Each channel processes modifiers in this order:
   - Apply override (if present).
   - Apply flat modifiers (addition/subtraction).
   - Apply percentage modifiers (multiplication).

2. Channels are processed in sequence, where the output of one channel becomes the input of the next:

```
Channel 1:  (BaseValue + FlatMod1) * PercentMod1  →  Result1
Channel 2:  (Result1 + FlatMod2) * PercentMod2    →  Result2
Channel 3:  (Result2 + FlatMod3) * PercentMod3    →  FinalValue
```

### Channel Configuration

When initializing an attribute, you can specify the number of channels:

```csharp
// Create attribute with 3 channels for complex calculations
var damage = InitializeAttribute(nameof(Damage), 10, 0, 100, channels: 3);
```

### Channel Use Cases

Channels enable complex formulas like `(x + y) * (z + w)` by separating modifiers into appropriate channels:

- **Channel 0**: Base stats and inherent modifiers.
- **Channel 1**: Equipment and item bonuses.
- **Channel 2**: Temporary buffs and status effects.
- **Channel 3**: Final adjustments like damage reduction.

Example: `(BaseAttack + WeaponDamage) * (1 + StrengthBonus) * (1 + CriticalMultiplier) * (1 - TargetArmor)`

## Working with AttributeSets

### Creating an AttributeSet

To create an AttributeSet, extend the base class and initialize attributes in the constructor using the provided `InitializeAttribute` method:

```csharp
public class ResourceAttributeSet : AttributeSet
{
    public EntityAttribute MaxMana { get; }
    public EntityAttribute CurrentMana { get; }
    public EntityAttribute ManaRegenRate { get; }

    public ResourceAttributeSet()
    {
        // Must use InitializeAttribute to properly register attributes with the system
        MaxMana = InitializeAttribute(nameof(MaxMana), 100, 0, 500);
        CurrentMana = InitializeAttribute(nameof(CurrentMana), 100, 0, MaxMana.CurrentValue);
        ManaRegenRate = InitializeAttribute(nameof(ManaRegenRate), 2, 0, 50);
    }

    protected override void AttributeOnValueChanged(EntityAttribute attribute, int change)
    {
        if (attribute == MaxMana)
        {
            // Update CurrentMana's max value
            SetAttributeMaxValue(CurrentMana, MaxMana.CurrentValue);
        }

        if (attribute == CurrentMana && change < 0)
        {
            // Log mana consumption
            Console.WriteLine($"Consumed {-change} mana");
        }
    }
}
```

### AttributeSet Protected Methods

AttributeSet provides several protected methods to manage attributes within the set:

| Method                      | Purpose                                            |
|-----------------------------|----------------------------------------------------|
| **InitializeAttribute**     | Creates and registers a new attribute with the set |
| **SetAttributeBaseValue**   | Sets the base value of an attribute                |
| **AddToAttributeBaseValue** | Adds to the base value of an attribute             |
| **SetAttributeMinValue**    | Sets the minimum value constraint                  |
| **SetAttributeMaxValue**    | Sets the maximum value constraint                  |
| **AttributeOnValueChanged** | Override to handle attribute value changes         |

Example usage:
```csharp
// In an AttributeSet method
SetAttributeBaseValue(Strength, 15);       // Set strength base to 15
AddToAttributeBaseValue(CurrentHealth, -5); // Reduce health by 5
SetAttributeMaxValue(MaxMana, 200);        // Set max mana limit to 200
```

### Attribute Dependencies

AttributeSets allow creating dependencies between attributes without using the [Effects system](docs/effects/README.md):

```csharp
public class CharacterAttributeSet : AttributeSet
{
    public EntityAttribute Strength { get; }
    public EntityAttribute Vitality { get; }
    public EntityAttribute MaxHealth { get; }

    public CharacterAttributeSet()
    {
        Strength = InitializeAttribute(nameof(Strength), 10, 1, 100);
        Vitality = InitializeAttribute(nameof(Vitality), 10, 1, 100);
        MaxHealth = InitializeAttribute(nameof(MaxHealth), 100, 10, 1000);
    }

    protected override void AttributeOnValueChanged(EntityAttribute attribute, int change)
    {
        if (attribute == Vitality)
        {
            // Health scales with Vitality
            SetAttributeBaseValue(MaxHealth, Vitality.CurrentValue * 10);
        }
    }
}
```

## Modifying Attributes

There are two primary ways to modify attributes:

### 1. Within AttributeSets

AttributeSets can modify their own attributes using protected methods for direct, permanent changes to the base value:

```csharp
protected override void AttributeOnValueChanged(EntityAttribute attribute, int change)
{
    // Add to the base value
    AddToAttributeBaseValue(CurrentHealth, -10);  // Take 10 damage

    // Set the base value directly
    SetAttributeBaseValue(CurrentHealth, 50);     // Set health to 50

    // Modify constraints
    SetAttributeMinValue(Strength, 5);            // Set minimum strength
    SetAttributeMaxValue(MaxHealth, 200);         // Set maximum health
}
```

### 2. Through the Effects System

During gameplay, attributes should be modified exclusively through the [Effects system](docs/effects/README.md), which applies temporary or permanent [modifiers](modifiers.md) to attributes without changing their base value.

```csharp
// Create a damage effect that applies a temporary modifier
var damageEffectData = new EffectData(
    "Damage Effect",
    new DurationData(DurationType.Instant),
    new[] {
        new Modifier("CombatAttributeSet.CurrentHealth", ModifierOperation.FlatBonus, new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-25)))
    }
);

var effect = new Effect(damageEffectData, new EffectOwnership(caster, caster));

// Apply effect to target
target.EffectsManager.ApplyEffect(effect);
```

**Important**: Direct manipulation of attributes outside of these two methods is not supported. All attribute methods besides properties are internal to enforce this pattern.

## Attribute Events

Attributes dispatch events when their values change, which can be handled within the `AttributeSet`:

```csharp
// Within an AttributeSet
protected override void AttributeOnValueChanged(EntityAttribute attribute, int change)
{
    if (attribute == CurrentHealth)
    {
        if (change < 0)
        {
            // Handle damage
            if (CurrentHealth.CurrentValue <= 0)
            {
                TriggerDeathEvent();
            }
        }
        else if (change > 0)
        {
            // Handle healing
            TriggerHealingEffect(change);
        }
    }
}
```

## Advanced Concepts

### Overflow and ValidModifier

When modifiers would push an attribute beyond its Min or Max constraints, the `Overflow` property tracks this excess value:

```
Example: An attribute with:
 - BaseValue = 100
 - Min = 0
 - Max = 150
 - Current applied modifier = +70

The attribute's properties will show:
 - BaseValue = 100 (unchanged)
 - CurrentValue = 150 (clamped at Max)
 - Modifier = +70 (total modification applied)
 - Overflow = +20 (the amount exceeding Max)
 - ValidModifier = +50 (the effective portion of the modifier: 70 - 20)
```

The `ValidModifier` property gives you the portion of the modifier that is actually affecting the attribute's value. This is useful for:

- Calculating partial effectiveness of buffs and debuffs
- Determining when effects are being wasted due to attribute caps
- Creating UI elements that show effective vs. total modifiers
- Triggering game events when modifiers are partially effective

### Multiple Attribute Sets

Entities can have multiple attribute sets for different aspects of gameplay:

```csharp
public class PlayerCharacter : IForgeEntity
{
    public EntityAttributes Attributes { get; }
    // Other IForgeEntity properties...

    public PlayerCharacter()
    {
        // Create different attribute sets
        var combatStats = new CombatAttributeSet();
        var resourceStats = new ResourceAttributeSet();
        var movementStats = new MovementAttributeSet();

        // Initialize entity attributes with all sets
        Attributes = new EntityAttributes([combatStats, resourceStats, movementStats]);
    }

    // Example of accessing an attribute
    public void PrintHealth()
    {
        var health = Attributes["CombatAttributeSet.CurrentHealth"].CurrentValue;
        Console.WriteLine($"Current health: {health}");
    }
}
```

## Integration with Other Systems

While detailed relationships with other systems are covered in their respective documentation, attributes are designed to work seamlessly with them:

- **[Effects](docs/effects/README.md)**: Apply temporary or permanent modifications to attributes.
- **[Tags](tags.md)**: Effects can have tag requirements for attribute modification.
- **[Custom Calculators](calculators.md)**: Complex attribute calculations can be encapsulated in custom calculators.

## Best Practices

1. **Group Related Attributes**: Organize attributes into logical sets.
2. **Use AttributeSet for Relationships**: Handle relationships between attributes in the AttributeSet when possible.
3. **Prefer Effects for Gameplay Changes**: During gameplay, modify attributes through Effects.
4. **Design Channel Strategy**: Plan which modifiers belong in which channels.
5. **Document Attribute Dependencies**: Keep track of which attributes affect others.
6. **Consistent Naming**: Use clear, consistent naming conventions for attributes.
7. **Respect Encapsulation**: Never attempt to directly modify attributes outside of AttributeSets or the Effects system.
8. **Use ValidModifier for UI**: When showing modifier values in UI, consider whether to show the total modifier or the ValidModifier.
