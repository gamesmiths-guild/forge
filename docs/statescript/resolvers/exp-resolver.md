# ExpResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ExpResolver`
> **Output Type:** `float` or `double`

Computes `e` raised to the specified power (`e^x`). Supports `float` and `double` types. Integer operand types are promoted to `double`. Decimal, vector, and quaternion types are not supported.

## Constructor

```csharp
new ExpResolver(operand)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operand | `IPropertyResolver` | The resolver for the exponent operand. |

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
- Computes `Math.Exp` (or `MathF.Exp` for `float`) and returns the result as a `Variant128`.
- `Exp(0) = 1`, `Exp(1) = e ≈ 2.71828`.
- Negative operands return values approaching zero: `Exp(-1) = 1/e`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Exponential growth: population = initial * Exp(rate * time)
graph.VariableDefinitions.DefineProperty("population",
    new MultiplyResolver(
        new VariableResolver("initialPopulation", typeof(double)),
        new ExpResolver(
            new MultiplyResolver(
                new VariableResolver("growthRate", typeof(double)),
                new VariableResolver("time", typeof(double))))));
```

## See Also

- [Resolvers Overview](README.md)
- [LogResolver](log-resolver.md)
- [PowResolver](pow-resolver.md)
