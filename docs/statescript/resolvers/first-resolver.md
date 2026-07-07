# FirstResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.FirstResolver` (value arrays), `ObjectFirstResolver<T>` (reference arrays), `EntityFirstResolver` (entity arrays)
> **Output Type:** *(the source's element type)*

Reads the first element of a nested array resolver. Combined with [OrderByResolver](order-by-resolver.md), this expresses "the best element by some key", e.g. the closest enemy.

## Constructors

```csharp
new FirstResolver(source)               // Variant128 arrays
new ObjectFirstResolver<T>(source)      // reference arrays
new EntityFirstResolver(source)         // entity arrays, usable as IEntityResolver
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` / `IObjectArrayResolver<T>` | The resolver providing the source array. |

## Behavior

- Resolves the source array and returns its first element.
- Empty arrays return a default `Variant128` (value lane) or `null` (object lane).
- `EntityFirstResolver` implements `IEntityResolver`, so it plugs into entity-aware resolvers.

## Usage

```csharp
new FirstResolver(new ArrayVariableResolver("damageRolls", typeof(int)))
```

## Composition

```csharp
// The single closest entity: nearby.OrderBy(distance).First()
new EntityFirstResolver(
    new ObjectOrderByResolver<IForgeEntity>(
        new EntityArrayVariableResolver("nearbyEntities"),
        new AttributeResolver("MovementAttributeSet.DistanceToOwner", new ElementEntityResolver())));
```

## See Also

- [Resolvers Overview](README.md)
- [LastResolver](last-resolver.md)
- [ElementAtResolver](element-at-resolver.md)
- [OrderByResolver](order-by-resolver.md)
