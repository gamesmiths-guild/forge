# Effect Recipes

Common effects, from a single hit of damage to stacking poison and tag-based immunity. Every guide here builds on the same small setup: a `PlayerAttributeSet`, a `Player` entity, and a `TagsManager` and `CuesManager`. It is defined once in [Guides](README.md#the-shared-setup); the code samples assume it.

Effects are the core way to modify entity attributes or apply status conditions.

## Instant Effect Example

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

## Buff Effect with Duration

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

## Infinite Effect Example (Equipment Buff)

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

## Periodic Effect Example

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
        Period: new ScalableFloat(2.0f),
        ExecuteOnApplication: true,
        PeriodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
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

## Stacking Poison Effect Example

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
        Period: new ScalableFloat(2.0f),
        ExecuteOnApplication: false,
        PeriodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
    ),
    stackingData: new StackingData(
        StackLimit: new ScalableInt(3), // Max 3 stacks
        InitialStack: new ScalableInt(1), // Starts with 1 stack
        OverflowPolicy: StackOverflowPolicy.DenyApplication, // Deny if max stacks reached
        MagnitudePolicy: StackMagnitudePolicy.Sum, // Total damage increases with stacks
        ExpirationPolicy: StackExpirationPolicy.ClearEntireStack, // All stacks expire together
        ApplicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication,
        StackPolicy: StackPolicy.AggregateBySource, // Aggregate stacks from the same source
        StackLevelPolicy: StackLevelPolicy.SegregateLevels, // Each stack can have its own level

        // The next two values must be defined because this is a periodic effect with stacking
        ExecuteOnSuccessfulApplication: false, // Do not execute on successful application
        ApplicationResetPeriodPolicy: StackApplicationResetPeriodPolicy.ResetOnSuccessfulApplication // Reset period on successful application
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

## Unique Effect Example

This example shows an effect that is unique to a target and can be overridden only by an equal or higher-level version of the same effect:

The level scaling uses a `Curve`, which stands for any `ICurve` implementation: Forge only defines the interface, so engines plug in their own curve assets. See [Understanding Curves](../effects/README.md#understanding-curves).

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
        StackLimit: new ScalableInt(1), // Only 1 instance allowed
        InitialStack: new ScalableInt(1), // Starts with 1 stack
        OverflowPolicy: StackOverflowPolicy.AllowApplication, // Allow application even if max stacks reached
        MagnitudePolicy: StackMagnitudePolicy.Sum, // Magnitude scales with the stack count
        ExpirationPolicy: StackExpirationPolicy.ClearEntireStack, // All stacks expire together
        ApplicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication,
        StackPolicy: StackPolicy.AggregateByTarget, // Only one effect per target
        OwnerDenialPolicy: StackOwnerDenialPolicy.AlwaysAllow, // Always allow application regardless of owner
        OwnerOverridePolicy: StackOwnerOverridePolicy.Override, // Override existing effect if applied again
        OwnerOverrideStackCountPolicy: StackOwnerOverrideStackCountPolicy.ResetStacks, // Reset stack count on override
        StackLevelPolicy: StackLevelPolicy.AggregateLevels, // Aggregate levels of the effect
        LevelOverridePolicy: LevelComparison.Equal | LevelComparison.Higher, // Allow equal or higher-level effects to override
        LevelDenialPolicy: LevelComparison.Lower, // Deny lower-level effects
        LevelOverrideStackCountPolicy: StackLevelOverrideStackCountPolicy.ResetStacks // Reset stack count on override
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

## Adding a Temporary Tag with an Effect

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
bool isStunned = player.Tags.AllTags.HasTag(Tag.RequestTag(tagsManager, "status.stunned"));
int currentSpeed = player.Attributes["PlayerAttributeSet.Speed"].CurrentValue;
Console.WriteLine($"Player stunned: {isStunned}, Speed: {currentSpeed}");
```

## Preventing Effect Application Based on Tags

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
                IgnoreTags: tagsManager.RequestTagContainer(new[] { "status.immune.fire" }) // Prevent application if target has "status.immune.fire"
            )
        )
    }
);

// Apply the fire attack effect
var fireAttack = new Effect(fireAttackData, new EffectOwnership(player, player));
player.EffectsManager.ApplyEffect(fireAttack);

// If the player had the "status.immune.fire" tag, no damage would be applied
```

