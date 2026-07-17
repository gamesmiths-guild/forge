# StateMachineNode

> **Type:** State Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.State.StateMachineNode`
> **Context:** `StateMachineNodeContext`

Keeps exactly one of several state subgraphs active, selected by a resolved integer — a graph-native state machine.

## Ports

Standard state ports, plus:

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 4 | OnStateChanged | Event | Emits when the active state changes. |
| 5..5+N-1 | State *i* | Subgraph | The subgraph for state *i* (`FirstStatePort == 5`). |

## Constructor

```csharp
new StateMachineNode(stateCount = 2)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| stateCount | `int` | How many state subgraph ports the machine has. Must be at least 1. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | State | `int` | The state selector, re-evaluated on activation and every update tick. Out-of-range selectors are clamped. |

**Output Variables:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Current State | `int` | The currently active state index. |

## Behavior

1. Evaluates the selector on activation and every update. Out-of-range selectors clamp to the valid range.
2. On a change, disables the previous state's subgraph, activates the new one, writes **Current State**, and emits `OnStateChanged`. Entering the first state after activation counts as a change.
3. All state subgraphs are cleaned up when the node deactivates.

## Usage

```csharp
graph.VariableDefinitions.DefineVariable("stance", 0);

var stateMachine = new StateMachineNode(stateCount: 3);
stateMachine.BindInput(StateMachineNode.StateInput, "stance");

graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    stateMachine.InputPorts[StateNode<StateMachineNodeContext>.InputPort]));

// Each state owns its own subgraph, cleaned up when the state is left
graph.AddConnection(new Connection(
    stateMachine.OutputPorts[StateMachineNode.FirstStatePort],
    idleBehavior.InputPorts[StateNode<TimerNodeContext>.InputPort]));
graph.AddConnection(new Connection(
    stateMachine.OutputPorts[StateMachineNode.FirstStatePort + 1],
    aggressiveBehavior.InputPorts[StateNode<TimerNodeContext>.InputPort]));
```

## See Also

- [State Nodes Overview](README.md)
- [Subgraphs](../../subgraphs.md)
- [SwitchNode](../action/switch-node.md)
