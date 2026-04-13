# ClampResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ClampResolver`
> **Output Type:** *(promoted from operand types)*

Clamps a value between a minimum and maximum bound. Supports all numeric types in `Variant128`. Vector and quaternion types are not supported. When operands have different numeric types, standard numeric promotion rules apply across all three operands.

## Constructor

```csharp
new ClampResolver(value, min, max)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| value | `IPropertyResolver` | The resolver for the value to clamp. |
| min | `IPropertyResolver` | The resolver for the minimum bound. |
| max | `IPropertyResolver` | The resolver for the maximum bound. |

## Type Promotion

The result type is determined by promoting all three operand types using standard C# numeric promotion rules:

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

When operands differ, the widest numeric type wins.

**Invalid types** (throw `ArgumentException` at construction time):
- `Vector2`, `Vector3`, `Vector4`, `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves all three operands through their respective `IPropertyResolver` instances.
- Converts each operand to the promoted result type.
- Returns `Math.Clamp(value, min, max)` as a `Variant128`.
- If `value` is within `[min, max]`, returns `value` unchanged.
- If `value < min`, returns `min`.
- If `value > max`, returns `max`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Clamp health between 0 and maxHealth after taking damage
graph.VariableDefinitions.DefineProperty("currentHealth",
    new ClampResolver(
        new SubtractResolver(
            new VariableResolver("health", typeof(int)),
            new VariableResolver("damage", typeof(int))),
        new VariantResolver(new Variant128(0), typeof(int)),
        new VariableResolver("maxHealth", typeof(int))));
```

## Composition

```csharp
// Clamp a multiplier between 0.5 and 2.0 based on computed value
graph.VariableDefinitions.DefineProperty("effectiveMultiplier",
    new ClampResolver(
        new DivideResolver(
            new VariableResolver("power", typeof(float)),
            new VariableResolver("baseline", typeof(float))),
        new VariantResolver(new Variant128(0.5f), typeof(float)),
        new VariantResolver(new Variant128(2.0f), typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [MinResolver](min-resolver.md)
- [MaxResolver](max-resolver.md)
