# TagQueryResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.TagQueryResolver`
> **Output Type:** `bool`

Evaluates a `TagQuery` against the owner entity's combined tags. This is the primary built-in resolver for tag-based conditions and supports both simple single-tag checks and more expressive nested query logic.

## Constructors

```csharp
new TagQueryResolver(query)
new TagQueryResolver(queryExpression)
new TagQueryResolver(tag)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| query | `TagQuery` | A prebuilt tag query to evaluate. |
| queryExpression | `TagQueryExpression` | A fluent tag-query expression that will be compiled into a `TagQuery`. |
| tag | `Tag` | Convenience overload for the common single-tag match case. |

## Behavior

- Retrieves the owner entity from the `AbilityBehaviorContext` in the graph's activation context.
- Evaluates the configured `TagQuery` against `abilityContext.Owner.Tags.CombinedTags`.
- Returns `true` if the query matches, `false` otherwise.
- Returns `false` if no activation context is available.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("isEnraged",
    new TagQueryResolver(Tag.RequestTag(tagsManager, "status.enraged")));
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
- [ExpressionNode](../nodes/condition/expression-node.md)
