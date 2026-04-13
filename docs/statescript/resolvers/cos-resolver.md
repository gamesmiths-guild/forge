# CosResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.CosResolver`
> **Output Type:** `float` or `double`

Computes the cosine of an operand (in radians). Supports `float` and `double` types. Integer operand types are promoted to `double`. Decimal, vector, and quaternion types are not supported.

## Constructor

```csharp
new CosResolver(operand)
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
- Computes `Math.Cos` (or `MathF.Cos` for `float`) and returns the result as a `Variant128`.
- `Cos(0) = 1`, `Cos(π/2) = 0`, `Cos(π) = -1`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Horizontal component of a directional force
graph.VariableDefinitions.DefineProperty("forceX",
    new MultiplyResolver(
        new VariableResolver("forceMagnitude", typeof(float)),
        new CosResolver(
            new VariableResolver("angle", typeof(float)))));
```

## See Also

- [Resolvers Overview](README.md)
- [SinResolver](sin-resolver.md)
- [TanResolver](tan-resolver.md)
- [ACosResolver](acos-resolver.md)
