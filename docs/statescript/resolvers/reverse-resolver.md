# ReverseResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ReverseResolver` (value arrays), `ObjectReverseResolver<T>` (reference arrays)
> **Output Type:** *(array of the source's element type)*

Reverses the element order of a nested array resolver.

## Constructors

```csharp
new ReverseResolver(source)              // Variant128 arrays
new ObjectReverseResolver<T>(source)     // reference arrays
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` / `IObjectArrayResolver<T>` | The resolver providing the source array. |

## Behavior

- Returns the source elements in reverse order.

## Usage

```csharp
new ReverseResolver(new ArrayVariableResolver("queue", typeof(int)))
```

## Composition

```csharp
// Farthest-first ordering without a descending sort
new ObjectReverseResolver<IForgeEntity>(
    new ObjectOrderByResolver<IForgeEntity>(
        new EntityArrayVariableResolver("nearbyEntities"),
        new AttributeResolver("MovementAttributeSet.DistanceToOwner", new ElementEntityResolver())));
```

## See Also

- [Resolvers Overview](README.md)
- [OrderByResolver](order-by-resolver.md)
