# RoundResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.RoundResolver`
> **Output Type:** *(same as operand type)*

Rounds the operand to the nearest integer using banker's rounding (`MidpointRounding.ToEven`). Only `float`, `double`, and `decimal` types are supported. Integer, vector, and quaternion types are not supported.

## Constructor

```csharp
new RoundResolver(operand)
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
- `int`, `long`, and all other integer types (rounding is identity for integers).
- `Vector2`, `Vector3`, `Vector4`, `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves the operand through its `IPropertyResolver` instance.
- Applies `Math.Round` (or `MathF.Round` for `float`) and returns the result as a `Variant128`.
- Uses **banker's rounding** (round half to even): `Round(2.5) = 2.0`, `Round(3.5) = 4.0`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Round a computed stat value to the nearest integer
graph.VariableDefinitions.DefineProperty("displayStat",
    new RoundResolver(
        new MultiplyResolver(
            new VariableResolver("rawStat", typeof(double)),
            new VariableResolver("modifier", typeof(double)))));
```

## Composition

```csharp
// Round after computing an average
graph.VariableDefinitions.DefineProperty("averageDamage",
    new RoundResolver(
        new DivideResolver(
            new VariableResolver("totalDamage", typeof(double)),
            new VariableResolver("hitCount", typeof(double)))));
```

## See Also

- [Resolvers Overview](README.md)
- [FloorResolver](floor-resolver.md)
- [CeilResolver](ceil-resolver.md)
- [TruncateResolver](truncate-resolver.md)
