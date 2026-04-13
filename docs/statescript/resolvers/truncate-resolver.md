# TruncateResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.TruncateResolver`
> **Output Type:** *(same as operand type)*

Removes the fractional part of the operand, always rounding toward zero. Only `float`, `double`, and `decimal` types are supported. Integer, vector, and quaternion types are not supported.

## Constructor

```csharp
new TruncateResolver(operand)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operand | `IPropertyResolver` | The resolver for the operand. |

## Type Promotion

The result type matches the operand type exactly:

| Operand Type | Result Type |
|--------------|-------------|
| `float` | `float` |
| `double` | `double` |
| `decimal` | `decimal` |

**Invalid types** (throw `ArgumentException` at construction time):
- `int`, `long`, and all other integer types (truncation is identity for integers).
- `Vector2`, `Vector3`, `Vector4`, `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves the operand through its `IPropertyResolver` instance.
- Applies `Math.Truncate` (or `MathF.Truncate` for `float`) and returns the result as a `Variant128`.
- Always rounds **toward zero**: `Truncate(2.7) = 2.0`, `Truncate(-2.7) = -2.0`.
- This differs from `Floor` (which rounds toward negative infinity) and `Ceil` (which rounds toward positive infinity).
- Type validation happens at construction time (fail-fast), not at runtime.

### Comparison of Rounding Resolvers

| Value | Floor | Ceil | Round | Truncate |
|-------|-------|------|-------|----------|
| 2.7 | 2.0 | 3.0 | 3.0 | 2.0 |
| 2.3 | 2.0 | 3.0 | 2.0 | 2.0 |
| -2.7 | -3.0 | -2.0 | -3.0 | -2.0 |
| -2.3 | -3.0 | -2.0 | -2.0 | -2.0 |

## Usage

```csharp
// Convert a float coordinate to a grid index (always toward zero)
graph.VariableDefinitions.DefineProperty("gridIndex",
    new TruncateResolver(
        new DivideResolver(
            new VariableResolver("position", typeof(double)),
            new VariableResolver("cellSize", typeof(double)))));
```

## Composition

```csharp
// Truncate integer division result for consistent behavior with negative numbers
graph.VariableDefinitions.DefineProperty("quotient",
    new TruncateResolver(
        new DivideResolver(
            new VariableResolver("dividend", typeof(double)),
            new VariableResolver("divisor", typeof(double)))));
```

## See Also

- [Resolvers Overview](README.md)
- [FloorResolver](floor-resolver.md)
- [CeilResolver](ceil-resolver.md)
- [RoundResolver](round-resolver.md)
