# ComparisonResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ComparisonResolver`
> **Output Type:** `bool`

Compares two values using a `ComparisonOperation` and returns a `bool`. Both operands are resolved through their own `IPropertyResolver` instances and converted to `double` for comparison, allowing any numeric property (int attributes, float variables, etc.) to be compared directly.

## Constructor

```csharp
new ComparisonResolver(left, operation, right)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| left | `IPropertyResolver` | The resolver for the left operand. |
| operation | `ComparisonOperation` | The comparison to apply. |
| right | `IPropertyResolver` | The resolver for the right operand. |

**Supported operations:** `Equal`, `NotEqual`, `LessThan`, `LessThanOrEqual`, `GreaterThan`, `GreaterThanOrEqual`.

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Converts both values to `double` based on each resolver's `ValueType`.
- Applies the specified comparison operation.
- Returns the boolean result.
- Supports all numeric `Variant128` types as operands (`int`, `float`, `double`, `long`, `short`, `byte`, `uint`, `ulong`, `ushort`, `sbyte`, `decimal`).

## Usage

```csharp
// "Is health greater than 50?"
graph.VariableDefinitions.DefineProperty("healthAbove50",
    new ComparisonResolver(
        new AttributeResolver("CombatAttributeSet.Health"),
        ComparisonOperation.GreaterThan,
        new VariantResolver(new Variant128(50), typeof(int))));
```

## Composition

Operands can be any `IPropertyResolver` implementation, including other `ComparisonResolver` instances or math resolvers, enabling arbitrarily complex expressions.

```csharp
// Compare a computed sum against a threshold
graph.VariableDefinitions.DefineProperty("combinedDamageAbove100",
    new ComparisonResolver(
        new AddResolver(
            new VariableResolver("baseDamage", typeof(int)),
            new VariableResolver("bonusDamage", typeof(int))),
        ComparisonOperation.GreaterThan,
        new VariantResolver(new Variant128(100), typeof(int))));
```

## See Also

- [Resolvers Overview](README.md)
- [AttributeResolver](attribute-resolver.md)
- [ExpressionNode](../nodes/condition/expression-node.md)
