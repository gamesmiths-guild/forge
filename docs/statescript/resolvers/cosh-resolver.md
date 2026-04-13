# CosHResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.CosHResolver`
> **Output Type:** `float` or `double`

Computes the hyperbolic cosine of an operand. Supports `float` and `double` types. Integer operand types are promoted to `double`. Decimal, vector, and quaternion types are not supported.

## Constructor

```csharp
new CosHResolver(operand)
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
- Computes `Math.Cosh` (or `MathF.Cosh` for `float`) and returns the result as a `Variant128`.
- `CosH(0) = 1`. CosH is an even function: `CosH(-x) = CosH(x)`.
- Type validation happens at construction time (fail-fast), not at runtime.

## See Also

- [Resolvers Overview](README.md)
- [SinHResolver](sinh-resolver.md)
- [TanHResolver](tanh-resolver.md)
- [ACosHResolver](acosh-resolver.md)
