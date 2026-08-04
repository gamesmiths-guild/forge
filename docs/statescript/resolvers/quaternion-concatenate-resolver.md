# QuaternionConcatenateResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.QuaternionConcatenateResolver`
> **Output Type:** `Quaternion`

Concatenates two quaternions using `Quaternion.Concatenate`: the result is the left rotation followed by the right one.

> Not to be confused with [`ConcatResolver`](concat-resolver.md), which joins two **arrays**. The `Quaternion` prefix is what tells the two apart.

## Constructor

```csharp
new QuaternionConcatenateResolver(left, right)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| left | `IPropertyResolver` | The resolver for the left quaternion operand. |
| right | `IPropertyResolver` | The resolver for the right quaternion operand. |

## Supported Types

| Left Type | Right Type | Result Type |
|-----------|------------|-------------|
| `Quaternion` | `Quaternion` | `Quaternion` |

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Delegates to `Quaternion.Concatenate(left, right)`.
- Returns the concatenated quaternion as a `Variant128`.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("combinedRotation",
    new QuaternionConcatenateResolver(
        new VariableResolver("baseRotation", typeof(Quaternion)),
        new VariableResolver("offsetRotation", typeof(Quaternion))));
```

## Composition

```csharp
// Concatenate two rotations, then transform a direction
graph.VariableDefinitions.DefineProperty("rotatedDirection",
    new TransformResolver(
        new VariableResolver("direction", typeof(Vector3)),
        new QuaternionConcatenateResolver(
            new VariableResolver("baseRotation", typeof(Quaternion)),
            new VariableResolver("offsetRotation", typeof(Quaternion)))));
```

## See Also

- [Resolvers Overview](README.md)
- [InverseResolver](inverse-resolver.md)
- [TransformResolver](transform-resolver.md)
- [ConcatResolver](concat-resolver.md) — the array operation with the similar name
