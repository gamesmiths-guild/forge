# ACosHResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ACosHResolver`
> **Output Type:** `float` or `double`

Computes the inverse hyperbolic cosine of an operand. Supports `float` and `double` types. Integer operand types are promoted to `double`. Decimal, vector, and quaternion types are not supported.

## Constructor

```csharp
new ACosHResolver(operand)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operand | `IPropertyResolver` | The resolver for the operand (value ≥ 1). |

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
- Computes `Math.Acosh` (or `MathF.Acosh` for `float`) and returns the result as a `Variant128`.
- `ACosH(1) = 0`.
- Values less than `1` produce `NaN`.
- Type validation happens at construction time (fail-fast), not at runtime.

## See Also

- [Resolvers Overview](README.md)
- [CosHResolver](cosh-resolver.md)
- [ACosResolver](acos-resolver.md)
