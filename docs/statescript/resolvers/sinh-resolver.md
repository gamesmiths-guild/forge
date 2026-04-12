# SinHResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.SinHResolver`
> **Output Type:** `float` or `double`

Computes the hyperbolic sine of an operand. Supports `float` and `double` types. Integer operand types are promoted to `double`. Decimal, vector, and quaternion types are not supported.

## Constructor

```csharp
new SinHResolver(operand)
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
- Computes `Math.Sinh` (or `MathF.Sinh` for `float`) and returns the result as a `Variant128`.
- `SinH(0) = 0`. SinH is an odd function: `SinH(-x) = -SinH(x)`.
- Type validation happens at construction time (fail-fast), not at runtime.

## See Also

- [Resolvers Overview](README.md)
- [CosHResolver](cosh-resolver.md)
- [TanHResolver](tanh-resolver.md)
- [ASinHResolver](asinh-resolver.md)
