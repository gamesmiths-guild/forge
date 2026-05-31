# EntityArrayVariableResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.EntityArrayVariableResolver`
> **Output Type:** `IForgeEntity?[]`

Reads an entity-reference array from graph or shared object-backed variables. This is a typed convenience wrapper around
`ObjectArrayVariableResolver<IForgeEntity>`.

## Constructors

```csharp
new EntityArrayVariableResolver(variableName)
new EntityArrayVariableResolver(variableName, scope)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| variableName | `StringKey` | The name of the object-backed array variable to read. |
| scope | `VariableScope` | Which variable bag to read from: `Graph` (default) or `Shared`. |

## Behavior

- Reads an `IForgeEntity?[]` object-backed array from `Variables`.
- Supports both graph-local and shared variable scopes.
- Returns an empty array if the variable does not exist or no shared variable bag is available.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectArrayVariable<IForgeEntity>("nearbyEntities");

graph.VariableDefinitions.DefineObjectArrayProperty("candidateTargets",
    new EntityArrayVariableResolver("nearbyEntities"));
```

## See Also

- [Resolvers Overview](README.md)
- [ArrayResolver](array-resolver.md)
- [ArrayVariableResolver](array-variable-resolver.md)
- [EntityArrayResolver](entity-array-resolver.md)
- [EntityVariableResolver](entity-variable-resolver.md)
