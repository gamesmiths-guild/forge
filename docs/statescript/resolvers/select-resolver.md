# SelectResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.SelectResolver` (projects to values), `SelectObjectResolver<TResult>` (projects to references)
> **Output Type:** *(array of the projection's value type)*

Projects each element of a nested source array through a nested projection resolver, a LINQ `Select`. The projection is evaluated once per element with the current element published on the element stack, so it reads the element through the element resolvers. The **source may come from either lane**, a value array (`IArrayPropertyResolver`) or a reference array (`IObjectArrayResolver`), enabling projections such as "the health of each entity in the array".

## Constructors

```csharp
new SelectResolver(source, projection)                  // → Variant128 array
new SelectObjectResolver<TResult>(source, projection)   // → TResult array
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` or `IObjectArrayResolver` | The resolver providing the source array (either lane). |
| projection | `IPropertyResolver` (`SelectResolver`) / `IObjectResolver<TResult>` (`SelectObjectResolver`) | The resolver evaluated per element to produce the projected value. |

## Behavior

- Evaluates the projection for each source element and returns the projected array, same length and order.
- The resulting element type is the projection's value type.
- Empty or missing sources produce an empty array.

## Usage

```csharp
// numbers.Select(x => x * 2)
new SelectResolver(
    new ArrayVariableResolver("numbers", typeof(int)),
    new MultiplyResolver(
        new ElementValueResolver(typeof(int)),
        new VariantResolver(new Variant128(2), typeof(int))));
```

## Composition

```csharp
// entities.Select(e => e.Health) — object lane in, value lane out
new SelectResolver(
    new EntityArrayVariableResolver("targets"),
    new AttributeResolver("CombatAttributeSet.Health", new ElementEntityResolver()));

// Sum the health of all targets
new SumResolver(
    new SelectResolver(
        new EntityArrayVariableResolver("targets"),
        new AttributeResolver("CombatAttributeSet.Health", new ElementEntityResolver())));
```

## See Also

- [Resolvers Overview](README.md)
- [WhereResolver](where-resolver.md)
- [SumResolver](sum-resolver.md)
- [ElementValueResolver](element-value-resolver.md)
