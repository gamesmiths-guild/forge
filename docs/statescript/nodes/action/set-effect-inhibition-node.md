# SetEffectInhibitionNode

> **Type:** Action Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Action.SetEffectInhibitionNode`

Sets the inhibition state of one or more active effects through their `ActiveEffectHandle`.

Inhibited effects keep their remaining duration ticking but suspend their modifiers and periodic executions until the inhibition is lifted.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers the change. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | Output | Event | Emits after the change. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Active Effect | `ActiveEffectHandle` or `ActiveEffectHandle[]` | The handle(s) to update. Invalid handles are skipped. |
| 1 | Inhibited | `bool` | The desired inhibition state. |

## Behavior

1. Resolves the handle input as a single handle or an array of handles.
2. Resolves the boolean **Inhibited** value.
3. Calls `ActiveEffectHandle.SetInhibit(value)` on each valid handle.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable<ActiveEffectHandle>("buff");
graph.VariableDefinitions.DefineVariable("suppressed", true);

var inhibit = new SetEffectInhibitionNode();
inhibit.BindInput(SetEffectInhibitionNode.HandleInput, "buff");
inhibit.BindInput(SetEffectInhibitionNode.InhibitedInput, "suppressed");

graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    inhibit.InputPorts[ActionNode.InputPort]));
```

## See Also

- [Action Nodes Overview](README.md)
- [RemoveEffectNode](remove-effect-node.md)
- [ActiveEffectDataResolver](../../resolvers/active-effect-data-resolver.md)
