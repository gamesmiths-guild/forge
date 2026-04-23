# MaxResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.MaxResolver`
> **Output Type:** *(promoted from operand types, or same vector type)*

Returns the larger of two operands. Supports all numeric types in `Variant128` as well as `Vector2`, `Vector3`, and `Vector4`. Quaternion types are not supported. When the two numeric operands have different types, standard numeric promotion rules apply. For vector types, the maximum is computed component-wise.

## Constructor

```csharp
new MaxResolver(left, right)
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
- Returns the larger value as a `Variant128` using `Math.Max` for numeric types, or `Vector2.Max`, `Vector3.Max`, or `Vector4.Max` for vector types.
- For vector types, the maximum is selected component-wise.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Ensure a stat never goes below a minimum floor
graph.VariableDefinitions.DefineProperty("effectiveStat",
    new MaxResolver(
        new AddResolver(
            new VariableResolver("baseStat", typeof(int)),
            new VariableResolver("modifier", typeof(int))),
        new VariantResolver(new Variant128(1), typeof(int))));

// Compute the component-wise maximum of two extents
graph.VariableDefinitions.DefineProperty("maxExtents",
    new MaxResolver(
        new VariableResolver("extentsA", typeof(Vector3)),
        new VariableResolver("extentsB", typeof(Vector3))));
```

## Composition

```csharp
// Non-negative damage after armor reduction
graph.VariableDefinitions.DefineProperty("effectiveDamage",
    new MaxResolver(
        new SubtractResolver(
            new VariableResolver("rawDamage", typeof(int)),
            new VariableResolver("armor", typeof(int))),
        new VariantResolver(new Variant128(0), typeof(int))));
```

## See Also

- [Resolvers Overview](README.md)
- [MinResolver](min-resolver.md)
- [ClampResolver](clamp-resolver.md)
- [AbsResolver](abs-resolver.md)
