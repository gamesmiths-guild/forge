# MinResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.MinResolver`
> **Output Type:** *(promoted from operand types, or same vector type)*

Returns the smaller of two operands. Supports all numeric types in `Variant128` as well as `Vector2`, `Vector3`, and `Vector4`. Quaternion types are not supported. When the two numeric operands have different types, standard numeric promotion rules apply. For vector types, the minimum is computed component-wise.

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

### Vector Types

| Operand Types | Result Type |
|---------------|-------------|
| `Vector2`, `Vector2` | `Vector2` |
| `Vector3`, `Vector3` | `Vector3` |
| `Vector4`, `Vector4` | `Vector4` |

**Invalid types** (throw `ArgumentException` at construction time):
- `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Converts each operand to the promoted result type.
- Returns the smaller value as a `Variant128` using `Math.Min` for numeric types, or `Vector2.Min`, `Vector3.Min`, or `Vector4.Min` for vector types.
- For vector types, the minimum is selected component-wise.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Cap a stat to its maximum allowed value
graph.VariableDefinitions.DefineProperty("effectiveArmor",
    new MinResolver(
        new VariableResolver("armor", typeof(int)),
        new VariableResolver("armorCap", typeof(int))));

// Compute the component-wise minimum of two bounds
graph.VariableDefinitions.DefineProperty("minBounds",
    new MinResolver(
        new VariableResolver("boundsA", typeof(Vector3)),
        new VariableResolver("boundsB", typeof(Vector3))));
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
- [AbsResolver](abs-resolver.md)
