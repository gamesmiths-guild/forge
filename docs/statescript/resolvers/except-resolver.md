# ExceptResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ExceptResolver` (value arrays), `ObjectExceptResolver<T>` (reference arrays)
> **Output Type:** *(array of the source's element type)*

Removes from a nested array resolver every element that appears in a second nested array resolver, preserving the source order. The object-lane variant matches elements by reference identity.

## Constructors

```csharp
new ExceptResolver(source, other)              // Variant128 arrays
new ObjectExceptResolver<T>(source, other)     // reference arrays
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` / `IObjectArrayResolver<T>` | The resolver providing the source array. |
| other | `IArrayPropertyResolver` / `IObjectArrayResolver<T>` | The resolver providing the elements to remove. The value-lane variant requires both element types to match. |

## Behavior

- Keeps the source elements that are not present in `other`, in their original order.
- Unlike LINQ's set-based `Except`, duplicates in the source are preserved unless they appear in `other`.
- Value-lane elements are compared by value (floating-point values exactly); object-lane elements by reference identity.
- The value-lane variant throws `ArgumentException` at construction for mismatched element types.

## Usage

```csharp
new ExceptResolver(
    new ArrayVariableResolver("allLanes", typeof(int)),
    new ArrayVariableResolver("blockedLanes", typeof(int)))
```

## Composition

```csharp
// All nearby entities except the ones already hit
new ObjectExceptResolver<IForgeEntity>(
    new EntityArrayVariableResolver("nearbyEntities"),
    new EntityArrayVariableResolver("alreadyHit"));
```

## See Also

- [Resolvers Overview](README.md)
- [DistinctResolver](distinct-resolver.md)
- [WhereResolver](where-resolver.md)
