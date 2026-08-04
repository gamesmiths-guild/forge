# Abilities System

The Abilities system in Forge provides a framework for defining, granting, activating, and managing gameplay abilities. Abilities encapsulate discrete actions or powers that entities can perform, with built-in support for cooldowns, costs, tag requirements, instancing policies, and triggered activation.

## Core Concepts

- **Granting**: Abilities are granted through [Effects](effects/README.md) or directly via the `EntityAbilities` manager.
- **Identity**: An ability is uniquely identified by the combination of the **Owner**, the **AbilityData**, and the **Source Entity**.
- **Activation**: Each ability has configurable activation requirements, costs, and cooldowns.
- **Instancing**: Policies control how multiple concurrent activations are handled.
- **Triggers**: Activation can be triggered manually, by events, or by tag changes.
- **Interruption**: Abilities can be canceled or interrupted, with configurable behavior.
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
- **CooldownEffects**: Duration effects with tags preventing reactivation.
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
    ScalableLevel: new ScalableInt(1, ScalingCurve: myLevelCurve),
    RemovalPolicy: AbilityDeactivationPolicy.CancelImmediately,
    InhibitionPolicy: AbilityDeactivationPolicy.CancelImmediately,
    TryActivateOnGrant: false,
    TryActivateOnEnable: false,
    LevelOverridePolicy: LevelComparison.Higher);

var grantComponent = new GrantAbilityEffectComponent([grantAbilityConfig]);

var grantEffect = new EffectData(
    "Grant Fireball",
    new DurationData(DurationType.Infinite),
    effectComponents: [grantComponent]);

// If the effect is applied at level 5, the ScalableInt calculates the ability level accordingly
entity.EffectsManager.ApplyEffect(new Effect(grantEffect, ownership, level: 5));
```

**Tip:** By holding a reference to the `GrantAbilityEffectComponent` used in your `EffectData`, you can access the `grantComponent.GrantedAbilities` list. This provides a direct reference to the `AbilityHandle`s created by the effect, which can be more reliable than searching via `TryGetAbility` if you need to manipulate that specific instance immediately.

Abilities granted by **instant effects** become permanent, while abilities granted by **duration or infinite effects** are temporary and tied to the effect's lifecycle.

`TryActivateOnGrant` attempts to activate the ability immediately when it is granted, while `TryActivateOnEnable` attempts activation when the granting effect is re-enabled after inhibition.

### Granting Permanently

There are three ways to grant an ability that persists permanently:

1.  **Direct API**: Use `entity.Abilities.GrantAbilityPermanently(...)`. These abilities cannot be removed or inhibited by the effects system.
2.  **Instant Effects**: Apply an effect with `DurationType.Instant` that contains a `GrantAbilityEffectComponent`. These behave exactly like manually granted permanent abilities.
3.  **Ignore Policy**: Apply a Duration/Infinite effect with a `GrantAbilityEffectComponent` configured with `RemovalPolicy = AbilityDeactivationPolicy.Ignore`.
    *   Unlike the other two methods, abilities granted this way *can* still be inhibited if the source effect is inhibited (depending on `InhibitionPolicy`).
    *   They will simply not be removed when the source effect is removed.

```csharp
AbilityHandle handle = entity.Abilities.GrantAbilityPermanently(
    abilityData: fireballAbility,
    abilityLevel: 1,
    levelOverridePolicy: LevelComparison.Higher,
    sourceEntity: null);
```

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

A generic overload passes strongly-typed activation data to the procced ability, the same data a behavior reads through `IAbilityBehavior<TData>.OnStarted`:

```csharp
AbilityHandle? handle = entity.Abilities.GrantAbilityAndActivateOnce(
    abilityData: consumableAbility,
    abilityLevel: 1,
    levelOverridePolicy: LevelComparison.None,
    data: new ConsumeData(itemId, stackCount),
    out AbilityActivationFailures failureFlags,
    targetEntity: enemy,
    sourceEntity: item);
