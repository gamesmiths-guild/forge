# AbilityTargetResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AbilityTargetResolver`
> **Output Type:** `IForgeEntity?`

Resolves the current ability target entity from `AbilityBehaviorContext.Target`.

## Constructor

```csharp
new AbilityTargetResolver()
```

## Behavior

- Reads the active `AbilityBehaviorContext` from `GraphContext.ActivationContext`.
- Returns the target entity supplied when the current ability instance was activated.
- Returns `null` when no compatible ability activation context is available or the activation had no target.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("targetIsFrozen",
    new TagQueryResolver(
        Tag.RequestTag(tagsManager, "status.frozen"),
        new AbilityTargetResolver(),
        TagQuerySource.ModifierTags));
```

## Composition

```csharp
graph.VariableDefinitions.DefineObjectProperty(
    "ownership",
    new OwnershipResolver(
        new AbilityTargetResolver(),
        new AbilitySourceResolver()));
```

## See Also

- [Resolvers Overview](README.md)
- [AbilitySourceResolver](ability-source-resolver.md)
- [OwnershipResolver](ownership-resolver.md)
- [AttributeResolver](attribute-resolver.md)
- [TagQueryResolver](tag-query-resolver.md)
