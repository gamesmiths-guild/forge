# EntityArrayVariableResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.EntityArrayVariableResolver`
> **Output Type:** `IForgeEntity[]`

Reads an entity-reference array from graph or shared reference variables. This is a typed convenience wrapper around
`ReferenceArrayVariableResolver<IForgeEntity>`.

## Constructors

```csharp
new EntityArrayVariableResolver(variableName)
new EntityArrayVariableResolver(variableName, scope)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| variableName | `StringKey` | The name of the reference array variable to read. |
| scope | `VariableScope` | Which variable bag to read from: `Graph` (default) or `Shared`. |

## Behavior

- Reads an `IForgeEntity[]` reference array from `Variables`.
- Supports both graph-local and shared variable scopes.
- Returns an empty array if the variable does not exist or no shared variable bag is available.

## Usage

```csharp
graph.VariableDefinitions.DefineReferenceArrayVariable<IForgeEntity>("nearbyEntities");

graph.VariableDefinitions.DefineReferenceArrayProperty("candidateTargets",
    new EntityArrayVariableResolver("nearbyEntities"));
```

## See Also

- [Resolvers Overview](README.md)
- [ArrayVariableResolver](array-resolver.md)
- [EntityVariableResolver](entity-variable-resolver.md)
