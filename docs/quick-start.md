# Forge Quick Start Guide

This guide will help you quickly get started with the Forge framework, showing you how to set up a simple entity with attributes, tags, effects, stacking, and cues. It integrates examples from core systems such as modifiers, periodic effects, stacking effects, unique effects, cues, and custom components.

---

## Installation

Install Forge via NuGet (recommended):

```shell
dotnet add package Gamesmiths.Forge
```

For other installation methods, see the [main README](../README.md).

---

## Creating a Basic Entity

Let's create a simple player entity with three attributes: health, mana, strength and speed.

For that we need to first define an `AttributeSet` that will hold those attributes.

```csharp
// First we need to create the player attribute set
public class PlayerAttributeSet : AttributeSet
{
    public EntityAttribute Health { get; }
    public EntityAttribute Mana { get; }
    public EntityAttribute Strength { get; }
    public EntityAttribute Speed { get; }

    public PlayerAttributeSet()
    {
        // Initialize the attributes with the current, min and max values.
        Health = InitializeAttribute(nameof(Health), 100, 0, 150);
        Mana = InitializeAttribute(nameof(Mana), 100, 0, 100);
        Strength = InitializeAttribute(nameof(Strength), 10, 0, 99);
        Speed = InitializeAttribute(nameof(Speed), 5, 0, 10);
    }
}

// Then create a new entity that implements IForgeEntity
public class Player : IForgeEntity
{
    public EntityAttributes Attributes { get; }
    public EntityTags Tags { get; }
    public EffectsManager EffectsManager { get; }
    public EntityAbilities Abilities { get; }
    public EventManager Events { get; }

    public Player(TagsManager tagsManager, CuesManager cuesManager)
    {
        // Initialize base tags during construction
        var baseTags = new TagContainer(
            tagsManager,
            new HashSet<Tag>()
            {
                Tag.RequestTag(tagsManager, "character.player"),
                Tag.RequestTag(tagsManager, "class.warrior")
            });

        Attributes = new EntityAttributes(new PlayerAttributeSet());
        Tags = new EntityTags(baseTags);
        EffectsManager = new EffectsManager(this, cuesManager);
        Abilities = new(this);
        Events = new();
    }
}

// Initialize managers
var cuesManager = new CuesManager();
var tagsManager = new TagsManager(new string[]
{
    "character.player",
    "class.warrior",
    "status.stunned",
    "status.burning",
    "status.enraged",
    "status.immune.fire",
    "cues.damage.fire",
    "events.combat.damage",
    "events.combat.hit",
    "cooldown.fireball"
});

// Create player instance
var player = new Player(tagsManager, cuesManager);

// Access the player's attribute values
var health = player.Attributes["PlayerAttributeSet.Health"].CurrentValue; // 100
var mana = player.Attributes["PlayerAttributeSet.Mana"].CurrentValue; // 100
var strength = player.Attributes["PlayerAttributeSet.Strength"].CurrentValue; // 10
var speed = player.Attributes["PlayerAttributeSet.Speed"].CurrentValue; // 5
```

---

## Working with Tags

Tags allow you to classify entities and define conditions for effects. Base tags are immutable and assignable only at creation time.

You should generally do checks against the `CombinedTags` property since it includes both the `BaseTags` and `ModifierTags`.

```csharp
// Tags must be requested through the static method Tag.RequestTag
var playerTag = Tag.RequestTag(tagsManager, "character.player");
var warriorTag = Tag.RequestTag(tagsManager, "class.warrior");

// Check if the player has specific tags
bool isPlayer = player.Tags.CombinedTags.HasTag(playerTag);
bool isWarrior = player.Tags.CombinedTags.HasTag(warriorTag);
```

---

## Applying Effects

Effects are the core way to modify entity attributes or apply status conditions.

### Instant Effect Example

Instant effects modify the `BaseValue` of an attribute directly.

The following is an effect that causes 10 damage directly to the player's base health:

```csharp
// Create a damage effect data
var damageEffectData = new EffectData(
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

// Create an effect instance and apply the effect
var damageEffect = new Effect(damageEffectData, new EffectOwnership(player, player));
player.EffectsManager.ApplyEffect(damageEffect);

// Check the current health value
int currentHealth = player.Attributes["PlayerAttributeSet.Health"].CurrentValue;
int baseHealth = player.Attributes["PlayerAttributeSet.Health"].BaseValue;

Console.WriteLine($"Player health after damage: {currentHealth}"); // 90 - Assuming initial health was 100
Console.WriteLine($"Player base health after damage: {baseHealth}"); // 90 - Same as CurrentValue
```

---

### Buff Effect with Duration

You can create temporary buffs through `DurationType.HasDuration`. Temporary effects modify the Modifier value of an attribute and last for the configured time.

