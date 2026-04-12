# SinResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.SinResolver`
> **Output Type:** `float` or `double`

Computes the sine of an operand (in radians). Supports `float` and `double` types. Integer operand types are promoted to `double`. Decimal, vector, and quaternion types are not supported.

## Constructor

```csharp
new SinResolver(operand)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operand | `IPropertyResolver` | The resolver for the operand (angle in radians). |

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
- Computes `Math.Sin` (or `MathF.Sin` for `float`) and returns the result as a `Variant128`.
- `Sin(0) = 0`, `Sin(π/2) = 1`, `Sin(π) = 0`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Oscillating value for bobbing animation
graph.VariableDefinitions.DefineProperty("bobOffset",
    new MultiplyResolver(
        new VariableResolver("amplitude", typeof(float)),
        new SinResolver(
            new MultiplyResolver(
                new VariableResolver("frequency", typeof(float)),
                new VariableResolver("time", typeof(float))))));
```

## See Also

- [Resolvers Overview](README.md)
- [CosResolver](cos-resolver.md)
- [TanResolver](tan-resolver.md)
- [ASinResolver](asin-resolver.md)
