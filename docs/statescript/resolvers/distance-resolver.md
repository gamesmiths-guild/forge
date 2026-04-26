# DistanceResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.DistanceResolver`
> **Output Type:** `float`

Resolves the Euclidean distance between two vector operands. Returns a `float`. Both operands must be the same vector type. Supports `Vector2`, `Vector3`, and `Vector4`. Scalar, quaternion, and plane types are not supported.

## Constructor

```csharp
new DistanceResolver(left, right)
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
- Delegates to `Vector2.Distance`, `Vector3.Distance`, or `Vector4.Distance` depending on the operand type.
- Returns the Euclidean distance: `sqrt((x₂-x₁)² + (y₂-y₁)² + ...)`.
- For performance-sensitive relative comparisons, prefer [DistanceSquaredResolver](distancesquared-resolver.md) to avoid the square root.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Compute the distance between two positions
graph.VariableDefinitions.DefineProperty("distanceToTarget",
    new DistanceResolver(
        new VariableResolver("currentPosition", typeof(Vector3)),
        new VariableResolver("targetPosition", typeof(Vector3))));
```

## Composition

```csharp
// Check if within interaction range
graph.VariableDefinitions.DefineProperty("isInRange",
    new ComparisonResolver(
        new DistanceResolver(
            new VariableResolver("playerPosition", typeof(Vector3)),
            new VariableResolver("objectPosition", typeof(Vector3))),
        ComparisonOperation.LessThan,
        new VariantResolver(new Variant128(5.0f), typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [DistanceSquaredResolver](distancesquared-resolver.md)
- [LengthResolver](length-resolver.md)
- [SubtractResolver](subtract-resolver.md)