The following effect increases the player's strength by +5 for 10 seconds:

```csharp
// Create a strength buff effect that lasts for 10 seconds
var strengthBuffEffectData = new EffectData(
    "Strength Potion",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(10.0f) // 10 seconds duration
        )
    ),
    new[] {
        new Modifier(
            "PlayerAttributeSet.Strength",
            ModifierOperation.FlatBonus,
            new ModifierMagnitude(
                MagnitudeCalculationType.ScalableFloat,
                new ScalableFloat(5) // +5 strength
            )
        )
    }
);

// Create and apply the effect
var strengthBuffEffect = new Effect(strengthBuffEffectData, new EffectOwnership(player, player));

ActiveEffectHandle? buffHandle = player.EffectsManager.ApplyEffect(strengthBuffEffect);

// Check strength values
int buffedStrength = player.Attributes["PlayerAttributeSet.Strength"].CurrentValue;
int baseStrength = player.Attributes["PlayerAttributeSet.Strength"].BaseValue;
int strengthModifier = player.Attributes["PlayerAttributeSet.Strength"].ValidModifier;

Console.WriteLine($"Player strength with buff: {buffedStrength}"); // 15 - Assuming base strength was 10
Console.WriteLine($"Player base strength: {baseStrength}"); // 10 - Base value remains unchanged
Console.WriteLine($"Player strength modifier: {strengthModifier}"); // +5 from the buff
```

---

## Updating Effects

Remember to update your `EffectsManager` periodically in your game loop:

```csharp
// In your game's update loop
void Update(float deltaTime)
{
    player.EffectsManager.UpdateEffects(deltaTime);
}

// Or in turn-based games
void EndTurn()
{
    player.EffectsManager.UpdateEffects(1.0f);
}
```

---

### Infinite Effect Example (Equipment Buff)

Use `DurationType.Infinite` for effects that remain active indefinitely until manually removed. Infinite effects affect attribute Modifiers in the same way as temporary effects.

The following effect increases the player's strength by +10 until actively removed:

```csharp
// Create an infinite effect for a piece of equipment
var equipmentBuffEffectData = new EffectData(
    "Magic Sword Bonus",
    new DurationData(DurationType.Infinite),
    new[] {
        new Modifier(
            "PlayerAttributeSet.Strength",
            ModifierOperation.FlatBonus,
            new ModifierMagnitude(
                MagnitudeCalculationType.ScalableFloat,
                new ScalableFloat(10) // +10 Strength
            )
        )
    }
);

// Apply the equipment buff
var equipmentBuffEffect = new Effect(equipmentBuffEffectData, new EffectOwnership(player, player));
var activeEffectHandle = player.EffectsManager.ApplyEffect(equipmentBuffEffect);

// Remove the effect manually (e.g., when the item is unequipped)
if (activeEffectHandle != null)
{
    player.EffectsManager.RemoveEffect(activeEffectHandle);
}
```

---

### Periodic Effect Example

You can create effects that "tick" at pre-defined intervals. Periodic effects, like instant effects, directly modify the BaseValue of an attribute.

This poison effect ticks every 2 seconds for 10 seconds, causing -5 damage per tick. It also ticks immediately when applied, resulting in 6 total ticks (1 initial + 5 periodic) for -30 total damage:

```csharp
// Create a poison effect that ticks every 2 seconds for 10 seconds
var poisonEffectData = new EffectData(
    "Poison",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(10.0f)
        )
    ),
    new[] {
        new Modifier(
            "PlayerAttributeSet.Health",
            ModifierOperation.FlatBonus,
            new ModifierMagnitude(
                MagnitudeCalculationType.ScalableFloat,
                new ScalableFloat(-5) // -5 damage per tick
            )
        )
    },
    periodicData: new PeriodicData(
        period: new ScalableFloat(2.0f),
        executeOnApplication: true,
        periodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
    )
);

// Apply the poison effect
var poisonEffect = new Effect(poisonEffectData, new EffectOwnership(player, player));
player.EffectsManager.ApplyEffect(poisonEffect);

// Simulate 10 seconds of game time
player.EffectsManager.UpdateEffects(10f);

// After 6 ticks total (including the initial execute), health should be 70 if it started at 100
int currentHealthAfterPoison = player.Attributes["PlayerAttributeSet.Health"].CurrentValue; // 70
int baseHealthAfterPoison = player.Attributes["PlayerAttributeSet.Health"].BaseValue; // 70
```

---

### Stacking Poison Effect Example

This example shows a poison effect that ticks every 2 seconds for -3 damage per tick over 6 seconds. It can stack up to three times, with each stack adding -3 damage to each tick. With three applications, it will cause -27 total damage over 6 seconds:

