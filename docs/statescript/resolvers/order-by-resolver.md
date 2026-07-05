# OrderByResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.OrderByResolver` (value arrays), `ObjectOrderByResolver<T>` (reference arrays)
> **Output Type:** *(array of the source's element type)*

Sorts a nested array resolver by a nested numeric key selector resolver, a LINQ `OrderBy` for statescript graphs. The key selector is evaluated once per element with the current element published on the element stack, so it reads the element through [ElementValueResolver](element-value-resolver.md) (value arrays) or [ElementEntityResolver](element-entity-resolver.md) (entity arrays, composing with `AttributeResolver`, `DistanceResolver`, etc. for the key).

## Constructors

```csharp
new OrderByResolver(source, keySelector, direction)              // Variant128 arrays
new ObjectOrderByResolver<T>(source, keySelector, direction)     // reference arrays
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` / `IObjectArrayResolver<T>` | The resolver providing the source array. |
| keySelector | `IPropertyResolver` | The resolver evaluated per element to produce its sort key. Must resolve to a numeric type. |
| direction | `SortDirection` | Optional. `Ascending` (default) or `Descending`. |

## Behavior

- Computes one numeric key per element, then returns the elements sorted by key.
- The sort is **stable**: elements with equal keys keep their original relative order.
- Empty or missing sources produce an empty array.
- Throws `ArgumentException` at construction when the key selector does not resolve to a numeric type.

## Usage

```csharp
// numbers.OrderByDescending(x => x)
new OrderByResolver(
    new ArrayVariableResolver("numbers", typeof(int)),
    new ElementValueResolver(typeof(int)),
    SortDirection.Descending);
```

## Composition

```csharp
// The motivating skill example: keep the three closest entities.
new ObjectTakeResolver<IForgeEntity>(
    new ObjectOrderByResolver<IForgeEntity>(
        new EntityArrayVariableResolver("nearbyEntities"),
        new AttributeResolver("MovementAttributeSet.DistanceToOwner", new ElementEntityResolver())),
    new VariantResolver(new Variant128(3), typeof(int)));
```

## See Also

- [Resolvers Overview](README.md)
- [TakeResolver](take-resolver.md)
- [WhereResolver](where-resolver.md)
- [MinElementResolver](min-element-resolver.md)
