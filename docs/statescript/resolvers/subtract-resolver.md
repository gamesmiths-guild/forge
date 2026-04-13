# SubtractResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.SubtractResolver`
> **Output Type:** *(promoted from operand types)*

Subtracts the right operand from the left operand. Supports all numeric types in `Variant128` as well as `Vector2`, `Vector3`, `Vector4`, and `Quaternion`. When the two operands have different numeric types, standard numeric promotion rules apply.

## Constructor

```csharp
new SubtractResolver(left, right)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| left | `IPropertyResolver` | The resolver for the left (minuend) operand. |
| right | `IPropertyResolver` | The resolver for the right (subtrahend) operand. |

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
| `int - float` | `float` |
| `int - double` | `double` |
| `float - double` | `double` |
| `byte - int` | `int` |
| `short - long` | `long` |
| `int - decimal` | `decimal` |

**Invalid combinations** (throw `ArgumentException` at construction time):
- Mixing vector/quaternion types (e.g., `Vector2 - Vector3`).
- Mixing vector/quaternion with numeric types (e.g., `Vector3 - float`).
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Converts each operand to the promoted result type.
- Performs the subtraction (`left - right`) and returns the result as a `Variant128`.
- For vectors, subtraction is component-wise.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("remainingHealth",
    new SubtractResolver(
        new VariableResolver("maxHealth", typeof(int)),
        new VariableResolver("damageTaken", typeof(int))));

graph.VariableDefinitions.DefineProperty("displacement",
    new SubtractResolver(
        new VariableResolver("currentPosition", typeof(Vector3)),
        new VariableResolver("startPosition", typeof(Vector3))));
```

## Composition

```csharp
// Compute remaining health as a percentage: (max - current) / max
graph.VariableDefinitions.DefineProperty("healthLostFraction",
    new DivideResolver(
        new SubtractResolver(
            new VariableResolver("maxHealth", typeof(float)),
            new VariableResolver("currentHealth", typeof(float))),
        new VariableResolver("maxHealth", typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [AddResolver](add-resolver.md)
- [NegateResolver](negate-resolver.md)
