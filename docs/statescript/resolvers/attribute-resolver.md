# AttributeResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AttributeResolver`
> **Output Type:** `int`

Reads a selected value from a specific entity attribute. By default it inspects the owner entity, but it can also target any entity provided by an `IEntityResolver`.

## Constructors

```csharp
new AttributeResolver(attributeKey)
new AttributeResolver(attributeKey, attributeCalculationType)
new AttributeResolver(attributeKey, attributeCalculationType, finalChannel)
new AttributeResolver(attributeKey, entityResolver)
new AttributeResolver(attributeKey, entityResolver, attributeCalculationType)
new AttributeResolver(attributeKey, entityResolver, attributeCalculationType, finalChannel)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| attributeKey | `StringKey` | The fully qualified attribute key (e.g., `"CombatAttributeSet.Health"`). |
| entityResolver | `IEntityResolver` | Selects which entity to inspect. Defaults to `OwnerEntityResolver`. |
| attributeCalculationType | `AttributeCalculationType` | Which value to read from the attribute. Defaults to `CurrentValue`. |
| finalChannel | `int` | Only used with `AttributeCalculationType.MagnitudeEvaluatedUpToChannel`. |

## Behavior

- Resolves an entity using the configured `IEntityResolver`.
- Looks up the attribute by key on that entity's `Attributes` collection.
- Returns the selected attribute value as an `int`.
- Returns `0` (default `Variant128`) if the entity cannot be resolved or the attribute doesn't exist.

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

## Dynamic Entity Example

```csharp
graph.VariableDefinitions.DefineReferenceVariable<IForgeEntity>("selectedEntity");

graph.VariableDefinitions.DefineProperty("selectedHealth",
    new AttributeResolver(
        "CombatAttributeSet.Health",
        new EntityVariableResolver("selectedEntity")));
```

```csharp
graph.VariableDefinitions.DefineProperty("targetHealth",
    new AttributeResolver(
        "CombatAttributeSet.Health",
        new TargetEntityResolver()));
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
- [EntityVariableResolver](entity-variable-resolver.md)
- [OwnerEntityResolver](owner-entity-resolver.md)
- [TargetEntityResolver](target-entity-resolver.md)
- [TagQueryResolver](tag-query-resolver.md)
