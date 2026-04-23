# AbsResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AbsResolver`
> **Output Type:** *(same as operand, promoted for sub-int types)*

Computes the absolute value of a single operand. Supports all **signed** numeric types in `Variant128`: `sbyte`, `short`, `int`, `long`, `float`, `double`, and `decimal`, as well as `Vector2`, `Vector3`, and `Vector4`. Unsigned types and quaternion types are not supported.

## Constructor

```csharp
new AbsResolver(operand)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| operand | `IPropertyResolver` | The resolver for the operand. |

## Type Promotion

The result type matches the operand type, with sub-int types promoted to `int`:

| Operand Type | Result Type |
|--------------|-------------|
| `sbyte`, `short` | `int` |
| `int` | `int` |
| `long` | `long` |
| `float` | `float` |
| `double` | `double` |
| `decimal` | `decimal` |
| `Vector2` | `Vector2` |
| `Vector3` | `Vector3` |
| `Vector4` | `Vector4` |

**Invalid types** (throw `ArgumentException` at construction time):
- `byte`, `ushort`, `uint`, `ulong` (unsigned, absolute value is identity).
- `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves the operand through its `IPropertyResolver` instance.
- Applies `Math.Abs` for numeric types and `Vector2.Abs`, `Vector3.Abs`, or `Vector4.Abs` for vectors, then returns the result as a `Variant128`.
- For vector types, absolute value is applied component-wise.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Distance is always positive
graph.VariableDefinitions.DefineProperty("distance",
    new AbsResolver(
        new SubtractResolver(
            new VariableResolver("positionA", typeof(int)),
            new VariableResolver("positionB", typeof(int)))));

// Make all vector components positive
graph.VariableDefinitions.DefineProperty("positiveExtents",
    new AbsResolver(
        new VariableResolver("extents", typeof(Vector3))));
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
- [MaxResolver](max-resolver.md)
- [ComparisonResolver](comparison-resolver.md)
