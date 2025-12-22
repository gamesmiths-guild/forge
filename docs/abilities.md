# Abilities System

The Abilities system in Forge provides a framework for defining, granting, activating, and managing gameplay abilities. Abilities encapsulate discrete actions or powers that entities can perform, with built-in support for cooldowns, costs, tag requirements, instancing policies, and triggered activation.

## Core Concepts

- **Granting**: Abilities are granted through [Effects](effects/README.md) or directly via the `EntityAbilities` manager.
- **Identity**: An ability is uniquely identified by the combination of the **Owner**, the **AbilityData**, and the **Source Entity**.
- **Activation**: Each ability has configurable activation requirements, costs, and cooldowns.
- **Instancing**: Policies control how multiple concurrent activations are handled.
- **Triggers**: Activation can be triggered manually, by events, or by tag changes.
- **Behaviors**: Custom logic is implemented through the `IAbilityBehavior` interface.

## Ability Data

`AbilityData` defines the configuration for an ability:

```csharp
var abilityData = new AbilityData(
    name: "Fireball",
    costEffect: costEffectData,
    cooldownEffects: [cooldownEffectData, globalCooldownData],
    abilityTags: fireballTags,
    instancingPolicy: AbilityInstancingPolicy.PerEntity,
    retriggerInstancedAbility: false,
    abilityTriggerData: null,
    cancelAbilitiesWithTag: null,
    blockAbilitiesWithTag: null,
    activationOwnedTags: castingTags,
    activationRequiredTags: null,
    activationBlockedTags: stunnedTags,
    sourceRequiredTags: null,
    sourceBlockedTags: null,
    targetRequiredTags: enemyTags,
    targetBlockedTags: immuneTags,
    behaviorFactory: () => new FireballBehavior());
```

### Configuration Options

- **Name**: Identifier for the ability.
- **CostEffect**: An instant effect defining resource costs.
- **CooldownEffects**: Duration effects preventing reactivation.
- **AbilityTags**: Tags identifying this ability for blocking/cancellation.
- **InstancingPolicy**: Controls concurrent activation handling.
- **RetriggerInstancedAbility**: Restarts persistent instances on re-activation.
- **AbilityTriggerData**: Configuration for automatic activation triggers.
- **CancelAbilitiesWithTag**: Cancels matching abilities on activation.
- **BlockAbilitiesWithTag**: Blocks matching abilities while active.
- **ActivationOwnedTags**: Tags applied to owner while active.
- **ActivationRequiredTags**: Owner tags required to activate.
- **ActivationBlockedTags**: Owner tags preventing activation.
- **SourceRequiredTags**: Source tags required to activate.
- **SourceBlockedTags**: Source tags preventing activation.
- **TargetRequiredTags**: Target tags required to activate.
- **TargetBlockedTags**: Target tags preventing activation.
- **BehaviorFactory**: Factory creating the behavior instance.

## Granting Abilities

Abilities can be granted to entities in several ways: through effects, permanently, or transiently for one-time activation.

### Granting Through Effects

Use `GrantAbilityEffectComponent` to grant abilities that are tied to an effect's lifecycle. The ability's level is determined by the `abilityLevel` ScalableInt evaluated against the **granting effect's level**.

```csharp
var grantAbilityConfig = new GrantAbilityConfig(
    abilityData,
    abilityLevel: new ScalableInt(1, curve: myLevelCurve),
    grantedAbilityRemovalPolicy: AbilityDeactivationPolicy.CancelImmediately,
    grantedAbilityInhibitionPolicy: AbilityDeactivationPolicy.CancelImmediately,
    levelOverridePolicy: LevelComparison.Higher);

var grantEffect = new EffectData(
    "Grant Fireball",
    new DurationData(DurationType.Infinite),
    effectComponents: [new GrantAbilityEffectComponent([grantAbilityConfig])]);

// If the effect is applied at level 5, the ScalableInt calculates the ability level accordingly
entity.EffectsManager.ApplyEffect(new Effect(grantEffect, ownership, level: 5));
```

