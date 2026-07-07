# MaxElementResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.MaxElementResolver`
> **Output Type:** *(the source's element type)*

Resolves the largest element of a nested numeric array resolver. Unlike the binary [MaxResolver](max-resolver.md), which compares two operands, this resolver aggregates over an array.

## Constructor

```csharp
new MaxElementResolver(source)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` | The resolver providing the source array. Must have a numeric element type. |

## Behavior

- Returns the largest element unchanged, so the result type matches the source element type.
- Empty arrays return a default `Variant128` (zero); ties resolve to the first occurrence.
- Throws `ArgumentException` at construction for non-numeric element types.

## Usage

```csharp
new MaxElementResolver(new ArrayVariableResolver("damageRolls", typeof(int)))
```

## Composition

```csharp
// The hardest hit taken this fight
new MaxElementResolver(new ArrayVariableResolver("damageTaken", typeof(int)));
```

## See Also

- [Resolvers Overview](README.md)
- [MinElementResolver](min-element-resolver.md)
- [MaxResolver](max-resolver.md)
