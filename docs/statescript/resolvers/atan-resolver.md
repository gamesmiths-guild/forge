# ATanResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ATanResolver`
> **Output Type:** `float` or `double`

Computes the arc tangent (inverse tangent) of an operand, returning the angle in radians. Supports `float` and `double` types. Integer operand types are promoted to `double`. Decimal, vector, and quaternion types are not supported.

## Constructor

```csharp
new ATanResolver(operand)
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
- Computes `Math.Atan` (or `MathF.Atan` for `float`) and returns the result as a `Variant128`.
- `ATan(0) = 0`, `ATan(1) = π/4`, `ATan(-1) = -π/4`.
- Type validation happens at construction time (fail-fast), not at runtime.

## See Also

- [Resolvers Overview](README.md)
- [TanResolver](tan-resolver.md)
- [ATan2Resolver](atan2-resolver.md)
- [ATanHResolver](atanh-resolver.md)