```csharp
// Create a poison effect that stacks up to 3 times
var stackingPoisonEffectData = new EffectData(
    "Stacking Poison",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(6.0f) // Each stack lasts 6 seconds
        )
    ),
    new[] {
        new Modifier(
            "PlayerAttributeSet.Health",
            ModifierOperation.FlatBonus,
            new ModifierMagnitude(
                MagnitudeCalculationType.ScalableFloat,
                new ScalableFloat(-3) // -3 damage per stack
            )
        )
    },
    periodicData: new PeriodicData(
        period: new ScalableFloat(2.0f),
        executeOnApplication: false,
        periodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
    ),
    stackingData: new StackingData(
        stackLimit: new ScalableInt(3), // Max 3 stacks
        initialStack: new ScalableInt(1), // Starts with 1 stack
        overflowPolicy: StackOverflowPolicy.DenyApplication, // Deny if max stacks reached
        magnitudePolicy: StackMagnitudePolicy.Sum, // Total damage increases with stacks
        expirationPolicy: StackExpirationPolicy.ClearEntireStack, // All stacks expire together
        applicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication,
        stackPolicy: StackPolicy.AggregateBySource, // Aggregate stacks from the same source
        stackLevelPolicy: StackLevelPolicy.SegregateLevels, // Each stack can have its own level

        // The next two values must be defined because this is a periodic effect with stacking
        executeOnSuccessfulApplication: false, // Do not execute on successful application
        applicationResetPeriodPolicy: StackApplicationResetPeriodPolicy.ResetOnSuccessfulApplication // Reset period on successful application
    )
);

// Apply the stacking poison effect multiple times to demonstrate stacking
var stackingPoisonEffect = new Effect(stackingPoisonEffectData, new EffectOwnership(player, player));

// Apply the effect three times, each time adds a stack for a total of -9 damage per tick
player.EffectsManager.ApplyEffect(stackingPoisonEffect);
player.EffectsManager.ApplyEffect(stackingPoisonEffect);
player.EffectsManager.ApplyEffect(stackingPoisonEffect);

// Simulate 6 seconds of game time
player.EffectsManager.UpdateEffects(6f);

// After three ticks at -9 per tick, total damage is -27
var healthAfterStacks = player.Attributes["PlayerAttributeSet.Health"].CurrentValue; // 73 if starting at 100
```

---

### Unique Effect Example

This example shows an effect that is unique to a target and can be overridden only by a higher-level version of the same effect:

```csharp
// Define the unique effect data
var uniqueEffectData = new EffectData(
    "Unique Buff",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(10.0f) // Lasts 10 seconds
        )
    ),
    new[] {
        new Modifier(
            "PlayerAttributeSet.Strength",
            ModifierOperation.FlatBonus,
            new ModifierMagnitude(
                MagnitudeCalculationType.ScalableFloat,
                new ScalableFloat(5.0f, new Curve(new[]
                {
                    new CurveKey(1, 1.0f), // Level 1: base value × 1 = +5 Strength
                    new CurveKey(2, 2.0f)  // Level 2: base value × 2 = +10 Strength
                }))
            )
        )
    },
    stackingData: new StackingData(
        stackLimit: new ScalableInt(1), // Only 1 instance allowed
        initialStack: new ScalableInt(1), // Starts with 1 stack
        overflowPolicy: StackOverflowPolicy.AllowApplication, // Allow application even if max stacks reached
        magnitudePolicy: StackMagnitudePolicy.Sum, // Total damage increases with stacks
        expirationPolicy: StackExpirationPolicy.ClearEntireStack, // All stacks expire together
        applicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication,
        stackPolicy: StackPolicy.AggregateByTarget, // Only one effect per target
        ownerDenialPolicy: StackOwnerDenialPolicy.AlwaysAllow, // Always allow application regardless of owner
        ownerOverridePolicy: StackOwnerOverridePolicy.Override, // Override existing effect if applied again
        ownerOverrideStackCountPolicy: StackOwnerOverrideStackCountPolicy.ResetStacks, // Reset stack count on override
        stackLevelPolicy: StackLevelPolicy.AggregateLevels, // Aggregate levels of the effect
        levelOverridePolicy: LevelComparison.Equal | LevelComparison.Higher, // Allow equal or higher-level effects to override
        levelDenialPolicy: LevelComparison.Lower, // Deny lower-level effects
        levelOverrideStackCountPolicy: StackLevelOverrideStackCountPolicy.ResetStacks // Reset stack count on override
    )
);

// Apply the unique effect at level 1
var uniqueEffectLevel1 = new Effect(uniqueEffectData, new EffectOwnership(player, player), 1);
player.EffectsManager.ApplyEffect(uniqueEffectLevel1);
Console.WriteLine("Level 1 Unique Buff applied: +5 Strength.");

// Apply the unique effect at level 2 (overrides level 1)
var uniqueEffectLevel2 = new Effect(uniqueEffectData, new EffectOwnership(player, player), 2);
player.EffectsManager.ApplyEffect(uniqueEffectLevel2);
Console.WriteLine("Level 2 Unique Buff applied, overriding Level 1: +10 Strength.");
```