Abilities granted by **duration or infinite effects** are generally temporary and tied to the effect's lifecycle, unless configured otherwise (see below).

### Granting Permanently

There are three primary ways to grant an ability that persists permanently on an entity:

1.  **GrantAbilityPermanently Method**:
    Using `entity.Abilities.GrantAbilityPermanently(...)` creates a grant that cannot be removed or inhibited by any means. Useful for base character skills.

    ```csharp
    entity.Abilities.GrantAbilityPermanently(
        abilityData: fireballAbility,
        abilityLevel: 1,
        levelOverridePolicy: LevelComparison.Higher,
        sourceEntity: null);
    ```

2.  **Instant Effects**:
    Using `GrantAbilityEffectComponent` inside an effect with `DurationType.Instant`. Because the effect applies and immediately expires (leaving no handle behind), the grant becomes permanent and cannot be inhibited.

3.  **Removal Policy Ignore**:
    Using a standard Duration/Infinite effect but setting `grantedAbilityRemovalPolicy` to `Ignore`.
    *   **Note**: Unlike the first two methods, the granting effect *remains* on the entity.
    *   If you set `grantedAbilityInhibitionPolicy` to something other than `Ignore`, the ability **can still be inhibited** if the effect is inhibited (e.g., via tags), even though the ability won't be removed when the effect ends.

### Granting and Activating Once

Use `GrantAbilityAndActivateOnce` to grant an ability temporarily and immediately attempt to activate it: 

```csharp
AbilityHandle? handle = entity.Abilities.GrantAbilityAndActivateOnce(
    abilityData: consumableAbility,
    abilityLevel: 1,
    levelOverridePolicy: LevelComparison.None,
    out AbilityActivationFailures failureFlags,
    targetEntity: enemy,
    sourceEntity: item);

if (handle is not null)
{
    // Ability activated successfully (failureFlags == AbilityActivationFailures.None)
    // The grant will be removed automatically when the ability ends
}
else
{
    // Activation failed, the grant was already removed
    // Check failureFlags for the specific reasons (e.g. failureFlags.HasFlag(AbilityActivationFailures.InsufficientResources))
}
```

The ability grant is automatically removed when the ability ends. If activation fails, the grant is removed immediately and the method returns `null`.

## Grant Sources and Policies

Each time an ability is granted, a **grant source** is created that tracks how that specific grant should behave. An ability can have multiple grant sources if it's granted multiple times (e.g., by different effects or methods).

### Deactivation Policies

`AbilityDeactivationPolicy` controls behavior when a grant source is removed or inhibited:

- **CancelImmediately**: Cancel all active instances and remove/inhibit immediately.
- **RemoveOnEnd**: Wait for all active instances to end before removing/inhibiting.
- **Ignore**: The grant source ignores removal/inhibition requests entirely.

### Policy Interactions Between Grant Sources

When an ability has multiple grant sources, each source has its own policies. The behavior depends on how these policies interact:

```csharp
// Create two effects that grant the same ability with different policies
var grantConfig1 = new GrantAbilityConfig(
    abilityData,
    new ScalableInt(1),
    removalPolicy: AbilityDeactivationPolicy.RemoveOnEnd,
    inhibitionPolicy: AbilityDeactivationPolicy.Ignore);

var grantConfig2 = new GrantAbilityConfig(
    abilityData,
    new ScalableInt(1),
    removalPolicy: AbilityDeactivationPolicy.CancelImmediately,
    inhibitionPolicy: AbilityDeactivationPolicy.Ignore);

// Assume grantEffect1 and grantEffect2 are created using the configs above...

// Apply both effects - they grant the same ability
ActiveEffectHandle? effectHandle1 = entity.EffectsManager.ApplyEffect(grantEffect1);
ActiveEffectHandle? effectHandle2 = entity.EffectsManager.ApplyEffect(grantEffect2);

// Get the ability handle (both grants reference the same ability)
entity.Abilities.TryGetAbility(abilityData, out AbilityHandle? handle);
handle.Activate(out _);

// Removing effect 1 (RemoveOnEnd): ability stays active, waits for end
entity.EffectsManager.UnapplyEffect(effectHandle1);
// Ability is still active and granted

// Removing effect 2 (CancelImmediately): cancels immediately and removes
entity.EffectsManager.UnapplyEffect(effectHandle2);
// Ability is now canceled and removed (no more grant sources)
```

