# AddResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AddResolver`
> **Output Type:** *(promoted from operand types)*

Adds two numeric or vector values. Supports all numeric types in `Variant128` as well as `Vector2`, `Vector3`, `Vector4`, and `Quaternion`. When the two operands have different numeric types, standard numeric promotion rules apply.

## Constructor

```csharp
new AddResolver(left, right)
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
| `int + float` | `float` |
| `int + double` | `double` |
| `float + double` | `double` |
| `byte + int` | `int` |
| `short + long` | `long` |
| `int + decimal` | `decimal` |

**Invalid combinations** (throw `ArgumentException` at construction time):
- Mixing vector/quaternion types (e.g., `Vector2 + Vector3`).
- Mixing vector/quaternion with numeric types (e.g., `Vector3 + float`).
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Converts each operand to the promoted result type.
- Performs the addition and returns the result as a `Variant128`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Add two integer variables
graph.VariableDefinitions.DefineProperty("totalDamage",
    new AddResolver(
        new VariableResolver("baseDamage", typeof(int)),
        new VariableResolver("bonusDamage", typeof(int))));

// Add two Vector3 positions
graph.VariableDefinitions.DefineProperty("offsetPosition",
    new AddResolver(
        new VariableResolver("position", typeof(Vector3)),
        new VariableResolver("offset", typeof(Vector3))));
```

## Composition

AddResolver is nestable and composes with all other resolvers:

```csharp
// Chain: (baseDamage + bonusDamage) + critBonus
graph.VariableDefinitions.DefineProperty("finalDamage",
    new AddResolver(
        new AddResolver(
            new VariableResolver("baseDamage", typeof(int)),
            new VariableResolver("bonusDamage", typeof(int))),
        new VariableResolver("critBonus", typeof(int))));

// Compose with ComparisonResolver: (a + b) > threshold
graph.VariableDefinitions.DefineProperty("isOverThreshold",
    new ComparisonResolver(
        new AddResolver(
            new VariableResolver("valueA", typeof(int)),
            new VariableResolver("valueB", typeof(int))),
        ComparisonOperation.GreaterThan,
        new VariantResolver(new Variant128(100), typeof(int))));
```

## See Also

- [Resolvers Overview](README.md)
- [ComparisonResolver](comparison-resolver.md)
- [VariableResolver](variable-resolver.md)
- [VariantResolver](variant-resolver.md)
