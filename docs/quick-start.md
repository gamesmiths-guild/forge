# Forge Quick Start Guide

This guide will help you quickly get started with the Forge framework, showing you how to set up a simple entity with attributes, tags, and effects.

## Creating a Basic Entity

Let's create a simple player entity:

```csharp
// Create the player attribute set
public class PlayerAttributeSet : AttributeSet
{
	public EntityAttribute Health { get; }
	public EntityAttribute Strength { get; }
	public EntityAttribute Speed { get; }

	public TestAttributeSet()
	{
		Health = InitializeAttribute(nameof(Health), 100, 0, 100);
		Strength = InitializeAttribute(nameof(Strength), 10, 0, 99);
		Speed = InitializeAttribute(nameof(Speed), 5, 0, 10);
	}
}

// Create a new entity that implements IForgeEntity
public class Player : IForgeEntity
{
    public EntityAttributes Attributes { get; }
    public EntityTags Tags { get; }
    public EffectsManager EffectsManager { get; }

    public Player(TagsManager tagsManager, CuesManager cuesManager)
    {
        // Initialize entity components
        Attributes = new EntityAttributes(new PlayerAttributeSet());
        Tags = new EntityTags(new TagContainer(tagsManager));
        EffectsManager = new EffectsManager(this, cuesManager);
    }
}

// Initialize managers
var tagsManager = new TagsManager(new string[] {
    "player.character",
    "class.warrior",
    "status.stunned"
});
var cuesManager = new CuesManager();

// Create player instance
var player = new Player(tagsManager, cuesManager);
```

## Working with Tags

Add and check tags on your entity:

```csharp
// Add tags to the player
player.Tags.AddBaseTag(Tag.RequestTag(tagsManager, "player.character"));
player.Tags.AddBaseTag(Tag.RequestTag(tagsManager, "class.warrior"));

// Check if the player has specific tags
bool isPlayer = player.Tags.CombinedTags.HasTag(Tag.RequestTag(tagsManager, "player.character"));
bool isWarrior = player.Tags.CombinedTags.HasTag(Tag.RequestTag(tagsManager, "class.warrior"));

// Create a tag requirement (used in effects)
var warriorRequirement = new TagRequirements(
    requiredTags: new[] { Tag.RequestTag(tagsManager, "class.warrior") },
    ignoreTags: new[] { Tag.RequestTag(tagsManager, "status.stunned") }
);
```

For more details about the tag system, see the [Tags documentation](tags.md).

## Creating and Applying Effects

Here's how to create and apply different types of effects:

### Instant Damage Effect

```csharp
// Create a damage effect
var damageEffect = new EffectData(
    "Basic Attack",
    new DurationData(DurationType.Instant),
    new[] {
        new Modifier(
            "PlayerAttributeSet.Health",
            ModifierOperation.FlatBonus,
            new ModifierMagnitude(
                MagnitudeCalculationType.ScalableFloat,
                new ScalableFloat(-10) // -10 damage
            )
        )
    });

// Apply the effect
var effect = new Effect(
    damageEffect,
    new EffectOwnership(player, player) // Source and owner are both the player in this case
);

// Target could be another entity, using player for demonstration
player.EffectsManager.ApplyEffect(effect);

// Check the current health value
int currentHealth = player.Attributes["PlayerAttributeSet.Health"].CurrentValue;
Console.WriteLine($"Player health after damage: {currentHealth}");
```

### Buff Effect with Duration

```csharp
// Create a warrior only strength buff effect that lasts for 10 seconds
var strengthBuffEffect = new EffectData(
    "Strength Potion",
    new DurationData(DurationType.HasDuration, new ScalableFloat(10.0f)), // 10 seconds duration
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
    effectComponents: new[] {
        new TargetTagRequirementsEffectComponent(
            new TagRequirements(
                tagsManager.RequestTagContainer(new[] { "class.warrior" }), // Required tags
                new TagContainer(), // No ignored tags
                new TagQuery() // No tag query
            ),
            new TagRequirements(), // No removal requirements
            new TagRequirements() // No ongoing requirements
        )
    }
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

For more information about effect duration, see the [Duration documentation](duration.md).

### Effect with Tags

```csharp
// Create a "Stunned" effect that adds a tag and reduces speed to 0
var stunnedEffect = new EffectData(
    "Stunned",
    new DurationData(DurationType.HasDuration, new ScalableFloat(3.0f)), // 3 seconds duration
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
    effectComponents: new[] {
        new ModifierTagsEffectComponent(
            tagsManager.RequestTagContainer(new[] { "status.stunned" })
        )
    }
);

// Apply the stun effect
var stunEffect = new Effect(
    stunnedEffect,
    new EffectOwnership(player, player)
);

ActiveEffectHandle? stunHandle = player.EffectsManager.ApplyEffect(stunEffect);

// Check if player is stunned
bool isStunned = player.Tags.CombinedTags.HasTag(Tag.RequestTag(tagsManager, "status.stunned"));
int currentSpeed = player.Attributes["MovementAttributeSet.Speed"].CurrentValue;
Console.WriteLine($"Player stunned: {isStunned}, Speed: {currentSpeed}");
```

Learn more about modifier tags in the [Components documentation](components.md).

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
    new DurationData(DurationType.Instant),
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
    }
);

// Apply the effect
var strengthDamageEffect = new Effect(
    strengthBasedDamageEffect,
    new EffectOwnership(player, player)
);

player.EffectsManager.ApplyEffect(strengthDamageEffect);
```

For more advanced calculator usage, see the [Custom Calculators documentation](calculators.md).

## Updating Effects

Remember to update your effects manager periodically:

```csharp
// In your game loop for real-time games
void Update(float deltaTime)
{
    player.EffectsManager.UpdateEffects(deltaTime);
}

// Or for turn-based games
void EndTurn()
{
    player.EffectsManager.UpdateEffects(1.0f);
}
```

## Next Steps

Now that you've seen the basics of Forge, you can:

1. Set up [periodic effects](periodic.md) for damage or healing over time.
2. Implement [stacking effects](stacking.md) with different stacking rules.
3. Use [channels](attributes.md) to create advanced attribute calculations.
4. Create [custom executions](calculators.md) to modify multiple attributes at once.
5. Add [visual feedback](cues.md) using the cues system.

For more detailed documentation on each system, refer to the specific system documentation linked above, or return to the [main documentation index](README.md).
