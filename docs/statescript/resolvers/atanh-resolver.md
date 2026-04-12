# ATanHResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ATanHResolver`
> **Output Type:** `float` or `double`

Computes the inverse hyperbolic tangent of an operand. Supports `float` and `double` types. Integer operand types are promoted to `double`. Decimal, vector, and quaternion types are not supported.

## Constructor

```csharp
new ATanHResolver(operand)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operand | `IPropertyResolver` | The resolver for the operand (value in the range (-1, 1)). |

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
- Computes `Math.Atanh` (or `MathF.Atanh` for `float`) and returns the result as a `Variant128`.
- `ATanH(0) = 0`. ATanH is an odd function.
- Values at `±1` produce `±∞`. Values outside `(-1, 1)` produce `NaN`.
- Type validation happens at construction time (fail-fast), not at runtime.

## See Also

- [Resolvers Overview](README.md)
- [TanHResolver](tanh-resolver.md)
- [ATanResolver](atan-resolver.md)
