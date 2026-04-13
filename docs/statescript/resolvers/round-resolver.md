# RoundResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.RoundResolver`
> **Output Type:** *(same as operand type)*

Rounds the operand to a specified number of fractional digits using a configurable rounding strategy. Only `float`, `double`, and `decimal` types are supported. Integer, vector, and quaternion types are not supported.

## Constructor

```csharp
new RoundResolver(operand)
new RoundResolver(operand, digits: 2)
new RoundResolver(operand, mode: MidpointRounding.AwayFromZero)
new RoundResolver(operand, digits: 2, mode: MidpointRounding.AwayFromZero)
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| operand | `IPropertyResolver` | *(required)* | The resolver for the operand. |
| digits | `int` | `0` | Number of fractional digits in the result (0–15). |
| mode | `MidpointRounding` | `ToEven` | The rounding strategy (banker's rounding by default). |

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

**Invalid digits** (throw `ArgumentOutOfRangeException` at construction time):
- Values less than `0` or greater than `15`.

## Rounding Modes

| Mode | Behavior | Example (`2.5`) |
|------|----------|-----------------|
| `ToEven` (default) | Banker's rounding — rounds to nearest even | `2.0` |
| `AwayFromZero` | Traditional rounding — always rounds away from zero | `3.0` |
| `ToZero` | Truncates toward zero | `2.0` |
| `ToPositiveInfinity` | Rounds toward +∞ (like Ceiling) | `3.0` |
| `ToNegativeInfinity` | Rounds toward -∞ (like Floor) | `2.0` |

## Behavior

- Resolves the operand through its `IPropertyResolver` instance.
- Applies `Math.Round(value, digits, mode)` and returns the result as a `Variant128`.
- With default parameters, preserves the original behavior: `Round(2.5) = 2.0` (banker's rounding to integer).
- Type validation and digits validation happen at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Round a computed stat value to the nearest integer (default behavior)
graph.VariableDefinitions.DefineProperty("displayStat",
    new RoundResolver(
        new MultiplyResolver(
            new VariableResolver("rawStat", typeof(double)),
            new VariableResolver("modifier", typeof(double)))));
```

## Composition

```csharp
// Display DPS with 1 decimal place
graph.VariableDefinitions.DefineProperty("dps",
    new RoundResolver(
        new DivideResolver(
            new VariableResolver("totalDamage", typeof(double)),
            new VariableResolver("elapsed", typeof(double))),
        digits: 1));

// Traditional rounding for player-facing values
graph.VariableDefinitions.DefineProperty("displayScore",
    new RoundResolver(
        new VariableResolver("rawScore", typeof(double)),
        mode: MidpointRounding.AwayFromZero));
```

## See Also

- [Resolvers Overview](README.md)
- [FloorResolver](floor-resolver.md)
- [CeilResolver](ceil-resolver.md)
- [TruncateResolver](truncate-resolver.md)
