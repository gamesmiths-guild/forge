# VariableResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.VariableResolver`
> **Output Type:** *(configured at construction time)*

Reads a graph variable by name. Useful for property expressions that reference mutable graph variables.

## Constructor

```csharp
new VariableResolver(referencedVariableName, valueType)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| referencedVariableName | `StringKey` | The name of the graph variable to read at runtime. |
| valueType | `Type` | The type this resolver produces (e.g., `typeof(int)`, `typeof(double)`). |

## Behavior

- Looks up the named variable in `GraphContext.GraphVariables`.
- Returns the current value as a `Variant128`.
- Returns a default `Variant128` (zero) if the variable does not exist.
- Reflects runtime changes: if the variable is updated between resolves, subsequent calls return the new value.

## Usage

```csharp
new VariableResolver("counter", typeof(int))
new VariableResolver("speed", typeof(double))
```

## See Also

- [Resolvers Overview](README.md)
- [SharedVariableResolver](shared-variable-resolver.md)
- [VariantResolver](variant-resolver.md)
- [Variables and Data](../variables.md)
