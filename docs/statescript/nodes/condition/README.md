# Condition Nodes

Condition nodes evaluate a boolean test and route the message to one of two outputs.

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers evaluation. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | True | Event | Emits if test is `true`. |
| 1 | False | Event | Emits if test is `false`. |

**Behavior:**

1. A message arrives on the input port.
2. The node's `Test` method evaluates.
3. Either the True or False port emits a message (never both).

## Creating Custom Condition Nodes

Extend `ConditionNode` and override `Test`:

```csharp
public class HasTargetConditionNode : ConditionNode
{
    protected override bool Test(GraphContext graphContext)
    {
        if (!graphContext.TryGetActivationContext<AbilityBehaviorContext>(out var context))
        {
            return false;
        }

        return context.Target is not null;
    }
}
```

## Built-in Condition Nodes

| Node | Description |
|------|-------------|
| [ExpressionNode](expression-node.md) | Evaluates a boolean input property to choose the output. |