---

### Adding a Temporary Tag with an Effect

This example shows how to add a temporary "status.stunned" tag to the target while an effect is active. The effect also sets the target's speed to 0:

```csharp
// Create a "Stunned" effect that adds a tag and reduces speed to 0
var stunEffectData = new EffectData(
    "Stunned",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(3.0f) // 3 seconds duration
        )
    ),
    new[] {
        new Modifier(
            "PlayerAttributeSet.Speed",
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
var stunEffect = new Effect(stunEffectData, new EffectOwnership(player, player));
ActiveEffectHandle? stunHandle = player.EffectsManager.ApplyEffect(stunEffect);

// Check if player is stunned
bool isStunned = player.Tags.CombinedTags.HasTag(Tag.RequestTag(tagsManager, "status.stunned"));
int currentSpeed = player.Attributes["PlayerAttributeSet.Speed"].CurrentValue;
Console.WriteLine($"Player stunned: {isStunned}, Speed: {currentSpeed}");
```

---

### Preventing Effect Application Based on Tags

This example shows an effect that won't be applied if the target has the "status.immune.fire" tag:

```csharp
// Create an effect that cannot be applied if the target has the "status.immune.fire" tag
var fireAttackData = new EffectData(
    "Fire Attack",
    new DurationData(DurationType.Instant),
    new[] {
        new Modifier(
            "PlayerAttributeSet.Health",
            ModifierOperation.FlatBonus,
            new ModifierMagnitude(
                MagnitudeCalculationType.ScalableFloat,
                new ScalableFloat(-15) // -15 damage
            )
        )
    },
    effectComponents: new[] {
        new TargetTagRequirementsEffectComponent(
            applicationTagRequirements: new TagRequirements(
                ignoreTags: tagsManager.RequestTagContainer(new[] { "status.immune.fire" }) // Prevent application if target has "status.immune.fire"
            )
        )
    }
);

// Apply the fire attack effect
var fireAttack = new Effect(fireAttackData, new EffectOwnership(player, player));
player.EffectsManager.ApplyEffect(fireAttack);

// If the player had the "status.immune.fire" tag, no damage would be applied
```

---

## Advanced: Custom Components

You can extend effects with custom logic using components.

This component applies an additional effect when the target's Health attribute falls below a specified threshold at the time of application:

```csharp
// Custom component that applies extra effect when Health is below threshold
public class DamageThresholdComponent : IEffectComponent
{
    private readonly float _threshold;
    private readonly Effect _bonusEffect;

    public DamageThresholdComponent(float threshold, Effect bonusEffect)
    {
        _threshold = threshold;
        _bonusEffect = bonusEffect;
    }

    public void OnEffectApplied(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
    {
        // Check if target health is below threshold and apply bonus effect if it is
        var health = target.Attributes["PlayerAttributeSet.Health"];
        if (health.CurrentValue <= _threshold)
        {
            target.EffectsManager.ApplyEffect(_bonusEffect);
        }
    }
}

// Create a damage effect with threshold component
var thresholdAttackData = new EffectData(
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
    },
    effectComponents: [
        new DamageThresholdComponent(90, stunEffect) // Apply stun if health drops below 90
    ]
);

// Apply the effect twice (second application should trigger the stun)
var thresholdAttack = new Effect(thresholdAttackData, new EffectOwnership(player, player));
player.EffectsManager.ApplyEffect(thresholdAttack);
player.EffectsManager.ApplyEffect(thresholdAttack);

// Check if the stun was applied (will be true if health was 90 or less after damage)
bool isStunned = player.Tags.CombinedTags.HasTag(Tag.RequestTag(tagsManager, "status.stunned"));
```

---

## Advanced: Creating a Custom Calculator

Custom Calculators let you create modifiers that depend on multiple attributes.

This calculator calculates damage based on both strength and speed attributes: `(speed * 2) + (strength * 0.5f)`:

```csharp
// Create a custom calculator that scales damage based on strength and speed
public class StrengthDamageCalculator : CustomModifierMagnitudeCalculator
{
    public AttributeCaptureDefinition StrengthAttribute { get; }
    public AttributeCaptureDefinition SpeedAttribute { get; }

    public StrengthDamageCalculator()
    {
        StrengthAttribute = new AttributeCaptureDefinition(
            "PlayerAttributeSet.Strength",
            AttributeCaptureSource.Source,
            snapshot: true);

        SpeedAttribute = new AttributeCaptureDefinition(
            "PlayerAttributeSet.Speed",
            AttributeCaptureSource.Source,
            snapshot: true);

        AttributesToCapture.Add(StrengthAttribute);
        AttributesToCapture.Add(SpeedAttribute);
    }

    public override float CalculateBaseMagnitude(Effect effect, IForgeEntity target, EffectEvaluatedData? effectEvaluatedData)
    {
        int strength = CaptureAttributeMagnitude(StrengthAttribute, effect, target, effectEvaluatedData);
        int speed = CaptureAttributeMagnitude(SpeedAttribute, effect, target, effectEvaluatedData);

        // Calculate damage based on strength and speed
        float damage = (speed * 2) + (strength * 0.5f);

        return -damage;  // Negative for damage
    }
}

// Use the custom calculator in an effect
var calculatedDamageEffectData = new EffectData(
    "Power Attack",
    new DurationData(DurationType.Instant),
    new[] {
        new Modifier(
            "PlayerAttributeSet.Health",
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
var calculatedDamageEffect = new Effect(calculatedDamageEffectData, new EffectOwnership(player, player));
player.EffectsManager.ApplyEffect(calculatedDamageEffect);
```

---

## Advanced: Creating a Custom Execution

Custom Executions allow you to modify multiple attributes with a single calculation.

This execution drains health from the target based on the source's strength, and returns 80% of that health to the source:

```csharp
public class HealthDrainExecution : CustomExecution
{
    // Define attributes to capture and modify
    public AttributeCaptureDefinition TargetHealth { get; }
    public AttributeCaptureDefinition SourceHealth { get; }
    public AttributeCaptureDefinition SourceStrength { get; }

    public HealthDrainExecution()
    {
        // Capture target health
        TargetHealth = new AttributeCaptureDefinition(
            "PlayerAttributeSet.Health",
            AttributeCaptureSource.Target,
            snapshot: false);

        SourceHealth = new AttributeCaptureDefinition(
            "PlayerAttributeSet.Health",
            AttributeCaptureSource.Source,
            snapshot: false);

        SourceStrength = new AttributeCaptureDefinition(
            "PlayerAttributeSet.Strength",
            AttributeCaptureSource.Source,
            snapshot: false);

        // Register attributes for capture
        AttributesToCapture.Add(TargetHealth);
        AttributesToCapture.Add(SourceHealth);
        AttributesToCapture.Add(SourceStrength);
    }

    public override ModifierEvaluatedData[] EvaluateExecution(Effect effect, IForgeEntity target, EffectEvaluatedData? effectEvaluatedData)
    {
        var results = new List<ModifierEvaluatedData>();

        // Get attribute values
        int targetHealth = CaptureAttributeMagnitude(TargetHealth, effect, target, effectEvaluatedData);
        int sourceHealth = CaptureAttributeMagnitude(SourceHealth, effect, effect.Ownership.Owner, effectEvaluatedData);
        int sourceStrength = CaptureAttributeMagnitude(SourceStrength, effect, effect.Ownership.Owner, effectEvaluatedData);

        // Calculate health drain amount based on source strength
        float drainAmount = sourceStrength * 0.5f;

        // Cap the drain at the target's available health
        drainAmount = Math.Min(drainAmount, targetHealth);

        // Apply health reduction to target if attribute exists
        if (TargetHealth.TryGetAttribute(target, out EntityAttribute? targetHealthAttribute))
        {
            results.Add(new ModifierEvaluatedData(
                targetHealthAttribute,
                ModifierOperation.FlatBonus,
                -drainAmount,  // Negative for drain
                channel: 0
            ));
        }

        // Apply health gain to source if attribute exists
        if (SourceHealth.TryGetAttribute(effect.Ownership.Source, out EntityAttribute? sourceHealthAttribute))
        {
            results.Add(new ModifierEvaluatedData(
                sourceHealthAttribute,
                ModifierOperation.FlatBonus,
                drainAmount * 0.8f,  // Transfer 80% of drained health
                channel: 0
            ));
        }

        return results.ToArray();
    }
}

// Use the custom execution in an effect
var drainEffectData = new EffectData(
    "Life Drain",
    new DurationData(DurationType.Instant),
    customExecutions: new[] {
        new HealthDrainExecution()
    }
);

// Apply the effect (using player1 as source and player2 as target)
var drainEffect = new Effect(drainEffectData, new EffectOwnership(player1, player1));
player2.EffectsManager.ApplyEffect(drainEffect);
```

---

## Cue Examples