```

Because the ability is known up front, `TData` can be matched to it. An ability whose behavior does not accept `TData` still activates, ignoring the data. See [Strongly-Typed Activation Data](#strongly-typed-activation-data) for how behaviors consume it.

## Grant Sources and Policies

Each time an ability is granted, a **grant source** is created that tracks how that specific grant should behave. An ability can have multiple grant sources if it's granted multiple times (e.g., by different effects or methods).

### Multiple Grant Sources

If an ability is granted by multiple sources, it remains granted until all sources are removed:

```csharp
// Apply two effects that grant the same ability
ActiveEffectHandle? effectHandle0 = entity.EffectsManager.ApplyEffect(grantEffect1);
ActiveEffectHandle? effectHandle1 = entity.EffectsManager.ApplyEffect(grantEffect2);

// Only one ability instance exists, with two grant sources behind it
entity.Abilities.GrantedAbilities.Count; // 1

// Remove first grant - ability still exists
entity.EffectsManager.RemoveEffect(effectHandle0);
entity.Abilities.GrantedAbilities.Count; // 1

// Remove second grant - now the ability is removed
entity.EffectsManager.RemoveEffect(effectHandle1);
entity.Abilities.GrantedAbilities.Count; // 0
```

Grant sources are only shared when both grants target the same `AbilityData` **and** carry the same source entity. Granting the same `AbilityData` from two different source entities produces two separate abilities, each with its own handle, level and grant sources.

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
    LevelOverridePolicy: LevelComparison.Higher, ...);
entity.EffectsManager.ApplyEffect(grantEffect2);
// handle.Level == 3

// Third grant at level 1 with Higher policy: level stays at 3
var config3 = new GrantAbilityConfig(
    abilityData,
    new ScalableInt(1),
    LevelOverridePolicy: LevelComparison.Higher, ...);
entity.EffectsManager.ApplyEffect(grantEffect3);
// handle.Level == 3
```

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
    RemovalPolicy: AbilityDeactivationPolicy.RemoveOnEnd,
    InhibitionPolicy: AbilityDeactivationPolicy.Ignore);

var grantConfig2 = new GrantAbilityConfig(
    abilityData,
    new ScalableInt(1),
    RemovalPolicy: AbilityDeactivationPolicy.CancelImmediately,
    InhibitionPolicy: AbilityDeactivationPolicy.Ignore);

// Assume grantEffect1 and grantEffect2 are created using the configs above...

// Apply both effects - they grant the same ability
ActiveEffectHandle? effectHandle1 = entity.EffectsManager.ApplyEffect(grantEffect1);
ActiveEffectHandle? effectHandle2 = entity.EffectsManager.ApplyEffect(grantEffect2);

// Get the ability handle (both grants reference the same ability)
entity.Abilities.TryGetAbility(abilityData, out AbilityHandle? handle);
handle.Activate(out _);

// Removing effect 1 (RemoveOnEnd): ability stays active, waits for end
entity.EffectsManager.RemoveEffect(effectHandle1);
// Ability is still active and granted

// Removing effect 2 (CancelImmediately): cancels immediately and removes
entity.EffectsManager.RemoveEffect(effectHandle2);
// Ability is now canceled and removed (no more grant sources)
```

**Key behaviors:**

1. **Multiple sources, one removed**: The ability remains granted as long as at least one grant source exists.
2. **CancelImmediately takes precedence**: If any remaining grant source has `CancelImmediately` policy when removed, it will cancel the ability immediately regardless of other sources' policies.
3. **Inhibition is cumulative**: an ability is inhibited only once **every one of its own** grant sources has stopped providing it. A source keeps the ability enabled while it is not inhibited, and also whenever its `InhibitionPolicy` is `Ignore` — that policy means the grant does not react to its effect being inhibited at all. Grant sources belonging to *other* abilities on the same entity never affect this.

## Entity Abilities Manager

`EntityAbilities` is the manager that handles all ability operations for an entity: 

```csharp
// Access through the entity
EntityAbilities abilities = entity.Abilities;

