# ArrayVariableResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ArrayVariableResolver`
> **Output Type:** *(configured array element type)*

Resolves an array variable by name from either graph-local or shared scope.

## Constructor

```csharp
new ArrayVariableResolver(variableName, elementType)
new ArrayVariableResolver(variableName, elementType, scope)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| variableName | `StringKey` | The name of the array variable to read at runtime. |
| elementType | `Type` | The type of each element in the array (e.g., `typeof(int)`, `typeof(float)`). |
| scope | `VariableScope` | Which variable bag to read from: `Graph` (default) or `Shared`. |

## Behavior

- Reads an array variable from either `GraphContext.GraphVariables` or `GraphContext.SharedVariables`.
- Returns the current array contents as `Variant128[]`.
- Returns an empty array if the named variable does not exist.
- Reflects runtime changes made to the underlying variable bag.

## Usage

```csharp
var graph = new Graph();
graph.VariableDefinitions.DefineArrayVariable("targets", 10, 20, 30);

graph.VariableDefinitions.DefineArrayProperty("selectedTargets",
    new ArrayVariableResolver("targets", typeof(int)));
```

## Composition

```csharp
graph.VariableDefinitions.DefineArrayProperty("sharedTargets",
    new ArrayVariableResolver("targets", typeof(int), VariableScope.Shared));
```

## See Also
 
- [Resolvers Overview](README.md)
- [EntityArrayVariableResolver](entity-array-variable-resolver.md)
- [VariableResolver](variable-resolver.md)
- [Variables and Data](../variables.md)
