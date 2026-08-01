# AdditionalEffectsEffectComponent

> **Type:** `Gamesmiths.Forge.Effects.Components.AdditionalEffectsEffectComponent`
> **State:** Stateful — returns a new instance from `CreateInstance`
> **Applies to:** Any effect (the `onComplete` sets and `RemoveOnEnd` need a non-instant one)

Applies further effects off the back of its own: one set when the effect lands, and three more when it ends. Every entry, on either side, is a [`ConditionalEffect`](#conditionaleffect), so it can be gated on the source's tags, pointed at an entity other than the target, and — on the application side — taken back when its applier ends.

This is how one authored effect becomes a package — a fireball that also applies Burning, a poison that leaves a Weakened debuff behind when it wears off, a hit that heals whoever landed it — without writing a `CustomExecution` or a custom component.

## Constructor

```csharp
new AdditionalEffectsEffectComponent(
    onApplication,
    onCompleteAlways,
    onCompleteNormal,
    onCompletePrematurely,
    copyDataFromOriginalEffect)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| onApplication | `ConditionalEffect[]?` | The effects to apply when this effect is applied. Defaults to none. |
| onCompleteAlways | `ConditionalEffect[]?` | The effects to apply when this effect is removed, however it ended. Defaults to none. |
| onCompleteNormal | `ConditionalEffect[]?` | The effects to apply when this effect ends by running out of duration. Defaults to none. |
| onCompletePrematurely | `ConditionalEffect[]?` | The effects to apply when this effect is taken away before it could expire. Defaults to none. |
| copyDataFromOriginalEffect | `bool` | Whether the applied effects inherit this effect's `SetByCaller` magnitudes. Defaults to `false`. |

### ConditionalEffect

```csharp
new ConditionalEffect(EffectData, SourceTagRequirements, RemovalPolicy, StacksToRemove, Target)
```

| Field | Type | Description |
|-------|------|-------------|
| EffectData | `EffectData` | The effect to apply. |
| SourceTagRequirements | `TagRequirements?` | Requirements the effect's **source** must meet. `null` or empty always applies. |
| RemovalPolicy | `ConditionalEffectRemovalPolicy` | `Ignore` (the default) leaves the applied effect alone; `RemoveOnEnd` takes it back when the applier ends. **Application entries only.** |
| StacksToRemove | `int` | How many stacks `RemoveOnEnd` takes. Any negative value, the default, removes the effect entirely. Ignored under `Ignore`. |
| Target | `EffectApplicationTarget` | `Target` (the default), `Source`, or `Owner`. |

`RemovalPolicy` is the one field that means nothing on a completion entry: the end it would take the effect back at is the very one applying it. Validation rejects a completion entry asking for `RemoveOnEnd` rather than letting it read as configured.

## Lifecycle Hooks

| Hook | What this component does |
|------|--------------------------|
| `OnEffectApplied` | Evaluates each conditional's source requirements and applies the ones that pass, tracking the handles of any `RemoveOnEnd` entries. |
| `OnActiveEffectUnapplied` | On full removal only: applies `onCompleteAlways`, then whichever of `onCompleteNormal` / `onCompletePrematurely` matches the removal reason, then takes back the tracked `RemoveOnEnd` effects. |

## Behavior

### On application

- Conditionals are evaluated in order, each independently. A failed condition skips that entry and nothing else.
- Source requirements read the **source's** tags (`Ownership.Source`), not the target's. A missing source is evaluated as an empty container, so required tags fail and ignored tags pass.
- A conditional pointed at an ownership entity the effect does not have — thorns on an effect with no source — is skipped rather than redirected back at the target.
- `OnEffectApplied` fires again for **each successfully applied stack**, so a stacking applier applies its effects again every time a stack lands.
- Instant effects reach this hook too, so an instant effect can apply a duration debuff.
- The hook runs **before** the applier's own modifiers execute. An applied effect reading an attribute the applier modifies sees the value from before the applier touched it.

### On removal

- Only full removal counts. Losing a stack leaves everything in place.
- `EffectRemovalReason.Expired` selects `onCompleteNormal`; `EffectRemovalReason.Removed` selects `onCompletePrematurely`. `Infinite` effects have no natural end, so every removal of one is premature.
- `onCompleteAlways` is applied first, then whichever of the two reason-specific sets matches.
- Source conditions and `EffectApplicationTarget` work exactly as they do on the application side, evaluated against the same ownership. A completion effect pointed at `Source` is how a debuff pays its caster back when it ends.
- Completion effects are applied **before** the `RemoveOnEnd` clean-up, so a completion effect can replace something the applier was keeping alive without the clean-up pass taking its replacement away.

### What the applied effects inherit

Applied effects always carry over the applier's **ownership** and **evaluated level**, so they credit the same source and land at the same power. `copyDataFromOriginalEffect` adds the applier's `SetByCaller` magnitudes on top, for an applied effect keyed on the same values the caller set on the parent:

```csharp
var fireball = new Effect(fireballData, ownership);
fireball.SetSetByCallerMagnitude(damageTag, 40);

// With copyDataFromOriginalEffect, the Burning effect resolves damageTag to 40 as well.
target.EffectsManager.ApplyEffect(fireball);
```

The magnitudes are copied, not shared: setting one on the applier afterwards does not reach effects it has already applied. Unlike Unreal's equivalent, the flag governs the `onComplete` sets too rather than only the application ones.

### Taking effects back

`RemoveOnEnd` tracks the **handles** it applied, not the `EffectData`. An identical effect that arrived from somewhere else is never touched, and an applied effect that has already expired or been dispelled is left alone. Removal reaches whichever entity the effect landed on, so a `RemoveOnEnd` conditional pointed at `Source` still cleans up correctly.

## Validation

- **Completion effects on an instant effect are rejected.** They hang off removal, and an instant effect never becomes active, so it is never removed. Use the application effects instead.
- **`RemoveOnEnd` with an instant applier is rejected.** There is no end to hook.
- **`RemoveOnEnd` with an instant applied effect is rejected.** It executes and is gone immediately, so there is nothing left to take back.

Application effects on an instant applier are fine, as are instant applied effects under `Ignore`.

## Usage

A fireball that burns, and burns harder when the caster is attuned:

```csharp
var fireballData = new EffectData(
    "Fireball",
    new DurationData(DurationType.Instant),
    modifiers: [/* direct damage */],
    effectComponents: new IEffectComponent[] {
        new AdditionalEffectsEffectComponent([
            new ConditionalEffect(burningData),
            new ConditionalEffect(
                infernoData,
                new TagRequirements(RequiredTags: tagsManager.RequestTagContainer(new[] { "status.attuned.fire" })))
        ])
    }
);
```

Lifesteal, with no custom execution:

```csharp
new AdditionalEffectsEffectComponent([
    new ConditionalEffect(healSourceData, Target: EffectApplicationTarget.Source)
]);
```

A stance that grants a companion buff and takes it back with itself, and leaves an exhaustion debuff behind only if it is cancelled early:

```csharp
new AdditionalEffectsEffectComponent(
    onApplication: [
        new ConditionalEffect(hasteData, RemovalPolicy: ConditionalEffectRemovalPolicy.RemoveOnEnd)
    ],
    onCompletePrematurely: [new ConditionalEffect(exhaustionData)]);
```

## Key Points

- **Applied effects go through the full application pipeline.** Their own `CanApplyEffect` components, the target's [application blockers](../README.md#blocking-effect-application), and chance-to-apply all get their say, so an applied effect can be refused.
- **Conditions read the source, targets read ownership.** `SourceTagRequirements` is the same shape as [SourceTagRequirementsEffectComponent](source-tag-requirements-effect-component.md)'s, and `EffectApplicationTarget` resolves through the same `EffectOwnership` the effect was built with.
- **A stacking applier is a repeating applier.** That is the intended way to make application effects fire more than once; there is no per-execution policy.
- **Cycles are a configuration bug.** Nothing stops two effects from applying each other. `EffectsManager` cuts a cascade off after 16 levels of nesting and asserts, so a cycle drops applications instead of overflowing the stack — but see [Avoid Circular Dependencies](README.md#best-practices).
- **`Effect.CreateLinkedEffect`** is the same inheritance rule as a public helper, for custom components that spawn their own child effects.

## See Also

- [Effect Components Overview](README.md)
- [SourceTagRequirementsEffectComponent](source-tag-requirements-effect-component.md)
- [GrantAbilityEffectComponent](grant-ability-effect-component.md)
- [RemoveOtherEffectComponent](remove-other-effect-component.md)
- [EffectOwnership](../README.md#effectownership)
- [ActiveEffectHandle](../README.md#activeeffecthandle)
