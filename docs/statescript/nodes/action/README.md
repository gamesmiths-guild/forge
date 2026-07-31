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
| [ApplyEffectNode](apply-effect-node.md) | Applies one or more effects to one or more targets. |
| [CancelAbilitiesNode](cancel-abilities-node.md) | Cancels active abilities on an entity, selected by the ability tags they carry. |
| [CancelAbilityNode](cancel-ability-node.md) | Cancels the ability driving the current graph. |
| [CommitAbilityNode](commit-ability-node.md) | Commits the cost and/or cooldown of the ability driving the graph. |
| [ExecuteCueNode](execute-cue-node.md) | Executes one or more one-shot cues on one or more targets. |
| [GrantAbilityPermanentlyNode](grant-ability-permanently-node.md) | Permanently grants an ability to an entity (cannot be revoked). |
| [RaiseEventNode](raise-event-node.md) | Raises an event on one or more target entities' event buses. |
| [RemoveEffectNode](remove-effect-node.md) | Removes active effects through their handles. |
| [SetByCallerMagnitudeNode](set-by-caller-magnitude-node.md) | Sets a SetByCaller magnitude on effects, keyed by tag. |
| [SetEffectInhibitionNode](set-effect-inhibition-node.md) | Sets the inhibition state of active effects. |
| [SetEffectLevelNode](set-effect-level-node.md) | Levels up effects or sets their level to a resolved value. |
| [SetVariableNode](set-variable-node.md) | Copies a value from an input property to a graph or shared variable. |
| [SwitchNode](switch-node.md) | Routes the incoming message to a case port picked by an integer selector. |
| [UpdateCueNode](update-cue-node.md) | Updates one or more active cues on one or more targets. |
