# CancelAbilityTagsEffectComponent

> **Type:** `Gamesmiths.Forge.Effects.Components.CancelAbilityTagsEffectComponent`
> **State:** Stateless — shared across every application
> **Applies to:** Any effect

Cancels the target's active abilities. Abilities carrying any of `withTags` and none of `withoutTags` are canceled, either once on application or on each execution. See the [Abilities documentation](../../abilities.md) for more on ability tags.

## Constructor

```csharp
new CancelAbilityTagsEffectComponent(withTags, withoutTags, policy)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| withTags | `TagContainer?` | Abilities must carry any of these tags to be canceled. `null` or empty means "don't filter on this side". Defaults to `null`. |
| withoutTags | `TagContainer?` | Abilities carrying any of these tags are spared. `null` or empty means "don't filter on this side". Defaults to `null`. |
| policy | `CancelAbilityTagsPolicy` | When the cancellation runs. Defaults to `OnApplication`. |

### CancelAbilityTagsPolicy

| Policy | Fires on | Notes |
|--------|----------|-------|
| `OnApplication` | `OnEffectApplied` | Every effect, including instant ones. Fires again for each successfully applied stack. |
| `OnExecution` | `OnEffectExecuted` | Only instant and periodic effects are executed, so a duration effect using this policy must be periodic. |

## Lifecycle Hooks

| Hook | What this component does |
|------|--------------------------|
| `OnEffectApplied` | Cancels the matching abilities, when the policy is `OnApplication`. |
| `OnEffectExecuted` | Cancels the matching abilities, when the policy is `OnExecution`. |

## Behavior

Both hooks delegate to `EntityAbilities.CancelAbilities(withTags, withoutTags)`. Each container is an independent filter, so the two combine as "carries any of these, and none of those".

An ability with no ability tags is never matched by `withTags`, but it always satisfies `withoutTags`, since it carries none of them.

Leaving **both** filters empty would mean "no filter" to `CancelAbilities`, canceling every active ability. For a component configured by tags that is a misconfiguration rather than an intent, so the component cancels nothing instead.

## Validation

- `OnExecution` on a duration effect that isn't periodic is rejected — it would never fire.
- A component with both filters empty is rejected, rather than silently canceling nothing at runtime.

## Usage

```csharp
// Create an "Interrupt" effect that cancels the target's channeled abilities,
// sparing the ones flagged as uninterruptible
var interruptEffectData = new EffectData(
    "Interrupt",
    new DurationData(DurationType.Instant),
    effectComponents: new[] {
        new CancelAbilityTagsEffectComponent(
            withTags: tagsManager.RequestTagContainer(new[] { "ability.channeled" }),
            withoutTags: tagsManager.RequestTagContainer(new[] { "ability.uninterruptible" }),
            policy: CancelAbilityTagsPolicy.OnApplication
        )
    }
);
```

Canceling repeatedly from a periodic effect:

```csharp
// A suppression field that re-interrupts every second for as long as it lasts
var suppressionFieldData = new EffectData(
    "Suppression Field",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(6.0f)
        )
    ),
    periodicData: new PeriodicData(
        Period: new ScalableFloat(1.0f),
        ExecuteOnApplication: true,
        PeriodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
    ),
    effectComponents: new[] {
        new CancelAbilityTagsEffectComponent(
            withTags: tagsManager.RequestTagContainer(new[] { "ability.channeled" }),
            policy: CancelAbilityTagsPolicy.OnExecution
        )
    }
);
```

## Key Points

- Works on any effect, including instant ones. This is the usual way to cancel abilities from an effect.
- Supplying only `withoutTags` cancels everything except the abilities carrying those tags.
- Canceling is a one-shot action; it does not prevent the ability from being activated again the next frame. Pair it with [BlockAbilityTagsEffectComponent](block-ability-tags-effect-component.md) to also keep abilities from restarting.
- The component is stateless and is shared across all applications of its effect.

## See Also

- [Effect Components Overview](README.md)
- [Abilities](../../abilities.md)
- [BlockAbilityTagsEffectComponent](block-ability-tags-effect-component.md)
- [Periodic Effects](../periodic.md)
