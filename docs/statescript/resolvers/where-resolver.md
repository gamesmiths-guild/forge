# WhereResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.WhereResolver` (value arrays), `ObjectWhereResolver<T>` (reference arrays)
> **Output Type:** *(array of the source's element type)*

Filters a nested array resolver by a nested boolean predicate resolver, preserving element order, a LINQ `Where` for statescript graphs. The predicate is evaluated once per element with the current element published on the element stack, so it reads the element through [ElementValueResolver](element-value-resolver.md) (value arrays) or [ElementResolver&lt;T&gt;](element-resolver.md)/[ElementEntityResolver](element-entity-resolver.md) (reference arrays).

## Constructors

```csharp
new WhereResolver(source, predicate)              // Variant128 arrays
new ObjectWhereResolver<T>(source, predicate)     // reference arrays
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` / `IObjectArrayResolver<T>` | The resolver providing the source array. |
| predicate | `IPropertyResolver` | The resolver evaluated per element. Must resolve to `bool`. |

## Behavior

- Evaluates the predicate for each element and keeps the elements that resolve to `true`, in their original order.
- To remove matching elements instead, wrap the predicate in a `NotResolver`.
- Empty or missing sources produce an empty array.
- Throws `ArgumentException` at construction when the predicate does not resolve to `bool`.

## Usage

```csharp
// numbers.Where(x => x > 2)
new WhereResolver(
    new ArrayVariableResolver("numbers", typeof(int)),
    new ComparisonResolver(
        new ElementValueResolver(typeof(int)),
        ComparisonOperation.GreaterThan,
        new VariantResolver(new Variant128(2), typeof(int))));
```

## Composition

```csharp
// Keep entities with health above 30, using a per-element attribute read
new ObjectWhereResolver<IForgeEntity>(
    new EntityArrayVariableResolver("targets"),
    new ComparisonResolver(
        new AttributeResolver("CombatAttributeSet.Health", new ElementEntityResolver()),
        ComparisonOperation.GreaterThan,
        new VariantResolver(new Variant128(30), typeof(int))));
```

## See Also

- [Resolvers Overview](README.md)
- [OrderByResolver](order-by-resolver.md)
- [CountResolver](count-resolver.md)
- [NotResolver](not-resolver.md)
