# ElementResolver&lt;T&gt;

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ElementResolver<T>`
> **Output Type:** `T?`

Resolves the object-backed array element currently being iterated by an enclosing array resolver (`ObjectWhereResolver<T>`, `ObjectOrderByResolver<T>`, etc.). Use it inside nested "lambda" resolvers as the stand-in for the lambda parameter when iterating reference arrays.

## Constructor

```csharp
new ElementResolver<T>()
```

*(no parameters)*

## Behavior

- Reads the innermost element frame published by an enclosing array resolver on the graph context.
- Returns `null` when evaluated outside an array iteration or when the current element is not compatible with `T`.

## Usage

```csharp
// The lambda parameter of an object array operation
new ElementResolver<IForgeEntity>()
```

## Composition

```csharp
// Drop null entries from a reference array: targets.Where(x => x is not null)
new ObjectWhereResolver<IForgeEntity>(
    new EntityArrayVariableResolver("targets"),
    new IsValidResolver(new ElementResolver<IForgeEntity>()));
```

## See Also

- [Resolvers Overview](README.md)
- [ElementEntityResolver](element-entity-resolver.md)
- [IsValidResolver](is-valid-resolver.md)
- [WhereResolver](where-resolver.md)