Cues bridge gameplay mechanics with visual and audio feedback. Here's how to define, trigger, and implement them.

### Registering a Cue

For a cue handler to work, you must register it with the CuesManager using the appropriate tag:

```csharp
// Register the cue with the manager
cuesManager.RegisterCue(
    Tag.RequestTag(tagsManager, "cues.damage.fire"),
    new FireDamageCueHandler()
);
```

---

### Triggering a Cue Through Effects

This example shows how to trigger cues as part of an effect:

```csharp
// Define a burning effect that includes the fire damage cue
var burningEffectData = new EffectData(
    "Burning",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(5.0f)
        )
    ),
    new[] {
        new Modifier(
            "PlayerAttributeSet.Health",
            ModifierOperation.FlatBonus,
            new ModifierMagnitude(
                MagnitudeCalculationType.ScalableFloat,
                new ScalableFloat(-5) // Damage per tick
            )
        )
    },
    periodicData: new PeriodicData(
        period: new ScalableFloat(1.0f), // Ticks every second
        executeOnApplication: true,
        periodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
    ),
    cues: new[] {
        new CueData(
            cueTags: tagsManager.RequestTagContainer(new[] { "cues.damage.fire" }),
            minValue: 0,
            maxValue: 100,
            magnitudeType: CueMagnitudeType.AttributeValueChange,
            magnitudeAttribute: "PlayerAttributeSet.Health" // Tracks health changes
        )
    }
);

// Apply the burning effect
var burningEffect = new Effect(burningEffectData, new EffectOwnership(player, player));
player.EffectsManager.ApplyEffect(burningEffect);
player.EffectsManager.UpdateEffects(5f);
```

---

### Manually Triggering a Cue

You can trigger cues directly through the CuesManager:

```csharp
// Manually trigger a fire damage cue with custom parameters
var cueParameters = new CueParameters(
    magnitude: 25, // Raw damage value
    normalizedMagnitude: 0.25f, // Normalized between 0-1
    source: player,
    customParameters: new Dictionary<StringKey, object>
    {
        { "DamageType", "Fire" },
        { "IsCritical", true }
    }
);

cuesManager.ExecuteCue(
    cueTag: Tag.RequestTag(tagsManager, "cues.damage.fire"),
    target: player,
    parameters: cueParameters
);
```

---

### Implementing a Cue Handler

This example shows a simple cue handler that outputs messages to the console. In a real game, you would use this to trigger visual effects, sounds, or other feedback:

```csharp
public class FireDamageCueHandler : ICueHandler
{
    public void OnExecute(IForgeEntity? target, CueParameters? parameters)
    {
        if (parameters.HasValue)
        {
            Console.WriteLine($"Fire damage executed: {parameters.Value.Magnitude}");
        }
    }

    public void OnApply(IForgeEntity? target, CueParameters? parameters)
    {
        // Logic for when a persistent cue starts (e.g., play fire animation)
        if (target != null)
        {
            Console.WriteLine("Fire damage cue applied to target.");
        }
    }

    public void OnRemove(IForgeEntity? target, bool interrupted)
    {
        // Logic for when a cue ends (e.g., stop fire animation)
        Console.WriteLine("Fire damage cue removed.");
    }

    public void OnUpdate(IForgeEntity? target, CueParameters? parameters)
    {
        // Logic for updating persistent cues (e.g., adjust fire intensity)
        if (parameters.HasValue)
        {
            Console.WriteLine($"Fire damage cue updated with magnitude: {parameters.Value.Magnitude}");
        }
    }
}
```

---

## Events

The Events system allows entities to communicate and trigger reactions through tagged events.

### Subscribing and Raising an Event

```csharp
// Subscribe to the damage event
var damageTag = Tag.RequestTag(tagsManager, "events.combat.damage");
player.Events.Subscribe(damageTag, eventData =>
{
    Console.WriteLine($"Player took {eventData.EventMagnitude} damage!");
});

// Raise the event
player.Events.Raise(new EventData
{
    EventTags = damageTag.GetSingleTagContainer(),
    Source = null,
    Target = player,
    EventMagnitude = 50f
});
```

You can also instantiate your own `EventManager` and use it in any part of your code, providing a way to handle global or system-specific events independently of entities.

### Strongly Typed Events

You can optimize events to avoid boxing by using generic `EventData`.

```csharp
// Define a strongly typed payload
public record struct DamageInfo(int Value, DamageType DamageType, bool IsCritical);

var damageTag = Tag.RequestTag(tagsManager, "events.combat.damage");

// Subscribe using the specific payload type
player.Events.Subscribe<CombatLogPayload>(damageTag, eventData =>
{
    Console.WriteLine(
        $"[Combat Log] Damage: {eventData.Payload.Value}, " +
        $"Type: {eventData.Payload.DamageType}, " +
        $"Critical: {eventData.Payload.IsCritical}"
    );
});

// Raise the event with the typed payload
player.Events.Raise(new EventData<DamageInfo>
{
	EventTags = damageTag.GetSingleTagContainer(),
	Source = null,
	Target = player,
	Payload = new DamageInfo(120, DamageType.Physical, true)
});
```

