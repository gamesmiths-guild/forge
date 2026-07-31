# ActiveEffectSourceResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ActiveEffectSourceResolver`
> **Output Type:** `IForgeEntity?`

Reads the entity that applied an active effect — its `Ownership.Source` — from an `ActiveEffectHandle` produced by a nested resolver. Implements `IEntityResolver`, so it composes with entity-aware resolvers and nodes.

`Source` is *what actually caused* the effect (the weapon, the projectile, the trap). For *who triggered* it, use [`ActiveEffectOwnerResolver`](active-effect-owner-resolver.md).

## Constructor

```csharp
new ActiveEffectSourceResolver(handleResolver)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| handleResolver | `IObjectResolver<ActiveEffectHandle>` | Produces the active effect handle to inspect. |

## Behavior

- Resolves the handle and returns its effect's `Ownership.Source`. Invalid or missing handles resolve to `null`.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable<ActiveEffectHandle>("poison");

graph.VariableDefinitions.DefineObjectProperty("poisoner",
    new ActiveEffectSourceResolver(new ObjectVariableResolver<ActiveEffectHandle>("poison")));
```

## Composition

```csharp
// Retaliate against whoever poisoned us
var applyNode = new ApplyEffectNode();
applyNode.BindInput(ApplyEffectNode.TargetInput, "poisoner");

// Or filter a dispel down to a single attacker's work
graph.VariableDefinitions.DefineProperty("appliedByTheBoss",
    new ObjectEqualsResolver(
        new ActiveEffectSourceResolver(new ElementResolver<ActiveEffectHandle>()),
        new ObjectVariableResolver<IForgeEntity>("boss")));
```

## See Also

- [Resolvers Overview](README.md)
- [ActiveEffectOwnerResolver](active-effect-owner-resolver.md)
- [ActiveEffectTargetResolver](active-effect-target-resolver.md)
- [EffectQueryMatchResolver](effect-query-match-resolver.md)
