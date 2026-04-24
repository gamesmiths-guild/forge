# ConcatenateResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ConcatenateResolver`
> **Output Type:** `Quaternion`

Concatenates two quaternions using `Quaternion.Concatenate`.

## Constructor

```csharp
new ConcatenateResolver(left, right)
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
    new ConcatenateResolver(
        new VariableResolver("baseRotation", typeof(Quaternion)),
        new VariableResolver("offsetRotation", typeof(Quaternion))));
```

## Composition

```csharp
// Concatenate two rotations, then transform a direction
graph.VariableDefinitions.DefineProperty("rotatedDirection",
    new TransformResolver(
        new VariableResolver("direction", typeof(Vector3)),
        new ConcatenateResolver(
            new VariableResolver("baseRotation", typeof(Quaternion)),
            new VariableResolver("offsetRotation", typeof(Quaternion)))));
```

## See Also

- [Resolvers Overview](README.md)
- [InverseResolver](inverse-resolver.md)
- [TransformResolver](transform-resolver.md)
