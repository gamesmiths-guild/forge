# AttributeAccumulatorEffectComponent

> **Type:** `Gamesmiths.Forge.Effects.Components.AttributeAccumulatorEffectComponent`
> **State:** Stateful — returns a new instance from `CreateInstance`
> **Applies to:** Instant and periodic effects

Tallies how much **this application** has moved one of the target's attributes, and publishes the running total as a `SetByCaller` magnitude on its own effect. It answers "how much did this actually do?" — a question the effect itself has no way to record, since its declared magnitudes describe intent rather than outcome.

The total is readable from anywhere a `SetByCaller` magnitude is, which is what makes it useful: an effect applied by [AdditionalEffectsEffectComponent](additional-effects-effect-component.md) with `copyDataFromOriginalEffect` inherits it, a [RaiseEventEffectComponent](raise-event-effect-component.md) can report it, and a Statescript graph can read it. That covers a curse healing its caster for the damage it dealt, lifesteal over time, a shield reporting what it absorbed, and damage-meter UI — none of which need a `CustomExecution`.

## Constructor

```csharp
new AttributeAccumulatorEffectComponent(attribute, magnitudeTag, policy)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| attribute | `StringKey` | The fully qualified key of the attribute to measure on the target. |
| magnitudeTag | `Tag` | The tag the running total is published under. |
| policy | `AccumulationPolicy` | Which movements to count. Defaults to `Losses`. |

### AccumulationPolicy

| Value | Counts | Sign |
|-------|--------|------|
| `Losses` | Only the executions that lowered the attribute. | Positive |
| `Gains` | Only the executions that raised the attribute. | Positive |
| `Net` | Every execution, so gains and losses cancel. | Signed — positive means it ended up higher |

## Lifecycle Hooks

| Hook | What this component does |
|------|--------------------------|
| `OnActiveEffectAdded` | Seeds the tag to zero, takes the first baseline, and subscribes to the attribute's `OnValueChanged`. |
| `OnEffectApplied` | Does the same for an instant effect, which never becomes active. No subscription: it executes once, immediately. |
| `OnEffectExecuted` | Measures the attribute against the baseline, adds the result under the policy, and republishes the total. |
| `OnActiveEffectUnapplied` | Unsubscribes on full removal. A stack removal changes nothing. |

## Behavior

### Why it measures the attribute

Summing `EffectEvaluatedData.ModifiersEvaluatedData[].Magnitude` is the obvious implementation and it is wrong twice over:

1. **It counts what the target could not absorb.** That array holds the *intended* magnitudes. A curse aiming 5 per tick at a pool holding 5 tallies 10 over two ticks where only 5 landed, and a payout built from it refunds the overkill.
2. **It never contains `CustomExecution` output.** `Effect.Execute` keeps execution modifiers in a local list that never reaches the evaluated data. An effect whose damage comes out of a resistance formula — which is where damage usually comes from — tallies **zero**.

Measuring the attribute across the execution gets both right, since the attribute has already been clamped by its own `Min` and `Max` by the time it is read.

### How the measurement stays honest

The reading is taken in `OnEffectExecuted`, which runs after the modifiers have landed but before `ApplyPendingValueChanges`, against a baseline held from the previous execution. The baseline is kept current by an `OnValueChanged` subscription that absorbs **every** change the component did not cause — other effects, direct writes, another copy of the same curse from a different caster. Each measurement therefore spans this effect's own execution and nothing else.

- **Each application has its own total.** `CreateInstance` isolates it, so two curses on one victim pay their own casters their own figures. An attribute on the entity could not do this.
- **The tag is seeded to zero on arrival**, before anything executes, so an effect removed before its first tick pays out nothing rather than tripping the [unset-magnitude check](../README.md#a-magnitude-nobody-set). The seed is a real dictionary entry, so `copyDataFromOriginalEffect` carries a genuine zero to anything the effect applies.
- **An execution that moves nothing publishes nothing.** The published total is already current, and republishing it would re-evaluate anything keyed on the tag for no change.
- **A missing attribute publishes zero** rather than throwing, matching how modifiers naming an absent attribute are skipped.
- **The total is also readable directly** through `Total`, via `handle.GetComponent<AttributeAccumulatorEffectComponent>()`, for a damage meter that wants the number without a `SetByCaller` round trip.

### One attribute per component

This is deliberate. To tally two — damage stopped by a shield as well as damage through to health — use two components with two tags. Read them separately, or add them together by giving the consuming effect two modifiers on the same attribute, which sum.

Folding both into one component would throw the split away, and there is no single sensible reading of `Losses` across a set of attributes that moved in opposite directions in the same execution.

## Validation

- **A duration effect that is not periodic is rejected.** The tally is taken as the effect executes, and only instant and periodic effects execute, so the total would always be zero. Make the effect periodic, or put the component on the effect that does the executing.

## Usage

A shield reporting what it absorbed, and a regeneration reporting what it actually restored:

```csharp
new AttributeAccumulatorEffectComponent("CombatAttributeSet.Shield", absorbedTag);
new AttributeAccumulatorEffectComponent("CombatAttributeSet.CurrentHealth", restoredTag, AccumulationPolicy.Gains);
```

Both halves of a hit that a shield partly stopped, kept apart so the UI can report them separately and the payout can still use the sum:

```csharp
effectComponents: new IEffectComponent[] {
    new AttributeAccumulatorEffectComponent("CombatAttributeSet.Shield", absorbedTag),
    new AttributeAccumulatorEffectComponent("CombatAttributeSet.CurrentHealth", throughTag)
}

