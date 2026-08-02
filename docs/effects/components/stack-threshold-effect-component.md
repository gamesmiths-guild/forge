# StackThresholdEffectComponent

> **Type:** `Gamesmiths.Forge.Effects.Components.StackThresholdEffectComponent`
> **State:** Stateful — returns a new instance from `CreateInstance`
> **Applies to:** Stackable effects only

Applies further effects once its own stack count reaches a threshold — five stacks of Bleed causing a Hemorrhage — and, for each entry that asks to be taken back, removes it again as the count drops below.

This leans on Forge's stacking model rather than counting applications itself. Every stacking policy — aggregation by source or target, level segregation, overflow, expiration — decides what the count is; this component only decides what happens once it gets there.

Every entry is a [`ConditionalEffect`](additional-effects-effect-component.md#conditionaleffect), the same shape [AdditionalEffectsEffectComponent](additional-effects-effect-component.md) uses. Learn it once and it covers both: a threshold effect can be gated on the source's tags, pointed at an entity other than the target, and taken back by the stack count it hangs off.

## Constructor

```csharp
new StackThresholdEffectComponent(threshold, thresholdEffects, copyDataFromOriginalEffect)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| threshold | `int` | The stack count at which the threshold effects are applied. Must be two or more. |
| thresholdEffects | `ConditionalEffect[]` | The effects to apply, each with its own condition, target, and removal policy. |
| copyDataFromOriginalEffect | `bool` | Whether the threshold effects inherit this effect's `SetByCaller` magnitudes. Defaults to `false`. |

### What `RemovalPolicy` means here

The `ConditionalEffect`'s `RemovalPolicy` asks whether the applied effect outlives the condition that caused it. Each component decides what "the end" is; for a threshold it is the stack count falling back below. Entries keep their own policies, so one crossing can both raise something sustained by the count and fire something that then lives on its own.

| Value | Behavior |
|-------|----------|
| `RemoveOnEnd` | The effect is tied to the condition: applied while the count is at or above the threshold, taken back as soon as it drops below **or** the effect carrying the component ends, and applied again if the count climbs back. A Hemorrhage that subsides as the stacks do. |
| `Ignore` | The crossing is a trigger, not a sustaining condition: applied the first time the count reaches the threshold, then left alone — never removed here and never applied a second time. It lives by its own duration. |

`StacksToRemove` follows the same rule as everywhere else — it only means anything under `RemoveOnEnd`, and a negative value (the default) takes the effect entirely rather than a set number of its stacks.

## Lifecycle Hooks

| Hook | What this component does |
|------|--------------------------|
| `OnPostActiveEffectAdded` | Evaluates the initial stack count, so an effect arriving with enough stacks crosses on arrival. |
| `OnActiveEffectChanged` | Re-evaluates whenever the count changes, applying or taking back accordingly. |
| `OnActiveEffectUnapplied` | Takes the `RemoveOnEnd` entries back on full removal. |

## Behavior

- **The threshold is met at or above**, not strictly above: a threshold of 5 fires on the fifth stack.
- **Entries are evaluated in order, each independently.** A failed condition skips that entry and nothing else.
- **Climbing further changes nothing.** The threshold effects are applied once per crossing, not once per stack past it.
- **An effect whose `InitialStack` already meets the threshold crosses on arrival**, evaluated after the effect is fully applied rather than mid-application.
- **Under `RemoveOnEnd` the crossing can be earned again.** Falling below takes the effect back; climbing back up re-applies it. Under `Ignore` it fires exactly once for the lifetime of that application.
- **The threshold effects inherit ownership and evaluated level** from its applier, exactly as [AdditionalEffectsEffectComponent](additional-effects-effect-component.md)'s do. `copyDataFromOriginalEffect` carries the `SetByCaller` magnitudes over as well.
- **Each application tracks its own.** `CreateInstance` isolates the handles, so two attackers whose bleeds are kept apart by `StackPolicy.AggregateBySource` each reach their own threshold and each keep their own hemorrhage.
- **A threshold effect whose source condition fails is skipped**, and so is one pointed at an ownership entity the applier does not have — thorns on an effect with no source. Neither is redirected back at the target.
- **`StacksToRemove` decides how much is taken back**, so a stacking threshold effect can lose one stack as the count drops rather than the whole thing.
- **Removal tracks the handles, not the `EffectData`.** An identical effect that arrived from elsewhere is never touched, and one that already expired or was dispelled is left alone.

### Inhibition

**The threshold watches the stack count alone.** Inhibiting the effect does not take the threshold effects back.

This is deliberate rather than an oversight: a periodic effect does not report its inhibition changes through `OnActiveEffectChanged`, so an inhibition-aware threshold would work on some owners and silently not on others — and the archetypal owner here, a stacking damage-over-time, is periodic. Gate on inhibition through the threshold effects' own [tag](target-tag-requirements-effect-component.md) or [attribute](attribute-requirements-effect-component.md) requirements when it matters.

### Re-application under `RemoveOnEnd`

If the threshold effect runs out of its own duration while the count is still high, the next change to the count re-applies it, since the condition it hangs off never stopped holding. Give it an `Infinite` duration to let this component own its lifetime outright, which is what the policy is for.

## Validation

- **A non-stackable owner is rejected.** The count never changes, so the threshold is either met at application or never. Use `AdditionalEffectsEffectComponent` to apply an effect on application instead.
- **A threshold of one or less is rejected.** It is met by the initial stack of every application, which is again what `AdditionalEffectsEffectComponent` already expresses.
- **An instant threshold effect under `RemoveOnEnd` is rejected.** It executes and is gone immediately, so there is nothing left to take back — the same rule `AdditionalEffectsEffectComponent` applies to its own `RemoveOnEnd` entries. Use `Ignore`, which accepts one.

## Usage

Five stacks of Bleed causing a Hemorrhage that subsides as the stacks do:

```csharp
var bleedData = new EffectData(
    "Bleed",
    new DurationData(DurationType.HasDuration, /* 10s */),
    modifiers: [/* damage per tick */],
    stackingData: /* stack limit 10, aggregate by source */,
    periodicData: new PeriodicData(new ScalableFloat(1f), true, PeriodInhibitionRemovedPolicy.NeverReset),
    effectComponents: new IEffectComponent[] {
        new StackThresholdEffectComponent(5, [
            new ConditionalEffect(hemorrhageData, RemovalPolicy: ConditionalEffectRemovalPolicy.RemoveOnEnd)
        ])
    }
);
```

A stacking curse that, once it reaches its limit, detonates on the victim and leaves the blast to run its own course. `Ignore` is the default, so nothing has to be said:

```csharp
new StackThresholdEffectComponent(10, [new ConditionalEffect(detonationData)]);
```

One crossing driving two entries with different lifetimes — a debuff that stays only while the stacks do, and a one-off burst that then runs its own course:

```csharp
new StackThresholdEffectComponent(5, [
    new ConditionalEffect(hemorrhageData, RemovalPolicy: ConditionalEffectRemovalPolicy.RemoveOnEnd),
    new ConditionalEffect(burstData)
]);
```

Rage stacks that reward the attacker rather than the victim, and only an attacker who is Berserk:

```csharp
new StackThresholdEffectComponent(3, [
    new ConditionalEffect(
        frenzyData,
        new TagRequirements(RequiredTags: tagsManager.RequestTagContainer(new[] { "status.berserk" })),
        Target: EffectApplicationTarget.Source)
]);
```

## Key Points

- **This is a threshold, not a per-application trigger.** For effects that fire on *every* application, use [AdditionalEffectsEffectComponent](additional-effects-effect-component.md) — its application effects fire once per successfully applied stack.
- **One threshold per component.** The count is a single number; for a second threshold on the same effect — 5 stacks Hemorrhage, 10 stacks Rupture — add a second component.
- **`RemoveOnEnd` wants a non-expiring threshold effect.** Give it `Infinite` and let the component decide when it ends.
- **The stacking policy decides the count, not this component.** Whether two attackers share a stack or keep their own is `StackPolicy`'s business, and the threshold follows whatever it decides.
- **Applied effects go through the full pipeline.** Each threshold effect's own `CanApplyEffect` components, the target's [application blockers](../README.md#blocking-effect-application), and chance-to-apply all get their say, so any of them can be refused — and under `Ignore` a refusal is not retried.
- **Nested appliers count against the depth guard.** A threshold effect that applies further effects is a chain like any other; see [Application cycles](README.md#application-cycles).

## See Also

- [Effect Components Overview](README.md)
- [AdditionalEffectsEffectComponent](additional-effects-effect-component.md) — shares the `ConditionalEffect` shape
- [RaiseEventEffectComponent](raise-event-effect-component.md)
- [Stacking Data](../README.md#stacking-data)
- [ActiveEffectHandle](../README.md#activeeffecthandle)
