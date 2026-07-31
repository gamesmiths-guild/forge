# GrantAbilityEffectComponent

> **Type:** `Gamesmiths.Forge.Effects.Components.GrantAbilityEffectComponent`
> **State:** Stateful — returns a new instance from `CreateInstance`
> **Applies to:** Any effect

Grants one or more abilities to the target entity. This is the primary bridge between the Effects system and the [Abilities system](../../abilities.md).

## Constructor

```csharp
new GrantAbilityEffectComponent(grantAbilityConfigs)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| grantAbilityConfigs | `GrantAbilityConfig[]` | One configuration per ability to grant. Each carries the ability data, a `ScalableInt` level evaluated against the granting effect's level, and the removal, inhibition and level-override policies. |

The component exposes the handles it created:

```csharp
public IReadOnlyList<AbilityHandle> GrantedAbilities { get; }
```

## Lifecycle Hooks

| Hook | What this component does |
|------|--------------------------|
| `OnActiveEffectAdded` | Grants each configured ability, tied to the effect's lifecycle. |
| `OnPostActiveEffectAdded` | Inhibits the grants if the effect landed inhibited; otherwise honours `TryActivateOnGrant`. |
| `OnActiveEffectChanged` | Tracks inhibition flips, inhibiting or re-enabling the grants and honouring `TryActivateOnEnable`. |
| `OnActiveEffectUnapplied` | Removes the grants when the effect is fully removed. |
| `OnEffectExecuted` | Grants the abilities **permanently** — the instant-effect path. |

## Behavior

- On a **duration** or **infinite** effect the abilities live only as long as the effect, subject to each config's `RemovalPolicy`.
- On an **instant** effect the abilities are granted permanently, exactly as if `EntityAbilities.GrantAbilityPermanently` had been called.
- Ability level comes from `ScalableLevel` evaluated against the **granting effect's level**, so applying the effect at level 5 can grant a level-5 ability.
- Inhibiting the effect inhibits its grants according to each config's `InhibitionPolicy`.

## Usage

```csharp
var grantConfig = new GrantAbilityConfig(
    AbilityData: fireballData,
    ScalableLevel: new ScalableInt(1), // Scales with effect level if a curve is defined
    RemovalPolicy: AbilityDeactivationPolicy.CancelImmediately, // Cancels running instances immediately when effect ends
    InhibitionPolicy: AbilityDeactivationPolicy.CancelImmediately, // Cancels running instances immediately if effect is inhibited
    TryActivateOnGrant: false, // Do not try to activate automatically when granted
    TryActivateOnEnable: false, // Do not try to activate automatically when enabled back from inhibition
    LevelOverridePolicy: LevelComparison.Higher // Update level if higher than existing grant
);

// Keep a reference to the component if you need to access the granted ability handles later
var grantComponent = new GrantAbilityEffectComponent([grantConfig]);

var grantEffect = new EffectData(
    "Grant Fireball",
    new DurationData(DurationType.Infinite),
    effectComponents: [grantComponent]
);

// Apply the effect
entity.EffectsManager.ApplyEffect(new Effect(grantEffect, ownership));

// Access the handle directly from the component instance
AbilityHandle fireballHandle = grantComponent.GrantedAbilities[0];
```

## Key Points

- **Direct handle access**: keep a reference to the component instance to read its `GrantedAbilities`, which holds the `AbilityHandle`s created by this specific effect application. Alternatively call the effect handle's `GetComponent<GrantAbilityEffectComponent>()` to retrieve the runtime instance when needed — see [Accessing Component Instances at Runtime](README.md#accessing-component-instances-at-runtime).
- **Lifecycle management**: granting, removing and inhibiting are all handled automatically from the effect's lifecycle and the configured policies.
- **Permanent vs. temporary**: instant effects grant permanently; duration effects grant for the effect's lifetime, unless `RemovalPolicy` is `Ignore`.

## See Also

- [Effect Components Overview](README.md)
- [Abilities](../../abilities.md)
- [BlockAbilityTagsEffectComponent](block-ability-tags-effect-component.md)
- [CancelAbilityTagsEffectComponent](cancel-ability-tags-effect-component.md)
