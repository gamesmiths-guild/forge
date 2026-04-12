# ATan2Resolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ATan2Resolver`
> **Output Type:** `float` or `double`

Computes the angle (in radians) whose tangent is the quotient of two operands: `ATan2(y, x)`. This is the standard two-argument arc tangent that correctly handles all four quadrants. Supports `float` and `double` types. Integer operand types are promoted to `double`. Decimal, vector, and quaternion types are not supported.

## Constructor

```csharp
new ATan2Resolver(y, x)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| y | `IPropertyResolver` | The resolver for the y-coordinate operand. |
| x | `IPropertyResolver` | The resolver for the x-coordinate operand. |

## Type Promotion

| Operand Types | Result Type |
|---------------|-------------|
| Both `float` | `float` |
| Any `double` | `double` |
| Any integer type | `double` |

**Invalid types** (throw `ArgumentException` at construction time):
- `decimal` (use `double` instead).
- `Vector2`, `Vector3`, `Vector4`, `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Computes `Math.Atan2(y, x)` (or `MathF.Atan2` for `float`) and returns the result as a `Variant128`.
- Returns values in the range `(-π, π]`.
- `ATan2(0, 1) = 0`, `ATan2(1, 0) = π/2`, `ATan2(1, 1) = π/4`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Compute angle to target for aiming
graph.VariableDefinitions.DefineProperty("angleToTarget",
    new ATan2Resolver(
        new SubtractResolver(
            new VariableResolver("targetY", typeof(double)),
            new VariableResolver("currentY", typeof(double))),
        new SubtractResolver(
            new VariableResolver("targetX", typeof(double)),
            new VariableResolver("currentX", typeof(double)))));
```

## See Also

- [Resolvers Overview](README.md)
- [ATanResolver](atan-resolver.md)
- [SinResolver](sin-resolver.md)
- [CosResolver](cos-resolver.md)
