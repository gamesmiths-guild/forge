# SourceEntityResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.SourceEntityResolver`
> **Output Type:** `IForgeEntity?`

Resolves the source entity from the current `AbilityBehaviorContext`.

## Constructors

```csharp
new SourceEntityResolver()
```

## Behavior

- Reads `AbilityBehaviorContext.Source` from `GraphContext.ActivationContext`.
- Returns the source entity on the currently executing ability.
- Returns `null` if no compatible activation context is available or the ability has no source entity.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("sourceHasFireAffinity",
    new TagQueryResolver(
        Tag.RequestTag(tagsManager, "element.fire"),
        new SourceEntityResolver()));
```

## See Also

- [Resolvers Overview](README.md)
- [AttributeResolver](attribute-resolver.md)
- [TagQueryResolver](tag-query-resolver.md)
