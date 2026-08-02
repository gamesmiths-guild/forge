# RemoveOtherEffectComponent

> **Type:** `Gamesmiths.Forge.Effects.Components.RemoveOtherEffectComponent`
> **State:** Stateful — returns a new instance from `CreateInstance`
> **Applies to:** Any non-periodic effect

Removes other active effects from the target when its own effect is applied. Every active effect matching one of the component's [`EffectQuery`](../README.md#effectquery) filters is removed — or loses a set number of stacks — and never the effect carrying the component.

This is the natural shape for a cleanse, a dispel, or a purge, and the reason to prefer it over tag-driven removal is that it works from an **instant** effect. See [Choosing between tag requirements and Immunity/RemoveOther](README.md#choosing-between-tag-requirements-and-immunityremoveother) for when the simpler option suffices.

## Constructor

```csharp
new RemoveOtherEffectComponent(removeQueries, stacksToRemove)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| removeQueries | `EffectQuery[]` | The queries selecting which active effects to remove. An effect matching **any** of them is removed. |
| stacksToRemove | `int` | How many stacks to take from each match. Any negative value, the default, removes the effects entirely regardless of stack count. |

## Lifecycle Hooks

| Hook | What this component does |
|------|--------------------------|
| `OnActiveEffectAdded` | Stashes its own handle so the removal pass can exclude it. Instant effects never get here, and never enter the active list either. |
| `OnEffectApplied` | Runs one removal pass per query, through `EffectsManager.RemoveEffects`. |

## Behavior

Each query gets its own call to [`EffectsManager.RemoveEffects`](../README.md#the-query-api), with this component's own handle in the ignore set. Partial removal is explicit — driven by `stacksToRemove` rather than by `StackExpirationPolicy` — and never refreshes the remaining duration of what survives.

- **It never removes itself**, even when its own effect tags match its own queries. Only its own handle is excluded, so another application of the *same* `EffectData` that is already active is fair game.
- Removal happens on application, which includes **each successful stack application** of the remover. A stacking cleanse dispels again every time a stack lands.
- Removal callbacks on what is being removed can apply or remove further effects; the pass snapshots its matches first, so that is safe.
- Instant owners work exactly the same way; they simply have no handle to exclude.

## Validation

- **Periodic owners are rejected.** Removal is tied to application, not to execution, so a periodic remover looks like it dispels on every tick while only ever firing once. For recurring removal, use the removal requirements of [TargetTagRequirementsEffectComponent](target-tag-requirements-effect-component.md), which are re-evaluated on every tag change.
- **Empty queries are rejected.** An empty `EffectQuery` matches every effect, so the component would strip the target of everything it has. At runtime an empty query removes nothing rather than everything, so a build with validation disabled degrades to a no-op instead of wiping the target.

A component with no queries at all is accepted — it simply removes nothing.

## Usage

```csharp
// An instant cleanse that strips every curse
var cleanseData = new EffectData(
    "Cleanse",
    new DurationData(DurationType.Instant),
    effectComponents: new IEffectComponent[] {
        new RemoveOtherEffectComponent([
            new EffectQuery(
                EffectTagQuery: TagQuery.MakeQueryMatchAnyTags(
                    tagsManager.RequestTagContainer(new[] { "effect.curse" })))
        ])
    }
);
```

Weakening a stack instead of clearing it, and dispelling by source:

```csharp
// Takes two stacks of Bleed off, leaving the rest
new RemoveOtherEffectComponent([new EffectQuery(EffectDefinition: bleedData)], stacksToRemove: 2);

// Strips everything that specific caster put on this target
new RemoveOtherEffectComponent([new EffectQuery(EffectSource: enemyCaster)]);
```

## Reacting to a Dispel

[`EffectRemovalReason`](README.md#onactiveeffectunapplied) is deliberately two-valued: `Expired` or `Removed`. A dispelled effect reports `Removed`, exactly like a manual `RemoveEffect` call or a tag-driven removal, so the removal reason alone cannot tell you that a *dispel specifically* happened, nor who did it.

The idiom is to stop asking the removed effect and let the dispel announce itself instead — **a dispel is just an effect**, so give it the two things a reaction needs: an event tag, and the dispeller as its `Source`.

```csharp
// The dispel announces itself, then strips the curses.
// Component order matters: the event is raised before the removal pass runs, so a listener
// can still inspect what is about to be taken off.
var dispelData = new EffectData(
    "Dispel Magic",
    new DurationData(DurationType.Instant),
    effectComponents: new IEffectComponent[] {
        new RaiseEventEffectComponent(
            tagsManager.RequestTagContainer(new[] { "events.dispel" }),
            EffectEventTrigger.Applied),
        new RemoveOtherEffectComponent([
            new EffectQuery(
                EffectTagQuery: TagQuery.MakeQueryMatchAnyTags(
                    tagsManager.RequestTagContainer(new[] { "effect.curse" })))
        ])
    }
);

// Ownership carries the attribution: the dispeller is the Source
var dispel = new Effect(dispelData, new EffectOwnership(dispeller, dispeller));
victim.EffectsManager.ApplyEffect(dispel);
```

The raised [event](../../events.md) reaches the victim's bus with `Target` set to the victim and `Source` set to the dispeller, which is everything a punish mechanic needs:

```csharp
// Unstable Affliction: dispelling the curse damages whoever dispelled it
victim.Events.Subscribe(Tag.RequestTag(tagsManager, "events.dispel"), eventData =>
{
    if (eventData.Source is not null)
    {
        eventData.Source.EffectsManager.ApplyEffect(
            new Effect(backlashData, new EffectOwnership(victim, victim)));
    }
});
```

The same shape works from a graph — an `EventListenerNode` on `events.dispel` exposes `Source` — or from an ability triggered by [`AbilityTriggerData.ForEvent`](../../abilities.md#event-trigger). Because the reaction keys off the tag rather than off a removal reason, several dispel flavors (purge, cleanse, steal) can be distinguished by giving each its own event tag, all without growing `EffectRemovalReason`.

## Key Points

- The one thing tag-driven removal cannot do: dispel from an instant effect. `ModifierTagsEffectComponent` is rejected on instant effects, so the tag-based alternative forces a cleanse to become a short duration effect purely to hold a tag long enough for the target to see it.
- Partial stack removal is the other gap it closes; tag-driven removal always takes all stacks.
- It excludes its own handle, not its own `EffectData`. Two applications of the same cleanse can remove each other.
- Fires again on every stack application, so a stacking remover is a repeating dispel — the intended way to get recurring behavior out of it, since periodic owners are rejected.
- Anything it can do can also be done in code or in a graph through `EffectsManager.RemoveEffects` / [QueryActiveEffectsResolver](../../statescript/resolvers/query-active-effects-resolver.md); the component is the data-driven wrapper over the same call.

## See Also

- [Effect Components Overview](README.md)
- [Choosing between tag requirements and Immunity/RemoveOther](README.md#choosing-between-tag-requirements-and-immunityremoveother)
- [RaiseEventEffectComponent](raise-event-effect-component.md)
- [ImmunityEffectComponent](immunity-effect-component.md)
- [TargetTagRequirementsEffectComponent](target-tag-requirements-effect-component.md)
- [EffectQuery](../README.md#effectquery)
- [The Query API](../README.md#the-query-api)
