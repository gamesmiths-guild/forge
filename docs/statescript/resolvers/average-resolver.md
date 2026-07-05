# AverageResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AverageResolver`
> **Output Type:** `double` (`float` for float elements, `decimal` for decimal elements)

Computes the arithmetic mean of all elements of a nested numeric array resolver, a LINQ `Average`.

## Constructor

```csharp
new AverageResolver(source)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` | The resolver providing the source array. Must have a numeric element type. |

## Behavior

- Returns the mean of all elements; an empty array averages to zero (unlike LINQ, it never throws).
- `float` elements average to `float`, `decimal` elements to `decimal`, all other numeric element types to `double`.
- Throws `ArgumentException` at construction for non-numeric element types.

## Usage

```csharp
new AverageResolver(new ArrayVariableResolver("recentDamage", typeof(int)))
```

## Composition

```csharp
// "Is the group's average health below half?"
new ComparisonResolver(
    new AverageResolver(
        new SelectResolver(
            new EntityArrayVariableResolver("party"),
            new AttributeResolver("CombatAttributeSet.Health", new ElementEntityResolver()))),
    ComparisonOperation.LessThan,
    new VariantResolver(new Variant128(50d), typeof(double)));
```

## See Also

- [Resolvers Overview](README.md)
- [SumResolver](sum-resolver.md)
- [MinElementResolver](min-element-resolver.md)
