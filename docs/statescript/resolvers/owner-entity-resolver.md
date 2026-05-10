# OwnerEntityResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.OwnerEntityResolver`
> **Output Type:** `IForgeEntity`

Resolves the owner entity from the current `AbilityBehaviorContext`.

## Constructors

```csharp
new OwnerEntityResolver()
```

## Behavior

- Reads `AbilityBehaviorContext.Owner` from `GraphContext.ActivationContext`.
- Returns the entity that owns the currently executing ability.
- Returns `null` if no compatible activation context is available.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("ownerHealth",
    new AttributeResolver(
        "CombatAttributeSet.Health",
        new OwnerEntityResolver()));
```

## See Also

- [Resolvers Overview](README.md)
- [AttributeResolver](attribute-resolver.md)
- [TagQueryResolver](tag-query-resolver.md)
