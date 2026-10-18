# Ability Recipes

Defining, granting, activating and triggering abilities. Every guide here builds on the same small setup: a `PlayerAttributeSet`, a `Player` entity, and a `TagsManager` and `CuesManager`. It is defined once in [Guides](README.md#the-shared-setup); the code samples assume it.

Abilities are discrete actions that can have costs, cooldowns, and custom behaviors. They can be triggered manually, by events, or in reaction to tag application.

## Defining an Ability

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
        // Apply costs and cooldowns; a commit can fail if the resources went away
        if (!context.AbilityHandle.TryCommitAbility())
        {
            context.InstanceHandle.End();
            return;
        }

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

## Granting and Removing an Ability

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

// The granted handles live on the component instance of this application, not on grantAbilityComponent:
// the definition's component is a template, and each application gets its own copy through CreateInstance()
AbilityHandle grantedHandle =
    grantEffectHandle!.GetComponent<GrantAbilityEffectComponent>()!.GrantedAbilities[0];

// The ability is now granted. To remove it, remove the effect (e.g., when the wand is unequipped):
// player.EffectsManager.RemoveEffect(grantEffectHandle);
```

## Activating an Ability

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
        if (cost.Attribute == "PlayerAttributeSet.Mana")
        {
             Console.WriteLine($"Mana Cost: {cost.Cost}");
        }
    }

    // Try to activate
    if (handle.TryActivate(out AbilityActivationFailures failures))
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

## Granting an Ability Permanently

One other way to grant an ability permanently is directly through the entity's `EntityAbilities` component.

```csharp
// Grant permanently
AbilityHandle handle = player.Abilities.GrantAbilityPermanently(
    fireballData,
    abilityLevel: 1,
    levelOverridePolicy: LevelComparison.None,
    sourceEntity: player);
```

## Granting an Ability and Activating Once

In some cases you just want a quick way to activate an ability on a target without creating a persistent effect or granting it permanently.

The example below shows the use of a "Scroll of Fireball" that grants the fireball ability transiently, attempts to activate it immediately, and then removes the grant once the ability concludes or fails. `enemy` and `scrollItem` stand for any two other `IForgeEntity` instances.

```csharp
bool cast = player.Abilities.TryGrantAbilityAndActivateOnce(
    abilityData: fireballData,
    abilityLevel: 1,
    levelOverridePolicy: LevelComparison.None,
    out AbilityActivationFailures failureFlags,
    out AbilityHandle? handle,
    targetEntity: enemy,  // The target of the fireball
    sourceEntity: scrollItem // The source (e.g., the scroll item)
);

if (cast)
{
    Console.WriteLine("Scroll used successfully! Fireball cast.");
}
else 
{
    Console.WriteLine($"Failed to use scroll: {failureFlags}");
}
```

The `handle` is only set while the procced ability is **still running**. A fireball that finishes inside the call leaves it `null` even though the cast succeeded, so read the return value — not the handle — to know whether the ability activated.

## Triggering an Ability via Events

You can configure abilities to trigger automatically when specific events occur.

```csharp
var hitTag = Tag.RequestTag(tagsManager, "events.combat.hit");

var autoShieldData = new AbilityData(
    name: "Auto Shield",
    // Configure the trigger
    abilityTriggerData: AbilityTriggerData.ForEvent(hitTag),
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

## Triggering an Ability through Tags

In this example, a granted ability (like a passive aura) is activated automatically while the character has a specific tag (e.g., "status.enraged").

```csharp
// Define an ability that triggers when the "status.enraged" tag is present
var rageAbilityData = new AbilityData(
    "Rage Aura",
    abilityTriggerData: AbilityTriggerData.ForTagPresent(
        Tag.RequestTag(tagsManager, "status.enraged")),
    instancingPolicy: AbilityInstancingPolicy.PerEntity,
    behaviorFactory: () => new RageBehavior()); // Assumes RageBehavior exists

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
