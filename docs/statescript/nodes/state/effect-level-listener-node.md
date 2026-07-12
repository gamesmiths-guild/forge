# EffectLevelListenerNode

> **Type:** State Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.State.EffectLevelListenerNode`
> **Context:** `EffectLevelListenerNodeContext`

Listens for level changes on an `Effect` instance while active, emitting an event with the new level. Reacts to level changes made by [SetEffectLevelNode](../action/set-effect-level-node.md), `SetLevel`, or `LevelUp`, including changes made by other graphs sharing the instance.

## Ports

Standard state ports, plus:

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 4 | OnLevelChanged | Event | Emits each time the effect's level changes. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Effect | `Effect` | The effect whose level is observed. A variable-held effect, or one bridged from an active-effect handle via [ActiveEffectEffectResolver](../../resolvers/active-effect-effect-resolver.md). |

**Output Variables:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | New Level | `int` | The effect's new level. |

## Behavior

1. On activation, subscribes to `Effect.OnLevelChanged` for the resolved effect.
2. On each change, writes **New Level** and emits `OnLevelChanged`.
3. Unsubscribes on deactivation.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable("stanceBuff", stanceBuffInstance);
graph.VariableDefinitions.DefineVariable("stanceLevel", 0);

var listener = new EffectLevelListenerNode();
listener.BindInput(EffectLevelListenerNode.EffectInput, "stanceBuff");
listener.BindOutput(EffectLevelListenerNode.NewLevelOutput, "stanceLevel");

graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    listener.InputPorts[StateNode<EffectLevelListenerNodeContext>.InputPort]));
graph.AddConnection(new Connection(
    listener.OutputPorts[EffectLevelListenerNode.OnLevelChangedPort],
    onLevelUpNode.InputPorts[ActionNode.InputPort]));
```

## See Also

- [State Nodes Overview](README.md)
- [SetEffectLevelNode](../action/set-effect-level-node.md)
- [ActiveEffectEffectResolver](../../resolvers/active-effect-effect-resolver.md)
