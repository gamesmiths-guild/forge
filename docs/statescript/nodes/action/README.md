# Action Nodes

Action nodes perform an **instant operation** then pass the message forward. They are the workhorses of imperative logic in Statescript.

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers the action. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | Output | Event | Emits after execution. |

**Behavior:**

1. A message arrives on the input port.
2. The node's `Execute` method runs.
3. The output port emits a message.

Action nodes are stateless and instantaneous. They do not persist between frames.

## Creating Custom Action Nodes

Extend `ActionNode` and override `Execute`:

```csharp
public class ApplyEffectActionNode : ActionNode
{
    private readonly EffectData _effectData;

    public ApplyEffectActionNode(EffectData effectData)
    {
        _effectData = effectData;
    }

    protected override void Execute(GraphContext graphContext)
    {
        if (!graphContext.TryGetActivationContext<AbilityBehaviorContext>(out var context))
        {
            return;
        }

        var effect = new Effect(_effectData, new EffectOwnership(context.Owner, context.Source));
        context.Owner.EffectsManager.ApplyEffect(effect);
    }
}
```

## Built-in Action Nodes

| Node | Description |
|------|-------------|
| [SetVariableNode](set-variable-node.md) | Copies a value from an input property to a graph or shared variable. |