// The payout adds them by declaring one modifier per tag; two FlatBonus modifiers on one attribute sum.
var siphonData = new EffectData(
    "Siphon",
    new DurationData(DurationType.Instant),
    [
        new Modifier("CombatAttributeSet.CurrentHealth", ModifierOperation.FlatBonus,
            new ModifierMagnitude(MagnitudeCalculationType.SetByCaller, setByCallerFloat: new SetByCallerFloat(absorbedTag))),
        new Modifier("CombatAttributeSet.CurrentHealth", ModifierOperation.FlatBonus,
            new ModifierMagnitude(MagnitudeCalculationType.SetByCaller, setByCallerFloat: new SetByCallerFloat(throughTag)))
    ]);
```

Reading the total back is somebody else's job — usually [AdditionalEffectsEffectComponent](additional-effects-effect-component.md) with `copyDataFromOriginalEffect`, which carries the tag to whatever it applies. The [quick start](../../quick-start.md#advanced-composing-components) builds that pairing end to end: a curse that heals its caster for the damage it dealt.

## Key Points

- **`Losses` reports a positive number.** So does `Gains`. Only `Net` is signed, and it is positive when the attribute ended up higher.
- **The total is published on the `Effect`, not on the application.** `DataTag` lives on the `Effect` instance, so one `Effect` reused across three targets has one dictionary and three colliding tallies. Build a fresh `Effect` per cast.
- **A magnitude reading the total while it climbs must be non-snapshot.** `SetByCallerFloat` snapshots by default, caching the first value it reads. An effect built fresh at payout time — the usual case — reads the final total correctly either way, but a per-tick reader needs `new SetByCallerFloat(tag, false)`.
- **Component order matters when something else reads the tag in the same hook.** The accumulator publishes from `OnEffectExecuted`; anything reading the tally from that same hook must sit after it in the `EffectComponents` array.
- **It measures the target only.** There is no source-side twin, because the source is not the entity the effect executes on.

## See Also

- [Effect Components Overview](README.md)
- [AdditionalEffectsEffectComponent](additional-effects-effect-component.md)
- [RaiseEventEffectComponent](raise-event-effect-component.md)
- [AttributeRequirementsEffectComponent](attribute-requirements-effect-component.md)
- [Modifiers and magnitudes](../README.md#modifiers)