**Key behaviors:**

1. **Multiple sources, one removed**: The ability remains granted as long as at least one grant source exists.
2. **CancelImmediately takes precedence**: If any remaining grant source has `CancelImmediately` policy when removed, it will cancel the ability immediately regardless of other sources' policies.
3. **Inhibition is cumulative**: The ability is only inhibited when ALL non-ignored grant sources are inhibited. 

### Multiple Grant Sources

If an ability is granted by multiple sources, it remains granted until all sources are removed:

```csharp
// Apply two effects that grant the same ability
ActiveEffectHandle? effectHandle1 = entity.EffectsManager.ApplyEffect(grantEffect1);
ActiveEffectHandle? effectHandle2 = entity.EffectsManager.ApplyEffect(grantEffect2);

// Only one ability instance exists
entity.Abilities.GrantedAbilities.Count; // 1

// Remove first grant - ability still exists
entity.EffectsManager.UnapplyEffect(effectHandle1);
entity.Abilities.GrantedAbilities.Count; // 1

// Remove second grant - now the ability is removed
entity.EffectsManager.UnapplyEffect(effectHandle2);
entity.Abilities.GrantedAbilities.Count; // 0
```

### Level Override Policy

When an ability is granted multiple times, the `LevelOverridePolicy` determines whether the level should be updated:

```csharp
// First grant at level 2
var config1 = new GrantAbilityConfig(abilityData, new ScalableInt(2), ...);
entity.EffectsManager.ApplyEffect(grantEffect1);
// handle.Level == 2

// Second grant at level 3 with Higher policy: level updates
var config2 = new GrantAbilityConfig(
    abilityData,
    new ScalableInt(3),
    levelOverridePolicy: LevelComparison.Higher, ... );
entity.EffectsManager.ApplyEffect(grantEffect2);
// handle.Level == 3

// Third grant at level 1 with Higher policy: level stays at 3
var config3 = new GrantAbilityConfig(
    abilityData,
    new ScalableInt(1),
    levelOverridePolicy: LevelComparison.Higher, ...);
entity.EffectsManager.ApplyEffect(grantEffect3);
// handle.Level == 3
```

## Entity Abilities Manager

`EntityAbilities` is the manager that handles all ability operations for an entity: 

```csharp
// Access through the entity
EntityAbilities abilities = entity.Abilities;

// Get all granted abilities
HashSet<AbilityHandle> granted = abilities.GrantedAbilities;

// Get blocked ability tags (used internally for ability blocking)
EntityTags blockedTags = abilities.BlockedAbilityTags;
```

### Finding Abilities

Use `TryGetAbility` to find a granted ability by its data. 

**Note on Identity:** An ability is uniquely identified by its `AbilityData` **and** its `SourceEntity`. You can have the same ability granted multiple times if the sources differ (e.g., one from an Item, one from a Class).

```csharp
if (entity.Abilities.TryGetAbility(fireballData, out AbilityHandle? handle))
{
    // Ability is granted, use the handle
    handle.Activate(out AbilityActivationFailures failures);
}

// With a specific source entity
if (entity.Abilities.TryGetAbility(buffData, out AbilityHandle? handle, sourceEntity: caster))
{
    // Found the ability granted by this specific source
}
```

### Activating Abilities by Tag

Use `TryActivateAbilitiesByTag` to activate all abilities that match specific tags:

