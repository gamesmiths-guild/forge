# SqrtResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.SqrtResolver`
> **Output Type:** `float`, `double`, `Vector2`, `Vector3`, or `Vector4`

Computes the square root of a single operand. Supports `float` and `double` types, as well as `Vector2`, `Vector3`, and `Vector4`. Integer operand types are promoted to `double`. Decimal and quaternion types are not supported. For vector types, the square root is computed component-wise.

## Constructor

```csharp
new SqrtResolver(operand)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operand | `IPropertyResolver` | The resolver for the operand. |

## Type Promotion

| Operand Type | Result Type |
|--------------|-------------|
| `float` | `float` |
| `double` | `double` |
| Any integer type | `double` |
| `Vector2` | `Vector2` |
| `Vector3` | `Vector3` |
| `Vector4` | `Vector4` |

**Invalid types** (throw `ArgumentException` at construction time):
- `decimal` (use `double` instead).
- `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves the operand through its `IPropertyResolver` instance.
- Computes `Math.Sqrt` (or `MathF.Sqrt` for `float`) for scalar types, or `Vector2.SquareRoot`, `Vector3.SquareRoot`, or `Vector4.SquareRoot` for vector types, and returns the result as a `Variant128`.
- `Sqrt(0)` returns `0`. `Sqrt(1)` returns `1`.
- Negative operands produce `NaN` (consistent with `Math.Sqrt` behavior).
- For vector types, negative components produce `NaN` in the corresponding result components.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Compute distance between two 2D points
graph.VariableDefinitions.DefineProperty("distance",
    new SqrtResolver(
        new AddResolver(
            new PowResolver(
                new SubtractResolver(
                    new VariableResolver("x2", typeof(double)),
                    new VariableResolver("x1", typeof(double))),
                new VariantResolver(new Variant128(2.0), typeof(double))),
            new PowResolver(
                new SubtractResolver(
                    new VariableResolver("y2", typeof(double)),
                    new VariableResolver("y1", typeof(double))),
                new VariantResolver(new Variant128(2.0), typeof(double))))));

// Compute component-wise square roots of a variance vector
graph.VariableDefinitions.DefineProperty("standardDeviation",
    new SqrtResolver(
        new VariableResolver("variance", typeof(Vector3))));
```

## Composition

```csharp
// Level-based stat scaling: stat = baseStat + Floor(Sqrt(level) * growthRate)
graph.VariableDefinitions.DefineProperty("scaledStat",
    new AddResolver(
        new VariableResolver("baseStat", typeof(double)),
        new FloorResolver(
            new MultiplyResolver(
                new SqrtResolver(
                    new VariableResolver("level", typeof(double))),
                new VariableResolver("growthRate", typeof(double))))));
```

## See Also

- [Resolvers Overview](README.md)
- [PowResolver](pow-resolver.md)
- [FloorResolver](floor-resolver.md)
- [LengthResolver](length-resolver.md)
