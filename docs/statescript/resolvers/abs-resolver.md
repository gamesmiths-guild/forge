# AbsResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AbsResolver`
> **Output Type:** *(same as operand, promoted for sub-int types)*

Computes the absolute value of a single operand. Supports all **signed** numeric types in `Variant128`: `sbyte`, `short`, `int`, `long`, `float`, `double`, and `decimal`. Unsigned types, vector types, and quaternion types are not supported (absolute value is meaningless for unsigned types and has no standard definition for vectors/quaternions).

## Constructor

```csharp
new AbsResolver(operand)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operand | `IPropertyResolver` | The resolver for the operand. |

## Type Promotion

The result type matches the operand type, with sub-int types promoted to `int`. Only **signed** numeric types are supported:

| Operand Type | Result Type |
|--------------|-------------|
| `sbyte`, `short` | `int` |
| `int` | `int` |
| `long` | `long` |
| `float` | `float` |
| `double` | `double` |
| `decimal` | `decimal` |

**Invalid types** (throw `ArgumentException` at construction time):
- `byte`, `ushort`, `uint`, `ulong` (unsigned, absolute value is identity).
- `Vector2`, `Vector3`, `Vector4`, `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves the operand through its `IPropertyResolver` instance.
- Applies `Math.Abs` and returns the result as a `Variant128`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Distance is always positive
graph.VariableDefinitions.DefineProperty("distance",
    new AbsResolver(
        new SubtractResolver(
            new VariableResolver("positionA", typeof(int)),
            new VariableResolver("positionB", typeof(int)))));
```

## Composition

```csharp
// Check if two positions are within threshold distance:
// Abs(a - b) < threshold
graph.VariableDefinitions.DefineProperty("isInRange",
    new ComparisonResolver(
        new AbsResolver(
            new SubtractResolver(
                new VariableResolver("positionA", typeof(float)),
                new VariableResolver("positionB", typeof(float)))),
        ComparisonOperation.LessThan,
        new VariantResolver(new Variant128(5.0f), typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [NegateResolver](negate-resolver.md)
- [ComparisonResolver](comparison-resolver.md)