```csharp
var attackTags = new TagContainer(tagsManager, [attackTag]);

bool anyActivated = entity.Abilities.TryActivateAbilitiesByTag(
    attackTags,
    target: enemy,
    out AbilityActivationFailures[] failures);

if (anyActivated)
{
    // At least one ability with matching tags was activated
}
```

This is useful for input handling where a single button might activate different abilities based on context.

### Canceling Abilities by Tag

Use `CancelAbilitiesWithTag` to cancel all active abilities that match specific tags:

```csharp
var interruptibleTags = new TagContainer(tagsManager, [interruptibleTag]);

// Cancel all interruptible abilities (e.g., when stunned)
entity.Abilities.CancelAbilitiesWithTag(interruptibleTags);
```

### Ability Events

Subscribe to `OnAbilityEnded` to react when abilities end:

```csharp
entity.Abilities.OnAbilityEnded += data =>
{
    AbilityHandle ability = data.Ability;
    bool wasCanceled = data.WasCanceled;

    if (wasCanceled)
    {
        // Ability was interrupted
        ShowInterruptedFeedback();
    }
    else
    {
        // Ability completed normally
        ShowCompletedFeedback();
    }
};
```

## Ability Handle

`AbilityHandle` is the public interface for interacting with a granted ability:

```csharp
if (entity.Abilities.TryGetAbility(abilityData, out AbilityHandle? handle))
{
    if (handle.Activate(out AbilityActivationFailures failureFlags))
    {
        // Ability activated successfully
    }
    else
    {
        // Check specific failure flags
        if (failureFlags.HasFlag(AbilityActivationFailures.Cooldown))
        {
            // Show cooldown UI
        }
        
        if (failureFlags.HasFlag(AbilityActivationFailures.InsufficientResources))
        {
             // Show "not enough mana" message
        }
    }
}
```

### Handle Properties and Methods

- **IsActive**: Whether any instance of the ability is currently active. 
- **IsInhibited**: Whether the ability is inhibited by its granting effect.
- **IsValid**: Whether the handle still references a valid granted ability.
- **Level**: The current level of the ability. 
- **Activate(out failureFlags)**: Attempt to activate the ability. Returns true if successful.
- **Activate(out failureFlags, target)**: Attempt to activate with a specific target. 
- **Cancel()**: Cancel all active instances. 
- **CommitAbility()**: Helper that calls both `CommitCooldown()` and `CommitCost()`.
- **CommitCooldown()**: Apply the cooldown effects.
- **CommitCost()**: Apply the cost effect. 
- **GetCooldownData()**: Get information about all cooldowns.
- **GetRemainingCooldownTime(tag)**: Get remaining time for a specific cooldown.
- **GetCostData()**: Get information about all costs.
- **GetCostForAttribute(attribute)**: Get cost for a specific attribute. 

### Activation Failures

`AbilityActivationFailures` is a **Flags Enum** that indicates all reasons why an activation failed. Unlike a simple result code, this allows the system to report multiple failures simultaneously (e.g., Insufficient Resources AND Cooldown).

- **None**: Successfully activated.
- **InvalidHandler**: The ability handle is invalid.
- **Inhibited**: Ability is inhibited by its granting effect. 
- **PersistentInstanceActive**: A non-retriggerable persistent instance is already active. 
- **Cooldown**: Ability is on cooldown.
- **InsufficientResources**: Cannot afford the cost.
- **OwnerTagRequirements**: Owner doesn't meet tag requirements.
- **SourceTagRequirements**: Source doesn't meet tag requirements.
- **TargetTagRequirements**: Target doesn't meet tag requirements.
- **BlockedByTags**: Another active ability is blocking this one. 
- **TargetTagNotPresent**: No abilities matched the requested tags (when using `TryActivateAbilitiesByTag`).
- **InvalidTagConfiguration**: Invalid tag configuration provided. 

## Instancing Policies

`AbilityInstancingPolicy` determines how multiple activations are handled.

**Note on Identity**: Forge creates one instance of the Ability per entity + source entity. This means if you have a source entity configured (e.g., two different equipped swords granting "Slash"), you will have two distinct abilities that can execute independently with their own levels and cooldowns.

