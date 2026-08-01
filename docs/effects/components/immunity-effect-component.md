# ImmunityEffectComponent

> **Type:** `Gamesmiths.Forge.Effects.Components.ImmunityEffectComponent`
> **State:** Stateful — returns a new instance from `CreateInstance`
> **Applies to:** Duration effects only

Makes its target immune to incoming effects while the effect is active. Any effect matching one of the component's [`EffectQuery`](../README.md#effectquery) filters is denied before it can be applied, and the target's `OnEffectApplicationBlocked` event reports the denial.

This is the inverted form of the usual tag gate: instead of every fire effect declaring "I don't land on things tagged `immune.fire`", a single Fire Ward declares "nothing tagged `effect.fire` lands on me". See [Choosing between tag requirements and Immunity/RemoveOther](README.md#choosing-between-tag-requirements-and-immunityremoveother) before reaching for it — three ignore-tags are often the simpler answer.

## Constructor

```csharp
new ImmunityEffectComponent(immunityQueries)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| immunityQueries | `EffectQuery[]` | The queries selecting which incoming effects to block. An effect matching **any** of them is denied. |

## Lifecycle Hooks

| Hook | What this component does |
|------|--------------------------|
| `OnActiveEffectAdded` | Stashes its own handle and registers itself as an `IEffectApplicationBlocker` on the target's `EffectsManager`. |
| `OnActiveEffectUnapplied` | Unregisters when the effect is fully removed. A stack removal changes nothing. |

## Behavior

Immunity decides **on arrival**. It is consulted once, as an effect is being applied, and it either denies that application or lets it through for good — it never removes or suppresses something that already landed.

- Every query is evaluated in order; the first match denies the application and raises `EffectsManager.OnEffectApplicationBlocked` with the blocked effect and this component instance.
- **Inhibition-aware:** while the immunity effect is inhibited it blocks nothing, so a ward suppressed by its own ongoing requirements stops turning effects away. It resumes blocking when un-inhibited.
- **Instant effects are blocked too**, unlike anything driven by granted tags.
- Blocking is per target: the component registers on the manager of the entity the effect landed on.
- Each application gets its own instance, so removing one entity's ward never disarms another's.

The registry behind it is public. `IEffectApplicationBlocker` and `EffectsManager.RegisterApplicationBlocker` are documented in [Blocking effect application](../README.md#blocking-effect-application) — non-effect systems such as cutscene gates or a god mode toggle can register one directly, and get the same event.

## Validation

- **Instant owners are rejected.** The immunity lasts exactly as long as its effect is active, and instant effects never become active.
- **Empty queries are rejected.** An empty `EffectQuery` matches every effect, so an immunity built from one would block everything the target could ever receive. At runtime an empty query blocks nothing rather than everything, so a build with validation disabled degrades to a no-op instead of a locked-down entity.

A component with no queries at all is accepted — it simply blocks nothing.

## Usage

```csharp
// A ward that turns away every fire effect for 10 seconds
var fireWardData = new EffectData(
    "Fire Ward",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(10.0f)
        )
    ),
    effectComponents: new IEffectComponent[] {
        new ImmunityEffectComponent([
            new EffectQuery(
                EffectTagQuery: TagQuery.MakeQueryMatchAnyTags(
                    tagsManager.RequestTagContainer(new[] { "effect.fire" })))
        ])
    }
);
```

Since the filter is a full `EffectQuery`, immunity reaches conditions the target's own tags cannot express:

```csharp
// Immune to anything this specific attacker applies, and to anything that would modify Health
new ImmunityEffectComponent([
    new EffectQuery(EffectSource: attacker),
    new EffectQuery(ModifyingAttribute: "VitalAttributeSet.CurrentHealth")
]);
```

Reacting to a block, for a "resisted!" floater:

```csharp
entity.EffectsManager.OnEffectApplicationBlocked += (effect, blocker) =>
{
    if (blocker is ImmunityEffectComponent)
    {
        ShowResistedFloater(effect.EffectData.Name);
    }
};
```

## Key Points

- Blocks on arrival only. To suppress something already active, use the ongoing requirements of [TargetTagRequirementsEffectComponent](target-tag-requirements-effect-component.md) instead.
- Works against instant effects, which tag-driven gating on the incoming effect can also do, but which [RemoveOtherEffectComponent](remove-other-effect-component.md)'s tag-driven alternative cannot.
- An immunity whose own effect tags match its own queries blocks its own re-application. Give the ward a tag vocabulary distinct from what it blocks.
- Denials are observable through `EffectsManager.OnEffectApplicationBlocked`; effects that deny themselves through their own `CanApplyEffect` never reach the blockers and raise nothing.
- The blocked effect's `CanApplyEffect` components have already run by the time immunity is consulted, so anything they consume — a `ChanceToApplyEffectComponent` roll, for instance — is consumed even when the effect is then blocked.

## See Also

- [Effect Components Overview](README.md)
- [Choosing between tag requirements and Immunity/RemoveOther](README.md#choosing-between-tag-requirements-and-immunityremoveother)
- [RemoveOtherEffectComponent](remove-other-effect-component.md)
- [TargetTagRequirementsEffectComponent](target-tag-requirements-effect-component.md)
- [EffectQuery](../README.md#effectquery)
- [Blocking effect application](../README.md#blocking-effect-application)
