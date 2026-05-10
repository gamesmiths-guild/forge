# TagQueryResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.TagQueryResolver`
> **Output Type:** `bool`

Evaluates a `TagQuery` against one of a selected entity's tag containers. This is the primary built-in resolver for tag-based conditions and supports both simple single-tag checks and more expressive nested query logic.

## Constructors

```csharp
new TagQueryResolver(query)
new TagQueryResolver(query, tagQuerySource)
new TagQueryResolver(query, entityResolver)
new TagQueryResolver(query, entityResolver, tagQuerySource)
new TagQueryResolver(queryExpression)
new TagQueryResolver(queryExpression, tagQuerySource)
new TagQueryResolver(queryExpression, entityResolver)
new TagQueryResolver(queryExpression, entityResolver, tagQuerySource)
new TagQueryResolver(tag)
new TagQueryResolver(tag, tagQuerySource)
new TagQueryResolver(tag, entityResolver)
new TagQueryResolver(tag, entityResolver, tagQuerySource)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| query | `TagQuery` | A prebuilt tag query to evaluate. |
| queryExpression | `TagQueryExpression` | A fluent tag-query expression that will be compiled into a `TagQuery`. |
| tag | `Tag` | Convenience overload for the common single-tag match case. |
| entityResolver | `IEntityResolver` | Selects which entity to inspect. Defaults to `OwnerEntityResolver`. |
| tagQuerySource | `TagQuerySource` | Chooses whether to evaluate against `AllTags` (default), `BaseTags`, or `ModifierTags`. |

## Behavior

- Resolves an entity using the configured `IEntityResolver`.
- Evaluates the configured `TagQuery` against one of:
  - `entity.Tags.AllTags` (default)
  - `entity.Tags.BaseTags`
  - `entity.Tags.ModifierTags`
- Returns `true` if the query matches, `false` otherwise.
- Returns `false` if the entity cannot be resolved.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("isEnraged",
    new TagQueryResolver(Tag.RequestTag(tagsManager, "status.enraged")));
```

## Source Selection Example

```csharp
graph.VariableDefinitions.DefineProperty("hasTemporaryStun",
    new TagQueryResolver(
        Tag.RequestTag(tagsManager, "status.stunned"),
        TagQuerySource.ModifierTags));
```

## Dynamic Entity Example

```csharp
graph.VariableDefinitions.DefineReferenceVariable<IForgeEntity>("selectedEntity");

graph.VariableDefinitions.DefineProperty("selectedEntityIsBoss",
    new TagQueryResolver(
        Tag.RequestTag(tagsManager, "enemy.boss"),
        new EntityVariableResolver("selectedEntity")));
```

```csharp
graph.VariableDefinitions.DefineProperty("targetIsCrowdControlled",
    new TagQueryResolver(
        Tag.RequestTag(tagsManager, "status.stunned"),
        new TargetEntityResolver(),
        TagQuerySource.ModifierTags));
```

## Complex Query Example

```csharp
var queryExpression = new TagQueryExpression(tagsManager)
    .AllExpressionsMatch()
    .AddExpression(
        new TagQueryExpression(tagsManager)
            .AnyTagsMatch()
            .AddTag("enemy.undead")
            .AddTag("enemy.beast"))
    .AddExpression(
        new TagQueryExpression(tagsManager)
            .NoTagsMatch()
            .AddTag("status.silenced"));

graph.VariableDefinitions.DefineProperty("canAttackTarget",
    new TagQueryResolver(queryExpression));
```

## See Also

- [Resolvers Overview](README.md)
- [AttributeResolver](attribute-resolver.md)
- [EntityVariableResolver](entity-variable-resolver.md)
- [OwnerEntityResolver](owner-entity-resolver.md)
- [TargetEntityResolver](target-entity-resolver.md)
- [ExpressionNode](../nodes/condition/expression-node.md)
