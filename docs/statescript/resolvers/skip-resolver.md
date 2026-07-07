# SkipResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.SkipResolver` (value arrays), `ObjectSkipResolver<T>` (reference arrays)
> **Output Type:** *(array of the source's element type)*

Drops the first N elements of a nested array resolver, a LINQ `Skip`. The count is itself a nested numeric resolver, allowing both constant and computed counts.

## Constructors

```csharp
new SkipResolver(source, count)              // Variant128 arrays
new ObjectSkipResolver<T>(source, count)     // reference arrays
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` / `IObjectArrayResolver<T>` | The resolver providing the source array. |
| count | `IPropertyResolver` | The resolver providing the number of elements to skip. Must resolve to a numeric type; fractional values are truncated. |

## Behavior

- Returns the source elements after the first `count`.
- Counts are clamped to the source length; negative counts skip nothing.

## Usage

```csharp
new SkipResolver(
    new ArrayVariableResolver("queue", typeof(int)),
    new VariantResolver(new Variant128(1), typeof(int)))
```

## Composition

```csharp
// Everything except the closest entity (e.g. chain lightning bounces)
new ObjectSkipResolver<IForgeEntity>(
    new ObjectOrderByResolver<IForgeEntity>(
        new EntityArrayVariableResolver("nearbyEntities"),
        new AttributeResolver("MovementAttributeSet.DistanceToOwner", new ElementEntityResolver())),
    new VariantResolver(new Variant128(1), typeof(int)));
```

## See Also

- [Resolvers Overview](README.md)
- [TakeResolver](take-resolver.md)
- [OrderByResolver](order-by-resolver.md)
