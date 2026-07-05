# ElementEntityResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ElementEntityResolver`
> **Output Type:** `IForgeEntity?`

Resolves the `IForgeEntity` array element currently being iterated by an enclosing array resolver. Because it implements `IEntityResolver`, it composes with every entity-aware resolver (`AttributeResolver`, `TagQueryResolver`, etc.), which is what makes per-element predicates and sort keys like "the current entity's health" expressible.

## Constructor

```csharp
new ElementEntityResolver()
```

*(no parameters)*

## Behavior

- Reads the innermost element frame published by an enclosing array resolver on the graph context.
- Returns `null` when evaluated outside an array iteration or when the current element is not an `IForgeEntity`.

## Usage

```csharp
// A per-element sort key: the iterated entity's health
new AttributeResolver("CombatAttributeSet.Health", new ElementEntityResolver())
```

## Composition

```csharp
// Sort entities by health, then keep the three lowest.
new ObjectTakeResolver<IForgeEntity>(
    new ObjectOrderByResolver<IForgeEntity>(
        new EntityArrayVariableResolver("nearbyEntities"),
        new AttributeResolver("CombatAttributeSet.Health", new ElementEntityResolver())),
    new VariantResolver(new Variant128(3), typeof(int)));
```

## See Also

- [Resolvers Overview](README.md)
- [OrderByResolver](order-by-resolver.md)
- [AttributeResolver](attribute-resolver.md)
- [ElementResolver&lt;T&gt;](element-resolver.md)
