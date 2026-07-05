# ElementAtResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ElementAtResolver` (value arrays), `ObjectElementAtResolver<T>` (reference arrays), `EntityElementAtResolver` (entity arrays)
> **Output Type:** *(the source's element type)*

Reads the element at a given index of a nested array resolver. The index is itself a nested numeric resolver, allowing both constant indices and computed ones (a variable, an [ElementIndexResolver](element-index-resolver.md), math, etc.).

## Constructors

```csharp
new ElementAtResolver(source, index)                    // Variant128 arrays
new ObjectElementAtResolver<T>(source, index)           // reference arrays
new EntityElementAtResolver(source, index)              // entity arrays, usable as IEntityResolver
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` / `IObjectArrayResolver<T>` | The resolver providing the source array. |
| index | `IPropertyResolver` | The resolver providing the zero-based element index. Must resolve to a numeric type; fractional values are truncated. |

## Behavior

- Resolves the source array, then the index, and returns the element at that position.
- Out-of-range indices return a default `Variant128` (value lane) or `null` (object lane) — they never throw.
- `EntityElementAtResolver` implements `IEntityResolver`, so it plugs into `AttributeResolver`, `TagQueryResolver`, and other entity-aware resolvers.

## Usage

```csharp
new ElementAtResolver(
    new ArrayVariableResolver("damageTable", typeof(int)),
    new VariableResolver("comboStep", typeof(int)))
```

## Composition

```csharp
// Read the health of the second entity in a stored array
new AttributeResolver(
    "CombatAttributeSet.Health",
    new EntityElementAtResolver(
        new EntityArrayVariableResolver("targets"),
        new VariantResolver(new Variant128(1), typeof(int))));
```

## See Also

- [Resolvers Overview](README.md)
- [FirstResolver](first-resolver.md)
- [LastResolver](last-resolver.md)
