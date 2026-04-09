# TagResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.TagResolver`
> **Output Type:** `bool`

Checks whether the owner entity has a specific gameplay tag. Requires the graph to be driven by an ability (accesses the owner entity from `AbilityBehaviorContext` stored in `GraphContext.ActivationContext`).

## Constructor

```csharp
new TagResolver(tag)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| tag | `Tag` | The gameplay tag to check for on the owner entity. |

## Behavior

- Retrieves the owner entity from the `AbilityBehaviorContext` in the graph's activation context.
- Checks whether the entity's `CombinedTags` has the specified tag (including parent tag matching).
- Returns `true` if the entity has the tag, `false` otherwise.
- Returns `false` if no activation context is available.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("isEnraged",
    new TagResolver(Tag.RequestTag(tagsManager, "status.enraged")));
```

## Composition

```csharp
// Use as the condition input for an ExpressionNode
var expression = new ExpressionNode();
expression.BindInput(ExpressionNode.ConditionInput, "isEnraged");
```

## See Also

- [Resolvers Overview](README.md)
- [AttributeResolver](attribute-resolver.md)
- [ExpressionNode](../nodes/condition/expression-node.md)
