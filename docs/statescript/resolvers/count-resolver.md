# CountResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.CountResolver`
> **Output Type:** `int`

Counts the elements of a nested array resolver, optionally counting only the elements that satisfy a nested boolean predicate resolver, a LINQ `Count`. The source may come from either lane: a value array (`IArrayPropertyResolver`) or a reference array (`IObjectArrayResolver`).

## Constructors

```csharp
new CountResolver(source)               // count everything
new CountResolver(source, predicate)    // count matches only
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` or `IObjectArrayResolver` | The resolver providing the source array (either lane). |
| predicate | `IPropertyResolver` | Optional. Evaluated per element with the element published on the element stack. Must resolve to `bool`. |

## Behavior

- Without a predicate, returns the array length.
- With a predicate, returns the number of elements for which it resolves to `true`.
- Empty or missing sources count as zero.
- Throws `ArgumentException` at construction when the predicate does not resolve to `bool`.

## Usage

```csharp
new CountResolver(new EntityArrayVariableResolver("nearbyEntities"))
```

## Composition

```csharp
// "Are at least two enemies wounded?"
new ComparisonResolver(
    new CountResolver(
        new EntityArrayVariableResolver("nearbyEntities"),
        new ComparisonResolver(
            new AttributeResolver("CombatAttributeSet.Health", new ElementEntityResolver()),
            ComparisonOperation.LessThan,
            new VariantResolver(new Variant128(50), typeof(int)))),
    ComparisonOperation.GreaterThanOrEqual,
    new VariantResolver(new Variant128(2), typeof(int)));
```

## See Also

- [Resolvers Overview](README.md)
- [AnyResolver](any-resolver.md)
- [AllResolver](all-resolver.md)