---

## Abilities

Abilities are discrete actions that can have costs, cooldowns, and custom behaviors. They can be triggered manually, by events, or in reaction to tag application.

### Defining an Ability

When defining an ability, you typically configure effects for costs and cooldowns, implement a behavior, and then tie it all together in the `AbilityData`.

```csharp
// Define cost: 20 Mana
var fireballCostEffect = new EffectData(
    "Fireball Mana Cost",
    new DurationData(DurationType.Instant),
    new[] {
        new Modifier(
            "PlayerAttributeSet.Mana",
            ModifierOperation.FlatBonus,
            new ModifierMagnitude(
                MagnitudeCalculationType.ScalableFloat,
                new ScalableFloat(-20)
            )
        )
    });

// Define cooldown: 10 seconds
var fireballCooldownEffect = new EffectData(
    "Fireball Cooldown",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(10.0f))),
    effectComponents: new[] {
        new ModifierTagsEffectComponent(
            tagsManager.RequestTagContainer(new[] { "cooldown.fireball" })
        )
    });

// Define behavior
public class FireballBehavior : IAbilityBehavior
{
    public void OnStarted(AbilityBehaviorContext context)
    {
        // Apply costs and cooldowns
        context.AbilityHandle.CommitAbility();
        Console.WriteLine("Fireball cast!");
        context.InstanceHandle.End();
    }

    public void OnEnded(AbilityBehaviorContext context)
    {
        // Do any necessary cleanups
    }
}

// Define Ability Data
var fireballData = new AbilityData(
    name: "Fireball",
    costEffect: fireballCostEffect,
    cooldownEffects: [fireballCooldownEffect],
    instancingPolicy: AbilityInstancingPolicy.PerEntity,
    behaviorFactory: () => new FireballBehavior());
```

---

### Granting and Removing an Ability

Abilities can be granted through effects and will be tied to the effect's lifetime. If the effect has a duration, the ability will be granted only while the effect is active.

```csharp
// Grant an ability via a GrantAbilityEffectComponent
var grantConfig = new GrantAbilityConfig
{
    AbilityData = fireballData,
    ScalableLevel = new ScalableInt(1),
    LevelOverridePolicy = LevelComparison.None,
    RemovalPolicy = AbilityDeactivationPolicy.CancelImmediately,
    InhibitionPolicy = AbilityDeactivationPolicy.CancelImmediately,
};

var grantAbilityComponent = new GrantAbilityEffectComponent([grantConfig]);

// Wrap the component in an infinite effect
var grantFireballEffect = new EffectData(
    "Grant Fireball Effect",
    new DurationData(DurationType.Infinite),
    effectComponents: [grantAbilityComponent]
);

// Apply the effect to grant the ability (e.g., when Wand of Fireball is equipped)
var grantEffectHandle = player.EffectsManager.ApplyEffect(
    new Effect(grantFireballEffect, new EffectOwnership(player, player)));

// You can access the granted ability handle directly from the component
// This list contains handles for all abilities granted by this specific effect component instance
AbilityHandle grantedHandle = grantAbilityComponent.GrantedAbilities[0];

// The ability is now granted. To remove it, simply remove the effect. (e.g., when the wand is unequipped)
player.EffectsManager.RemoveEffect(grantEffectHandle);
```

---

### Activating an Ability

Abilities can be activated directly through their handle. You can also use the handle to find out what's the required cost and cooldown of the ability, useful for updating the UI. The activation returns flags indicating failure reasons, which is useful for player feedback.

```csharp
// Retrieve the handle from the granted ability component or via TryGetAbility
if (player.Abilities.TryGetAbility(fireballData, out AbilityHandle? handle))
{
    // Check cooldown state before activation (useful for UI)
    var cooldowns = handle.GetCooldownData();
    foreach (var cd in cooldowns)
    {
        Console.WriteLine($"Cooldown remaining: {cd.RemainingTime}");
    }

    // Check cost state before activation (useful for UI)
    var costs = handle.GetCostData();
    foreach (var cost in costs)
    {
         // Assuming you want to check Mana costs
        if (cost.AttributeName == "PlayerAttributeSet.Mana")
        {
             Console.WriteLine($"Mana Cost: {cost.Value}");
        }
    }

    // Try to activate
    if (handle.Activate(out AbilityActivationFailures failures))
    {
        Console.WriteLine("Activation successful");
    }
    else
    {
        Console.WriteLine($"Activation failed: {failures}");
        
        if (failures.HasFlag(AbilityActivationFailures.InsufficientResources))
        {
            Console.WriteLine("Not enough mana!");
        }
    }
}
```

