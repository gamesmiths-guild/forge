# EntityArrayResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.EntityArrayResolver`
> **Output Type:** `IForgeEntity?[]`

Builds an entity-reference array by evaluating nested entity resolvers in order.

## Constructor

```csharp
new EntityArrayResolver(elementResolvers)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| elementResolvers | `IEntityResolver[]` | The nested entity resolvers that produce each array element. |

## Behavior

- Resolves each nested entity resolver in order.
- Returns an `IForgeEntity?[]` array, preserving `null` results when a nested resolver cannot resolve an entity.
- Useful for composing arrays such as owner/target/source without first writing to a variable.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectArrayProperty("participants",
    new EntityArrayResolver(
        new AbilityOwnerResolver(),
        new AbilityTargetResolver(),
        new AbilitySourceResolver()));
```

## See Also

- [Resolvers Overview](README.md)
- [EntityArrayVariableResolver](entity-array-variable-resolver.md)
- [AbilityOwnerResolver](ability-owner-resolver.md)
- [AbilitySourceResolver](ability-source-resolver.md)
- [AbilityTargetResolver](ability-target-resolver.md)
