# BlockAbilityTagsEffectComponent

> **Type:** `Gamesmiths.Forge.Effects.Components.BlockAbilityTagsEffectComponent`
> **State:** Stateful — returns a new instance from `CreateInstance`
> **Applies to:** Duration effects only

Blocks the target's abilities while the effect is active. Abilities carrying any of the configured tags fail activation with `AbilityActivationFailures.BlockedByTags`, and are unblocked once the effect is removed. See the [Abilities documentation](../../abilities.md) for more on ability tags.

## Constructor

```csharp
new BlockAbilityTagsEffectComponent(tagsToBlock)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| tagsToBlock | `TagContainer` | Abilities carrying any of these tags cannot activate while the effect is active and uninhibited. |

## Lifecycle Hooks

| Hook | What this component does |
|------|--------------------------|
| `OnPostActiveEffectAdded` | Applies the blocks, unless the effect landed inhibited. |
| `OnActiveEffectChanged` | Drops or restores the blocks as the effect's inhibition state flips. |
| `OnActiveEffectUnapplied` | Removes the blocks when the effect is fully removed. |

The initial decision happens in `OnPostActiveEffectAdded` rather than `OnActiveEffectAdded` because inhibition is only settled after every component has processed `OnActiveEffectAdded`.

## Behavior

- The tags are added to `target.Abilities.BlockedAbilityTags`, the same container `BlockAbilitiesWithTag` uses while an ability is running.
- Blocking is **inhibition-aware**: an effect that lands inhibited blocks nothing, and blocks are dropped and restored as inhibition flips. An inhibited stun should not still lock abilities.
- Removal is refcounted, so blocks survive until every blocking effect is gone.
- Stack removals do not lift the blocks; only full removal does.

## Validation

Instant effects are rejected. They never become active effects, so the hooks this component relies on would never fire and the blocks would silently do nothing.

## Usage

```csharp
// Create a "Silence" effect that prevents the target from casting spells for 5 seconds
var silenceEffectData = new EffectData(
    "Silence",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(5.0f)
        )
    ),
    effectComponents: new[] {
        new BlockAbilityTagsEffectComponent(
            tagsManager.RequestTagContainer(new[] { "ability.spell" })
        )
    }
);
```

## Key Points

- Only works with duration effects (not instant); instant effects are rejected by validation.
- Inhibition-awareness is the one behavioral difference from [ModifierTagsEffectComponent](modifier-tags-effect-component.md), which is deliberately inhibition-blind.
- Blocks are only lifted when the effect is removed completely, so stacked effects keep them until the last stack goes.
- Blocking prevents *activation*; it does not cancel abilities that are already running. Pair it with [CancelAbilityTagsEffectComponent](cancel-ability-tags-effect-component.md) to do both.

## See Also

- [Effect Components Overview](README.md)
- [Abilities](../../abilities.md)
- [CancelAbilityTagsEffectComponent](cancel-ability-tags-effect-component.md)
- [ModifierTagsEffectComponent](modifier-tags-effect-component.md)
