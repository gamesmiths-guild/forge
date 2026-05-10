# AttributeResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AttributeResolver`
> **Output Type:** `int`

Reads a selected value from a specific entity attribute. Requires the graph to be driven by an ability (accesses the owner entity from `AbilityBehaviorContext` stored in `GraphContext.ActivationContext`).

## Constructors

```csharp
new AttributeResolver(attributeKey)
new AttributeResolver(attributeKey, attributeCalculationType)
new AttributeResolver(attributeKey, attributeCalculationType, finalChannel)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| attributeKey | `StringKey` | The fully qualified attribute key (e.g., `"CombatAttributeSet.Health"`). |
| attributeCalculationType | `AttributeCalculationType` | Which value to read from the attribute. Defaults to `CurrentValue`. |
| finalChannel | `int` | Only used with `AttributeCalculationType.MagnitudeEvaluatedUpToChannel`. |

## Behavior

- Retrieves the owner entity from the `AbilityBehaviorContext` in the graph's activation context.
- Looks up the attribute by key on the owner entity's `Attributes` collection.
- Returns the selected attribute value as an `int`.
- Returns `0` (default `Variant128`) if no activation context is available or the attribute doesn't exist.

### Supported `AttributeCalculationType` values

- `CurrentValue`
- `BaseValue`
- `Modifier`
- `Overflow`
- `ValidModifier`
- `Min`
- `Max`
- `MagnitudeEvaluatedUpToChannel` (requires `finalChannel`)

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("ownerHealth",
    new AttributeResolver("CombatAttributeSet.Health"));
```

```csharp
graph.VariableDefinitions.DefineProperty("healthOverflow",
    new AttributeResolver(
        "CombatAttributeSet.Health",
        AttributeCalculationType.Overflow));
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
- [TagQueryResolver](tag-query-resolver.md)
