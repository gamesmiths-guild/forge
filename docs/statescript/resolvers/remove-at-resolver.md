# RemoveAtResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.RemoveAtResolver` (value arrays), `ObjectRemoveAtResolver<T>` (reference arrays)
> **Output Type:** *(array of the source's element type)*

Removes the element at a given index from a nested array resolver. The index is itself a nested numeric resolver.

## Constructors

```csharp
new RemoveAtResolver(source, index)              // Variant128 arrays
new ObjectRemoveAtResolver<T>(source, index)     // reference arrays
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` / `IObjectArrayResolver<T>` | The resolver providing the source array. |
| index | `IPropertyResolver` | The resolver providing the zero-based index to remove. Must resolve to a numeric type; fractional values are truncated. |

## Behavior

- Returns the source array without the element at the resolved index.
- Out-of-range indices return the source array unchanged, they never throw.

## Usage

```csharp
new RemoveAtResolver(
    new ArrayVariableResolver("queue", typeof(int)),
    new VariantResolver(new Variant128(0), typeof(int)))
```

## Composition

```csharp
// Remove a specific entity found by identity: targets.RemoveAt(targets.IndexOf(candidate))
new ObjectRemoveAtResolver<IForgeEntity>(
    new EntityArrayVariableResolver("targets"),
    new ObjectIndexOfResolver(
        new EntityArrayVariableResolver("targets"),
        new EntityVariableResolver("candidate")));
```

## See Also

- [Resolvers Overview](README.md)
- [ExceptResolver](except-resolver.md)
- [IndexOfResolver](index-of-resolver.md)
