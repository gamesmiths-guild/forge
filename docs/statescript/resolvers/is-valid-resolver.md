# IsValidResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.IsValidResolver`
> **Output Type:** `bool`

Checks whether a nested object-backed resolver produces a valid (non-null) value. Use it to validate object variables (entities, effects, handles) in condition nodes before acting on them, e.g. "is the stored target still set?".

## Constructor

```csharp
new IsValidResolver(source)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IObjectResolver` | The object-backed resolver whose result is checked. |

## Behavior

- Resolves `source` and returns `true` when the result is not `null`.
- Missing variables resolve to `null` and are therefore reported as invalid.
- For an "is null" check, wrap this resolver in a [NotResolver](not-resolver.md) or, when driving an `ExpressionNode`, simply connect the opposite port.

## Usage

```csharp
new IsValidResolver(new EntityVariableResolver("storedTarget"))              // true when set
new NotResolver(new IsValidResolver(new EntityVariableResolver("storedTarget")))  // true when null
```

## Composition

```csharp
// Gate a branch on the stored target being valid
graph.VariableDefinitions.DefineProperty(
    "hasTarget",
    new IsValidResolver(new EntityVariableResolver("storedTarget")));

// Or drop null entries from a reference array
new ObjectWhereResolver<IForgeEntity>(
    new EntityArrayVariableResolver("targets"),
    new IsValidResolver(new ElementResolver<IForgeEntity>()));
```

## See Also

- [Resolvers Overview](README.md)
- [NotResolver](not-resolver.md)
- [ObjectEqualsResolver](object-equals-resolver.md)
- [EntityVariableResolver](entity-variable-resolver.md)
