# AnyResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AnyResolver`
> **Output Type:** `bool`

Checks whether a nested array resolver produces any elements, optionally testing them against a nested boolean predicate resolver, a LINQ `Any`, answering questions like "is any enemy in range?". The source may come from either lane.

## Constructors

```csharp
new AnyResolver(source)               // any element at all?
new AnyResolver(source, predicate)    // any element matching?
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` or `IObjectArrayResolver` | The resolver providing the source array (either lane). |
| predicate | `IPropertyResolver` | Optional. Evaluated per element with the element published on the element stack. Must resolve to `bool`. |

## Behavior

- Without a predicate, returns `true` when the array is not empty.
- With a predicate, returns `true` at the first element for which it resolves to `true` (evaluation stops there).
- Empty or missing sources resolve to `false`.

## Usage

```csharp
new AnyResolver(new EntityArrayVariableResolver("nearbyEntities"))
```

## Composition

```csharp
// "Is any target below 25% health?"
new AnyResolver(
    new EntityArrayVariableResolver("targets"),
    new ComparisonResolver(
        new AttributeResolver("CombatAttributeSet.Health", new ElementEntityResolver()),
        ComparisonOperation.LessThan,
        new VariantResolver(new Variant128(25), typeof(int))));
```

## See Also

- [Resolvers Overview](README.md)
- [AllResolver](all-resolver.md)
- [CountResolver](count-resolver.md)
