# ExpressionNode

> **Type:** Condition Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Condition.ExpressionNode`

Evaluates a boolean input property to choose the output. This eliminates the need to create custom `ConditionNode` subclasses for data-driven conditions. Instead, compose an expression from [property resolvers](../../variables.md#property-resolvers) at graph construction time.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers evaluation. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | True | Event | Emits if the test returns `true`. |
| 1 | False | Event | Emits if the test returns `false`. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Condition | `bool` | The expression to evaluate. |

The condition can be bound to any resolver producing a `bool`: a [TagQueryResolver](../../resolvers/tag-query-resolver.md), [ComparisonResolver](../../resolvers/comparison-resolver.md), [AndResolver](../../resolvers/and-resolver.md), [OrResolver](../../resolvers/or-resolver.md), [NotResolver](../../resolvers/not-resolver.md), [XorResolver](../../resolvers/xor-resolver.md), [VariableResolver](../../resolvers/variable-resolver.md) (reading a bool variable), or a nested chain.

## Behavior

1. The node resolves the bound condition input property as a `bool`.
2. If the value is `true`, the True output port emits a message.
3. If the value is `false` (or the property cannot be resolved), the False output port emits a message.

## Usage

```csharp
// Define a composed boolean property
graph.VariableDefinitions.DefineProperty("shouldUseStrongEffect",
    new AndResolver(
        new ComparisonResolver(
            new AttributeResolver("CombatAttributeSet.Health"),
            ComparisonOperation.GreaterThan,
            new VariantResolver(new Variant128(50), typeof(int))),
        new NotResolver(
            new TagQueryResolver(Tag.RequestTag(tagsManager, "status.silenced")))));

// Create and bind the expression node
var expression = new ExpressionNode();
expression.BindInput(ExpressionNode.ConditionInput, "shouldUseStrongEffect");

graph.AddNode(expression);

// Wire it up
graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    expression.InputPorts[ConditionNode.InputPort]));
graph.AddConnection(new Connection(
    expression.OutputPorts[ConditionNode.TruePort],
    strongEffectNode.InputPorts[ActionNode.InputPort]));
graph.AddConnection(new Connection(
    expression.OutputPorts[ConditionNode.FalsePort],
    weakEffectNode.InputPorts[ActionNode.InputPort]));
```

## See Also

- [Condition Nodes Overview](README.md)
- [ComparisonResolver](../../resolvers/comparison-resolver.md)
- [TagQueryResolver](../../resolvers/tag-query-resolver.md)
