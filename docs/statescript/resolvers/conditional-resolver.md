# ConditionalResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ConditionalResolver`
> **Output Type:** *(matches the branches)*

Selects one of two value-lane values based on a boolean condition — the ternary select (`condition ? a : b`). Only the selected branch is evaluated.

## Constructor

```csharp
new ConditionalResolver(condition, whenTrue, whenFalse)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| condition | `IPropertyResolver` | The boolean condition. |
| whenTrue | `IPropertyResolver` | Evaluated when the condition is `true`. |
| whenFalse | `IPropertyResolver` | Evaluated when the condition is `false`. |

## Behavior

- Both branches must produce the same value type (the resolver's `ValueType`); otherwise the constructor throws.
- Evaluates the condition, then resolves and returns only the selected branch.

## Usage

```csharp
// Pick a damage value based on whether the owner is enraged
graph.VariableDefinitions.DefineProperty("damage",
    new ConditionalResolver(
        new TagQueryResolver(enragedQuery),
        new VariantResolver(new Variant128(20), typeof(int)),
        new VariantResolver(new Variant128(10), typeof(int))));
```

## Composition

```csharp
// Nest conditionals or feed the result into other math resolvers
graph.VariableDefinitions.DefineProperty("scaledDamage",
    new MultiplyResolver(
        new ConditionalResolver(
            new TagQueryResolver(critQuery),
            new VariantResolver(new Variant128(2f), typeof(float)),
            new VariantResolver(new Variant128(1f), typeof(float))),
        new AttributeResolver("CombatAttributeSet.Power")));
```

## See Also

- [Resolvers Overview](README.md)
- [ConditionalObjectResolver](conditional-object-resolver.md)
- [ComparisonResolver](comparison-resolver.md)
