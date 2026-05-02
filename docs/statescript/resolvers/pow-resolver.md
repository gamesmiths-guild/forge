# PowResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.PowResolver`
> **Output Type:** `float`, `double`, `Vector2`, `Vector3`, or `Vector4`

Raises a base value to a specified exponent. Supports `float` and `double` types, as well as `Vector2`, `Vector3`, and `Vector4` for component-wise exponentiation. Integer operand types are promoted to `double`. Decimal and quaternion types are not supported. Vector operands must be the same type.

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
| `Vector2`, `Vector2` | `Vector2` |
| `Vector3`, `Vector3` | `Vector3` |
| `Vector4`, `Vector4` | `Vector4` |

**Invalid types** (throw `ArgumentException` at construction time):
- `decimal` (use `double` instead).
- Mixed vector types, scalar/vector mixes, and `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Computes `Math.Pow(base, exponent)` (or `MathF.Pow` for `float`) for scalar types.
- For vector types, computes the power component-wise using `MathF.Pow` for each corresponding component.
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
// Weight each distance component independently before summing
graph.VariableDefinitions.DefineProperty("xpRequired",
    new PowResolver(
        new VariableResolver("distanceByAxis", typeof(Vector3)),
        new VariantResolver(new Variant128(new Vector3(2.0f, 2.0f, 1.0f)), typeof(Vector3))));
```

## See Also

- [Resolvers Overview](README.md)
- [SqrtResolver](sqrt-resolver.md)
- [MultiplyResolver](multiply-resolver.md)
