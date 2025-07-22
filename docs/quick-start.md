# Forge Quick Start Guide

This guide will help you quickly get started with the Forge framework, showing you how to set up a simple entity with attributes, tags, and effects.

## Creating a Basic Entity

Let's create a simple player entity:

```csharp
// Initialize managers
var tagsManager = new TagsManager();
var cuesManager = new CuesManager();

// Create a new entity that implements IForgeEntity
public class Player : IForgeEntity
{
    public EntityAttributes Attributes { get; }
    public EntityTags Tags { get; }
    public EffectsManager EffectsManager { get; }

    public Player(TagsManager tagsManager, CuesManager cuesManager)
    {
        Attributes = new EntityAttributes();
        Tags = new EntityTags(new TagContainer(tagsManager));
        EffectsManager = new EffectsManager(this, cuesManager);

        // Initialize attributes
        InitializeAttributes();
    }

    private void InitializeAttributes()
    {
        // Create a health attribute with initial value 100, min 0, max 100
        var healthAttribute = new EntityAttribute(100, 0, 100);
        Attributes.AddAttribute("CombatAttributeSet.Health", healthAttribute);

        // Create a strength attribute with initial value 10
        var strengthAttribute = new EntityAttribute(10);
        Attributes.AddAttribute("StatAttributeSet.Strength", strengthAttribute);

        // Create a speed attribute with initial value 5
        var speedAttribute = new EntityAttribute(5);
        Attributes.AddAttribute("MovementAttributeSet.Speed", speedAttribute);
    }
}

// Create player instance
var player = new Player(tagsManager, cuesManager);
```

## Working with Tags

Add and check tags on your entity:

```csharp
// Add tags to the player
player.Tags.Add(Tag.RequestTag("player.character"));
player.Tags.Add(Tag.RequestTag("class.warrior"));

// Check if the player has specific tags
bool isPlayer = player.Tags.Has(Tag.RequestTag("player.character"));
bool isWarrior = player.Tags.Has(Tag.RequestTag("class.warrior"));

// Create a tag requirement (used in effects)
var warriorRequirement = new TagRequirements(
    requiredTags: new[] { Tag.RequestTag("class.warrior") },
    blockedTags: new[] { Tag.RequestTag("status.stunned") }
);
```

## Creating and Applying Effects

Here's how to create and apply different types of effects:

### Instant Damage Effect

```csharp
// Create a damage effect
var damageEffect = new EffectData(
    "Basic Attack",
    new[] {
        new Modifier(
            "CombatAttributeSet.Health",
            ModifierOperation.FlatBonus,
            new ModifierMagnitude(
                MagnitudeCalculationType.ScalableFloat,
                new ScalableFloat(-10) // -10 damage
            )
        )
    },
    new DurationData(DurationType.Instant),
    null,
    null
);

// Apply the effect
var effect = new Effect(
    damageEffect,
    new EffectOwnership(player, player) // Source and owner are both the player in this case
);

// Target could be another entity, using player for demonstration
player.EffectsManager.ApplyEffect(effect);

// Check the current health value
int currentHealth = player.Attributes["CombatAttributeSet.Health"].CurrentValue;
Console.WriteLine($"Player health after damage: {currentHealth}");
```

### Buff Effect with Duration

```csharp
// Create a strength buff effect that lasts for 10 seconds
var strengthBuffEffect = new EffectData(
    "Strength Potion",
    new[] {
        new Modifier(
            "StatAttributeSet.Strength",
            ModifierOperation.FlatBonus,
            new ModifierMagnitude(
                MagnitudeCalculationType.ScalableFloat,
                new ScalableFloat(5) // +5 strength
            )
        )
    },
    new DurationData(DurationType.HasDuration, 10f), // 10 seconds duration
    null,
    warriorRequirement // Only applies to warriors
);

// Create and apply the effect
var buffEffect = new Effect(
    strengthBuffEffect,
    new EffectOwnership(player, player)
);

ActiveEffectHandle? buffHandle = player.EffectsManager.ApplyEffect(buffEffect);

// Check strength value
int buffedStrength = player.Attributes["StatAttributeSet.Strength"].CurrentValue;
Console.WriteLine($"Player strength after buff: {buffedStrength}");

// Remove the effect early if needed
if (buffHandle != null)
{
    player.EffectsManager.UnapplyEffect(buffHandle);
}
```

