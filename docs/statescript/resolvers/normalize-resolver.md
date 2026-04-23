# NormalizeResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.NormalizeResolver`
> **Output Type:** `Vector2`, `Vector3`, `Vector4`, or `Quaternion`

Resolves the normalized (unit-length) form of a vector or quaternion operand. Supports `Vector2`, `Vector3`, `Vector4`, and `Quaternion`. Scalar and plane types are not supported.

## Constructor

```csharp
new NormalizeResolver(operand)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operand | `IPropertyResolver` | The resolver for the vector or quaternion operand. |

## Supported Types

| Operand Type | Result Type |
|--------------|-------------|
| `Vector2` | `Vector2` |
| `Vector3` | `Vector3` |
| `Vector4` | `Vector4` |
| `Quaternion` | `Quaternion` |

**Invalid types** (throw `ArgumentException` at construction time):
- Scalar types (`int`, `float`, `double`, etc.).
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves the operand through its `IPropertyResolver` instance.
- Delegates to `Vector2.Normalize`, `Vector3.Normalize`, `Vector4.Normalize`, or `Quaternion.Normalize` depending on the operand type.
- Returns a vector or quaternion with the same direction but unit length.
- For a zero-length vector, the result contains `NaN` components (standard `System.Numerics` behavior).
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Get a unit-length direction vector
graph.VariableDefinitions.DefineProperty("direction",
    new NormalizeResolver(
        new VariableResolver("velocity", typeof(Vector3))));

// Normalize a quaternion rotation
graph.VariableDefinitions.DefineProperty("normalizedRotation",
    new NormalizeResolver(
        new VariableResolver("rotation", typeof(Quaternion))));
```

## Composition

```csharp
// Compute a normalized surface normal from two edge vectors
graph.VariableDefinitions.DefineProperty("surfaceNormal",
    new NormalizeResolver(
        new CrossResolver(
            new VariableResolver("edgeA", typeof(Vector3)),
            new VariableResolver("edgeB", typeof(Vector3)))));
```

## See Also

- [Resolvers Overview](README.md)
- [LengthResolver](length-resolver.md)
- [DotResolver](dot-resolver.md)
- [CrossResolver](cross-resolver.md)
