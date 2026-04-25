# TanResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.TanResolver`
> **Output Type:** `float` or `double`

Computes the tangent of an operand (in radians). Supports `float` and `double` types. Integer operand types are promoted to `double`. Decimal, vector, and quaternion types are not supported.

## Constructor

```csharp
new TanResolver(operand)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operand | `IPropertyResolver` | The resolver for the operand (angle in radians). |

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
- Computes `Math.Tan` (or `MathF.Tan` for `float`) and returns the result as a `Variant128`.
- `Tan(0) = 0`, `Tan(π/4) = 1`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("slopeRatio",
    new TanResolver(
        new VariableResolver("inclineAngle", typeof(float))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("frustumHalfWidth",
    new MultiplyResolver(
        new VariableResolver("distance", typeof(double)),
        new TanResolver(
            new DivideResolver(
                new VariableResolver("fieldOfViewRadians", typeof(double)),
                new VariantResolver(new Variant128(2.0), typeof(double))))));
```

## See Also

- [Resolvers Overview](README.md)
- [SinResolver](sin-resolver.md)
- [CosResolver](cos-resolver.md)
- [ATanResolver](atan-resolver.md)
