# LastResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.LastResolver` (value arrays), `ObjectLastResolver<T>` (reference arrays), `EntityLastResolver` (entity arrays)
> **Output Type:** *(the source's element type)*

Reads the last element of a nested array resolver.

## Constructors

```csharp
new LastResolver(source)               // Variant128 arrays
new ObjectLastResolver<T>(source)      // reference arrays
new EntityLastResolver(source)         // entity arrays, usable as IEntityResolver
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` / `IObjectArrayResolver<T>` | The resolver providing the source array. |

## Behavior

- Resolves the source array and returns its last element.
- Empty arrays return a default `Variant128` (value lane) or `null` (object lane).
- `EntityLastResolver` implements `IEntityResolver`, so it plugs into entity-aware resolvers.

## Usage

```csharp
new LastResolver(new ArrayVariableResolver("comboDamage", typeof(int)))
```

## Composition

```csharp
// The farthest entity: nearby.OrderBy(distance).Last()
new EntityLastResolver(
    new ObjectOrderByResolver<IForgeEntity>(
        new EntityArrayVariableResolver("nearbyEntities"),
        new AttributeResolver("MovementAttributeSet.DistanceToOwner", new ElementEntityResolver())));
```

## See Also

- [Resolvers Overview](README.md)
- [FirstResolver](first-resolver.md)
- [ElementAtResolver](element-at-resolver.md)