### PerEntity

Only one instance can be active at a time per entity (per unique ability identity): 

```csharp
var abilityData = new AbilityData(
    "Shield Block",
    instancingPolicy: AbilityInstancingPolicy.PerEntity,
    retriggerInstancedAbility: false);
```

With `retriggerInstancedAbility: false`, attempting to activate while active fails with `AbilityActivationFailures.PersistentInstanceActive`.

With `retriggerInstancedAbility: true`, the active instance is canceled and a new one starts: 

```csharp
var abilityData = new AbilityData(
    "Channeled Beam",
    instancingPolicy: AbilityInstancingPolicy.PerEntity,
    retriggerInstancedAbility: true);
```

### PerExecution

Multiple instances can be active simultaneously: 

```csharp
var abilityData = new AbilityData(
    "Trap",
    instancingPolicy: AbilityInstancingPolicy.PerExecution);

// Each activation creates a new instance
handle.Activate(out _); // Instance 1
handle.Activate(out _); // Instance 2
handle.Activate(out _); // Instance 3

// Cancel ends all instances
handle.Cancel();
```

## Cooldowns

Cooldowns prevent ability reactivation for a duration. They are implemented as duration effects that grant tags.

**Requirements**: 
- Cooldown effects **must** have a Duration (not Instant, not Infinite).
- Cooldown effects **must** have a `ModifierTagsEffectComponent`.

The system receives an array of cooldown effects, allowing you to trigger multiple independent cooldowns at once (e.g., a short "Skill Cooldown" and a longer "Global Cooldown").

```csharp
var cooldownEffect = new EffectData(
    "Fireball Cooldown",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(5f))),
    effectComponents: [new ModifierTagsEffectComponent(cooldownTags)]);

var abilityData = new AbilityData(
    "Fireball",
    cooldownEffects: [cooldownEffect]);
```

Multiple cooldown effects can be used for abilities with multiple cooldown conditions: 

```csharp
// Ability has both a short cooldown and a charge system
var abilityData = new AbilityData(
    "Dash",
    cooldownEffects: [dashCooldownEffect, globalCooldownEffect]);
```

### Querying Cooldown State

```csharp
// Get all cooldown information
CooldownData[] cooldowns = handle.GetCooldownData();
foreach (CooldownData cd in cooldowns)
{
    float remaining = cd.RemainingTime;
    float total = cd.TotalTime;
    float progress = 1f - (remaining / total);
}

// Get specific cooldown by tag
float remainingTime = handle.GetRemainingCooldownTime(cooldownTag);
```

Cooldowns are checked during activation but only applied when `CommitCooldown()` or `CommitAbility()` is called.

## Costs

Costs are instant effects that modify attributes when committed.

**Requirements**: 
- Cost effects **must** be Instant.
- Attribute modifiers must be **negative** to consume resources (e.g., -30 Mana).

**Validation Logic**:

Cost modifiers are validated against the attribute's configured min/max bounds:
- If the modifier is **negative** (consumption), it tests against the attribute's **Minimum Value** (e.g., Do I have enough Mana to pay -30 without going below 0?)
- If the modifier is **positive** (restoration), it tests against the attribute's **Maximum Value**. (e.g., Is my Health low enough to receive +50 healing without exceeding Max Health?)

You can add multiple modifiers to the single `CostEffect`, allowing an ability to consume multiple different attributes (e.g., Mana and Health).

```csharp
var costEffect = new EffectData(
    "Fireball Cost",
    new DurationData(DurationType.Instant),
    [new Modifier(
        "ManaAttributeSet.CurrentMana",
        ModifierOperation.FlatBonus,
        new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-30f)))]);

var abilityData = new AbilityData(
    "Fireball",
    costEffect: costEffect);
```

Cost is checked during activation but only applied when `CommitCost()` or `CommitAbility()` is called.

## Ability Behavior

