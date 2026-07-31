# SourceTagRequirementsEffectComponent

> **Type:** `Gamesmiths.Forge.Effects.Components.SourceTagRequirementsEffectComponent`
> **State:** Stateful — returns a new instance from `CreateInstance`
> **Applies to:** Any effect

Validates tag requirements against the effect's **source** rather than its target. This is [TargetTagRequirementsEffectComponent](target-tag-requirements-effect-component.md) pointed at the other end of the effect, and it expresses conditions the target's own tags cannot reach — "this poison only applies if the attacker is Venomous", or "the aura suppresses itself while its caster is silenced".

## Constructor

```csharp
new SourceTagRequirementsEffectComponent(
    applicationTagRequirements,
    removalTagRequirements,
    ongoingTagRequirements,
    ownershipEntity)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| applicationTagRequirements | `TagRequirements?` | Must be met on the source at the moment of application, or the effect never lands. |
| removalTagRequirements | `TagRequirements?` | Once met on the source, the effect is removed. |
| ongoingTagRequirements | `TagRequirements?` | While unmet on the source, the effect is inhibited. |
| ownershipEntity | `OwnershipEntity` | Which ownership entity supplies the tags. Defaults to `Source`. |

All the requirement buckets are optional, and reuse the same [`TagRequirements`](target-tag-requirements-effect-component.md#the-tagrequirements-system) type the target-side component uses.

### OwnershipEntity

| Value | Reads |
|-------|-------|
| `Source` | `EffectOwnership.Source` — what actually caused the effect (the weapon, the trap, the caster). |
| `Owner` | `EffectOwnership.Owner` — who triggered the action that caused it. |

## Lifecycle Hooks

| Hook | What this component does |
|------|--------------------------|
| `CanApplyEffect` | Rejects the application if the source's application requirements are unmet, or its removal requirements are already met. |
| `OnActiveEffectAdded` | Subscribes to the *source's* `OnTagsChanged` and reports the initial inhibition state. |
| `OnActiveEffectUnapplied` | Unsubscribes when the effect is fully removed. |

## Behavior

The component is **reactive**, but it watches the source entity's tag container rather than the target's. When the source's tags change it removes the effect if the removal requirements are now met, otherwise toggles inhibition from the ongoing requirements.

A **null source** is evaluated against an empty tag container. That keeps the semantics honest: required tags cannot be satisfied by a missing source, while ignored tags trivially are. There is also nothing to subscribe to, so the requirements stay frozen at their initial evaluation for the life of the effect.

## Validation

Ongoing requirements are rejected on an instant effect, since inhibition acts on an active effect and instant effects never become one.

## Usage

```csharp
// A poison that only takes hold when the attacker is venomous, and fades if they lose that trait
var poisonEffectData = new EffectData(
    "Poison",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(8.0f)
        )
    ),
    [/*...*/],
    effectComponents: new[] {
        new SourceTagRequirementsEffectComponent(
            applicationTagRequirements: new TagRequirements(
                RequiredTags: tagsManager.RequestTagContainer(new[] { "trait.venomous" })
            ),
            removalTagRequirements: new TagRequirements(
                RequiredTags: tagsManager.RequestTagContainer(new[] { "status.cured" })
            )
        )
    }
);
```

Suppressing an aura while its caster is silenced:

```csharp
var auraEffectData = new EffectData(
    "Blessing Aura",
    new DurationData(DurationType.Infinite),
    [/*...*/],
    effectComponents: new[] {
        new SourceTagRequirementsEffectComponent(
            ongoingTagRequirements: new TagRequirements(
                IgnoreTags: tagsManager.RequestTagContainer(new[] { "status.silenced" })
            ),
            ownershipEntity: OwnershipEntity.Owner
        )
    }
);
```

## Key Points

- The target's own tags never satisfy these requirements — that is the whole point. Use [TargetTagRequirementsEffectComponent](target-tag-requirements-effect-component.md) for the target side, and both together when a condition spans the two.
- Reacts to the source's tag changes, so an aura can follow its caster's state after application.
- A null source fails required tags and passes ignored tags, and never becomes reactive.
- On an instant effect only `applicationTagRequirements` is fully meaningful. Ongoing requirements are rejected outright; removal requirements still deny application when already met, but nothing can be removed afterwards.
- Covers source-based gating without needing the `EffectQuery` machinery.

## See Also

- [Effect Components Overview](README.md)
- [TargetTagRequirementsEffectComponent](target-tag-requirements-effect-component.md)
- [SourceAttributeRequirementsEffectComponent](source-attribute-requirements-effect-component.md)
- [AttributeRequirementsEffectComponent](attribute-requirements-effect-component.md)
- [Tags](../../tags.md)
