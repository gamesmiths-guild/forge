# Log10Resolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.Log10Resolver`
> **Output Type:** `float` or `double`

Computes the base-10 logarithm of an operand. Supports `float` and `double` types. Integer operand types are promoted to `double`. Decimal, vector, and quaternion types are not supported.

## Constructor

```csharp
new Log10Resolver(operand)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operand | `IPropertyResolver` | The resolver for the operand (must be positive). |

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
- Computes `Math.Log10` (or `MathF.Log10` for `float`) and returns the result as a `Variant128`.
- `Log10(1) = 0`, `Log10(10) = 1`, `Log10(100) = 2`, `Log10(1000) = 3`.
- Negative operands produce `NaN`. Zero produces `-∞`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Decibel calculation: dB = 20 * Log10(amplitude / reference)
graph.VariableDefinitions.DefineProperty("decibels",
    new MultiplyResolver(
        new VariantResolver(new Variant128(20.0), typeof(double)),
        new Log10Resolver(
            new DivideResolver(
                new VariableResolver("amplitude", typeof(double)),
                new VariableResolver("reference", typeof(double))))));
```

## See Also

- [Resolvers Overview](README.md)
- [LogResolver](log-resolver.md)
- [Log2Resolver](log2-resolver.md)
- [ExpResolver](exp-resolver.md)
