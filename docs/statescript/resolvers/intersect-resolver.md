# IntersectResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.IntersectResolver`
> **Output Type:** *(element array)*
> **Object variant:** `ObjectIntersectResolver<T>`

Keeps the elements of a nested array resolver that also appear in a second nested array resolver, preserving their original order.

## Constructor

```csharp
new IntersectResolver(source, other)                 // value lane
new ObjectIntersectResolver<T>(source, other)        // object lane
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` / `IObjectArrayResolver<T>` | The source array. |
| other | *(same lane)* | The array of elements to keep. Must share the source's element type. |

## Behavior

- Unlike LINQ's set-based `Intersect`, duplicates in the source are preserved when they appear in `other`.
- Value-lane elements are compared exactly; object-lane elements are matched by reference identity.

## Usage

```csharp
// Entities that are both in range and currently marked
graph.VariableDefinitions.DefineObjectArrayProperty("validTargets",
    new ObjectIntersectResolver<IForgeEntity>(
        new EntityArrayVariableResolver("inRange"),
        new EntityArrayVariableResolver("marked")));
```

## Composition

```csharp
// Count how many overlap, then branch on it
graph.VariableDefinitions.DefineProperty("overlapCount",
    new CountResolver(
        new ObjectIntersectResolver<IForgeEntity>(
            new EntityArrayVariableResolver("inRange"),
            new EntityArrayVariableResolver("marked"))));
```

## See Also

- [Resolvers Overview](README.md)
- [ExceptResolver](except-resolver.md)
- [ConcatResolver](concat-resolver.md)
- [DistinctResolver](distinct-resolver.md)
