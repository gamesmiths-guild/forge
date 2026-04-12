# VariantResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.VariantResolver`
> **Output Type:** *(configured at construction time)*

Holds a fixed constant value. Useful as operands in other resolvers (e.g., the right-hand side of a `ComparisonResolver`) or as default values for node inputs.

## Constructor

```csharp
new VariantResolver(initialValue, valueType)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| initialValue | `Variant128` | The constant value to hold. |
| valueType | `Type` | The type this resolver produces (e.g., `typeof(int)`, `typeof(float)`). |

## Behavior

- Returns the stored `Variant128` value on every resolve call, regardless of the graph context.
- The value can be updated at runtime via the `Set<T>()` method or by directly setting the `Value` property.

## Usage

```csharp
new VariantResolver(new Variant128(50), typeof(int))       // Constant 50
new VariantResolver(new Variant128(3.14f), typeof(float))   // Constant 3.14
new VariantResolver(new Variant128(true), typeof(bool))     // Constant true
```

## Composition

```csharp
// Use as a constant operand in a comparison
graph.VariableDefinitions.DefineProperty("isOverThreshold",
    new ComparisonResolver(
        new AttributeResolver("CombatAttributeSet.Health"),
        ComparisonOperation.GreaterThan,
        new VariantResolver(new Variant128(50), typeof(int))));
```

## See Also

- [Resolvers Overview](README.md)
- [VariableResolver](variable-resolver.md)
- [ComparisonResolver](comparison-resolver.md)
