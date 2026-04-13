# DivideResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.DivideResolver`
> **Output Type:** *(promoted from operand types)*

Divides the left operand by the right operand. Supports all numeric types in `Variant128` as well as `Vector2`, `Vector3`, and `Vector4` (component-wise division). `Quaternion` is not supported.

## Constructor

```csharp
new DivideResolver(left, right)
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
| `Vector2` | `Vector2` |
| `Vector3` | `Vector3` |
| `Vector4` | `Vector4` |

When operands differ, the wider numeric type wins:

| Mixed Operands | Result Type |
|----------------|-------------|
| `int / float` | `float` |
| `int / double` | `double` |
| `float / double` | `double` |
| `byte / int` | `int` |
| `short / long` | `long` |
| `int / decimal` | `decimal` |

**Invalid combinations** (throw `ArgumentException` at construction time):
- `Quaternion` (no standard division operation).
- Mixing vector types (e.g., `Vector2 / Vector3`).
- Mixing vector with numeric types (e.g., `Vector3 / float`).
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Converts each operand to the promoted result type.
- Performs the division (`left / right`) and returns the result as a `Variant128`.
- For integer types, division follows C# truncation rules (truncates toward zero).
- For `Vector2`, `Vector3`, and `Vector4`, division is **component-wise**.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("damagePerHit",
    new DivideResolver(
        new VariableResolver("totalDamage", typeof(int)),
        new VariableResolver("hitCount", typeof(int))));

graph.VariableDefinitions.DefineProperty("normalizedDirection",
    new DivideResolver(
        new VariableResolver("direction", typeof(Vector3)),
        new VariableResolver("magnitude", typeof(Vector3))));
```

## Composition

```csharp
// Average of two attributes: (a + b) / 2
graph.VariableDefinitions.DefineProperty("averageAttribute",
    new DivideResolver(
        new AddResolver(
            new AttributeResolver("CombatAttributeSet.Strength"),
            new AttributeResolver("CombatAttributeSet.Dexterity")),
        new VariantResolver(new Variant128(2), typeof(int))));
```

## See Also

- [Resolvers Overview](README.md)
- [MultiplyResolver](multiply-resolver.md)
- [ModuloResolver](modulo-resolver.md)
