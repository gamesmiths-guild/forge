# RaiseEventEffectComponent

> **Type:** `Gamesmiths.Forge.Effects.Components.RaiseEventEffectComponent`
> **State:** Stateless — shared across every application
> **Applies to:** Any effect (each trigger has its own requirements)

Raises a Forge `EventData` at chosen points in its effect's lifetime, on the target, the source, or both. It closes the Effects → Events → Statescript loop: anything that can subscribe to an `EventManager` can react to an effect landing, ticking, or wearing off without polling for it.

This needs no new Statescript surface — `EventListenerNode` already exists — so a graph can react to effects the moment the component is attached.

## Constructor

```csharp
new RaiseEventEffectComponent(eventTags, triggers, magnitude, raiseOn)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| eventTags | `TagContainer` | The tags carried by the raised event. Subscribers match on these, so an event with none reaches nobody. |
| triggers | `EffectEventTrigger` | The lifecycle points at which to raise. The values combine. Defaults to `Applied`. |
| magnitude | `ModifierMagnitude?` | The value reported as `EventData.EventMagnitude`. Defaults to `null`, which reports zero. |
| raiseOn | `EffectApplicationTarget[]?` | The entities whose buses receive the event. Defaults to the target alone. |

### EffectEventTrigger

A `[Flags]` enum, so one component can cover several points.

| Value | Hook | Fires for |
|-------|------|-----------|
| `Applied` | `OnEffectApplied` | Every effect, including instant ones, and again for each successfully applied stack. |
| `Executed` | `OnEffectExecuted` | Instant and periodic effects only. |
| `ExpiredNormally` | `OnActiveEffectUnapplied` | Full removal after the duration ran out — `EffectRemovalReason.Expired`. `HasDuration` effects only. |
| `RemovedPrematurely` | `OnActiveEffectUnapplied` | Full removal before the effect could expire — `EffectRemovalReason.Removed`. Non-instant effects only. |
| `StackRemoved` | `OnActiveEffectUnapplied` | Each stack the effect loses and survives. Stackable effects only. |

`StackRemoved` is the counterpart of `Applied` firing again per stack gained — "the debuff is weakening" as against "the debuff is gone". Exactly one trigger fires per ending: the stack that takes the count to zero is a full removal, so it reports `ExpiredNormally` or `RemovedPrematurely` rather than `StackRemoved`.

There is no `StackApplied` to match it, and that asymmetry is the lifecycle's rather than a choice made here. `OnActiveEffectUnapplied` hands the component a flag saying whether this was a full removal, so the two cases are free to separate; `OnEffectApplied` carries no such flag, so telling the first application from a later stack would mean this component tracking state of its own — which is the whole reason it can be stateless today.

#### The two endings

The component receives `EffectRemovalReason` from the hook, so it can tell an effect that ran its course from one that was taken away, and the two are separate flag values rather than one `Removed`:

| Ending | Trigger |
|--------|---------|
| A `HasDuration` effect's duration lapsed | `ExpiredNormally` |
| Any `EffectsManager` removal — `RemoveEffect`, `RemoveEffects(query)`, a `RemoveOtherEffectComponent` dispel, a tag- or attribute-driven removal | `RemovedPrematurely` |
| An `Infinite` effect ending, whatever ended it | `RemovedPrematurely` — it has no duration to run out of, so it has no natural end |

Combine them (`ExpiredNormally | RemovedPrematurely`) to announce every ending, which is what a UI buff bar wants. Keep them apart when the reaction is the point: a bomb that detonates on expiry must **not** go off when it is dispelled, and that distinction used to cost an extra instant effect and an [AdditionalEffectsEffectComponent](additional-effects-effect-component.md) hop to express.

A stack removal carries a reason too — a stack can lapse under `StackExpirationPolicy.RemoveSingleStackAndRefreshDuration` just as it can be stripped — but `StackRemoved` deliberately does not split on it, since "one stack fewer" is the same event either way. Nothing built in reacts to a *stack's* reason; a custom `IEffectComponent` reading the `reason` argument of `OnActiveEffectUnapplied` is the only way to.

## Lifecycle Hooks

| Hook | What this component does |
|------|--------------------------|
| `OnEffectApplied` | Raises when `Applied` is set. |
| `OnEffectExecuted` | Raises when `Executed` is set. |
| `OnActiveEffectUnapplied` | On a full removal, raises `ExpiredNormally` or `RemovedPrematurely` according to the `EffectRemovalReason` it was handed; on a stack the effect survives, raises `StackRemoved`. |

## Behavior

### What the event says

The event is raised on the bus of each entity named by `raiseOn`, but its contents always describe the **effect**, not the bus carrying it:

| Field | Value |
|-------|-------|
| `EventTags` | The configured container. |
| `Source` | `Ownership.Source` — who caused the effect. |
| `Target` | The entity the effect landed on. |
| `EventMagnitude` | The evaluated `magnitude`, or zero. |
| `Payload` | The raising `Effect`. |

A listener therefore reads the same story from either side: raising on the source's bus does not swap `Source` and `Target` around.

- **An entity the effect does not have is skipped.** A `Source` entry on an effect with no source has no bus to raise on; the remaining entries still fire.
- **The raise is non-generic**, so it reaches catch-all subscriptions but not typed `Subscribe<TPayload>` handlers.
- Handlers run synchronously, inside the effect callback that triggered them.

### The magnitude

`magnitude` is a full `ModifierMagnitude` rather than a plain number, so the event can report a value that scales with level, reads an attribute, or reads a `SetByCaller` value — including a running total published by an [AttributeAccumulatorEffectComponent](attribute-accumulator-effect-component.md) on the same effect:

```csharp
effectComponents: new IEffectComponent[] {
    new AttributeAccumulatorEffectComponent("CombatAttributeSet.CurrentHealth", damageDealtTag),
    new RaiseEventEffectComponent(
        combatDamageTags,
        EffectEventTrigger.Executed,
        new ModifierMagnitude(
            MagnitudeCalculationType.SetByCaller,
            // Non-snapshot: a snapshot SetByCallerFloat caches the first value it reads and would
            // report the first tick's damage forever.
            setByCallerFloat: new SetByCallerFloat(damageDealtTag, false)))
}
```

Two things have to line up for that pairing: the accumulator must come **first** in the array, since both hang off `OnEffectExecuted` and the tally has to land before the event reads it, and the `SetByCallerFloat` must be non-snapshot.

## Validation

- **`Executed` on a duration effect that is not periodic is rejected.** Only instant and periodic effects execute, so it would never fire.
- **Either removal trigger on an instant effect is rejected.** An instant effect never becomes active and so is never removed. Use `Applied`.
- **`ExpiredNormally` on an `Infinite` effect is rejected.** It has no duration to run out of, and every removal of one is reported as `Removed`. Use `RemovedPrematurely`.
- **`StackRemoved` on a non-stackable effect is rejected.** It has no stack to lose and survive, so it would never fire. Use the removal triggers.
- **No event tags is rejected.** Subscribers match on the tags, so an untagged event is raised into nothing.
- **No trigger is rejected.** `EffectEventTrigger.None` never raises, which is always a mistake rather than a deliberate no-op.

## Usage

Announcing a debuff so UI and AI can both react, without either polling:

```csharp
var poisonData = new EffectData(
    "Poison",
    new DurationData(DurationType.HasDuration, /* 8s */),
    modifiers: [/* damage per tick */],
    periodicData: new PeriodicData(new ScalableFloat(1f), true, PeriodInhibitionRemovedPolicy.NeverReset),
    effectComponents: new IEffectComponent[] {
        new RaiseEventEffectComponent(
            tagsManager.RequestTagContainer(new[] { "events.status.poison" }),
            EffectEventTrigger.Applied
                | EffectEventTrigger.ExpiredNormally
                | EffectEventTrigger.RemovedPrematurely)
    }
);

