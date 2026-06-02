# AbilitySourceResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AbilitySourceResolver`
> **Output Type:** `IForgeEntity?`

Resolves the current ability source entity from `AbilityBehaviorContext.Source`.

## Constructor

```csharp
new AbilitySourceResolver()
```

## Behavior

- Reads the active `AbilityBehaviorContext` from `GraphContext.ActivationContext`.
- Returns the entity that granted or sourced the currently executing ability.
- Returns `null` when no compatible ability activation context is available or the ability has no source entity.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("sourceHasFireAffinity",
    new TagQueryResolver(
        Tag.RequestTag(tagsManager, "element.fire"),
        new AbilitySourceResolver()));
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
- [AbilityOwnerResolver](ability-owner-resolver.md)
- [AbilityOwnershipResolver](ability-ownership-resolver.md)
- [OwnershipResolver](ownership-resolver.md)
- [AttributeResolver](attribute-resolver.md)
- [TagQueryResolver](tag-query-resolver.md)
