# DistinctResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.DistinctResolver` (value arrays), `ObjectDistinctResolver<T>` (reference arrays)
> **Output Type:** *(array of the source's element type)*

De-duplicates a nested array resolver, keeping the first occurrence of each element and preserving the original order. The object-lane variant matches elements by reference identity, useful to avoid processing the same target twice.

## Constructors

```csharp
new DistinctResolver(source)              // Variant128 arrays
new ObjectDistinctResolver<T>(source)     // reference arrays
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` / `IObjectArrayResolver<T>` | The resolver providing the source array. |

## Behavior

- Returns the source elements with later duplicates removed; the first occurrence wins.
- Value-lane elements are compared by value (floating-point values exactly); object-lane elements by reference identity.

## Usage

```csharp
new DistinctResolver(new ArrayVariableResolver("rolledValues", typeof(int)))
```

## Composition

```csharp
// Merge two target lists without hitting anyone twice
new ObjectDistinctResolver<IForgeEntity>(
    new ObjectConcatResolver<IForgeEntity>(
        new EntityArrayVariableResolver("primaryTargets"),
        new EntityArrayVariableResolver("splashTargets")));
```

## See Also

- [Resolvers Overview](README.md)
- [ExceptResolver](except-resolver.md)
- [ConcatResolver](concat-resolver.md)
