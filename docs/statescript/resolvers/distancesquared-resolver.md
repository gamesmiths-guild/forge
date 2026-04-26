# DistanceSquaredResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.DistanceSquaredResolver`
> **Output Type:** `float`

Resolves the squared Euclidean distance between two vector operands. Returns a `float`. This is more efficient than [DistanceResolver](distance-resolver.md) when only relative comparisons are needed, as it avoids the square root computation. Both operands must be the same vector type. Supports `Vector2`, `Vector3`, and `Vector4`. Scalar, quaternion, and plane types are not supported.

## Constructor

```csharp
new DistanceSquaredResolver(left, right)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| left | `IPropertyResolver` | The resolver for the first point. |
| right | `IPropertyResolver` | The resolver for the second point. |

## Supported Types

| Operand Types | Result Type |
|---------------|-------------|
| `Vector2`, `Vector2` | `float` |
| `Vector3`, `Vector3` | `float` |
| `Vector4`, `Vector4` | `float` |

**Invalid combinations** (throw `ArgumentException` at construction time):
- Mismatched vector types (e.g., `Vector2` and `Vector3`).
- Scalar types (`int`, `float`, `double`, etc.).
- `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Delegates to `Vector2.DistanceSquared`, `Vector3.DistanceSquared`, or `Vector4.DistanceSquared` depending on the operand type.
- Returns `(x₂-x₁)² + (y₂-y₁)² + ...` without computing the square root.
- Useful for range checks and proximity tests where the actual distance value is not needed (comparing squared values preserves ordering).
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Efficiently check proximity without computing square root
graph.VariableDefinitions.DefineProperty("distanceSquaredToTarget",
    new DistanceSquaredResolver(
        new VariableResolver("currentPosition", typeof(Vector3)),
        new VariableResolver("targetPosition", typeof(Vector3))));
```

## Composition

```csharp
// Check if within range using squared distance (avoids sqrt)
graph.VariableDefinitions.DefineProperty("isInRange",
    new ComparisonResolver(
        new DistanceSquaredResolver(
            new VariableResolver("playerPosition", typeof(Vector3)),
            new VariableResolver("objectPosition", typeof(Vector3))),
        ComparisonOperation.LessThan,
        new VariantResolver(new Variant128(25.0f), typeof(float))));  // 5 units squared
```

## See Also

- [Resolvers Overview](README.md)
- [DistanceResolver](distance-resolver.md)
- [LengthSquaredResolver](lengthsquared-resolver.md)
- [SubtractResolver](subtract-resolver.md)
