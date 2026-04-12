# MinResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.MinResolver`
> **Output Type:** *(promoted from operand types)*

Returns the smaller of two operands. Supports all numeric types in `Variant128`. Vector and quaternion types are not supported. When the two operands have different numeric types, standard numeric promotion rules apply.

## Constructor

```csharp
new MinResolver(left, right)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| left | `IPropertyResolver` | The resolver for the left operand. |
| right | `IPropertyResolver` | The resolver for the right operand. |

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

When operands differ, the wider numeric type wins (e.g., `int` + `double` → `double`).

**Invalid types** (throw `ArgumentException` at construction time):
- `Vector2`, `Vector3`, `Vector4`, `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Converts each operand to the promoted result type.
- Returns the smaller value as a `Variant128` using `Math.Min`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Cap a stat to its maximum allowed value
graph.VariableDefinitions.DefineProperty("effectiveArmor",
    new MinResolver(
        new VariableResolver("armor", typeof(int)),
        new VariableResolver("armorCap", typeof(int))));
```

## Composition

```csharp
// Effective damage is at most the target's remaining health
graph.VariableDefinitions.DefineProperty("actualDamage",
    new MinResolver(
        new VariableResolver("rawDamage", typeof(int)),
        new VariableResolver("targetHealth", typeof(int))));
```

## See Also

- [Resolvers Overview](README.md)
- [MaxResolver](max-resolver.md)
- [ClampResolver](clamp-resolver.md)