`IAbilityBehavior` defines custom logic that runs during an ability's lifecycle. It gives the developer total control, but comes with important responsibilities.

### Developer Responsibilities

1.  **Ending Instances**: It is up to the developer to call `context.InstanceHandle.End()` when the ability logic is complete. If you fail to do this, the system will consider the ability "Active" indefinitely.
    *   **Passive Abilities**: For passive abilities, you may intentionally **not** call `End()`. This keeps the ability active (and listening to events/tags) until it is manually canceled or the grant is removed.
2.  **Committing**: Resources and Cooldowns are not applied automatically. You must call `context.AbilityHandle.CommitAbility()` (or `CommitCost` / `CommitCooldown` separately).
    *   `CommitAbility()` calls both `CommitCost()` and `CommitCooldown()`.
    *   Do **not** call all three; it is redundant.
    *   Deferring commits allows for mechanics like "free cast if cancelled early."

```csharp
public class FireballBehavior : IAbilityBehavior
{
    public void OnStarted(AbilityBehaviorContext context)
    {
        // Called when the ability instance starts
        IForgeEntity owner = context.Owner;
        IForgeEntity? source = context.Source;
        IForgeEntity? target = context.Target;
        int level = context.Level;
        AbilityHandle abilityHandle = context.AbilityHandle;
        AbilityInstanceHandle instanceHandle = context.InstanceHandle;

        // Commit cooldown and cost
        // This calls both CommitCooldown() and CommitCost()
        abilityHandle.CommitAbility();

        // Spawn projectile, start animation, etc. 
        SpawnFireball(owner, target, level);
    }

    public void OnEnded(AbilityBehaviorContext context)
    {
        // Called when the ability instance ends
        // Clean up effects, stop animations, etc. 
    }
}
```

### Behavior Context

`AbilityBehaviorContext` provides access to ability state:

- **Owner**: The entity that owns this ability.
- **Source**: The entity that granted this ability (may be null).
- **Target**: The target passed during activation (may be null).
- **Level**: The ability's current level.
- **AbilityHandle**: Handle to the ability for committing cost/cooldown. 
- **InstanceHandle**: Handle to this specific instance for ending it.

### Ending Instances

Behaviors can end their instance at any time:

```csharp
public class InstantAbilityBehavior : IAbilityBehavior
{
    public void OnStarted(AbilityBehaviorContext context)
    {
        context.AbilityHandle.CommitAbility();

        // Do the instant effect
        ApplyDamage(context.Target);

        // End immediately
        context.InstanceHandle.End();
    }

    public void OnEnded(AbilityBehaviorContext context)
    {
        // Cleanup if needed
    }
}
```

### Behavior Factory

The behavior factory creates a new behavior instance for **each activation**:

```csharp
// Simple factory
var abilityData = new AbilityData(
    "Fireball",
    behaviorFactory: () => new FireballBehavior());

// Factory with dependencies
var abilityData = new AbilityData(
    "Fireball",
    behaviorFactory: () => new FireballBehavior(projectilePool, audioManager));

// Per-execution instancing creates separate behavior instances
var abilityData = new AbilityData(
    "Trap",
    instancingPolicy: AbilityInstancingPolicy.PerExecution,
    behaviorFactory: () => new TrapBehavior()); // Each trap gets its own behavior
```

## Ability Triggers

Abilities can be automatically activated in response to events or tag changes:

### Event Trigger

Activate when a specific event is raised:

```csharp
var abilityData = new AbilityData(
    "Counter Attack",
    abilityTriggerData: new AbilityTriggerData(
        TriggerTag: Tag.RequestTag(tagsManager, "events.combat.blocked"),
        TriggerSource: AbilityTriggerSource.Event));

// Later, when the entity blocks an attack:
entity.Events.Raise(new EventData
{
    EventTags = blockedEventTags,
    Source = attacker,
    Target = entity
});
// Counter Attack activates automatically
```

### Tag Added Trigger

Activate when a tag is added to the entity:

```csharp
var abilityData = new AbilityData(
    "Rage",
    abilityTriggerData: new AbilityTriggerData(
        TriggerTag: Tag.RequestTag(tagsManager, "status.enraged"),
        TriggerSource: AbilityTriggerSource.TagAdded));

// When the entity gains the "status.enraged" tag, Rage activates
```

### Tag Present Trigger

Stay active while a tag is present. This acts as a toggle:

```csharp
var abilityData = new AbilityData(
    "Burning Aura",
    abilityTriggerData: new AbilityTriggerData(
        TriggerTag: Tag.RequestTag(tagsManager, "status.on_fire"),
        TriggerSource: AbilityTriggerSource.TagPresent));

// 1. Tag "status.on_fire" added -> Ability Activates
// 2. Tag "status.on_fire" removed -> Ability is Canceled
```

## Tag Interactions

### Blocking and Canceling

Abilities can block or cancel other abilities based on tags:

```csharp
// This ability cancels any active ability with "ability.interruptible" tag
var interruptAbility = new AbilityData(
    "Interrupt",
    cancelAbilitiesWithTag: interruptibleTags);

// This ability prevents abilities with "ability.movement" from activating
var rootAbility = new AbilityData(
    "Root",
    blockAbilitiesWithTag: movementTags);
```

Blocking tags are tracked per-instance. If multiple instances of a blocking ability are active, the blocked abilities remain blocked until all instances end.

### Activation Owned Tags

Tags that are applied to the owner while the ability is active: 

```csharp
var channelAbility = new AbilityData(
    "Channel",
    activationOwnedTags: channelingTags);

// While Channel is active, owner has "status.channeling" tag
// Other abilities can check for this tag in requirements
```

## Inhibition

When a granting effect is inhibited (e.g., due to tag requirements), the granted ability becomes inhibited:

```csharp
// Grant ability with ongoing tag requirements
var grantEffect = new EffectData(
    "Grant Fireball",
    new DurationData(DurationType.Infinite),
    effectComponents: 
    [
        new GrantAbilityEffectComponent([grantConfig]),
        new TargetTagRequirementsEffectComponent(
            ongoingTagRequirements: new TagRequirements(IgnoreTags: silencedTags))
    ]);

// When entity gains "status.silenced", the ability becomes inhibited
// Activation fails with AbilityActivationFailures.Inhibited
```

With `GrantedAbilityInhibitionPolicy.RemoveOnEnd`, an active ability continues running but becomes inhibited after it ends. 

Abilities granted permanently via `GrantAbilityPermanently` cannot be inhibited. 

## Best Practices

1. **Separate Data from Behavior**: Define ability configuration in `AbilityData` and implement logic in `IAbilityBehavior`.
2. **Use Appropriate Instancing**: Choose `PerEntity` for abilities that should have one active instance, `PerExecution` for stackable abilities.
3. **Commit Explicitly**: Call `CommitAbility()` (or individual commits) inside your behavior.
4. **End Instances**: Always call `context.InstanceHandle.End()` when logic completes to prevent "stuck" abilities.
5. **Handle Failure Flags**: Use the `AbilityActivationFailures` flags to provide specific feedback to the player (e.g. check for `Cooldown` and `InsufficientResources`).
6. **Clean Up in OnEnded**: Always clean up spawned objects, effects, and state in `OnEnded`.
7. **Use Tag Requirements**: Leverage tag-based requirements for complex activation conditions.
8. **Consider Policy Interactions**: When granting abilities from multiple sources, be aware that `CancelImmediately` policies take precedence. 
9. **Query Before Activation**: Use `GetCooldownData()` and `GetCostData()` to show UI state before attempting activation. 
10. **Use Permanent Grants for Innate Abilities**: Use `GrantAbilityPermanently` for abilities that should always be available.
11. **Use Tag-Based Activation**: Use `TryActivateAbilitiesByTag` for flexible input handling where multiple abilities share activation contexts.
12. **Check Validation Rules**: Ensure cooldowns have durations/tags and costs are instant.