---

### Granting an Ability Permanently

One other way to grant an ability permanently is directly through the entity's `EntityAbilities` component.

```csharp
// Grant permanently
AbilityHandle handle = player.Abilities.GrantAbilityPermanently(
    fireballData,
    abilityLevel: 1,
    levelOverridePolicy: LevelComparison.None,
    sourceEntity: player);
```

---

### Granting an Ability and Activating Once

In some cases you just want a quick way to activate an ability on a target without creating a persistent effect or granting it permanently.

The example below shows the use of a "Scroll of Fireball" that grants the fireball ability transiently, attempts to activate it immediately, and then removes the grant once the ability concludes or fails.

```csharp
AbilityHandle? handle = player.Abilities.GrantAbilityAndActivateOnce(
    abilityData: fireballData,
    abilityLevel: 1,
    levelOverridePolicy: LevelComparison.None,
    out AbilityActivationFailures failureFlags,
    targetEntity: enemy,  // The target of the fireball
    sourceEntity: scrollItem // The source (e.g., the scroll item)
);

if (handle is not null)
{
    Console.WriteLine("Scroll used successfully! Fireball cast.");
}
else 
{
    Console.WriteLine($"Failed to use scroll: {failureFlags}");
}
```

---

### Triggering an Ability via Events

You can configure abilities to trigger automatically when specific events occur.

```csharp
var hitTag = Tag.RequestTag(tagsManager, "events.combat.hit");

var autoShieldData = new AbilityData(
    name: "Auto Shield",
    // Configure the trigger
    abilityTriggerData: new AbilityTriggerData(
        TriggerTag: hitTag,
        TriggerSource: AbilityTriggerSource.Event
    ),
    instancingPolicy: AbilityInstancingPolicy.PerEntity,
    behaviorFactory: () => new ShieldBehavior()); // Assumes ShieldBehavior exists

// Grant the ability
player.Abilities.GrantAbilityPermanently(autoShieldData, 1, LevelComparison.None, player);

// Raising the event will automatically trigger the ability
player.Events.Raise(new EventData
{
    EventTags = hitTag.GetSingleTagContainer(),
    Target = player
});
```

---

### Triggering an Ability through Tags

In this example, a granted ability (like a passive aura) is activated automatically while the character has a specific tag (e.g., "status.enraged").

```csharp
// Define an ability that triggers when the "status.enraged" tag is present
var rageAbilityData = new AbilityData(
    "Rage Aura",
    abilityTriggerData: new AbilityTriggerData(
        TriggerTag: Tag.RequestTag(tagsManager, "status.enraged"),
        TriggerSource: AbilityTriggerSource.TagPresent),
    instancingPolicy: AbilityInstancingPolicy.PerEntity,
    behaviorFactory: () => new RageBehavior());

// Grant the ability permanently so it monitors tags
player.Abilities.GrantAbilityPermanently(rageAbilityData, 1, LevelComparison.None, player);

// Apply an effect that grants the "status.enraged" tag
// The Rage Aura ability will automatically activate when this tag is added
var enrageEffect = new EffectData(
    "Enrage",
    new DurationData(
        DurationType.HasDuration,
	    new ModifierMagnitude(
			MagnitudeCalculationType.ScalableFloat,
			new ScalableFloat(10f))),
    effectComponents: [
        new ModifierTagsEffectComponent(tagsManager.RequestTagContainer(["status.enraged"]))
    ]);

player.EffectsManager.ApplyEffect(new Effect(enrageEffect, new EffectOwnership(player, player)));
```

## Next Steps

Now that you've seen the basics of Forge, you can:

1. Explore [Attributes](attributes.md) for more on attribute channels and dependencies.
2. Check [Modifiers](effects/modifiers.md) for advanced attribute modifications.
3. Apply [Effect Durations](effects/duration.md) to create infinite, timed, or instant effects.
4. Implement [Stacking](effects/stacking.md) for cumulative effects.
5. Use [Periodic Effects](effects/periodic.md) for recurring gameplay mechanics.
6. Extend effects with [Components](effects/components.md) for custom behaviors.
7. Integrate [Cues](cues.md) for visual and audio feedback.
8. Orchestrate gameplay reactions with [Events](events.md).
9. Define discrete actions and skills using [Abilities](abilities.md).
10. For catching configuration errors during development, see [Validation and Debugging](README.md#validation-and-debugging).

For more detailed documentation, refer to the [Forge Documentation Index](README.md).
