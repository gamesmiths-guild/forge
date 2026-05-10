# TargetEntityResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.TargetEntityResolver`
> **Output Type:** `IForgeEntity?`

Resolves the target entity from the current `AbilityBehaviorContext`.

## Constructors

```csharp
new TargetEntityResolver()
```

## Behavior

- Reads `AbilityBehaviorContext.Target` from `GraphContext.ActivationContext`.
- Returns the target entity supplied when the current ability instance was activated.
- Returns `null` if no compatible activation context is available or the activation had no target.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("targetIsFrozen",
    new TagQueryResolver(
        Tag.RequestTag(tagsManager, "status.frozen"),
        new TargetEntityResolver(),
        TagQuerySource.ModifierTags));
```

## See Also

- [Resolvers Overview](README.md)
- [AttributeResolver](attribute-resolver.md)
- [TagQueryResolver](tag-query-resolver.md)
