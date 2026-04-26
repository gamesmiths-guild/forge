# DotResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.DotResolver`
> **Output Type:** `float`

Computes the dot product of two operands. Returns a `float` for all supported types. Supports `Vector2`, `Vector3`, `Vector4`, and `Quaternion`. Both operands must be the same supported type. Scalar and plane types are not supported.

## Constructor

```csharp
new DotResolver(left, right)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| left | `IPropertyResolver` | The resolver for the left vector operand. |
| right | `IPropertyResolver` | The resolver for the right vector operand. |

## Supported Types

| Operand Types | Result Type |
|---------------|-------------|
| `Vector2`, `Vector2` | `float` |
| `Vector3`, `Vector3` | `float` |
| `Vector4`, `Vector4` | `float` |
| `Quaternion`, `Quaternion` | `float` |

**Invalid combinations** (throw `ArgumentException` at construction time):
- Mismatched vector types (e.g., `Vector2` and `Vector3`).
- Scalar types (`int`, `float`, `double`, etc.).
- `Plane`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Delegates to `Vector2.Dot`, `Vector3.Dot`, `Vector4.Dot`, or `Quaternion.Dot` depending on the operand type.
- The dot product measures the alignment between two vectors: positive when they point in the same direction, zero when perpendicular, and negative when opposing.
- For quaternions, the dot product measures rotational similarity and is commonly used in interpolation logic.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Check how aligned a direction is with the forward vector
graph.VariableDefinitions.DefineProperty("forwardAlignment",
    new DotResolver(
        new VariableResolver("direction", typeof(Vector3)),
        new VariableResolver("forward", typeof(Vector3))));

// Measure similarity between two rotations
graph.VariableDefinitions.DefineProperty("rotationSimilarity",
    new DotResolver(
        new VariableResolver("currentRotation", typeof(Quaternion)),
        new VariableResolver("targetRotation", typeof(Quaternion))));
```

## Composition

```csharp
// Check if two vectors are roughly perpendicular (dot product near zero)
graph.VariableDefinitions.DefineProperty("isPerpendicular",
    new ComparisonResolver(
        new AbsResolver(
            new DotResolver(
                new VariableResolver("directionA", typeof(Vector3)),
                new VariableResolver("directionB", typeof(Vector3)))),
        ComparisonOperation.LessThan,
        new VariantResolver(new Variant128(0.01f), typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [CrossResolver](cross-resolver.md)
- [NormalizeResolver](normalize-resolver.md)
- [LengthResolver](length-resolver.md)
