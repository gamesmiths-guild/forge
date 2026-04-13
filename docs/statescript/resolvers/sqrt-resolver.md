# SqrtResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.SqrtResolver`
> **Output Type:** `float` or `double`

Computes the square root of a single operand. Supports `float` and `double` types. Integer operand types are promoted to `double`. Decimal, vector, and quaternion types are not supported.

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

**Invalid types** (throw `ArgumentException` at construction time):
- `decimal` (use `double` instead).
- `Vector2`, `Vector3`, `Vector4`, `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves the operand through its `IPropertyResolver` instance.
- Computes `Math.Sqrt` (or `MathF.Sqrt` for `float`) and returns the result as a `Variant128`.
- `Sqrt(0)` returns `0`. `Sqrt(1)` returns `1`.
- Negative operands produce `NaN` (consistent with `Math.Sqrt` behavior).
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