### Effect with Tags

```csharp
// Create a "Stunned" effect that adds a tag and reduces speed to 0
var stunnedEffect = new EffectData(
    "Stunned",
    new[] {
        new Modifier(
            "MovementAttributeSet.Speed",
            ModifierOperation.Override,
            new ModifierMagnitude(
                MagnitudeCalculationType.ScalableFloat,
                new ScalableFloat(0) // Set speed to 0
            )
        )
    },
    new DurationData(DurationType.HasDuration, 3f), // 3 seconds duration
    new[] { Tag.RequestTag("status.stunned") }, // Add this tag while active
    null
);

// Apply the stun effect
var stunEffect = new Effect(
    stunnedEffect,
    new EffectOwnership(player, player)
);

ActiveEffectHandle? stunHandle = player.EffectsManager.ApplyEffect(stunEffect);

// Check if player is stunned
bool isStunned = player.Tags.Has(Tag.RequestTag("status.stunned"));
int currentSpeed = player.Attributes["MovementAttributeSet.Speed"].CurrentValue;
Console.WriteLine($"Player stunned: {isStunned}, Speed: {currentSpeed}");
```

## Advanced: Creating a Custom Calculator

```csharp
// Create a custom calculator that scales damage based on strength
public class StrengthDamageCalculator : CustomModifierMagnitudeCalculator
{
    public AttributeCaptureDefinition StrengthAttribute { get; }

    public StrengthDamageCalculator()
    {
        StrengthAttribute = new AttributeCaptureDefinition(
            "StatAttributeSet.Strength",
            AttributeCaptureSource.Source,
            snapshot: true);

        AttributesToCapture.Add(StrengthAttribute);
    }

    public override float CalculateBaseMagnitude(Effect effect, IForgeEntity target)
    {
        int strength = CaptureAttributeMagnitude(StrengthAttribute, effect, target);

        // Base damage plus 50% of strength
        float damage = 10 + (strength * 0.5f);

        return -damage;  // Negative for damage
    }
}

// Use the custom calculator in an effect
var strengthBasedDamageEffect = new EffectData(
    "Power Attack",
    new[] {
        new Modifier(
            "CombatAttributeSet.Health",
            ModifierOperation.FlatBonus,
            new ModifierMagnitude(
                MagnitudeCalculationType.CustomCalculatorClass,
                customCalculationBasedFloat: new CustomCalculationBasedFloat(
                    new StrengthDamageCalculator(),
                    new ScalableFloat(1.0f),  // Coefficient
                    new ScalableFloat(0),     // PreMultiply
                    new ScalableFloat(0)      // PostMultiply
                )
            )
        )
    },
    new DurationData(DurationType.Instant),
    null,
    null
);

// Apply the effect
var strengthDamageEffect = new Effect(
    strengthBasedDamageEffect,
    new EffectOwnership(player, player)
);

player.EffectsManager.ApplyEffect(strengthDamageEffect);
```

## Next Steps

Now that you've seen the basics of Forge, you can:

1. Create more complex entity types with custom attribute sets
2. Implement stacking effects with different stacking rules
3. Use channels to create advanced attribute calculations
4. Create custom executions to modify multiple attributes at once

For more detailed documentation on each system, refer to the specific system documentation:

- [Attributes](attributes.md)
- [Effects](effects.md)
- [Modifiers](modifiers.md)
- [Tags](tags.md)
- [Cues](cues.md)
- [Calculators](calculators.md)
