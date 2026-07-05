# SumResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.SumResolver`
> **Output Type:** *(the source element type, promoted)*

Adds up all elements of a nested numeric array resolver, a LINQ `Sum`. The result type follows the standard numeric promotion rules (`int` elements sum to `int`, `float` to `float`, `uint` to `long`, etc.).

## Constructor

```csharp
new SumResolver(source)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` | The resolver providing the source array. Must have a numeric element type. |

## Behavior

- Returns the sum of all elements; an empty array sums to zero.
- Throws `ArgumentException` at construction for non-numeric element types.

## Usage

```csharp
new SumResolver(new ArrayVariableResolver("damageRolls", typeof(int)))
```

## Composition

```csharp
// Total health across all targets: targets.Select(e => e.Health).Sum()
new SumResolver(
    new SelectResolver(
        new EntityArrayVariableResolver("targets"),
        new AttributeResolver("CombatAttributeSet.Health", new ElementEntityResolver())));
```

## See Also

- [Resolvers Overview](README.md)
- [AverageResolver](average-resolver.md)
- [SelectResolver](select-resolver.md)
