# IndexOfResolver

Resolves the zero-based index of the first occurrence of a given value or reference in a nested array resolver, or `-1` when it is not present.

## Value arrays: IndexOfResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.IndexOfResolver`
> **Output Type:** `int`

```csharp
new IndexOfResolver(source, value)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` | The resolver providing the source array. |
| value | `IPropertyResolver` | The resolver providing the value to search for. Must resolve to the source element type. |

- Elements are compared by value; floating-point values are compared exactly.
- Throws `ArgumentException` at construction when the value type does not match the source element type.

## Reference arrays: ObjectIndexOfResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ObjectIndexOfResolver`
> **Output Type:** `int`

```csharp
new ObjectIndexOfResolver(source, value)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IObjectArrayResolver` | The resolver providing the source array. |
| value | `IObjectResolver` | The resolver providing the reference to search for. |

- Elements are matched by reference identity. A `null` search value matches stored `null` elements.

## Composition

```csharp
// Remove a specific entity from a list by identity
new ObjectRemoveAtResolver<IForgeEntity>(
    new EntityArrayVariableResolver("targets"),
    new ObjectIndexOfResolver(
        new EntityArrayVariableResolver("targets"),
        new EntityVariableResolver("candidate")));
```

## See Also

- [Resolvers Overview](README.md)
- [ContainsResolver](contains-resolver.md)
- [RemoveAtResolver](remove-at-resolver.md)
