# ElementValueResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ElementValueResolver`
> **Output Type:** *(configured at construction time)*

Resolves the value-typed array element currently being iterated by an enclosing array resolver (`WhereResolver`, `OrderByResolver`, `SelectResolver`, etc.). Use it inside nested "lambda" resolvers as the stand-in for the lambda parameter, the `x` in `x => x > 2`.

## Constructor

```csharp
new ElementValueResolver(valueType)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| valueType | `Type` | The element type this resolver produces. Must match the iterated array's element type. |

## Behavior

- Reads the innermost element frame published by an enclosing array resolver on the graph context.
- Returns a default `Variant128` (zero) when evaluated outside an array iteration.
- Frames form a stack, so nested array operations always observe the innermost element.

## Usage

```csharp
// The lambda parameter of a filter over an int array
new ElementValueResolver(typeof(int))
```

## Composition

```csharp
// Keep elements greater than 2: numbers.Where(x => x > 2)
new WhereResolver(
    new ArrayVariableResolver("numbers", typeof(int)),
    new ComparisonResolver(
        new ElementValueResolver(typeof(int)),
        ComparisonOperation.GreaterThan,
        new VariantResolver(new Variant128(2), typeof(int))));
```

## See Also

- [Resolvers Overview](README.md)
- [WhereResolver](where-resolver.md)
- [SelectResolver](select-resolver.md)
- [ElementIndexResolver](element-index-resolver.md)
