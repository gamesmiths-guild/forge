# PowResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.PowResolver`
> **Output Type:** `float`, `double`, `Vector2`, `Vector3`, or `Vector4`

Raises a base value to a specified exponent. Supports `float` and `double` types, as well as `Vector2`, `Vector3`, and `Vector4` for component-wise exponentiation. Integer operand types are promoted to `double`. For vector bases, the exponent must be a scalar numeric value applied uniformly to all components. Decimal and quaternion types are not supported.

## Constructor

```csharp
new PowResolver(baseOperand, exponent)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| baseOperand | `IPropertyResolver` | The resolver for the base operand. |
| exponent | `IPropertyResolver` | The resolver for the exponent operand. |

## Type Promotion

| Operand Types | Result Type |
|---------------|-------------|
| Both `float` | `float` |
| Any `double` | `double` |
| Any integer type | `double` |
| `Vector2`, scalar numeric | `Vector2` |
| `Vector3`, scalar numeric | `Vector3` |
| `Vector4`, scalar numeric | `Vector4` |

**Invalid types** (throw `ArgumentException` at construction time):
- `decimal` (use `double` instead).
- `Quaternion` (not supported).
- Any vector type as exponent (e.g., `Vector2`/`Vector3`).
- Any scalar/vector mix where the exponent is a vector (e.g., `float`/`Vector2`).
- Unsupported non-numeric exponent types for vectors.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Computes `Math.Pow(base, exponent)` (or `MathF.Pow` for `float`) for scalar types.
- For vector types, computes the power component-wise using `MathF.Pow` with one shared scalar exponent applied to each component.
- Any exponent of `0` returns `1`.
- Any exponent of `1` returns the base value.
- Fractional exponents compute roots (e.g., `Pow(4, 0.5) = 2`).
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Compute exponential scaling: damage = baseDamage * level^exponent
graph.VariableDefinitions.DefineProperty("scaledDamage",
    new MultiplyResolver(
        new VariableResolver("baseDamage", typeof(double)),
        new PowResolver(
            new VariableResolver("level", typeof(double)),
            new VariantResolver(new Variant128(1.5), typeof(double)))));
```

## Composition

```csharp
// Apply component-wise exponentiation to scale each distance axis independently
graph.VariableDefinitions.DefineProperty("scaledDistances",
    new PowResolver(
        new VariableResolver("distanceByAxis", typeof(Vector3)),
        new VariantResolver(new Variant128(2.0f), typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [SqrtResolver](sqrt-resolver.md)
- [MultiplyResolver](multiply-resolver.md)
