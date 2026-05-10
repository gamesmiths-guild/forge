# VariableResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.VariableResolver`
> **Output Type:** *(configured at construction time)*

Reads a mutable variable by name from either graph-local or shared scope.

## Constructor

```csharp
new VariableResolver(referencedVariableName, valueType)
new VariableResolver(referencedVariableName, valueType, scope)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| referencedVariableName | `StringKey` | The name of the variable to read at runtime. |
| valueType | `Type` | The type this resolver produces (e.g., `typeof(int)`, `typeof(double)`). |
| scope | `VariableScope` | Which variable bag to read from: `Graph` (default) or `Shared`. |

## Behavior

- Looks up the named variable in either `GraphContext.GraphVariables` or `GraphContext.SharedVariables`.
- Returns the current value as a `Variant128`.
- Returns a default `Variant128` (zero) if the variable does not exist.
- Reflects runtime changes: if the variable is updated between resolves, subsequent calls return the new value.

## Usage

```csharp
new VariableResolver("counter", typeof(int))
new VariableResolver("speed", typeof(double))
new VariableResolver("comboCounter", typeof(int), VariableScope.Shared)
```

## Composition

```csharp
// Use a graph variable as an operand in arithmetic
graph.VariableDefinitions.DefineProperty("boostedSpeed",
    new MultiplyResolver(
        new VariableResolver("baseSpeed", typeof(float)),
        new VariantResolver(new Variant128(1.5f), typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [ArrayVariableResolver](array-resolver.md)
- [VariantResolver](variant-resolver.md)
- [Variables and Data](../variables.md)