// Get all granted abilities
IReadOnlyCollection<AbilityHandle> granted = abilities.GrantedAbilities;

// Get blocked ability tags (used internally for ability blocking)
EntityTags blockedTags = abilities.BlockedAbilityTags;
```

`GrantedAbilities` is read-only and live: the manager keeps it in step with the grant sources behind each ability, so abilities are added and removed through the granting API and the [effect components](effects/components/grant-ability-effect-component.md), never through the set itself. Copy it before iterating when the loop body can remove abilities.

Abilities whose `AbilityTags` overlap `BlockedAbilityTags` fail activation with `AbilityActivationFailures.BlockedByTags`. The container is populated by `BlockAbilitiesWithTag` while an ability is running, and by [`BlockAbilityTagsEffectComponent`](effects/components/block-ability-tags-effect-component.md) while an effect is active.

### Finding Abilities

Use `TryGetAbility` to find a granted ability by its data.

**Note on Identity:** An ability is uniquely identified by its `AbilityData` **and** its `SourceEntity`. You can have the same ability granted multiple times if the sources differ (e.g., one from an Item, one from a Class).

Without a source, the lookup matches the ability regardless of who granted it (if several sources granted the same data, which instance you get is unspecified). Pass a source entity to find the instance granted by that specific source. Because `null` is itself a valid granting source, pass `exactSourceMatch: true` to find specifically the instance granted without a source.

```csharp
if (entity.Abilities.TryGetAbility(fireballData, out AbilityHandle? handle))
{
    // Ability is granted (by any source), use the handle
    handle.Activate(out AbilityActivationFailures failures);
}

// With a specific source entity
if (entity.Abilities.TryGetAbility(buffData, out AbilityHandle? handle, source: caster))
{
    // Found the ability granted by this specific source
}

