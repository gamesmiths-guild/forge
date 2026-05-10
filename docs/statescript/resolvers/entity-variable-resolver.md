# EntityVariableResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.EntityVariableResolver`
> **Output Type:** `IForgeEntity?`

Reads an entity reference from graph or shared reference variables so other resolvers can inspect entities selected at
runtime.

## Constructors

```csharp
new EntityVariableResolver(variableName)
new EntityVariableResolver(variableName, scope)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| variableName | `StringKey` | The name of the reference variable to read. |
| scope | `VariableScope` | Chooses whether to read from graph variables (default) or shared variables. |

## Behavior

- Reads an `IForgeEntity` reference from `Variables`.
- Supports both graph-local and shared variable scopes.
- Returns `null` if the variable does not exist or currently has no entity assigned.

## Usage

```csharp
graph.VariableDefinitions.DefineReferenceVariable<IForgeEntity>("selectedEntity");

graph.VariableDefinitions.DefineProperty("selectedHealth",
    new AttributeResolver(
        "CombatAttributeSet.Health",
        new EntityVariableResolver("selectedEntity")));
```

```csharp
graph.VariableDefinitions.DefineProperty("sharedSelectionIsBoss",
    new TagQueryResolver(
        Tag.RequestTag(tagsManager, "enemy.boss"),
        new EntityVariableResolver("selectedEntity", VariableScope.Shared)));
```

## See Also

- [Resolvers Overview](README.md)
- [AttributeResolver](attribute-resolver.md)
- [TagQueryResolver](tag-query-resolver.md)
