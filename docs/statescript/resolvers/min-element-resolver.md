# MinElementResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.MinElementResolver`
> **Output Type:** *(the source's element type)*

Resolves the smallest element of a nested numeric array resolver. Unlike the binary [MinResolver](min-resolver.md), which compares two operands, this resolver aggregates over an array.

## Constructor

```csharp
new MinElementResolver(source)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` | The resolver providing the source array. Must have a numeric element type. |

## Behavior

- Returns the smallest element unchanged, so the result type matches the source element type.
- Empty arrays return a default `Variant128` (zero); ties resolve to the first occurrence.
- Throws `ArgumentException` at construction for non-numeric element types.

## Usage

```csharp
new MinElementResolver(new ArrayVariableResolver("cooldowns", typeof(float)))
```

## Composition

```csharp
// The lowest health among all targets
new MinElementResolver(
    new SelectResolver(
        new EntityArrayVariableResolver("targets"),
        new AttributeResolver("CombatAttributeSet.Health", new ElementEntityResolver())));
```

## See Also

- [Resolvers Overview](README.md)
- [MaxElementResolver](max-element-resolver.md)
- [MinResolver](min-resolver.md)
- [OrderByResolver](order-by-resolver.md)
