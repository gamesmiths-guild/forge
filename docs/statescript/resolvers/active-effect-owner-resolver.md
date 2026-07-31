# ActiveEffectOwnerResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ActiveEffectOwnerResolver`
> **Output Type:** `IForgeEntity?`

Reads the entity that triggered an active effect — its `Ownership.Owner` — from an `ActiveEffectHandle` produced by a nested resolver. Implements `IEntityResolver`, so it composes with entity-aware resolvers and nodes.

`Owner` is *who triggered the action* that caused the effect. For *what actually caused* it, use [`ActiveEffectSourceResolver`](active-effect-source-resolver.md).

## Constructor

```csharp
new ActiveEffectOwnerResolver(handleResolver)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| handleResolver | `IObjectResolver<ActiveEffectHandle>` | Produces the active effect handle to inspect. |

## Behavior

- Resolves the handle and returns its effect's `Ownership.Owner`. Invalid or missing handles resolve to `null`.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable<ActiveEffectHandle>("curse");

graph.VariableDefinitions.DefineObjectProperty("curseCaster",
    new ActiveEffectOwnerResolver(new ObjectVariableResolver<ActiveEffectHandle>("curse")));
```

## Composition

```csharp
// Is the caster still alive enough to keep the curse interesting?
graph.VariableDefinitions.DefineProperty("casterHealth",
    new AttributeResolver(
        "CombatAttributeSet.CurrentHealth",
        new ActiveEffectOwnerResolver(new ObjectVariableResolver<ActiveEffectHandle>("curse"))));
```

## See Also

- [Resolvers Overview](README.md)
- [ActiveEffectSourceResolver](active-effect-source-resolver.md)
- [ActiveEffectTargetResolver](active-effect-target-resolver.md)
- [EffectOwnership](../../effects/README.md#effectownership)
