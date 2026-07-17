# ActiveEffectTargetResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ActiveEffectTargetResolver`
> **Output Type:** `IForgeEntity?`

Reads the entity an active effect is applied to, from an `ActiveEffectHandle` produced by a nested resolver. Implements `IEntityResolver`, so it composes with entity-aware resolvers and nodes.

## Constructor

```csharp
new ActiveEffectTargetResolver(handleResolver)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| handleResolver | `IObjectResolver<ActiveEffectHandle>` | Produces the active effect handle to inspect. |

## Behavior

- Resolves the handle and returns its `Target`. Invalid or missing handles resolve to `null`.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable<ActiveEffectHandle>("buff");

graph.VariableDefinitions.DefineObjectProperty("buffTarget",
    new ActiveEffectTargetResolver(new ObjectVariableResolver<ActiveEffectHandle>("buff")));
```

## Composition

```csharp
// Read the health of whatever entity the buff is on
graph.VariableDefinitions.DefineProperty("buffTargetHealth",
    new AttributeResolver(
        "CombatAttributeSet.Health",
        new ActiveEffectTargetResolver(new ObjectVariableResolver<ActiveEffectHandle>("buff"))));
```

## See Also

- [Resolvers Overview](README.md)
- [ActiveEffectDataResolver](active-effect-data-resolver.md)
- [ActiveEffectEffectResolver](active-effect-effect-resolver.md)
