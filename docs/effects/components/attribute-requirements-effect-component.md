# AttributeRequirementsEffectComponent

> **Type:** `Gamesmiths.Forge.Effects.Components.AttributeRequirementsEffectComponent`
> **State:** Stateful — returns a new instance from `CreateInstance`
> **Applies to:** Any effect

Gates an effect on the target's attribute values: "only lands below 50% health", "drops when mana hits 0", "is suppressed while stamina is empty". It is the attribute-side twin of [TargetTagRequirementsEffectComponent](target-tag-requirements-effect-component.md), with the same three buckets and the same reactive behavior.

## Constructor

```csharp
new AttributeRequirementsEffectComponent(applicationRequirements, removalRequirements, ongoingRequirements)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| applicationRequirements | `AttributeRequirement[]?` | Must be met at the moment of application, or the effect never lands. |
| removalRequirements | `AttributeRequirement[]?` | Once met, the effect is removed. |
| ongoingRequirements | `AttributeRequirement[]?` | While unmet, the effect is inhibited; it resumes when met again. |

All three are optional. The requirements inside a bucket are AND-combined; an empty or omitted bucket is not evaluated at all.

### AttributeRequirement

A single condition on one attribute.

```csharp
public readonly record struct AttributeRequirement(
    StringKey Attribute,
    float? MinValue = null,
    float? MaxValue = null,
    AttributeThresholdType ThresholdType = AttributeThresholdType.Absolute,
    AttributeCalculationType CalculationType = AttributeCalculationType.CurrentValue,
    int FinalChannel = 0)
```

| Field | Description |
|-------|-------------|
| `Attribute` | The fully qualified attribute key, e.g. `"CombatAttributeSet.CurrentHealth"`. |
| `MinValue` | Lowest accepted value, inclusive. `null` leaves the lower side unbounded. |
| `MaxValue` | Highest accepted value, inclusive. `null` leaves the upper side unbounded. |
| `ThresholdType` | Whether the bounds are raw values or percentages of the attribute's `Max`. |
| `CalculationType` | Which value to read — `CurrentValue`, `BaseValue`, `Modifier`, `Min`, `Max`, and so on. |
| `FinalChannel` | Only used by `AttributeCalculationType.MagnitudeEvaluatedUpToChannel`. |

`AttributeThresholdType` decides how the bounds are read:

| Threshold type | Bounds mean |
|----------------|-------------|
| `Absolute` | Raw attribute values, compared directly. |
| `PercentOfMax` | Percentages from 0 to 100, measured against the attribute's `Max`. An attribute whose `Max` is zero or negative resolves to 0%. |

## Lifecycle Hooks

| Hook | What this component does |
|------|--------------------------|
| `CanApplyEffect` | Rejects the application if the application requirements are unmet, or the removal requirements are already met. |
| `OnActiveEffectAdded` | Subscribes to every watched attribute's `OnValueChanged` and reports the initial inhibition state. |
| `OnActiveEffectUnapplied` | Unsubscribes when the effect is fully removed. |

## Behavior

The component is **reactive**: it re-evaluates whenever any watched attribute changes, not only at application. On each change it removes the effect if the removal requirements are now met, otherwise toggles inhibition from the ongoing requirements.

Only the attributes named in the **removal** and **ongoing** buckets are watched. Application requirements are consulted once, in `CanApplyEffect`, so watching them would only produce no-op callbacks. An attribute appearing in several buckets is subscribed to once.

A requirement naming an attribute the entity does not have is **never met**. A gate on health cannot be satisfied by an entity with no health.

Attribute changes are flushed at the end of an effect application, so a component reacts to values that have already settled — including cascades, such as a `MaxHealth` change clamping `CurrentHealth`.

## Validation

- Every `AttributeRequirement` must define a `MinValue`, a `MaxValue`, or both. One with neither bound always passes, which is never the intent, so `EffectData` rejects it.
- Ongoing requirements are rejected on an instant effect, since inhibition acts on an active effect and instant effects never become one.

## Usage

```csharp
// An "Execute" effect that only lands on a target at or below 25% health
var executeEffectData = new EffectData(
    "Execute",
    new DurationData(DurationType.Instant),
    [/*...*/],
    effectComponents: new[] {
        new AttributeRequirementsEffectComponent(
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

Suppressing an aura while a resource runs dry, and dropping it entirely when the target dies:

```csharp
var channelledAuraData = new EffectData(
    "Channelled Aura",
    new DurationData(DurationType.Infinite),
    [/*...*/],
    effectComponents: new[] {
        new AttributeRequirementsEffectComponent(
            // Inhibited whenever mana falls to zero, and active again once it recovers
            ongoingRequirements: [
                new AttributeRequirement("CombatAttributeSet.CurrentMana", MinValue: 1)
            ],
            // Removed outright when health hits zero
            removalRequirements: [
                new AttributeRequirement("CombatAttributeSet.CurrentHealth", MaxValue: 0)
            ]
        )
    }
);
```

## Key Points

- Requirements within a bucket are AND-combined. Use several requirements to gate on several attributes at once.
- `MinValue` and `MaxValue` are both inclusive, and either can be omitted for a one-sided bound.
- `PercentOfMax` reads the attribute's own `Max`, so it tracks a max that changes at runtime.
- A requirement on an attribute the entity lacks is never met, so the effect is denied rather than silently allowed.
- On an instant effect only `applicationRequirements` is fully meaningful. Ongoing requirements are rejected outright; removal requirements still deny application when already met, but nothing can be removed afterwards.
- GAS makes you write a `CustomApplicationRequirement` for this.

## See Also

- [Effect Components Overview](README.md)
- [SourceAttributeRequirementsEffectComponent](source-attribute-requirements-effect-component.md)
- [TargetTagRequirementsEffectComponent](target-tag-requirements-effect-component.md)
- [SourceTagRequirementsEffectComponent](source-tag-requirements-effect-component.md)
- [Attributes](../../attributes.md)
