# SwitchNode

> **Type:** Flow Node (custom `Node`)
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Action.SwitchNode`

Routes an incoming message to one of several case ports based on a resolved integer selector.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers routing. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0..N-1 | Case *i* | Event | Emitted when the selector equals *i*. |
| N | Default | Event | Emitted for any out-of-range selector (including an unresolvable one). `N == caseCount`, exposed as `DefaultPort`. |

## Constructor

```csharp
new SwitchNode(caseCount = 2)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| caseCount | `int` | How many case ports the switch has, not counting the default port. Must be at least 1. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Selector | `int` | Selects which case port emits. |

## Behavior

1. Resolves the integer **Selector**.
2. Emits the matching case port when the selector is in `[0, caseCount)`, otherwise emits the default port.

## Usage

```csharp
graph.VariableDefinitions.DefineVariable("tier", 1);

var switchNode = new SwitchNode(caseCount: 3);
switchNode.BindInput(SwitchNode.SelectorInput, "tier");

graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    switchNode.InputPorts[SwitchNode.InputPort]));

// Wire each case (and the default) to its own downstream node
graph.AddConnection(new Connection(
    switchNode.OutputPorts[0], tier0Node.InputPorts[ActionNode.InputPort]));
graph.AddConnection(new Connection(
    switchNode.OutputPorts[switchNode.DefaultPort], fallbackNode.InputPorts[ActionNode.InputPort]));
```

## See Also

- [Action Nodes Overview](README.md)
- [StateMachineNode](../state/state-machine-node.md)
- [ExpressionNode](../condition/expression-node.md)
