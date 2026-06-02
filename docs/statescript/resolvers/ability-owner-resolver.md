# AbilityOwnerResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AbilityOwnerResolver`
> **Output Type:** `IForgeEntity?`

Resolves the current ability owner entity from `AbilityBehaviorContext.Owner`.

## Constructor

```csharp
new AbilityOwnerResolver()
```

## Behavior

- Reads the active `AbilityBehaviorContext` from `GraphContext.ActivationContext`.
- Returns the entity that owns the currently executing ability.
- Returns `null` when no compatible ability activation context is available.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("ownerHealth",
    new AttributeResolver(
        "CombatAttributeSet.Health",
        new AbilityOwnerResolver()));
```

## Composition

```csharp
graph.VariableDefinitions.DefineObjectProperty(
    "ownership",
    new OwnershipResolver(
        new AbilityOwnerResolver(),
        new AbilitySourceResolver()));
```

## See Also

- [Resolvers Overview](README.md)
- [AbilityOwnershipResolver](ability-ownership-resolver.md)
- [AbilitySourceResolver](ability-source-resolver.md)
- [OwnershipResolver](ownership-resolver.md)
- [AttributeResolver](attribute-resolver.md)
- [TagQueryResolver](tag-query-resolver.md)
