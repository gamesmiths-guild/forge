# FloorResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.FloorResolver`
> **Output Type:** *(same as operand type)*

Computes the largest integer less than or equal to the operand (floor). Only `float`, `double`, and `decimal` types are supported. Integer, vector, and quaternion types are not supported.

## Constructor

```csharp
new FloorResolver(operand)
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
- `int`, `long`, and all other integer types (floor is identity for integers).
- `Vector2`, `Vector3`, `Vector4`, `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves the operand through its `IPropertyResolver` instance.
- Applies `Math.Floor` (or `MathF.Floor` for `float`) and returns the result as a `Variant128`.
- For positive values, removes the fractional part: `Floor(2.7) = 2.0`.
- For negative values, rounds away from zero: `Floor(-2.3) = -3.0`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Snap a world position to grid coordinates
graph.VariableDefinitions.DefineProperty("gridX",
    new MultiplyResolver(
        new FloorResolver(
            new DivideResolver(
                new VariableResolver("worldX", typeof(double)),
                new VariableResolver("cellSize", typeof(double)))),
        new VariableResolver("cellSize", typeof(double))));
```

## Composition

```csharp
// Floor damage before applying it
graph.VariableDefinitions.DefineProperty("finalDamage",
    new FloorResolver(
        new MultiplyResolver(
            new VariableResolver("baseDamage", typeof(double)),
            new VariableResolver("critMultiplier", typeof(double)))));
```

## See Also

- [Resolvers Overview](README.md)
- [CeilResolver](ceil-resolver.md)
- [RoundResolver](round-resolver.md)
- [TruncateResolver](truncate-resolver.md)
