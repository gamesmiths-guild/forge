# SharedVariableResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.SharedVariableResolver`
> **Output Type:** *(configured at construction time)*

Reads a shared variable from the entity's shared variable bag. Enables cross-ability communication by accessing entity-level state that persists across graph instances.

## Constructor

```csharp
new SharedVariableResolver(referencedVariableName, valueType)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| referencedVariableName | `StringKey` | The name of the shared variable to read at runtime. |
| valueType | `Type` | The type this resolver produces (e.g., `typeof(int)`, `typeof(bool)`). |

## Behavior

- Reads the named variable from `GraphContext.SharedVariables`.
- Returns the current value as a `Variant128`.
- Returns a default `Variant128` (zero) if `SharedVariables` is `null` or the variable does not exist.
- Changes to the shared variable are visible across all graph contexts that share the same entity.

## Usage

```csharp
new SharedVariableResolver("comboCounter", typeof(int))
new SharedVariableResolver("abilityLock", typeof(bool))
```

## Composition

```csharp
// Compare a shared counter against a threshold
graph.VariableDefinitions.DefineProperty("comboReady",
    new ComparisonResolver(
        new SharedVariableResolver("comboCounter", typeof(int)),
        ComparisonOperation.GreaterThanOrEqual,
        new VariantResolver(new Variant128(3), typeof(int))));
```

## See Also

- [Resolvers Overview](README.md)
- [VariableResolver](variable-resolver.md)
- [Variables and Data](../variables.md)