target.Events.Subscribe(Tag.RequestTag(tagsManager, "events.status.poison"), data =>
{
    UpdateDebuffBar(data.Target);
});
```

Telling the attacker what their hit was worth, by raising on the source:

```csharp
new RaiseEventEffectComponent(
    tagsManager.RequestTagContainer(new[] { "events.combat.damage_dealt" }),
    EffectEventTrigger.Executed,
    damageMagnitude,
    raiseOn: [EffectApplicationTarget.Source]);
```

A time bomb that only goes off if nobody cleansed it — the reason a `RaiseEventEffectComponent` can discriminate at all:

```csharp
var timeBombData = new EffectData(
    "Time Bomb",
    new DurationData(DurationType.HasDuration, /* 3s */),
    effectComponents: new IEffectComponent[] {
        // A dispel reports RemovedPrematurely, so it never reaches this event.
        new RaiseEventEffectComponent(
            tagsManager.RequestTagContainer(new[] { "events.bomb.detonate" }),
            EffectEventTrigger.ExpiredNormally,
            detonationMagnitude)
    }
);
```

An ability triggered by `AbilityTriggerData.ForEvent(detonateTag)` on the victim then runs the explosion, and the event's `Target` and `EventMagnitude` reach it as `context.Target` and `context.Magnitude`. Swap the trigger for `RemovedPrematurely` to punish the dispel instead, or list both to react either way.

A stacking debuff narrating its whole life, so a debuff bar can follow the count up and down without polling:

```csharp
new RaiseEventEffectComponent(
    tagsManager.RequestTagContainer(new[] { "events.status.stacks_changed" }),
    EffectEventTrigger.Applied
        | EffectEventTrigger.StackRemoved
        | EffectEventTrigger.ExpiredNormally
        | EffectEventTrigger.RemovedPrematurely);
