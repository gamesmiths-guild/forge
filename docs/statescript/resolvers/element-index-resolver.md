# ElementIndexResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ElementIndexResolver`
> **Output Type:** `int`

Resolves the zero-based index of the array element currently being iterated by an enclosing array resolver. Use it inside nested "lambda" resolvers for index-aware predicates and projections.

## Constructor

```csharp
new ElementIndexResolver()
```

*(no parameters)*

## Behavior

- Reads the innermost element frame published by an enclosing array resolver on the graph context.
- Returns a default `Variant128` (zero) when evaluated outside an array iteration.

## Usage

```csharp
// The current element's position in the iterated array
new ElementIndexResolver()
```

## Composition

```csharp
// Keep only the elements at even positions: numbers.Where((x, i) => i % 2 == 0)
new WhereResolver(
    new ArrayVariableResolver("numbers", typeof(int)),
    new ComparisonResolver(
        new ModuloResolver(new ElementIndexResolver(), new VariantResolver(new Variant128(2), typeof(int))),
        ComparisonOperation.Equal,
        new VariantResolver(new Variant128(0), typeof(int))));
```

## See Also

- [Resolvers Overview](README.md)
- [ElementValueResolver](element-value-resolver.md)
- [WhereResolver](where-resolver.md)
