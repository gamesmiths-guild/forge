# LengthSquaredResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.LengthSquaredResolver`
> **Output Type:** `float`

Resolves the squared length (squared magnitude) of a vector operand. Returns a `float`. This is more efficient than [LengthResolver](length-resolver.md) when only relative comparisons are needed, as it avoids the square root computation. Supports `Vector2`, `Vector3`, and `Vector4`. Scalar, quaternion, and plane types are not supported.

## Constructor

```csharp
new LengthSquaredResolver(operand)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operand | `IPropertyResolver` | The resolver for the vector operand. |

## Supported Types

| Operand Type | Result Type |
|--------------|-------------|
| `Vector2` | `float` |
| `Vector3` | `float` |
| `Vector4` | `float` |

**Invalid types** (throw `ArgumentException` at construction time):
- Scalar types (`int`, `float`, `double`, etc.).
- `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves the operand through its `IPropertyResolver` instance.
- Calls `.LengthSquared()` on the resolved vector value.
- Returns `x² + y² + ...` without computing the square root.
- Useful for distance comparisons where the actual magnitude is not needed (comparing squared values preserves ordering).
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Get the squared speed for efficient comparison
graph.VariableDefinitions.DefineProperty("speedSquared",
    new LengthSquaredResolver(
        new VariableResolver("velocity", typeof(Vector3))));
```

## Composition

```csharp
// Check if an entity is within range using squared distance (avoids sqrt)
graph.VariableDefinitions.DefineProperty("isInRange",
    new ComparisonResolver(
        new LengthSquaredResolver(
            new SubtractResolver(
                new VariableResolver("targetPosition", typeof(Vector3)),
                new VariableResolver("currentPosition", typeof(Vector3)))),
        ComparisonOperation.LessThan,
        new VariantResolver(new Variant128(100.0f), typeof(float))));  // 10 units squared
```

## See Also

- [Resolvers Overview](README.md)
- [LengthResolver](length-resolver.md)
- [DistanceSquaredResolver](distancesquared-resolver.md)
- [NormalizeResolver](normalize-resolver.md)
