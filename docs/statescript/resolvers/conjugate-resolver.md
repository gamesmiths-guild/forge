# ConjugateResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ConjugateResolver`
> **Output Type:** `Quaternion`

Computes the conjugate of a quaternion using `Quaternion.Conjugate`.

## Constructor

```csharp
new ConjugateResolver(operand)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operand | `IPropertyResolver` | The resolver for the quaternion operand. |

## Supported Types

| Operand Type | Result Type |
|--------------|-------------|
| `Quaternion` | `Quaternion` |

## Behavior

- Resolves the operand through its `IPropertyResolver` instance.
- Delegates to `Quaternion.Conjugate`.
- Returns the conjugate quaternion as a `Variant128`.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("conjugateRotation",
    new ConjugateResolver(
        new VariableResolver("rotation", typeof(Quaternion))));
```

## Composition

```csharp
// Compare a quaternion with its conjugate
graph.VariableDefinitions.DefineProperty("rotationDifference",
    new ConcatenateResolver(
        new VariableResolver("rotation", typeof(Quaternion)),
        new ConjugateResolver(
            new VariableResolver("rotation", typeof(Quaternion)))));
```

## See Also

- [Resolvers Overview](README.md)
- [InverseResolver](inverse-resolver.md)
- [ConcatenateResolver](concatenate-resolver.md)
