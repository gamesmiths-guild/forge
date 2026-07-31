# SourceAttributeRequirementsEffectComponent

> **Type:** `Gamesmiths.Forge.Effects.Components.SourceAttributeRequirementsEffectComponent`
> **State:** Stateful — returns a new instance from `CreateInstance`
> **Applies to:** Any effect

Gates an effect on the attribute values of its **source** rather than its target. This is [AttributeRequirementsEffectComponent](attribute-requirements-effect-component.md) pointed at the other end of the effect, completing the pair that `TargetTagRequirements` and `SourceTagRequirements` form for tags. It expresses conditions the target's own attributes cannot reach — "this execute only lands while the attacker is above half rage", or "the channelled beam suppresses itself when its caster runs out of mana".

## Constructor

```csharp
new SourceAttributeRequirementsEffectComponent(
    applicationRequirements,
    removalRequirements,
    ongoingRequirements,
    ownershipEntity)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| applicationRequirements | `AttributeRequirement[]?` | Must be met on the source at the moment of application, or the effect never lands. |
| removalRequirements | `AttributeRequirement[]?` | Once met on the source, the effect is removed. |
| ongoingRequirements | `AttributeRequirement[]?` | While unmet on the source, the effect is inhibited. |
| ownershipEntity | `OwnershipEntity` | Which ownership entity supplies the attributes. Defaults to `Source`. |

All the buckets are optional and reuse the same [`AttributeRequirement`](attribute-requirements-effect-component.md#attributerequirement) type the target-side component uses, including its `AttributeThresholdType` and `AttributeCalculationType` options.

### OwnershipEntity

| Value | Reads |
|-------|-------|
| `Source` | `EffectOwnership.Source` — what actually caused the effect (the weapon, the trap, the caster). |
| `Owner` | `EffectOwnership.Owner` — who triggered the action that caused it. |

## Lifecycle Hooks

| Hook | What this component does |
|------|--------------------------|
| `CanApplyEffect` | Rejects the application if the source's application requirements are unmet, or its removal requirements are already met. |
| `OnActiveEffectAdded` | Subscribes to the *source's* watched attributes and reports the initial inhibition state. |
| `OnActiveEffectUnapplied` | Unsubscribes when the effect is fully removed. |

## Behavior

The component is **reactive**, but it watches the source entity's attributes rather than the target's. When one of them changes it removes the effect if the removal requirements are now met, otherwise toggles inhibition from the ongoing requirements.

As on the target side, only the attributes named in the **removal** and **ongoing** buckets are watched, and an attribute appearing in several buckets is subscribed to once.

A **null source** satisfies nothing, so any non-empty bucket evaluated against it is unmet: application is denied, ongoing requirements inhibit, and removal requirements never fire. An empty bucket is still not evaluated at all, so a component that only constrains the target side is unaffected by a missing source. There is also nothing to subscribe to, so the requirements stay frozen at their initial evaluation.

## Validation

- Every `AttributeRequirement` must define a `MinValue`, a `MaxValue`, or both.
- Ongoing requirements are rejected on an instant effect, since inhibition acts on an active effect and instant effects never become one.

## Usage

```csharp
// A desperate strike that only lands while the attacker is at or below a quarter of their health
var desperateStrikeData = new EffectData(
    "Desperate Strike",
    new DurationData(DurationType.Instant),
    [/*...*/],
    effectComponents: new[] {
        new SourceAttributeRequirementsEffectComponent(
            applicationRequirements: [
                new AttributeRequirement(
                    "CombatAttributeSet.CurrentHealth",
                    MaxValue: 25,
                    ThresholdType: AttributeThresholdType.PercentOfMax)
            ]
        )
    }
);
```

A channelled beam that suppresses itself when its caster runs dry, and breaks when they fall:

```csharp
var channelledBeamData = new EffectData(
    "Channelled Beam",
    new DurationData(DurationType.Infinite),
    [/*...*/],
    effectComponents: new[] {
        new SourceAttributeRequirementsEffectComponent(
            ongoingRequirements: [
                new AttributeRequirement("CombatAttributeSet.CurrentMana", MinValue: 1)
            ],
            removalRequirements: [
                new AttributeRequirement("CombatAttributeSet.CurrentHealth", MaxValue: 0)
            ],
            ownershipEntity: OwnershipEntity.Owner
        )
    }
);
```

## Key Points

- The target's own attributes never satisfy these requirements — that is the whole point. Use [AttributeRequirementsEffectComponent](attribute-requirements-effect-component.md) for the target side, and both together when a condition spans the two.
- Reacts to the source's attribute changes, so a link can follow its caster's state after application.
- A null source satisfies no non-empty bucket, and never becomes reactive.
- Completes the four-way symmetry: target/source × tags/attributes.

## See Also

- [Effect Components Overview](README.md)
- [AttributeRequirementsEffectComponent](attribute-requirements-effect-component.md)
- [SourceTagRequirementsEffectComponent](source-tag-requirements-effect-component.md)
- [Attributes](../../attributes.md)
