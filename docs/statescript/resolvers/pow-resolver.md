# PowResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.PowResolver`
> **Output Type:** `float` or `double`

Raises a base value to a specified exponent. Supports `float` and `double` types. Integer operand types are promoted to `double`. Decimal, vector, and quaternion types are not supported.

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

**Invalid types** (throw `ArgumentException` at construction time):
- `decimal` (use `double` instead).
- `Vector2`, `Vector3`, `Vector4`, `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Computes `Math.Pow(base, exponent)` (or `MathF.Pow` for `float`) and returns the result as a `Variant128`.
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
// Experience curve: xpRequired = baseXP * Pow(level, growthExponent)
graph.VariableDefinitions.DefineProperty("xpRequired",
    new FloorResolver(
        new MultiplyResolver(
            new VariableResolver("baseXP", typeof(double)),
            new PowResolver(
                new VariableResolver("level", typeof(double)),
                new VariableResolver("growthExponent", typeof(double))))));
```

## See Also

- [Resolvers Overview](README.md)
- [SqrtResolver](sqrt-resolver.md)
- [MultiplyResolver](multiply-resolver.md)
