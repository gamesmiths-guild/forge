# MultiplyResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.MultiplyResolver`
> **Output Type:** *(promoted from operand types)*

Multiplies two operands. Supports all numeric types in `Variant128` as well as `Vector2`, `Vector3`, `Vector4` (component-wise multiplication), and `Quaternion` (quaternion multiplication). When the two operands have different numeric types, standard numeric promotion rules apply.

## Constructor

```csharp
new MultiplyResolver(left, right)
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
| `Vector2` | `Vector2` |
| `Vector3` | `Vector3` |
| `Vector4` | `Vector4` |
| `Quaternion` | `Quaternion` |

When operands differ, the wider numeric type wins:

| Mixed Operands | Result Type |
|----------------|-------------|
| `int * float` | `float` |
| `int * double` | `double` |
| `float * double` | `double` |
| `byte * int` | `int` |
| `short * long` | `long` |
| `int * decimal` | `decimal` |

**Invalid combinations** (throw `ArgumentException` at construction time):
- Mixing vector/quaternion types (e.g., `Vector2 * Vector3`).
- Mixing vector/quaternion with numeric types (e.g., `Vector3 * float`).
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Converts each operand to the promoted result type.
- Performs the multiplication and returns the result as a `Variant128`.
- For `Vector2`, `Vector3`, and `Vector4`, multiplication is **component-wise**.
- For `Quaternion`, multiplication follows standard quaternion multiplication rules (non-commutative).
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("scaledDamage",
    new MultiplyResolver(
        new VariableResolver("baseDamage", typeof(int)),
        new VariableResolver("multiplier", typeof(int))));

graph.VariableDefinitions.DefineProperty("scaledSize",
    new MultiplyResolver(
        new VariableResolver("baseSize", typeof(Vector3)),
        new VariableResolver("scaleFactors", typeof(Vector3))));
```

## Composition

```csharp
// Final damage = (base + bonus) * multiplier
graph.VariableDefinitions.DefineProperty("finalDamage",
    new MultiplyResolver(
        new AddResolver(
            new VariableResolver("baseDamage", typeof(int)),
            new VariableResolver("bonusDamage", typeof(int))),
        new VariableResolver("damageMultiplier", typeof(int))));
```

## See Also

- [Resolvers Overview](README.md)
- [DivideResolver](divide-resolver.md)
- [AddResolver](add-resolver.md)
