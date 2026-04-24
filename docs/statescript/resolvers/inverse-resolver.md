# InverseResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.InverseResolver`
> **Output Type:** `Quaternion`

Computes the inverse of a quaternion using `Quaternion.Inverse`.

## Constructor

```csharp
new InverseResolver(operand)
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
- Delegates to `Quaternion.Inverse`.
- Returns the inverse quaternion as a `Variant128`.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("inverseRotation",
    new InverseResolver(
        new VariableResolver("rotation", typeof(Quaternion))));
```

## Composition

```csharp
// Undo a rotation before transforming a vector
graph.VariableDefinitions.DefineProperty("localDirection",
    new TransformResolver(
        new VariableResolver("worldDirection", typeof(Vector3)),
        new InverseResolver(
            new VariableResolver("rotation", typeof(Quaternion)))));
```

## See Also

- [Resolvers Overview](README.md)
- [ConjugateResolver](conjugate-resolver.md)
- [TransformResolver](transform-resolver.md)
