# LengthResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.LengthResolver`
> **Output Type:** `float`

Resolves the length (magnitude) of a vector or quaternion operand. Returns a `float`. Supports `Vector2`, `Vector3`, `Vector4`, and `Quaternion`. Scalar and plane types are not supported.

## Constructor

```csharp
new LengthResolver(operand)
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
| `Quaternion` | `float` |

**Invalid types** (throw `ArgumentException` at construction time):
- Scalar types (`int`, `float`, `double`, etc.).
- `Plane`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves the operand through its `IPropertyResolver` instance.
- Calls `.Length()` on the resolved vector or quaternion value.
- Returns the Euclidean length (L2 norm): `sqrt(x² + y² + ...)`.
- For quaternions, returns `sqrt(x² + y² + z² + w²)`.
- For performance-sensitive relative comparisons, prefer [LengthSquaredResolver](lengthsquared-resolver.md) to avoid the square root.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Get the speed (magnitude of velocity)
graph.VariableDefinitions.DefineProperty("speed",
    new LengthResolver(
        new VariableResolver("velocity", typeof(Vector3))));
```

## Composition

```csharp
// Check if an entity is moving faster than a threshold
graph.VariableDefinitions.DefineProperty("isMovingFast",
    new ComparisonResolver(
        new LengthResolver(
            new VariableResolver("velocity", typeof(Vector3))),
        ComparisonOperation.GreaterThan,
        new VariantResolver(new Variant128(10.0f), typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [LengthSquaredResolver](lengthsquared-resolver.md)
- [NormalizeResolver](normalize-resolver.md)
- [DistanceResolver](distance-resolver.md)
