# ObjectEqualsResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ObjectEqualsResolver`
> **Output Type:** `bool`

Checks whether two nested object-backed resolvers produce the same instance, using reference identity. Use it to test whether two object variables point at the same entity, effect, or handle, e.g. "was this effect applied to the same target?".

## Constructor

```csharp
new ObjectEqualsResolver(left, right)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| left | `IObjectResolver` | The resolver for the left operand of the comparison. |
| right | `IObjectResolver` | The resolver for the right operand of the comparison. |

## Behavior

- Resolves both operands and compares them with `ReferenceEquals`.
- Two `null` results compare as equal. Combine with [IsValidResolver](is-valid-resolver.md) when missing values must not count as a match.

## Usage

```csharp
new ObjectEqualsResolver(
    new EntityVariableResolver("currentTarget"),
    new EntityVariableResolver("previousTarget"))
```

## Composition

```csharp
// "Is the ability target the entity we stored earlier?"
graph.VariableDefinitions.DefineProperty(
    "isSameTarget",
    new AndResolver(
        new IsValidResolver(new EntityVariableResolver("storedTarget")),
        new ObjectEqualsResolver(
            new AbilityTargetResolver(),
            new EntityVariableResolver("storedTarget"))));
```

## See Also

- [Resolvers Overview](README.md)
- [IsValidResolver](is-valid-resolver.md)
- [ObjectContainsResolver](contains-resolver.md#reference-arrays-objectcontainsresolver)
