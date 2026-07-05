# ConcatResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ConcatResolver` (value arrays), `ObjectConcatResolver<T>` (reference arrays)
> **Output Type:** *(array of the shared element type)*

Concatenates two nested array resolvers, producing the elements of the first followed by the elements of the second.

## Constructors

```csharp
new ConcatResolver(first, second)              // Variant128 arrays
new ObjectConcatResolver<T>(first, second)     // reference arrays
```

| Parameter | Type | Description |
|-----------|------|-------------|
| first | `IArrayPropertyResolver` / `IObjectArrayResolver<T>` | The resolver providing the leading elements. |
| second | `IArrayPropertyResolver` / `IObjectArrayResolver<T>` | The resolver providing the trailing elements. The value-lane variant requires both element types to match. |

## Behavior

- Returns one array holding the first source's elements followed by the second source's elements.
- When either side is empty, the other side is returned as-is.
- The value-lane variant throws `ArgumentException` at construction for mismatched element types.

## Usage

```csharp
new ConcatResolver(
    new ArrayVariableResolver("baseDamage", typeof(int)),
    new ArrayVariableResolver("bonusDamage", typeof(int)))
```

## Composition

```csharp
// Merge two target lists, then de-duplicate shared members
new ObjectDistinctResolver<IForgeEntity>(
    new ObjectConcatResolver<IForgeEntity>(
        new EntityArrayVariableResolver("meleeTargets"),
        new EntityArrayVariableResolver("rangedTargets")));
```

## See Also

- [Resolvers Overview](README.md)
- [AppendResolver](append-resolver.md)
- [DistinctResolver](distinct-resolver.md)
