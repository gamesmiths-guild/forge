# AttributeResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AttributeResolver`
> **Output Type:** `int`

Reads the `CurrentValue` of a specific entity attribute. Requires the graph to be driven by an ability (accesses the owner entity from `AbilityBehaviorContext` stored in `GraphContext.ActivationContext`).

## Constructor

```csharp
new AttributeResolver(attributeKey)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| attributeKey | `StringKey` | The fully qualified attribute key (e.g., `"CombatAttributeSet.Health"`). |

## Behavior

- Retrieves the owner entity from the `AbilityBehaviorContext` in the graph's activation context.
- Looks up the attribute by key on the owner entity's `Attributes` collection.
- Returns the attribute's `CurrentValue` as an `int`.
- Returns `0` (default `Variant128`) if no activation context is available or the attribute doesn't exist.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("ownerHealth",
    new AttributeResolver("CombatAttributeSet.Health"));
```

## Composition

```csharp
// Use with ComparisonResolver for branching
graph.VariableDefinitions.DefineProperty("healthAbove50",
    new ComparisonResolver(
        new AttributeResolver("CombatAttributeSet.Health"),
        ComparisonOperation.GreaterThan,
        new VariantResolver(new Variant128(50), typeof(int))));
```

## See Also

- [Resolvers Overview](README.md)
- [ComparisonResolver](comparison-resolver.md)
- [TagResolver](tag-resolver.md)
