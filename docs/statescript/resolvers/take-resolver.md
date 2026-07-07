# TakeResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.TakeResolver` (value arrays), `ObjectTakeResolver<T>` (reference arrays)
> **Output Type:** *(array of the source's element type)*

Keeps the first N elements of a nested array resolver, a LINQ `Take`. The count is itself a nested numeric resolver, allowing both constant and computed counts.

## Constructors

```csharp
new TakeResolver(source, count)              // Variant128 arrays
new ObjectTakeResolver<T>(source, count)     // reference arrays
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` / `IObjectArrayResolver<T>` | The resolver providing the source array. |
| count | `IPropertyResolver` | The resolver providing the number of elements to keep. Must resolve to a numeric type; fractional values are truncated. |

## Behavior

- Returns the first `count` elements of the source array.
- Counts are clamped to the source length; negative counts produce an empty array.

## Usage

```csharp
new TakeResolver(
    new ArrayVariableResolver("damageRolls", typeof(int)),
    new VariantResolver(new Variant128(3), typeof(int)))
```

## Composition

```csharp
// Sort by distance, keep the three closest
new ObjectTakeResolver<IForgeEntity>(
    new ObjectOrderByResolver<IForgeEntity>(
        new EntityArrayVariableResolver("nearbyEntities"),
        new AttributeResolver("MovementAttributeSet.DistanceToOwner", new ElementEntityResolver())),
    new VariantResolver(new Variant128(3), typeof(int)));
```

## See Also

- [Resolvers Overview](README.md)
- [SkipResolver](skip-resolver.md)
- [OrderByResolver](order-by-resolver.md)