```

The event itself does not carry the stack count — `EventData` has no field for it, and `Payload` is the raising `Effect`, which has no count of its own. A magnitude built from a `CustomCalculatorClass` is how to report one, since the calculator receives the `EffectEvaluatedData` the hook was given:

```csharp
public sealed class StackCountCalculator : CustomModifierMagnitudeCalculator
{
    public override float CalculateBaseMagnitude(Effect effect, IForgeEntity target, EffectEvaluatedData? data)
        => data?.Stack ?? 0;
}
```

Under `StackRemoved` that reports the count **before** the stack was taken, since the hook fires first and the count drops immediately after.

Both sides of the same blow, from one component:

```csharp
new RaiseEventEffectComponent(
    tagsManager.RequestTagContainer(new[] { "events.combat.hit" }),
    EffectEventTrigger.Executed,
    damageMagnitude,
    raiseOn: [EffectApplicationTarget.Target, EffectApplicationTarget.Source]);
```

## Key Points

- **`Applied` fires per stack, and `StackRemoved` is its mirror.** A stacking effect announces every stack it gains and every one it loses. Use `OnActiveEffectAdded`-shaped logic elsewhere if you need "once, when it first arrives".
- **The two endings are separate triggers.** `ExpiredNormally` and `RemovedPrematurely` split what `EffectRemovalReason` already tells the component, so "detonate only if nobody cleansed it" is one line here rather than an [AdditionalEffectsEffectComponent](additional-effects-effect-component.md) hop into a second effect. An `Infinite` effect only ever reports the premature one.
- **A stack removal reports the count before the stack was taken.** `ActiveEffectEvaluatedData` is handed to the hook before the count drops, so a magnitude reading it sees the outgoing figure, never zero.
- **Handlers run inside the effect callback.** A handler that applies further effects is doing so mid-application, and counts against the [application depth guard](README.md#application-cycles).
- **The same instance serves every application.** Nothing is remembered between raises, so unlike most components here there is no `CreateInstance` override.
- **Tags are matched by the subscriber, not filtered here.** `EventManager` delivers to any subscription whose tag the event carries, so a broad container reaches broad listeners.
- **For reacting to an effect's *own* stack count crossing a line**, use [StackThresholdEffectComponent](stack-threshold-effect-component.md) rather than counting `Applied` events.

## See Also

- [Effect Components Overview](README.md)
- [AdditionalEffectsEffectComponent](additional-effects-effect-component.md) — the same expiry/premature split, applying effects instead of raising events
- [AttributeAccumulatorEffectComponent](attribute-accumulator-effect-component.md)
- [StackThresholdEffectComponent](stack-threshold-effect-component.md)
- [EffectOwnership](../README.md#effectownership)
