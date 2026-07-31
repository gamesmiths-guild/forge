# ModifierTagsEffectComponent

> **Type:** `Gamesmiths.Forge.Effects.Components.ModifierTagsEffectComponent`
> **State:** Stateless — shared across every application
> **Applies to:** Duration effects only

Adds tags to the target entity while the effect is active. These tags are automatically removed once the effect ends. See the [Tags documentation](../../tags.md) for more on tags.

## Constructor

```csharp
new ModifierTagsEffectComponent(tagsToAdd)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| tagsToAdd | `TagContainer` | The tags added to the target's `ModifierTags` for as long as the effect is active. |

## Lifecycle Hooks

| Hook | What this component does |
|------|--------------------------|
| `OnActiveEffectAdded` | Adds the tags to `target.Tags`. |
| `OnActiveEffectUnapplied` | Removes the tags, but only when the effect is fully removed. |

## Behavior

- Tags land on the target's `ModifierTags` container, which feeds `AllTags` alongside its base tags.
- Removal is refcounted: two active effects granting the same tag both have to end before the tag actually goes.
- **Inhibition is ignored.** An inhibited effect keeps its tags. This is deliberate, and is the one place [BlockAbilityTagsEffectComponent](block-ability-tags-effect-component.md) behaves differently.

## Validation

Instant effects are rejected — they have no lifetime over which to hold a tag, and the callbacks the component needs never fire for them.

## Usage

```csharp
// Create a "Burning" effect that adds the "Status.Burning" tag to the target
var burningEffectData = new EffectData(
    "Burning",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(10.0f)
        )
    ),
    new[] {
        new Modifier("CombatAttributeSet.CurrentHealth", ModifierOperation.Add, new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-5)))
    },
    periodicData: new PeriodicData(
        Period: new ScalableFloat(2.0f),
        ExecuteOnApplication: true,
        PeriodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
    ),
    effectComponents: new[] {
        new ModifierTagsEffectComponent(
            tagsManager.RequestTagContainer(new[] { "status.burning" })
        )
    }
);
```

## Key Points

- Only works with duration effects (not instant).
- Tags are automatically added when the effect is applied.
- Tags are automatically removed when the effect ends completely.
- With stacked effects, tags remain until all stacks are removed.
- Granted tags represent **entity state**. They are what other systems key off — a debuff bar, a tag requirement, an AI check.

## See Also

- [Effect Components Overview](README.md)
- [Tags](../../tags.md)
- [TargetTagRequirementsEffectComponent](target-tag-requirements-effect-component.md)
- [BlockAbilityTagsEffectComponent](block-ability-tags-effect-component.md)
