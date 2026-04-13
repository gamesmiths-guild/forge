# ModuloResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ModuloResolver`
> **Output Type:** *(promoted from operand types)*

Computes the remainder of dividing the left operand by the right operand. Supports all numeric types in `Variant128`. Vector and quaternion types are not supported.

## Constructor

```csharp
new ModuloResolver(left, right)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| left | `IPropertyResolver` | The resolver for the left (dividend) operand. |
| right | `IPropertyResolver` | The resolver for the right (divisor) operand. |

## Type Promotion

When both operands have the same type, the result type is determined by promotion rules that mirror C# arithmetic:

| Operand Types | Result Type |
|---------------|-------------|
| `byte`, `sbyte`, `short`, `ushort` | `int` |
| `int` | `int` |
| `uint` | `long` |
| `long` | `long` |
| `ulong` | `double` |
| `float` | `float` |
| `double` | `double` |
| `decimal` | `decimal` |

When operands differ, the wider numeric type wins:

| Mixed Operands | Result Type |
|----------------|-------------|
| `int % float` | `float` |
| `int % double` | `double` |
| `float % double` | `double` |
| `byte % int` | `int` |
| `short % long` | `long` |
| `int % decimal` | `decimal` |

**Invalid combinations** (throw `ArgumentException` at construction time):
- `Vector2`, `Vector3`, `Vector4`, `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Converts each operand to the promoted result type.
- Performs the remainder operation (`left % right`) and returns the result as a `Variant128`.
- For integer types, follows C# truncation-toward-zero semantics (e.g., `-10 % 3 = -1`).
- For floating-point types, computes the truncated remainder (C# `%` operator), not the IEEE remainder (`Math.IEEERemainder`).
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Cycle a counter every N steps
graph.VariableDefinitions.DefineProperty("cycleIndex",
    new ModuloResolver(
        new VariableResolver("tickCount", typeof(int)),
        new VariantResolver(new Variant128(4), typeof(int))));
```

## Composition

```csharp
// Check if a counter is even: (counter % 2) == 0
graph.VariableDefinitions.DefineProperty("isEvenTick",
    new ComparisonResolver(
        new ModuloResolver(
            new VariableResolver("tickCount", typeof(int)),
            new VariantResolver(new Variant128(2), typeof(int))),
        ComparisonOperation.Equal,
        new VariantResolver(new Variant128(0), typeof(int))));
```

## See Also

- [Resolvers Overview](README.md)
- [DivideResolver](divide-resolver.md)
- [ComparisonResolver](comparison-resolver.md)