// Specifically the instance granted without a source (source == null)
if (entity.Abilities.TryGetAbility(buffData, out AbilityHandle? handle, source: null, exactSourceMatch: true))
{
    // Found the sourceless instance, even if other sources granted the same data
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

A generic overload passes strongly-typed activation data to every activated ability:

```csharp
bool anyActivated = entity.Abilities.TryActivateAbilitiesByTag(
    attackTags,
    target: enemy,
    data: new AttackData(comboStep, chargeTime),
    out AbilityActivationFailures[] failures);
```

A tag usually selects several abilities, and they need not share an activation-data type. Only abilities whose behavior implements `IAbilityBehavior<TData>` receive the data; the rest still activate and simply ignore it, so mismatched data is never an error. When each ability needs its own payload, activate them individually through their handles instead.

### Canceling Abilities by Tag

Use `CancelAbilities` to cancel active abilities selected by the tags they carry. It takes a required container and a blocking one, so you can cancel a whole category while sparing part of it:

```csharp
var interruptibleTags = new TagContainer(tagsManager, [interruptibleTag]);
var unstoppableTags = new TagContainer(tagsManager, [unstoppableTag]);

// Cancel all interruptible abilities (e.g., when stunned)
entity.Abilities.CancelAbilities(interruptibleTags, null);

// Cancel every interruptible ability except the ones flagged as unstoppable
entity.Abilities.CancelAbilities(interruptibleTags, unstoppableTags);

// Cancel everything that isn't unstoppable
entity.Abilities.CancelAbilities(null, unstoppableTags);
```

Each container is an independent filter, and a `null` or empty one means "don't filter on this side". An ability with no `AbilityTags` is never matched by the required container, but it always satisfies the blocking one, since it carries none of those tags.

> Passing nothing for either container cancels **every** active ability. That is deliberate — it is how you ask for a full wipe — but it means an empty container standing for "cancel nothing" has to be guarded at the call site.

To drive this from an effect instead of calling it directly, use [`CancelAbilityTagsEffectComponent`](effects/components/cancel-ability-tags-effect-component.md), which wraps `CancelAbilities` and can fire on application or on each periodic execution.

### Observing Abilities

`EntityAbilities` reports the whole ability lifecycle to whoever asks, which is what an ability bar needs to stay in sync without polling `GrantedAbilities` every frame. These are plain C# `event` members — **change notifications**, not to be confused with the tag-routed [Events system](events.md) that drives [event triggers](#event-trigger):

| Notification | Raised when | Payload |
|---|---|---|
| `OnAbilityGranted` | an ability is granted, once per ability | `AbilityHandle` |
| `OnAbilityChanged` | a granted ability's level or inhibition changes | `AbilityHandle` |
| `OnAbilityRemoved` | an ability loses its last grant source | `AbilityHandle` |
| `OnAbilityActivated` | an ability becomes active | `AbilityHandle` |
| `OnAbilityEnded` | an ability's last active instance ends | `AbilityEndedData` |
| `OnAbilityActivationFailed` | an activation attempt is refused | `AbilityHandle`, `AbilityActivationFailures` |

```csharp
entity.Abilities.OnAbilityGranted += handle => _slots.Add(handle, CreateSlot(handle));
entity.Abilities.OnAbilityChanged += handle => _slots[handle].SetEnabled(!handle.IsInhibited);
entity.Abilities.OnAbilityRemoved += handle =>
{
    _slots[handle].Dispose();
    _slots.Remove(handle);
};
```

The handle is safe to use as a key: it is created once per ability and stays the same object for that ability's whole life. Inside the removed handler it can still be read, and becomes invalid immediately after.

**Granted vs. changed.** Granting an ability the entity already has adds a [grant source](#grant-sources-and-policies) rather than a second ability, so it raises `OnAbilityChanged` — and only if the [level override policy](#level-override-policy) actually moved the level, or the new source flipped inhibition. A repeat grant that resolves to the same values is silent.

**Activated and ended are a matched pair.** Both track the *ability*, not its instances, so a second concurrent instance of a [`PerExecution`](#perexecution) ability raises neither: activation reports the inactive-to-active transition, ending reports the last instance going away. `OnAbilityActivated` is raised before the behavior starts, so it always arrives before the matching `OnAbilityEnded` — including for a behavior that finishes inside `OnStarted`.

```csharp
entity.Abilities.OnAbilityEnded += data =>
{
    AbilityHandle ability = data.Ability;

    if (data.WasCanceled)
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

`WasCanceled` is `true` when the ability was canceled (via `AbilityHandle.Cancel()` or `CancelAbilities`) and `false` when it ended gracefully (reaching its natural end, or a Statescript Exit node). `AbilityEndedData` also carries `AbilityData`, captured before the handle can be freed, because an ability granted with `RemoveOnEnd` is removed by the very same call.

**Failed activations.** Whoever calls the activation API already receives [`AbilityActivationFailures`](#activation-failures) as an out parameter. `OnAbilityActivationFailed` exists for the activations nobody holds the result of — those driven by [ability triggers](#ability-triggers) and by the Statescript activation nodes — which are otherwise completely silent:

```csharp
entity.Abilities.OnAbilityActivationFailed += (handle, failures) =>
{
    if (failures.HasFlag(AbilityActivationFailures.Cooldown))
    {
        FlashCooldown(handle);
    }
};
```

Handlers run inside the ability pipeline, synchronously, so keep them cheap. The [attribute](attributes.md#from-outside-the-attributeset), [tag](tags.md#reacting-to-tag-changes) and [effect](effects/README.md#observing-effects) change notifications cover the rest of an entity's observable state.

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
- **Activate(out failureFlags, target?, magnitude?)**: Attempt to activate the ability with optional target and magnitude.
- **Activate\<TData\>(data, out failureFlags, target?, magnitude?)**: Attempt to activate the ability passing additional typed activation data.
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

```csharp
var abilityData = new AbilityData(
    "Channeled Beam",
    instancingPolicy: AbilityInstancingPolicy.PerEntity,
    retriggerInstancedAbility: true);
```

With `retriggerInstancedAbility: true`, the active instance is canceled and a new one starts: 

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

The system receives an array of cooldown effects, allowing you to trigger multiple independent cooldowns at once (e.g., a long "Skill Cooldown" and a shorter "Global Cooldown").

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
// Ability has both a long cooldown and a global cooldown
var abilityData = new AbilityData(
    "Dash",
    cooldownEffects: [dashCooldownEffect, globalCooldownEffect]);
```

### Cooldown Reduction

There is no dedicated "cooldown reduction" primitive, and none is needed: a cooldown is a duration effect, and durations accept every [`ModifierMagnitude`](effects/modifiers.md#magnitude-calculation) type. Make the cooldown's duration `AttributeBased`, point it at a CDR attribute, and the reduction falls out of the existing machinery.

```csharp
// StatAttributeSet.CooldownReduction holds whole percent: 15 means 15% CDR.
// Base cooldown 5s, so each point of CDR removes 5 / 100 = 0.05s.
var fireballCooldown = new EffectData(
    "Fireball Cooldown",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.AttributeBased,
            attributeBasedFloat: new AttributeBasedFloat(
                new AttributeCaptureDefinition(
                    "StatAttributeSet.CooldownReduction",
                    AttributeCaptureSource.Target,
                    Snapshot: false),               // live: see below
                AttributeCalculationType.CurrentValue,
                Coefficient: new ScalableFloat(-0.05f),
                PreMultiplyAdditiveValue: new ScalableFloat(0),
                PostMultiplyAdditiveValue: new ScalableFloat(5f)))),   // the base cooldown
    effectComponents: [new ModifierTagsEffectComponent(cooldownTags)]);
```

The magnitude formula is `(coefficient * (CDR + preMultiply)) + postMultiply`, so `postMultiply` carries the base cooldown and `coefficient` is `-baseCooldown / 100`. Pass a `LookupCurve` if you need to floor the result, so stacked CDR cannot drive a cooldown to zero.

Two details make this behave the way players expect:

- **Capture from `Target`.** Cooldown effects are applied to the ability's owner, so the owner is the effect's target and their CDR attribute is the one being read.
- **`Snapshot: false` re-evaluates while the cooldown is running.** Gaining CDR mid-cooldown shortens the *remaining* time immediately, and losing it lengthens it; a cooldown whose remaining time drops to zero this way ends right there. Use `Snapshot: true` instead if the cooldown should be locked in at the moment it was committed.

The same composition covers haste, recharge and "reduce cooldown by X on crit" mechanics — the last one by having the proc effect modify the CDR attribute rather than by reaching into the cooldown itself.

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
- If the modifier is **negative** (consumption), it tests against the attribute's **Minimum Value**. (e.g., Do I have enough Mana to pay -30 without going below 0?)
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

1. **Ending Instances**: It is up to the developer to call `context.InstanceHandle.End()` when the ability logic is complete. If you fail to do this, the system will consider the ability "Active" indefinitely.
2. **Committing**: Resources and Cooldowns are not applied automatically. You must call `context.AbilityHandle.CommitAbility()` (or `CommitCost` / `CommitCooldown` separately).
   - `CommitAbility()` calls both `CommitCost()` and `CommitCooldown()`.
   - Do **not** call all three; it is redundant.
   - Deferring commits allows for mechanics like "free cast if cancelled early."

**Note**: It is entirely possible to **not end** an ability. This is useful for passive abilities or toggles that should run continuously until cancelled externally or by tag triggers.

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
- **Magnitude**: A numeric value associated with the activation attempt.

### Behavior Context \<TData\>

In addition to the core fields, the generic behavior context also carries:

- **Data**: Optional strongly-typed activation data when using generic activation or event triggers.

This context is primarily consumed by behaviors implementing `IAbilityBehavior<TData>`, allowing abilities to react to activation-specific data.

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

Abilities can be automatically activated in response to events or tag changes. Use the static factory methods on `AbilityTriggerData` to create trigger configurations:

| Factory method | Trigger |
|---|---|
| `AbilityTriggerData.ForEvent(tag, priority = 0)` | An event carrying `tag` is raised |
| `AbilityTriggerData.ForEvent<TPayload>(tag, priority = 0)` | An event carrying `tag` **and** a `TPayload` payload is raised |
| `AbilityTriggerData.ForTagAdded(tag)` | `tag` is added to the entity |
| `AbilityTriggerData.ForTagPresent(tag)` | `tag` is present on the entity (activates on add, cancels on remove) |

`AbilityTriggerData` has no public constructor — the factory methods are the only way to build one, which is what keeps the trigger tag and trigger source consistent with each other. The `AbilityTriggerSource` enum behind them is internal to that decision; you should never need to name it.

### Event Trigger

Activate when a specific event is raised:

```csharp
var blockedTag = Tag.RequestTag(tagsManager, "events.combat.blocked");

var abilityData = new AbilityData(
    "Counter Attack",
    abilityTriggerData: AbilityTriggerData.ForEvent(blockedTag));

// Later, when the entity blocks an attack:
entity.Events.Raise(new EventData
{
    EventTags = blockedTag.GetSingleTagContainer()!,
    Source = attacker,
    Target = entity
});
// Counter Attack activates automatically
```

For abilities that need access to a typed event payload, use the generic overload. The payload is forwarded to the behavior's `OnStarted` method when the behavior implements `IAbilityBehavior<TPayload>`:

```csharp
var abilityData = new AbilityData(
    "Counter Attack",
    abilityTriggerData: AbilityTriggerData.ForEvent<DamageInfo>(blockedTag));
```

### Tag Added Trigger

Activate when a tag is added to the entity:

```csharp
var abilityData = new AbilityData(
    "Rage",
    abilityTriggerData: AbilityTriggerData.ForTagAdded(
        Tag.RequestTag(tagsManager, "status.enraged")));

// When the entity gains the "status.enraged" tag, Rage activates
```

### Tag Present Trigger

Stay active while a tag is present. This acts as a toggle:

```csharp
var abilityData = new AbilityData(
    "Burning Aura",
    abilityTriggerData: AbilityTriggerData.ForTagPresent(
        Tag.RequestTag(tagsManager, "status.on_fire")));

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
// Other abilities can check for this tag in their requirements
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

The grant's `InhibitionPolicy` — an [`AbilityDeactivationPolicy`](#deactivation-policies), set on `GrantAbilityConfig` — decides what happens to an ability that is already running when its grant becomes inhibited:

- **CancelImmediately**: the active instances are canceled and the ability is inhibited right away.
- **RemoveOnEnd**: the active ability keeps running and only becomes inhibited once it ends.
- **Ignore**: the grant source ignores inhibition entirely.

Inhibition is cumulative across grant sources — see [Policy Interactions Between Grant Sources](#policy-interactions-between-grant-sources). Adding a new grant to an inhibited ability re-enables it.

Abilities granted permanently via `GrantAbilityPermanently` cannot be inhibited.

## Ability Activation Context

Ability activation supports passing additional contextual information at runtime. This context represents **dynamic execution data**, not static ability configuration.

Forge exposes this data through the ability behavior context during activation.

### Magnitude

`Magnitude` is a numeric value associated with an activation attempt.

- It can be passed explicitly when calling `AbilityHandle.Activate(...)`.
- It is automatically populated when abilities are triggered by **Event Triggers**.
- It is accessible via `context.Magnitude` inside the behavior.

Typical use cases include damage scaling, impulse strength, or contextual intensity values.

### Strongly-Typed Activation Data

For cases where a numeric magnitude is not sufficient, abilities can receive strongly-typed activation data.

This is done using the generic activation method:

```csharp
handle.Activate<HitLocationData>(
    new HitLocationData(HitZone.Head),
    out AbilityActivationFailures failures,
    target: enemy);
```

When using this overload, Forge automatically creates an `AbilityBehaviorContext<TData>` instance.

### AbilityBehaviorContext<TData>

When activated with typed data, the behavior receives an `AbilityBehaviorContext<TData>`, which provides:

- All standard ability context fields.
- Strongly-typed activation data via `context.Data`.

```csharp
public sealed class HitReactionBehavior : IAbilityBehavior<HitLocationData>
{
    public void OnStarted(AbilityBehaviorContext context, HitLocationData data)
    {
        context.AbilityHandle.CommitAbility();

        switch (data.Zone)
        {
            case HitZone.Head:
                ApplyCriticalDamage(context.Target);
                break;

            case HitZone.Arm:
                ApplyDisarm(context.Target);
                break;

            case HitZone.Leg:
                ApplySlow(context.Target);
                break;

            default:
                ApplyBaseDamage(context.Target);
                break;
        }

        context.InstanceHandle.End();
    }

    public void OnEnded(AbilityBehaviorContext context)
    {
        // Cleanup if needed
    }
}

```

### Event Triggers and Context Propagation

Abilities triggered by Event Triggers are the only automatic source of activation context.

- `EventMagnitude` is mapped to `context.Magnitude`.
- `EventData<TPayload>.Payload` is mapped to `context.Data`.

This allows external systems to inject runtime context into abilities without direct activation calls.

```csharp
entity.Events.Raise(new EventData<HitLocationData>
{
    EventTags = hitEventTags,
    Target = enemy,
    EventMagnitude = 1.0f,
    Payload = new HitLocationData(HitZone.Arm)
});
```

### Context Design Guidelines

- Context data should represent execution-specific state.
- Do not use activation data for static ability configuration.
- Prefer typed data over loosely structured objects.
- Event Triggers are ideal for world-driven context injection.

## Statescript Integration

Abilities can be driven by Statescript graphs instead of handwritten `IAbilityBehavior` classes. This is done through `GraphAbilityBehavior`, which connects the ability lifecycle to a graph's execution:

- When the ability **starts**, the graph begins processing from its Entry node.
- Each frame, `OnUpdate(deltaTime)` advances all active state nodes in the graph.
- When the graph **completes** (all state nodes deactivate) or an Exit node is reached, the ability instance ends.
- When the ability is **canceled**, the graph is stopped and all active nodes are disabled.

### GraphAbilityBehavior

```csharp
var graph = new Graph();
// ... build graph with nodes and connections ...

var behavior = new GraphAbilityBehavior(graph);

var abilityData = new AbilityData(
    "Fireball",
    instancingPolicy: AbilityInstancingPolicy.PerExecution,
    behaviorFactory: () => behavior);
```

### GraphAbilityBehavior&lt;TData&gt;

For abilities that receive typed activation data, use the generic variant. It can either expose supported payload members directly through `AbilityActivationDataResolver`:

```csharp
graph.VariableDefinitions.DefineProperty(
    "Distance",
    new AbilityActivationDataResolver(typeof(DashData), nameof(DashData.Distance)));

var behavior = new GraphAbilityBehavior<DashData>(graph);
```

Or use a data binder when you need to map or convert activation fields into graph variables:

```csharp
var behavior = new GraphAbilityBehavior<DashData>(graph, (data, variables) =>
{
    variables.SetVar(new StringKey("Distance"), data.Distance);
    variables.SetVar(new StringKey("Speed"), data.Speed);
});
```

For detailed documentation on Statescript concepts, see the [Statescript documentation](statescript/README.md).

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
13. **Use Activation Context for Runtime Data**: Pass external execution data via activation context, preferring typed data.
