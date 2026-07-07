# ContainsResolver

Checks whether a nested array resolver contains a given value or reference. Both variants resolve the search value through a nested resolver, allowing constants, variables, or computed values.

## Value arrays: ContainsResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ContainsResolver`
> **Output Type:** `bool`

```csharp
new ContainsResolver(source, value)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IArrayPropertyResolver` | The resolver providing the source array. |
| value | `IPropertyResolver` | The resolver providing the value to search for. Must resolve to the source element type. |

- Elements are compared by value; floating-point values are compared exactly.
- Throws `ArgumentException` at construction when the value type does not match the source element type.

## Reference arrays: ObjectContainsResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ObjectContainsResolver`
> **Output Type:** `bool`

```csharp
new ObjectContainsResolver(source, value)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IObjectArrayResolver` | The resolver providing the source array. |
| value | `IObjectResolver` | The resolver providing the reference to search for. |

- Elements are matched by reference identity.
- A `null` search value matches stored `null` elements; combine with [IsValidResolver](is-valid-resolver.md) when missing values must not count as a match.

## Composition

```csharp
// "Has this entity already been hit?" — skip it if so
new ObjectContainsResolver(
    new EntityArrayVariableResolver("alreadyHit"),
    new AbilityTargetResolver());
```

## See Also

- [Resolvers Overview](README.md)
- [IndexOfResolver](index-of-resolver.md)
- [AnyResolver](any-resolver.md)
