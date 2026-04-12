# TanHResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.TanHResolver`
> **Output Type:** `float` or `double`

Computes the hyperbolic tangent of an operand. Supports `float` and `double` types. Integer operand types are promoted to `double`. Decimal, vector, and quaternion types are not supported.

## Constructor

```csharp
new TanHResolver(operand)
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
- Computes `Math.Tanh` (or `MathF.Tanh` for `float`) and returns the result as a `Variant128`.
- `TanH(0) = 0`. Output is bounded to `(-1, 1)`.
- TanH is an odd function: `TanH(-x) = -TanH(x)`.
- Large positive values approach `1`, large negative values approach `-1`.
- Useful for smooth activation functions and easing curves.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Smooth step mapping: maps (-∞,+∞) to (0, 1)
graph.VariableDefinitions.DefineProperty("smoothValue",
    new DivideResolver(
        new AddResolver(
            new TanHResolver(
                new VariableResolver("rawInput", typeof(double))),
            new VariantResolver(new Variant128(1.0), typeof(double))),
        new VariantResolver(new Variant128(2.0), typeof(double))));
```

## See Also

- [Resolvers Overview](README.md)
- [SinHResolver](sinh-resolver.md)
- [CosHResolver](cosh-resolver.md)
- [ATanHResolver](atanh-resolver.md)
