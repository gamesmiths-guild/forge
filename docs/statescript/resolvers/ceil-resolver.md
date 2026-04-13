# CeilResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.CeilResolver`
> **Output Type:** *(same as operand type)*

Computes the smallest integer greater than or equal to the operand (ceiling). Only `float`, `double`, and `decimal` types are supported. Integer, vector, and quaternion types are not supported.

## Constructor

```csharp
new CeilResolver(operand)
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
- `int`, `long`, and all other integer types (ceiling is identity for integers).
- `Vector2`, `Vector3`, `Vector4`, `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves the operand through its `IPropertyResolver` instance.
- Applies `Math.Ceiling` (or `MathF.Ceiling` for `float`) and returns the result as a `Variant128`.
- For positive values, rounds up: `Ceil(2.3) = 3.0`.
- For negative values, rounds toward zero: `Ceil(-2.7) = -2.0`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Always round up resource requirements
graph.VariableDefinitions.DefineProperty("requiredItems",
    new CeilResolver(
        new DivideResolver(
            new VariableResolver("totalCost", typeof(double)),
            new VariableResolver("costPerItem", typeof(double)))));
```

## Composition

```csharp
// Minimum number of hits to defeat an enemy (always round up)
graph.VariableDefinitions.DefineProperty("hitsRequired",
    new CeilResolver(
        new DivideResolver(
            new VariableResolver("enemyHealth", typeof(float)),
            new VariableResolver("damagePerHit", typeof(float)))));
```

## See Also

- [Resolvers Overview](README.md)
- [FloorResolver](floor-resolver.md)
- [RoundResolver](round-resolver.md)
- [TruncateResolver](truncate-resolver.md)
