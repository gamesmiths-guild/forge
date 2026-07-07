# AllResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AllResolver`
> **Output Type:** `bool`

Checks whether every element of a nested array resolver satisfies a nested boolean predicate resolver, a LINQ `All`, answering questions like "are all targets dead?". The source may come from either lane.

## Constructor

```csharp
new AllResolver(source, predicate)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` or `IObjectArrayResolver` | The resolver providing the source array (either lane). |
| predicate | `IPropertyResolver` | Evaluated per element with the element published on the element stack. Must resolve to `bool`. |

## Behavior

- Returns `false` at the first element for which the predicate resolves to `false` (evaluation stops there).
- Empty or missing sources resolve to `true` (vacuous truth, matching LINQ).
- Throws `ArgumentException` at construction when the predicate does not resolve to `bool`.

## Usage

```csharp
new AllResolver(
    new ArrayVariableResolver("charges", typeof(int)),
    new ComparisonResolver(
        new ElementValueResolver(typeof(int)),
        ComparisonOperation.GreaterThan,
        new VariantResolver(new Variant128(0), typeof(int))))
```

## Composition

```csharp
// "Have all marked targets been reduced to zero health?"
new AllResolver(
    new EntityArrayVariableResolver("markedTargets"),
    new ComparisonResolver(
        new AttributeResolver("CombatAttributeSet.Health", new ElementEntityResolver()),
        ComparisonOperation.LessThanOrEqual,
        new VariantResolver(new Variant128(0), typeof(int))));
```

## See Also

- [Resolvers Overview](README.md)
- [AnyResolver](any-resolver.md)
- [CountResolver](count-resolver.md)
